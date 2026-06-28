// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Globalization;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Tiles;

namespace CivOne.Screens.GamePlayPanels
{
	internal partial class GameMap
	{
		/// <summary>
		/// Handles terrain editor overlay rendering.
		/// </summary>
		private sealed class GameTerrainEditorRenderDelegate(GameMap gameMap)
		{
			private readonly GameMap _gameMap = gameMap;

			public void DrawLandValuesOverlay()
			{
				if (!_gameMap.TerrainEditorEnabled || !_gameMap._editorState.ShowLandValues)
				{
					return;
				}

				for (int yy = 0; yy < _gameMap._tilesY; yy++)
				{
					for (int xx = 0; xx < _gameMap._tilesX; xx++)
					{
						ITile tile = _gameMap.Tiles[xx, yy];
						if (tile == null)
						{
							continue;
						}

						int px = (xx * _gameMap._tilePixelSize) + 1;
						int py = (yy * _gameMap._tilePixelSize) + Math.Max(1, _gameMap._tilePixelSize - 7);
						_gameMap.DrawText(tile.LandValue.ToString(CultureInfo.InvariantCulture), 0, 15, px, py, TextAlign.Left);
					}
				}
			}

			public void DrawSpawnUnitPreview()
			{
				if (!_gameMap.TerrainEditorEnabled || _gameMap._editorState.CurrentMode != EditorMode.SpawnUnit)
				{
					return;
				}

				ITile tile = Map[_gameMap._hoveredTileX, _gameMap._hoveredTileY];
				if (tile == null)
				{
					return;
				}

				int dx = _gameMap.GetX(tile);
				int dy = _gameMap.GetY(tile);
				bool isXOutOfBounds = dx < 0 || dx >= _gameMap._tilesX;
				bool isYOutOfBounds = dy < 0 || dy >= _gameMap._tilesY;
				if (isXOutOfBounds || isYOutOfBounds)
				{
					return;
				}

				dx *= _gameMap._tilePixelSize;
				dy *= _gameMap._tilePixelSize;
				Bytemap? unitSource = Graphics.Sprites.Unit.Base(_gameMap._editorState.SelectedUnitType, _gameMap._editorState.CityOwner).Bitmap;
				if (unitSource == null)
				{
					return;
				}
				using Bytemap unitPicture = GameMapZoomDelegate.ScaleBitmap(unitSource, _gameMap._tilePixelSize, _gameMap._tilePixelSize);
				_gameMap.AddLayer(unitPicture, dx, dy);
			}

			public void DrawBrushPreview()
			{
				if (!_gameMap.TerrainEditorEnabled)
				{
					return;
				}

				int brushSize = _gameMap._terrainEditorDelegate.GetBrushSize(_gameMap._editorState.PencilSizeIndex);
				int minRel = -((brushSize - 1) / 2);
				int maxRel = brushSize / 2;
				int centerX = _gameMap._hoveredTileX;
				int centerY = _gameMap._hoveredTileY;

				int startTileX = centerX + minRel;
				int endTileX = centerX + maxRel;
				int startTileY = centerY + minRel;
				int endTileY = centerY + maxRel;

				for (int ty = startTileY; ty <= endTileY; ty++)
				{
					for (int tx = startTileX; tx <= endTileX; tx++)
					{
						ITile tile = Map[tx, ty];
						if (tile == null)
						{
							continue;
						}

						int dx = _gameMap.GetX(tile);
						int dy = _gameMap.GetY(tile);
						bool isXOutOfBounds = dx < 0 || dx >= _gameMap._tilesX;
						bool isYOutOfBounds = dy < 0 || dy >= _gameMap._tilesY;
						if (isXOutOfBounds || isYOutOfBounds)
						{
							continue;
						}

						dx *= _gameMap._tilePixelSize;
						dy *= _gameMap._tilePixelSize;
						_gameMap.DrawRectangle(dx, dy, _gameMap._tilePixelSize, _gameMap._tilePixelSize, 15);
					}
				}
			}
		}
	}
}