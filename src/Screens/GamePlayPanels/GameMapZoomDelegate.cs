// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne
{
	public static class MapZoomSettings
	{
		public const int DefaultBasisPoints = 1000;
		public const int MinBasisPoints = 125;
		public const int MaxBasisPoints = 1000;

		/// Preset zoom levels in basis points, ordered from most zoomed-in to most zoomed-out.
		/// The values are chosen by testing.
		public static readonly int[] BasisPointsPresets = [DefaultBasisPoints, 900, 800, 700, 600, 500, 400, 300, 200, MinBasisPoints];

		public static int NormalizeBasisPoints(int basisPoints)
		{
			if (basisPoints <= 0)
			{
				return DefaultBasisPoints;
			}

			return Math.Clamp(basisPoints, MinBasisPoints, MaxBasisPoints);
		}
	}
}

namespace CivOne.Screens.GamePlayPanels
{
	internal partial class GameMap
	{
		/// <summary>
		/// Handles zoom state, viewport metrics, and cursor-focused wheel zooming.
		/// </summary>
		private sealed class GameMapZoomDelegate(GameMap gameMap)
		{
			private readonly GameMap _gameMap = gameMap;

			public bool SyncZoomState(bool keepFocus = false, Point? focusPixel = null)
			{
				int basisPoints = _gameMap.CurrentZoomBasisPoints;
				int tilePixelSize = TilePixelSizeFromBasisPoints(basisPoints);
				if (_gameMap._zoomBasisPoints == basisPoints && _gameMap._tilePixelSize == tilePixelSize)
				{
					return false;
				}

				Point? focusTile = null;
				if (keepFocus && focusPixel.HasValue)
				{
					focusTile = GetWorldTileAtPixel(
						focusPixel.Value,
						_gameMap._x,
						_gameMap._y,
						_gameMap._tilesX,
						_gameMap._tilesY,
						_gameMap._tilePixelSize);
				}

				_gameMap._zoomBasisPoints = basisPoints;
				_gameMap._tilePixelSize = tilePixelSize;
				UpdateViewportMetrics();

				if (keepFocus && focusPixel.HasValue && focusTile.HasValue)
				{
					_gameMap.SetViewOrigin(
						focusTile.Value.X - (focusPixel.Value.X / _gameMap._tilePixelSize),
						focusTile.Value.Y - (focusPixel.Value.Y / _gameMap._tilePixelSize));
				}

				_gameMap._fullRedraw = true;
				_gameMap._update = true;
				return true;
			}

			public void Resize(int width, int height)
			{
				_gameMap.Bitmap = new Bytemap(width, height);
				_gameMap._tilePixelSize = TilePixelSizeFromBasisPoints(_gameMap.CurrentZoomBasisPoints);
				_gameMap._zoomBasisPoints = _gameMap.CurrentZoomBasisPoints;
				UpdateViewportMetrics();

				if (_gameMap._y < 0)
				{
					_gameMap._y = 0;
				}

				while (_gameMap._y + _gameMap._tilesY > Map.HEIGHT)
				{
					_gameMap._y--;
				}

				_gameMap._update = true;
				_gameMap._fullRedraw = true;
			}

			public bool MouseWheel(ScreenEventArgs args)
			{
				if ((args.Modifier & KeyModifier.Control) == 0)
				{
					return false;
				}

				int currentIndex = FindZoomPresetIndex(_gameMap.CurrentZoomBasisPoints);
				int nextIndex = currentIndex;
				if (args.WheelDelta < 0)
				{
					nextIndex = Math.Min(currentIndex + 1, MapZoomSettings.BasisPointsPresets.Length - 1);
				}
				else if (args.WheelDelta > 0)
				{
					nextIndex = Math.Max(currentIndex - 1, 0);
				}

				int nextBasisPoints = MapZoomSettings.BasisPointsPresets[nextIndex];
				if (nextBasisPoints == _gameMap.CurrentZoomBasisPoints)
				{
					return true;
				}

				if (Game.CurrentPlayer != null)
				{
					Game.CurrentPlayer.MapZoomBasisPoints = nextBasisPoints;
				}

				SyncZoomState(keepFocus: true, focusPixel: args.Location);
				_gameMap.Refresh();
				return true;
			}

			private static int TilePixelSizeFromBasisPoints(int basisPoints)
				=> Math.Max(1, (BaseTilePixelSize * basisPoints + 500) / 1000);

			private static int FindZoomPresetIndex(int basisPoints)
			{
				int closestIndex = 0;
				int closestDistance = int.MaxValue;
				for (int i = 0; i < MapZoomSettings.BasisPointsPresets.Length; i++)
				{
					int distance = Math.Abs(MapZoomSettings.BasisPointsPresets[i] - basisPoints);
					if (distance >= closestDistance)
					{
						continue;
					}

					closestDistance = distance;
					closestIndex = i;
				}

				return closestIndex;
			}

			private static Point GetWorldTileAtPixel(
				Point pixel,
				int originX,
				int originY,
				int visibleTilesX,
				int visibleTilesY,
				int tilePixelSize)
			{
				int safeTilesX = Math.Max(1, visibleTilesX);
				int safeTilesY = Math.Max(1, visibleTilesY);
				int safeTilePixelSize = Math.Max(1, tilePixelSize);

				int localTileX = Math.Clamp(Math.Max(0, pixel.X) / safeTilePixelSize, 0, safeTilesX - 1);
				int localTileY = Math.Clamp(Math.Max(0, pixel.Y) / safeTilePixelSize, 0, safeTilesY - 1);
				int worldX = originX + localTileX;
				while (worldX < 0)
				{
					worldX += Map.WIDTH;
				}

				while (worldX >= Map.WIDTH)
				{
					worldX -= Map.WIDTH;
				}

				int worldY = Math.Clamp(originY + localTileY, 0, Map.HEIGHT - 1);
				return new Point(worldX, worldY);
			}

			private void UpdateViewportMetrics()
			{
				int width = _gameMap.Bitmap.Width;
				int height = _gameMap.Bitmap.Height;
				_gameMap._tilesX = Math.Min((int)Math.Ceiling((double)width / _gameMap._tilePixelSize), Map.WIDTH);
				_gameMap._tilesY = Math.Min((int)Math.Ceiling((double)height / _gameMap._tilePixelSize), Map.HEIGHT);
				if (_gameMap._tilesX < 1)
				{
					_gameMap._tilesX = 1;
				}

				if (_gameMap._tilesY < 1)
				{
					_gameMap._tilesY = 1;
				}

				_gameMap._y = Math.Min(_gameMap._y, Math.Max(0, Map.HEIGHT - _gameMap._tilesY));
			}
		}
	}
}