// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Graphics;
using CivOne.Services.Random;

namespace CivOne.Screens
{
	internal sealed class StarfieldDelegate(IRandomService randomService, int[] scrollDividers)
	{
		private static readonly byte[] LayerColors = [8, 7, 11, 15];
		private static int LayerCount => LayerColors.Length;

		public static int[] ZeroOffsets => new int[LayerCount];

		private readonly IRandomService _randomService = randomService;
		private (int x, int y, int layer)[] _starfieldPoints;
		private readonly int[] _lastParallaxOffsets = [-1, -1, -1, -1];
		private int _starFieldWidth;
		private int _starFieldHeight;

		public int[] GetParallaxOffsets(uint gameTick, int starFieldWidth)
		{
			if (starFieldWidth <= 0)
			{
				return ZeroOffsets;
			}

			int[] offsets = new int[LayerCount];
			for (int layer = 0; layer < LayerCount; layer++)
			{
				offsets[layer] = (int)(gameTick / scrollDividers[layer]) % starFieldWidth;
			}
			return offsets;
		}

		public bool HasParallaxMoved(int[] parallaxOffsets)
		{
			for (int layer = 0; layer < LayerCount; layer++)
			{
				if (parallaxOffsets[layer] != _lastParallaxOffsets[layer])
				{
					return true;
				}
			}
			return false;
		}

		public void CommitParallaxOffsets(int[] parallaxOffsets)
		{
			UpdateStarPositions(parallaxOffsets);
			Array.Copy(parallaxOffsets, _lastParallaxOffsets, LayerCount);
		}

		public void DrawStarfield(IBitmap target, int ox, int oy, int starFieldWidth, int starFieldHeight, int[] parallaxOffsets)
		{
			InitStarfield(starFieldWidth, starFieldHeight);
			foreach ((int sx, int sy, int layer) in _starfieldPoints)
			{
				target.DrawLine(ox + sx, oy + sy, ox + sx + 1, oy + sy + 1, LayerColors[layer]);
			}
		}

		private void UpdateStarPositions(int[] parallaxOffsets)
		{
			if (_starfieldPoints == null || _starFieldWidth <= 0 || _starFieldHeight <= 0)
			{
				return;
			}

			for (int layer = 0; layer < LayerCount; layer++)
			{
				int previousOffset = _lastParallaxOffsets[layer] < 0 ? parallaxOffsets[layer] : _lastParallaxOffsets[layer];
				int delta = (parallaxOffsets[layer] - previousOffset + _starFieldWidth) % _starFieldWidth;
				if (delta == 0)
				{
					continue;
				}

				for (int index = 0; index < _starfieldPoints.Length; index++)
				{
					(int x, int y, int starLayer) = _starfieldPoints[index];
					if (starLayer != layer)
					{
						continue;
					}

					x += delta;
					while (x >= _starFieldWidth)
					{
						x -= _starFieldWidth;
						y = _randomService.Next(_starFieldHeight);
					}

					_starfieldPoints[index] = (x, y, starLayer);
				}
			}
		}

		private void InitStarfield(int starFieldWidth, int starFieldHeight)
		{
			if (_starfieldPoints != null)
			{
				return;
			}

			_starFieldWidth = starFieldWidth;
			_starFieldHeight = starFieldHeight;

			const int StarsPerLayer = 18;
			var points = new (int x, int y, int layer)[LayerCount * StarsPerLayer];
			for (int i = 0; i < points.Length; i++)
			{
				int layer = i / StarsPerLayer;
				points[i] = (_randomService.Next(starFieldWidth), _randomService.Next(starFieldHeight), layer);
			}

			_starfieldPoints = points;
		}
	}
}
