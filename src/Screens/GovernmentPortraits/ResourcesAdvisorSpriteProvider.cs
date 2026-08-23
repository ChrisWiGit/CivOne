// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Drawing;
using CivOne.Graphics;
using CivOne.IO;


namespace CivOne.Screens.GovernmentPortraits
{
	/// <summary>
	/// Provides portraits for the various advisors, based on their type, face, government and era.
	/// This is an uncoupled version of <see cref="CivOne.Graphics.Icons.GovernmentPortrait"/>, which 
	/// is used by the various screens to request portraits without needing to know about the underlying logic of how portraits are determined.
	/// </summary>
	internal class ResourcesAdvisorSpriteProvider : IAdvisorPortraitSpriteProvider
	{
		// Small portraits: grid in x-direction, one per Advisor enum value
		// Source: x=1 + advisor*40, y=1, w=38, h=58
		private const int SmallPortraitX = 1;
		private const int SmallPortraitY = 1;
		private const int SmallPortraitWidth = 38;
		private const int SmallPortraitHeight = 58;
		private const int SmallPortraitStepX = 40;

		// Large portraits: grid in x-direction, one per Advisor enum value
		// Source: x=1 + advisor*80, y=101, w=78, h=98
		private const int LargePortraitX = 1;
		private const int LargePortraitY = 101;
		private const int LargePortraitWidth = 78;
		private const int LargePortraitHeight = 98;
		private const int LargePortraitStepX = 80;

		// Faces: 2D grid — x=AdvisorFace, y=Advisor
		// Source: x=160 + face*29, y=advisor*25, w=28, h=24
		private const int FaceX = 160;
		private const int FaceY = 0;
		private const int FaceWidth = 28;
		private const int FaceHeight = 24;
		private const int FaceStepX = 29;
		private const int FaceStepY = 25;

		private static readonly Point PortraitFaceOffset = new(10, 16);
		private static readonly Point FullPortraitFaceOffset = new(36, 18);

		private readonly Resources _resources;
		private readonly Dictionary<(AdvisorType Type, AdvisorFace Face, AdvisorGovernment Government, AdvisorEra Era, AdvisorPortraitSize Size), IBitmap> _cache = [];

		public ResourcesAdvisorSpriteProvider(Resources resources)
		{
			_resources = resources;
		}

		public IBitmap GetPortrait(
			AdvisorType portraitType,
			AdvisorFace face = AdvisorFace.Neutral,
			AdvisorGovernment government = AdvisorGovernment.Democracy,
			AdvisorEra era = AdvisorEra.Modern,
			AdvisorPortraitSize size = AdvisorPortraitSize.Small)
		{
			(AdvisorType Type, AdvisorFace Face, AdvisorGovernment Government, AdvisorEra Era, AdvisorPortraitSize Size) cacheKey = (portraitType, face, government, era, size);
			if (_cache.TryGetValue(cacheKey, out IBitmap? cachedPortrait))
			{
				return cachedPortrait;
			}

			int advisorIndex = (int)portraitType;
			string resourceName = ResolveResourceName(government, era);

			Picture portrait = size == AdvisorPortraitSize.Large
				? _resources[resourceName][LargePortraitX + advisorIndex * LargePortraitStepX, LargePortraitY, LargePortraitWidth, LargePortraitHeight]
				: _resources[resourceName][SmallPortraitX + advisorIndex * SmallPortraitStepX, SmallPortraitY, SmallPortraitWidth, SmallPortraitHeight];

			if (face == AdvisorFace.Neutral)
			{
				_cache[cacheKey] = portrait;
				return portrait;
			}

			int faceIndex = (int)face;
			Picture faceSprite = _resources[resourceName][FaceX + faceIndex * FaceStepX, FaceY + advisorIndex * FaceStepY, FaceWidth, FaceHeight];
			Picture output = new(portrait);
			Point faceOffset = size == AdvisorPortraitSize.Large ? FullPortraitFaceOffset : PortraitFaceOffset;
			output.Bitmap.AddLayer(faceSprite.Bitmap, faceOffset.X, faceOffset.Y);

			_cache[cacheKey] = output;
			return output;
		}

		private static string ResolveResourceName(AdvisorGovernment government, AdvisorEra era)
		{
			if (government == AdvisorGovernment.Communism)
			{
				return "GOVT3A";
			}

			string eraSuffix = era == AdvisorEra.Modern ? "M" : "A";
			string governmentPrefix = government switch
			{
				AdvisorGovernment.Despotism => "GOVT0",
				AdvisorGovernment.Monarchy => "GOVT1",
				AdvisorGovernment.Democracy => "GOVT2",
				_ => throw new KeyNotFoundException($"Unknown advisor government type: {government}")
			};

			return $"{governmentPrefix}{eraSuffix}";
		}
	}
}