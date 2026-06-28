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

				Human.MapZoomBasisPoints = nextBasisPoints;

				SyncZoomState(keepFocus: true, focusPixel: args.Location);
				_gameMap.Refresh();
				return true;
			}

			public bool KeyDown(KeyboardEventArgs args)
			{
				if ((args.Modifier & KeyModifier.Control) == 0)
				{
					return false;
				}

				int direction;
				switch (args.Key)
				{
					case Key.PageDown:
						direction = +1;
						break;
					case Key.PageUp:
						direction = -1;
						break;
					default:
						return false;
				}

				int currentIndex = FindZoomPresetIndex(_gameMap.CurrentZoomBasisPoints);
				int nextIndex = Math.Clamp(currentIndex + direction, 0, MapZoomSettings.BasisPointsPresets.Length - 1);
				int nextBasisPoints = MapZoomSettings.BasisPointsPresets[nextIndex];
				if (nextBasisPoints == _gameMap.CurrentZoomBasisPoints)
				{
					return true;
				}

				Human.MapZoomBasisPoints = nextBasisPoints;

				SyncZoomState();
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

			public static Bytemap ScaleBitmap(Bytemap source, int targetWidth, int targetHeight)
			{
				Bytemap output = new(targetWidth, targetHeight);
				if (source == null || targetWidth <= 0 || targetHeight <= 0)
				{
					return output;
				}

				if (source.Width == targetWidth && source.Height == targetHeight)
				{
					output.Dispose();
					return Bytemap.Copy(source);
				}

				double xRatio = targetWidth > 1 ? (double)(source.Width - 1) / (targetWidth - 1) : 0d;
				double yRatio = targetHeight > 1 ? (double)(source.Height - 1) / (targetHeight - 1) : 0d;

				for (int y = 0; y < targetHeight; y++)
				{
					double sampleY = y * yRatio;
					int sourceY = (int)sampleY;
					int sourceY2 = Math.Min(sourceY + 1, source.Height - 1);
					double yBlend = sampleY - sourceY;
					double invYBlend = 1d - yBlend;
					for (int x = 0; x < targetWidth; x++)
					{
						double sampleX = x * xRatio;
						int sourceX = (int)sampleX;
						int sourceX2 = Math.Min(sourceX + 1, source.Width - 1);
						double xBlend = sampleX - sourceX;
						double invXBlend = 1d - xBlend;

						byte c00 = source[sourceX, sourceY];
						byte c10 = source[sourceX2, sourceY];
						byte c01 = source[sourceX, sourceY2];
						byte c11 = source[sourceX2, sourceY2];

						double w00 = invXBlend * invYBlend;
						double w10 = xBlend * invYBlend;
						double w01 = invXBlend * yBlend;
						double w11 = xBlend * yBlend;

						double bestWeight = -1d;
						byte bestIndex = 0;

						double weight00 = w00 + (c10 == c00 ? w10 : 0d) + (c01 == c00 ? w01 : 0d) + (c11 == c00 ? w11 : 0d);
						if (weight00 > bestWeight)
						{
							bestWeight = weight00;
							bestIndex = c00;
						}

						double weight10 = (c00 == c10 ? w00 : 0d) + w10 + (c01 == c10 ? w01 : 0d) + (c11 == c10 ? w11 : 0d);
						if (weight10 > bestWeight || (Math.Abs(weight10 - bestWeight) < 0.0001d && bestIndex == 0 && c10 != 0))
						{
							bestWeight = weight10;
							bestIndex = c10;
						}

						double weight01 = (c00 == c01 ? w00 : 0d) + (c10 == c01 ? w10 : 0d) + w01 + (c11 == c01 ? w11 : 0d);
						if (weight01 > bestWeight || (Math.Abs(weight01 - bestWeight) < 0.0001d && bestIndex == 0 && c01 != 0))
						{
							bestWeight = weight01;
							bestIndex = c01;
						}

						double weight11 = (c00 == c11 ? w00 : 0d) + (c10 == c11 ? w10 : 0d) + (c01 == c11 ? w01 : 0d) + w11;
						if (weight11 > bestWeight || (Math.Abs(weight11 - bestWeight) < 0.0001d && bestIndex == 0 && c11 != 0))
						{
							bestIndex = c11;
						}

						output[x, y] = bestIndex;
					}
				}

				return output;
			}
		}
	}
}