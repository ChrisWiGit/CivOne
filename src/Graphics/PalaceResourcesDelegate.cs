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
	public sealed class PalaceResourcesDelegate(Func<string, Picture> getPictureByName, IPalaceSpriteLayout palaceSpriteLayout = null)
	{
		private const int TOWER_WIDTH = 35;
		private const int WALL_WIDTH = 48;
		private const int TOWER_WALL_WIDTH = 57;
		private const int PALACE_PART_HEIGHT = 101;
		private const int BUILDING_HEIGHT_COMPONENT = 99;
		private const int TOWER_LAYER_TOP_OFFSET = 2;
		private const int TOWER_LAYER_LEFT_OFFSET_CLASSICAL = 0;
		private const int TOWER_LAYER_LEFT_OFFSET_OTHER = 8;
		private const int LEFT_TOWER_WALL_LEFT_OFFSET = 33;
		private const int RIGHT_TOWER_WALL_LEFT_OFFSET_CLASSICAL = 22;
		private const int RIGHT_TOWER_WALL_LEFT_OFFSET_OTHER = 21;

		private readonly Func<string, Picture> _getPictureByName = getPictureByName ?? throw new ArgumentNullException(nameof(getPictureByName));
		private readonly IPalaceSpriteLayout _palaceSpriteLayout = palaceSpriteLayout ?? new PalaceSpriteLayout();
		private readonly Dictionary<int, Picture> _cache = [];

		internal void ClearCache() => _cache.Clear();

		private Picture GetCastleSourceImage(int level) => _getPictureByName($"CASTLE{level}");

		private Picture GetCastleSourcePartImage(
			int level,
			PalacePart part,
			PalaceStyle style,
			PalacePictureLayout layout,
			int offsetX = 0)
		{
			PalacePartSourceRect sourceRect = _palaceSpriteLayout.GetPartSourceRect(part, style, layout);
			return GetCastleSourceImage(level)[sourceRect.X + offsetX, sourceRect.Y, sourceRect.Width, sourceRect.Height];
		}

		private Picture GetCastle0SourceImage() => _getPictureByName("CASTLE0");

		private static Picture CreateTowerPicture() => new(TOWER_WIDTH, PALACE_PART_HEIGHT);

		private static Picture CreateWallPicture() => new(WALL_WIDTH, PALACE_PART_HEIGHT);

		private static Picture CreateTowerWallPicture() => new(TOWER_WALL_WIDTH, PALACE_PART_HEIGHT);

		private Picture BuildLeftTowerPart(PalaceStyle style, int level, PalacePictureLayout layout, int offsetX)
        {
			Picture picture = CreateTowerPicture();
			

			if (style == PalaceStyle.Classical)
			{
				picture.AddLayer(
					GetCastleSourcePartImage(level, PalacePart.LeftTower, style, layout),
					TOWER_LAYER_LEFT_OFFSET_CLASSICAL,
					TOWER_LAYER_TOP_OFFSET);
				return picture;
			}

			picture.AddLayer(GetCastleSourcePartImage(level, PalacePart.LeftTower, style, layout, offsetX), TOWER_LAYER_LEFT_OFFSET_OTHER, TOWER_LAYER_TOP_OFFSET);

			return picture;
		}

		private Picture BuildRightTowerPart(PalaceStyle style, int level, PalacePictureLayout layout, int offsetX)
		{
			if (style == PalaceStyle.Classical)
			{
				Picture classicalPicture = CreateTowerPicture();
				classicalPicture.AddLayer(
					GetCastleSourcePartImage(level, PalacePart.RightTower, style, layout),
					TOWER_LAYER_LEFT_OFFSET_CLASSICAL,
					TOWER_LAYER_TOP_OFFSET);
				return classicalPicture;
			}

			Picture picture = CreateTowerPicture();
			picture.AddLayer(GetCastleSourcePartImage(level, PalacePart.RightTower, style, layout, offsetX), TOWER_LAYER_LEFT_OFFSET_CLASSICAL, TOWER_LAYER_TOP_OFFSET);
			return picture;
		}

		private Picture BuildWallPart(int level, PalacePictureLayout layout, int offsetX, int offsetY, int wallFirstHalfX, int middleNormalX)
		{
			Picture picture = CreateWallPicture();
			if (level == 0)
			{
				picture.AddLayer(GetCastle0SourceImage()[layout.Q1Slices.MiddleShadow.StartInclusive + offsetX, offsetY, layout.Q1Slices.MiddleShadow.Length, BUILDING_HEIGHT_COMPONENT], 0);
				picture.AddLayer(GetCastle0SourceImage()[layout.Q1Slices.MiddleNormal.StartInclusive + offsetX, offsetY, layout.Q1Slices.MiddleNormal.Length, BUILDING_HEIGHT_COMPONENT], layout.Q1Slices.MiddleShadow.Length);
				return picture;
			}

			picture.AddLayer(GetCastleSourceImage(level)[wallFirstHalfX, offsetY, layout.Q3Slices.MiddleShadow.Length, BUILDING_HEIGHT_COMPONENT], left: 0);
			picture.AddLayer(GetCastleSourceImage(level)[middleNormalX, offsetY, layout.Q3Slices.MiddleNormal.Length, BUILDING_HEIGHT_COMPONENT], left: layout.Q3Slices.MiddleShadow.Length);
			return picture;
		}

		private Picture BuildLeftTowerWallPart(PalaceStyle style, int level, PalacePictureLayout layout, int offsetX, int offsetY, int middleNormalX)
		{
			Picture picture = CreateTowerWallPicture();
			if (level == 0)
			{
				picture.AddLayer(GetCastle0SourceImage()[layout.Q1Slices.MiddleNormal.StartInclusive + offsetX, offsetY, layout.Q1Slices.MiddleNormal.Length, BUILDING_HEIGHT_COMPONENT], left: LEFT_TOWER_WALL_LEFT_OFFSET);
				return picture;
			}

			picture.AddLayer(GetCastleSourceImage(level)[middleNormalX, offsetY, layout.Q3Slices.MiddleNormal.Length, BUILDING_HEIGHT_COMPONENT], LEFT_TOWER_WALL_LEFT_OFFSET);
			if (style == PalaceStyle.Classical)
			{
				picture.AddLayer(
					GetCastleSourcePartImage(level, PalacePart.LeftTowerWall, style, layout),
					left: TOWER_LAYER_LEFT_OFFSET_CLASSICAL,
					top: TOWER_LAYER_TOP_OFFSET);
				return picture;
			}

			picture.AddLayer(GetCastleSourcePartImage(level, PalacePart.LeftTowerWall, style, layout, offsetX), left: TOWER_LAYER_LEFT_OFFSET_OTHER, top: TOWER_LAYER_TOP_OFFSET);
			return picture;
		}

		private Picture BuildRightTowerWallPart(PalacePart part, PalaceStyle style, int level, PalacePictureLayout layout, int offsetX, int offsetY, int rightTowerWallX)
		{
			Picture picture = CreateTowerWallPicture();
			if (level == 0)
			{
				picture.AddLayer(GetCastle0SourceImage()[layout.Q1Slices.MiddleShadow.StartInclusive + offsetX, offsetY, layout.Q1Slices.MiddleShadow.Length, BUILDING_HEIGHT_COMPONENT]);
				return picture;
			}

			picture.AddLayer(GetCastleSourceImage(level)[rightTowerWallX, offsetY, layout.Q3Slices.MiddleShadow.Length, BUILDING_HEIGHT_COMPONENT], 0);
			if (style == PalaceStyle.Classical)
			{
				picture.AddLayer(
					GetCastleSourcePartImage(level, part, style, layout),
					left: RIGHT_TOWER_WALL_LEFT_OFFSET_CLASSICAL,
					top: TOWER_LAYER_TOP_OFFSET);
				return picture;
			}
			picture.AddLayer(GetCastleSourcePartImage(level, part, style, layout, offsetX), left: RIGHT_TOWER_WALL_LEFT_OFFSET_OTHER, top: TOWER_LAYER_TOP_OFFSET);

			return picture;
		}

		static int HashKey(int level, PalaceStyle style, PalacePart part) => (level * 100) + ((int)style * 10) + (int)part;

		internal Picture GetPalacePart(PalaceStyle style, PalacePart part, int level)
		{
			if (level == 0)
			{
				style = PalaceStyle.None;
			}

			int combine = HashKey(level, style, part);
			if (_cache.TryGetValue(combine, out Picture value))
			{
				return value;
			}

			Picture picture = null;
			PalacePictureLayout layout = _palaceSpriteLayout.GetLayout(level);
			PalacePartSourceSelection sourceLocation = _palaceSpriteLayout.GetSpriteCoordinatesForPart(style, part, layout);

			int offsetX = sourceLocation.OffsetX;
			int offsetY = sourceLocation.OffsetY;
			int middleNormalX = sourceLocation.MiddleNormalX;

			switch (part)
			{
				case PalacePart.LeftTower:
					picture = BuildLeftTowerPart(style, level, layout, offsetX);
					break;
				case PalacePart.RightTower:
					picture = BuildRightTowerPart(style, level, layout, offsetX);
					break;
				case PalacePart.Wall:
				case PalacePart.WallShadow:
					picture = BuildWallPart(level, layout, offsetX, offsetY, sourceLocation.WallFirstHalfX, middleNormalX);
					break;
				case PalacePart.LeftTowerWall:
					picture = BuildLeftTowerWallPart(style, level, layout, offsetX, offsetY, middleNormalX);
					break;
				case PalacePart.RightTowerWall:
				case PalacePart.RightTowerWallShadow:
					picture = BuildRightTowerWallPart(part, style, level, layout, offsetX, offsetY, sourceLocation.RightTowerWallX);
					break;
				case PalacePart.Center:
					{
						picture = GetCastleSourcePartImage(level, part, style, layout, offsetX);
						break;
					}
			}

			_cache[combine] = picture;
			return _cache[combine];
		}
	}
}