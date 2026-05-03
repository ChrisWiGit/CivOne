// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using CivOne.Enums;

namespace CivOne.Graphics
{
	internal sealed class PalaceResourcesDelegate
	{
		private const int ClassicalLeftTowerSourceOffsetX = 0;
		private const int ClassicalMiddleSourceOffsetX = 0;

		private readonly Func<string, Picture> _getPictureByName;
		private readonly Dictionary<int, Picture> _cache = new Dictionary<int, Picture>();
		private int _classicalDebugOffsetX;

		internal PalaceResourcesDelegate(Func<string, Picture> getPictureByName, int classicalDebugOffsetX = 0)
		{
			_getPictureByName = getPictureByName ?? throw new ArgumentNullException(nameof(getPictureByName));
			_classicalDebugOffsetX = classicalDebugOffsetX;
		}

		internal void ClearCache() => _cache.Clear();

		internal int ClassicalDebugOffsetX => _classicalDebugOffsetX;

		internal void AdjustClassicalDebugOffsetX(int delta)
		{
			_classicalDebugOffsetX += delta;
			ClearCache();
		}

		internal void SetClassicalDebugOffsetX(int value)
		{
			_classicalDebugOffsetX = value;
			ClearCache();
		}

		internal Picture GetPalacePart(PalaceStyle style, PalacePart part, int level)
		{
			if (level == 0)
			{
				style = PalaceStyle.None;
			}

			int combine = (level * 100) + ((int)style * 10) + (int)part;
			if (!_cache.ContainsKey(combine))
			{
				Picture picture = null;
				PalacePictureLayout layout = PalaceSpriteLayout.GetLayout(level);

				int offsetX = 0, offsetY = 0;
				if (style == PalaceStyle.Classical) offsetX = layout.Q3.X.StartInclusive + _classicalDebugOffsetX;
				if (style == PalaceStyle.Islamic) offsetY = 100;

				int middleShadowX = style == PalaceStyle.Classical
					? (layout.Q3.X.StartInclusive + layout.Q3Slices.MiddleShadow.StartInclusive + _classicalDebugOffsetX + ClassicalMiddleSourceOffsetX)
					: (layout.Q1Slices.MiddleShadow.StartInclusive + offsetX);

				int middleNormalX = style == PalaceStyle.Classical
					? (layout.Q3.X.StartInclusive + layout.Q3Slices.MiddleNormal.StartInclusive + _classicalDebugOffsetX + ClassicalMiddleSourceOffsetX)
					: (layout.Q1Slices.MiddleNormal.StartInclusive + offsetX);

				int classicalTowerOffsetY = style == PalaceStyle.Classical ? layout.Q4Slices.DrawOffsetY : 0;

				switch (part)
				{
					case PalacePart.LeftTower:
						picture = new Picture(35, 101);
						if (style == PalaceStyle.Classical)
						{
							picture.AddLayer(
								_getPictureByName($"CASTLE{level}")[layout.Q4Slices.LeftEnd.StartInclusive + _classicalDebugOffsetX + ClassicalLeftTowerSourceOffsetX, layout.Q4.Y.StartInclusive + 1, layout.Q4Slices.LeftEnd.Length, 99],
								0,
								2 + classicalTowerOffsetY);
							break;
						}
						picture.AddLayer(_getPictureByName($"CASTLE{level}")[104 + offsetX, 1 + offsetY, 27, 99], 8, 2);
						break;
					case PalacePart.RightTower:
						picture = new Picture(35, 101);
						if (style == PalaceStyle.Classical)
						{
							picture.AddLayer(
								_getPictureByName($"CASTLE{level}")[layout.Q4Slices.RightEnd.StartInclusive + _classicalDebugOffsetX, layout.Q4.Y.StartInclusive + 1, layout.Q4Slices.RightEnd.Length, 99],
								0,
								2 + classicalTowerOffsetY);
							break;
						}
						picture.AddLayer(_getPictureByName($"CASTLE{level}")[132 + offsetX, 1 + offsetY, 27, 99], 0, 2);
						break;
					case PalacePart.Wall:
					case PalacePart.WallShadow:
					{
						picture = new Picture(48, 101);
						if (level == 0)
						{
							picture.AddLayer(_getPictureByName("CASTLE0")[layout.Q1Slices.MiddleShadow.StartInclusive + offsetX, 1 + offsetY, layout.Q1Slices.MiddleShadow.Length, 99], 0);
							picture.AddLayer(_getPictureByName("CASTLE0")[layout.Q1Slices.MiddleNormal.StartInclusive + offsetX, 1 + offsetY, layout.Q1Slices.MiddleNormal.Length, 99], layout.Q1Slices.MiddleShadow.Length);
							break;
						}
						int firstHalfX = part == PalacePart.WallShadow ? middleShadowX : middleNormalX;
						picture.AddLayer(_getPictureByName($"CASTLE{level}")[firstHalfX, 1 + offsetY, layout.Q3Slices.MiddleShadow.Length, 99], left: 0);
						picture.AddLayer(_getPictureByName($"CASTLE{level}")[middleNormalX, 1 + offsetY, layout.Q3Slices.MiddleNormal.Length, 99], left: layout.Q3Slices.MiddleShadow.Length);
						break;
					}
					case PalacePart.LeftTowerWall:
					{
						picture = new Picture(57, 101);
						if (level == 0)
						{
							picture.AddLayer(_getPictureByName("CASTLE0")[layout.Q1Slices.MiddleNormal.StartInclusive + offsetX, 1 + offsetY, layout.Q1Slices.MiddleNormal.Length, 99], 
								left: 33);
							break;
						}
						picture.AddLayer(_getPictureByName($"CASTLE{level}")[middleNormalX, 1 + offsetY, layout.Q3Slices.MiddleNormal.Length, 99], 33);
						if (style == PalaceStyle.Classical)
						{
							picture.AddLayer(
								_getPictureByName($"CASTLE{level}")[layout.Q4Slices.LeftEnd.StartInclusive + _classicalDebugOffsetX + ClassicalLeftTowerSourceOffsetX, layout.Q4.Y.StartInclusive + 1, layout.Q4Slices.LeftEnd.Length, 99],
								left: 1,
								top: 2 + classicalTowerOffsetY);
							break;
						}
						picture.AddLayer(_getPictureByName($"CASTLE{level}")[104 + offsetX, 1 + offsetY, 27, 99], left: 8, top: 2);
						break;
					}
					case PalacePart.RightTowerWall:
					case PalacePart.RightTowerWallShadow:
					{
						picture = new Picture(57, 101);
						if (level == 0)
						{
							picture.AddLayer(_getPictureByName("CASTLE0")[layout.Q1Slices.MiddleShadow.StartInclusive + offsetX, 1 + offsetY, layout.Q1Slices.MiddleShadow.Length, 99]);
							break;
						}
						int rightWallX = part == PalacePart.RightTowerWall ? middleNormalX : middleShadowX;
						picture.AddLayer(_getPictureByName($"CASTLE{level}")[rightWallX, 1 + offsetY, layout.Q3Slices.MiddleShadow.Length, 99], 0);
						if (style == PalaceStyle.Classical)
						{
							picture.AddLayer(
								_getPictureByName($"CASTLE{level}")[layout.Q4Slices.RightEnd.StartInclusive + _classicalDebugOffsetX, layout.Q4.Y.StartInclusive + 1, layout.Q4Slices.RightEnd.Length, 99],
								left: 22,
								top: 2 + classicalTowerOffsetY);
							break;
						}
						picture.AddLayer(_getPictureByName($"CASTLE{level}")[132 + offsetX, 1 + offsetY, 27, 99], left: 21, top: 2);
						break;
					}
					case PalacePart.Center:
					{
						picture = _getPictureByName($"CASTLE{level}")[0 + offsetX, 1 + offsetY, 52, 99];
						break;
					}
				}

				_cache[combine] = picture;
			}
			return _cache[combine];
		}
	}
}