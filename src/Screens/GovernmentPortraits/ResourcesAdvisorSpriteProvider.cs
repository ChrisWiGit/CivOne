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

/*
MilitaryAdvisorPortrait: 1,1 w=39 h=59
TradeAdvisorPortrait: 41,1
ForeignAdvisorPortrait: 81,1
ScienceAdvisorPortrait: 121,1
FacePortraitPos: 11x17


MilitaryAdvisorFullPortrait: 1,101  w=79,99
TradeAdvisorFullPortrait: 81,101
ForeignAdvisorFullPortrait: 161,101
ScienceAdvisorFullPortrait: 241,101
FaceFullPortraitPos: 37x119

Faces werden auf die oberen sprites der jeweiligen Berater gezeichet
enum Face = neutral, grim, happy

w=28 h=24
MilitaryAdvisorFace[Face] = 161x1, 190x1, 219x1
TradeAdvisorFace[Face] = 161x26, 190x26, 219x26
ForeignAdvisorFace[Face] = 161x51, 190x51, 219x51
ScienceAdvisorFace[Face] = 161x76, 190x76, 219x76


*/

namespace CivOne.Screens.GovernmentPortraits
{
	internal class ResourcesAdvisorSpriteProvider : IAdvisorPortraitSpriteProvider
	{
		private static readonly Point PortraitFaceOffset = new Point(10, 16);
		private static readonly Point FullPortraitFaceOffset = new Point(36, 18);
		private readonly Resources _resources;
		private readonly Dictionary<(AdvisorType Type, AdvisorFace Face, AdvisorGovernment Government, AdvisorEra Era, AdvisorPortraitSize Size), Picture> _cache = [];

		private readonly Dictionary<AdvisorType, (int Left, int Top, int Width, int Height)> _smallPortraitAreas = new()
		{
			{ AdvisorType.MilitaryAdvisor, (0, 0, 39, 59) },
			{ AdvisorType.TradeAdvisor, (40, 0, 39, 59) },
			{ AdvisorType.ForeignAdvisor, (80, 0, 39, 59) },
			{ AdvisorType.ScienceAdvisor, (120, 0, 39, 59) }
		};

		private readonly Dictionary<AdvisorType, (int Left, int Top, int Width, int Height)> _largePortraitAreas = new()
		{
			{ AdvisorType.MilitaryAdvisor, (0, 100, 79, 99) },
			{ AdvisorType.TradeAdvisor, (80, 100, 79, 99) },
			{ AdvisorType.ForeignAdvisor, (160, 100, 79, 99) },
			{ AdvisorType.ScienceAdvisor, (240, 100, 79, 99) }
		};

		private readonly Dictionary<(AdvisorType Type, AdvisorFace Face), 
			(int Left, int Top, int Width, int Height)> _faceAreas = new()
		{
			{ (AdvisorType.MilitaryAdvisor, AdvisorFace.Neutral), (160, 0, 28, 24) },
			{ (AdvisorType.MilitaryAdvisor, AdvisorFace.Grim), (189, 0, 28, 24) },
			{ (AdvisorType.MilitaryAdvisor, AdvisorFace.Happy), (218, 0, 28, 24) },
			{ (AdvisorType.TradeAdvisor, AdvisorFace.Neutral), (160, 25, 28, 24) },
			{ (AdvisorType.TradeAdvisor, AdvisorFace.Grim), (189, 25, 28, 24) },
			{ (AdvisorType.TradeAdvisor, AdvisorFace.Happy), (218, 25, 28, 24) },
			{ (AdvisorType.ForeignAdvisor, AdvisorFace.Neutral), (160, 50, 28, 24) },
			{ (AdvisorType.ForeignAdvisor, AdvisorFace.Grim), (189, 50, 28, 24) },
			{ (AdvisorType.ForeignAdvisor, AdvisorFace.Happy), (218, 50, 28, 24) },
			{ (AdvisorType.ScienceAdvisor, AdvisorFace.Neutral), (160, 75, 28, 24) },
			{ (AdvisorType.ScienceAdvisor, AdvisorFace.Grim), (189, 75, 28, 24) },
			{ (AdvisorType.ScienceAdvisor, AdvisorFace.Happy), (218, 75, 28, 24) }
		};

		public ResourcesAdvisorSpriteProvider(Resources resources)
		{
			_resources = resources;
		}

		public Picture GetPortrait(
			AdvisorType portraitType,
			AdvisorFace face = AdvisorFace.Neutral,
			AdvisorGovernment government = AdvisorGovernment.Democracy,
			AdvisorEra era = AdvisorEra.Modern,
			AdvisorPortraitSize size = AdvisorPortraitSize.Small)
		{
			(AdvisorType Type, AdvisorFace Face, AdvisorGovernment Government, AdvisorEra Era, AdvisorPortraitSize Size) cacheKey = (portraitType, face, government, era, size);
			if (_cache.TryGetValue(cacheKey, out Picture cachedPortrait))
			{
				return cachedPortrait;
			}

			Dictionary<AdvisorType, (int Left, int Top, int Width, int Height)> portraitAreas = size == AdvisorPortraitSize.Large
				? _largePortraitAreas
				: _smallPortraitAreas;

			if (!portraitAreas.TryGetValue(portraitType, out (int Left, int Top, int Width, int Height) portraitArea))
			{
				throw new KeyNotFoundException($"Unknown advisor portrait type: {portraitType} ({size})");
			}

			string resourceName = ResolveResourceName(government, era);
			Picture portrait = _resources[resourceName][portraitArea.Left+1, portraitArea.Top+1, portraitArea.Width-1, portraitArea.Height-1];
			if (face == AdvisorFace.Neutral)
			{
				_cache[cacheKey] = portrait;
				return portrait;
			}

			(AdvisorType Type, AdvisorFace Face) faceKey = (portraitType, face);
			if (!_faceAreas.TryGetValue(faceKey, out (int Left, int Top, int Width, int Height) faceArea))
			{
				throw new KeyNotFoundException($"Unknown face sprite mapping for advisor '{portraitType}' and face '{face}'");
			}

			var res = _resources[resourceName];
			var palette = res.Palette; //todo: später wird AddLayer um Pallete parameter erweitert, damit die palette nicht extra übergeben werden muss

			Picture faceSprite = res[faceArea.Left, faceArea.Top, faceArea.Width, faceArea.Height];
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