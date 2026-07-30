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
using System.Globalization;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Services.StartPositions;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Screens
{
	/// <summary>
	/// Overview map screen.
	/// Each map tile is drawn as a square block of at least 4x4 pixels.
	/// Maps that are smaller than the canvas are drawn with enlarged tiles and centered, so the overview
	/// fills the screen instead of sitting in a small corner block.
	/// Maps that are still larger than the canvas keep the 4x4 blocks and can be scrolled with the cursor
	/// keys, Page Up/Down and the mouse wheel.
	/// </summary>
	[ScreenResizeable]
	internal class WorldMap : BaseScreen
	{
		/// <summary>Pixel size of a single map tile in the source tile sprites and smallest size used on screen.</summary>
		private const int TileSize = 4;

		/// <summary>Number of tiles scrolled per cursor key press.</summary>
		private const int ScrollStep = 4;

		/// <summary>Font used for the key hints at the bottom of the screen (1 = small font).</summary>
		private const int HintFont = 1;

		/// <summary>Pixel height of a single hint line.</summary>
		private const int HintHeight = 9;

		/// <summary>Palette index of the hint text.</summary>
		private const byte HintTextColour = 15;

		/// <summary>Palette index of the grey shadow drawn behind the hint text.</summary>
		private const byte HintShadowColour = 8;

		/// <summary>Palette index of the dashed start-position area borders (white).</summary>
		private const byte StartPositionAreaColour = 15;

		/// <summary>Palette index of the prime meridian line (yellow).</summary>
		private const byte PrimeMeridianColour = 14;

		/// <summary>Number of pixels drawn per dash of an area border.</summary>
		private const int DashLength = 2;

		/// <summary>Number of pixels left untouched between two dashes of an area border.</summary>
		private const int DashGap = 2;

		private bool _update = true;
		private bool _showStartPositionAreas;

		/// <summary>Map column shown at the left edge of the visible map area.</summary>
		private int _offsetX;

		/// <summary>Map row shown at the top edge of the visible map area.</summary>
		private int _offsetY;

		/// <summary>Tile sprites scaled to <see cref="_scaledTileSize"/>, keyed by their position in the tile sheet.</summary>
		private readonly Dictionary<int, Bytemap> _scaledTiles = [];

		/// <summary>Tile size the entries of <see cref="_scaledTiles"/> were built for.</summary>
		private int _scaledTileSize;

		private static bool CanToggleStartPositionAreas => Settings.DebugMenu && Settings.StartPositionAlgorithm == Settings.StartPositionAlgorithmType.AreaBased;

		/// <summary>
		/// Pixel size of a single map tile on screen.
		/// Small maps are enlarged so the whole map fills the canvas; the aspect ratio is kept by using the
		/// same size horizontally and vertically.
		/// Maps too large for the canvas stay at <see cref="TileSize"/> and are scrolled instead.
		/// </summary>
		private int TilePixelSize => Math.Max(TileSize, Math.Min(this.Width() / Map.WIDTH, this.Height() / Map.HEIGHT));

		private int ViewColumns => Math.Max(1, this.Width() / TilePixelSize);
		private int ViewRows => Math.Max(1, this.Height() / TilePixelSize);

		/// <summary>Left pixel column of the map, centering it when it is narrower than the canvas.</summary>
		private int OriginX => Math.Max(0, (this.Width() - (Map.WIDTH * TilePixelSize)) / 2);

		/// <summary>Top pixel row of the map, centering it when it is shorter than the canvas.</summary>
		private int OriginY => Math.Max(0, (this.Height() - (Map.HEIGHT * TilePixelSize)) / 2);

		/// <summary><c>true</c> when the whole map width fits on screen and horizontal scrolling is pointless.</summary>
		private bool FitsHorizontally => Map.WIDTH <= ViewColumns;

		/// <summary>Largest allowed value for <see cref="_offsetY"/>; 0 when the whole map height fits on screen.</summary>
		private int MaxOffsetY => Math.Max(0, Map.HEIGHT - ViewRows);

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;
			_update = false;
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (CanToggleStartPositionAreas && args.Key == Key.Character && char.ToLowerInvariant(args.KeyChar) == 'a')
			{
				_showStartPositionAreas = !_showStartPositionAreas;
				Redraw();
				return true;
			}

			if (HandleScrollKey(args.Key))
			{
				return true;
			}

			Destroy();
			return true;
		}

		/// <summary>
		/// Scrolls the overview map for navigation keys.
		/// Navigation keys are always reported as handled, even when the view is already at the map edge,
		/// so that scrolling past the edge stops instead of closing the screen.
		/// </summary>
		/// <param name="key">The pressed key.</param>
		/// <returns><c>true</c> if the key was a navigation key, otherwise <c>false</c>.</returns>
		private bool HandleScrollKey(Key key)
		{
			switch (key)
			{
				case Key.Left:
					Scroll(-ScrollStep, 0);
					return true;
				case Key.Right:
					Scroll(ScrollStep, 0);
					return true;
				case Key.Up:
					Scroll(0, -ScrollStep);
					return true;
				case Key.Down:
					Scroll(0, ScrollStep);
					return true;
				case Key.PageUp:
					Scroll(0, -ViewRows);
					return true;
				case Key.PageDown:
					Scroll(0, ViewRows);
					return true;
				case Key.Home:
					CenterOnStartPosition();
					Redraw();
					return true;
				default:
					return false;
			}
		}

		public override bool MouseWheel(ScreenEventArgs args)
		{
			int stepX = Math.Sign(args.WheelDeltaX) * ScrollStep;
			int stepY = -Math.Sign(args.WheelDelta) * ScrollStep;
			if (stepX == 0 && stepY == 0)
			{
				return false;
			}

			Scroll(stepX, stepY);
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			Destroy();
			return true;
		}

		/// <summary>
		/// Moves the visible section of the map.
		/// Horizontal scrolling wraps around the map seam, vertical scrolling is clamped to the map bounds.
		/// </summary>
		/// <param name="stepX">Number of tiles to scroll horizontally.</param>
		/// <param name="stepY">Number of tiles to scroll vertically.</param>
		/// <returns><c>true</c> if the visible section changed.</returns>
		private bool Scroll(int stepX, int stepY)
		{
			int offsetX = FitsHorizontally ? 0 : WrapX(_offsetX + stepX);
			int offsetY = Math.Clamp(_offsetY + stepY, 0, MaxOffsetY);
			if (offsetX == _offsetX && offsetY == _offsetY)
			{
				return false;
			}

			_offsetX = offsetX;
			_offsetY = offsetY;
			Redraw();
			return true;
		}

		private void Redraw()
		{
			Draw();
			_update = true;
			Refresh();
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			if (FitsHorizontally)
			{
				_offsetX = 0;
			}
			_offsetY = Math.Clamp(_offsetY, 0, MaxOffsetY);
			Redraw();
		}

		private static int WrapX(int x)
		{
			int width = Map.WIDTH;
			return ((x % width) + width) % width;
		}

		private static int VisibleTop
		{
			get
			{
				Player player = Human;
				for(int yy = 0; yy < Map.HEIGHT; yy++)
				for(int xx = 0; xx < Map.WIDTH; xx++)
				{
					if (player.Visible(xx, yy)) return yy;
				}
				return 0;
			}
		}

		private static int VisibleBottom
		{
			get
			{
				Player player = Human;
				for(int yy = Map.HEIGHT - 1; yy >= 0; yy--)
				for(int xx = 0; xx < Map.WIDTH; xx++)
				{
					if (player.Visible(xx, yy)) return yy;
				}
				return 0;
			}
		}

		public WorldMap()
		{
			Palette = Resources.WorldMapTiles.Palette;
			CenterOnStartPosition();
			Draw();
		}

		/// <summary>
		/// Centers the view horizontally on the human player's starting position and vertically on the
		/// explored part of the map.
		/// </summary>
		private void CenterOnStartPosition()
		{
			_offsetX = Settings.RevealWorld || FitsHorizontally ? 0 : WrapX(Human.StartX - (ViewColumns / 2));

			int top = 0;
			int bottom = Map.HEIGHT - 1;
			if (!Settings.RevealWorld)
			{
				top = VisibleTop;
				bottom = VisibleBottom;
			}

			_offsetY = Math.Clamp(((top + bottom) / 2) - (ViewRows / 2), 0, MaxOffsetY);
		}

		private void Draw()
		{
			this.Clear(5);

			int tilePixelSize = TilePixelSize;
			int originX = OriginX;
			int originY = OriginY;
			int columns = Math.Min(ViewColumns, Map.WIDTH);
			int rows = Math.Min(ViewRows, Map.HEIGHT - _offsetY);

			// A city or unit marker keeps the same proportions as on the original 4x4 blocks.
			int markerSize = Math.Max(1, tilePixelSize - (tilePixelSize / 4));
			int markerOffset = Math.Max(1, tilePixelSize / 4);

			for (int column = 0; column < columns; column++)
			for (int row = 0; row < rows; row++)
			{
				int x = WrapX(_offsetX + column);
				int y = _offsetY + row;
				if (!Settings.RevealWorld && !Human.Visible(x, y)) continue;

				ITile tile = Map[x, y];
				City? city = tile.City;
				Terrain type = tile.Type;
				if (type == Terrain.Grassland2) type = Terrain.Grassland1;
				bool altTile = (x + y) % 2 == 1;
				int xx = ((int)type) * TileSize;
				int yy = altTile ? TileSize : 0;

				int dx = originX + (column * tilePixelSize);
				int dy = originY + (row * tilePixelSize);

				this.AddLayer(GetTileBitmap(xx, yy, tilePixelSize), dx, dy);

				if (city is { Size: > 0 })
				{
					this.FillRectangle(dx, dy, tilePixelSize, tilePixelSize, Common.ColourLight[city.CityOwnerPlayerIndex]);
				}
				else if (tile.Units is { Length: > 0 } units)
				{
					this.FillRectangle(dx + markerOffset, dy + markerOffset, markerSize, markerSize, 5)
						.FillRectangle(dx, dy, markerSize, markerSize, Common.ColourLight[units[0].Owner]);
				}
			}

			if (!FitsHorizontally)
			{
				DrawPrimeMeridian(tilePixelSize, originX, originY, rows);
			}

			if (_showStartPositionAreas && CanToggleStartPositionAreas)
			{
				DrawStartPositionAreas();
			}

			DrawHints();
		}

		/// <summary>
		/// Draws a reference line at map column 0 (the prime/null meridian), so the horizontally scrollable
		/// overview always keeps a fixed longitude anchor to orient by while scrolling.
		/// Only drawn when column 0 is currently inside the visible section.
		/// </summary>
		/// <param name="tilePixelSize">Pixel size of a single map tile on screen.</param>
		/// <param name="originX">Left pixel column of the map.</param>
		/// <param name="originY">Top pixel row of the map.</param>
		/// <param name="rows">Number of map rows currently drawn.</param>
		private void DrawPrimeMeridian(int tilePixelSize, int originX, int originY, int rows)
		{
			int column = WrapX(-_offsetX);
			if (column >= ViewColumns)
			{
				return;
			}

			int x = originX + (column * tilePixelSize);
			int top = originY;
			int bottom = originY + (rows * tilePixelSize) - 1;
			this.DrawLine(x, top, x, bottom, PrimeMeridianColour);
		}

		/// <summary>
		/// Returns the tile sprite at the given position of the tile sheet, scaled to the current tile size.
		/// Scaled sprites are cached because the same few sprites are drawn for thousands of tiles; the cache
		/// is rebuilt whenever the tile size changes.
		/// </summary>
		/// <param name="sourceX">Left pixel column of the sprite in the tile sheet.</param>
		/// <param name="sourceY">Top pixel row of the sprite in the tile sheet.</param>
		/// <param name="tilePixelSize">Pixel size the sprite is drawn with.</param>
		/// <returns>The scaled tile sprite, owned by the cache.</returns>
		private Bytemap GetTileBitmap(int sourceX, int sourceY, int tilePixelSize)
		{
			if (_scaledTileSize != tilePixelSize)
			{
				ClearTileCache();
				_scaledTileSize = tilePixelSize;
			}

			int key = (sourceY << 16) | sourceX;
			if (_scaledTiles.TryGetValue(key, out Bytemap? cached))
			{
				return cached;
			}

			using Picture source = Resources.WorldMapTiles[sourceX, sourceY, TileSize, TileSize];
			Bytemap scaled = ScaleTile(source.Bitmap, tilePixelSize);
			_scaledTiles[key] = scaled;
			return scaled;
		}

		/// <summary>
		/// Enlarges a tile sprite by repeating its pixels.
		/// Each source pixel covers the same number of target pixels, so the tile pattern stays even; an
		/// interpolating scaler would distort the 4x4 pattern noticeably at this size.
		/// </summary>
		/// <param name="source">The unscaled tile sprite.</param>
		/// <param name="tilePixelSize">Width and height of the result in pixels.</param>
		/// <returns>A new bitmap of the requested size.</returns>
		private static Bytemap ScaleTile(Bytemap source, int tilePixelSize)
		{
			if (tilePixelSize == TileSize)
			{
				return Bytemap.Copy(source);
			}

			Bytemap output = new(tilePixelSize, tilePixelSize);
			for (int y = 0; y < tilePixelSize; y++)
			{
				ReadOnlySpan<byte> sourceRow = source.Row(y * TileSize / tilePixelSize);
				Span<byte> targetRow = output.Row(y);
				for (int x = 0; x < tilePixelSize; x++)
				{
					targetRow[x] = sourceRow[x * TileSize / tilePixelSize];
				}
			}
			return output;
		}

		private void ClearTileCache()
		{
			foreach (Bytemap tile in _scaledTiles.Values)
			{
				tile.Dispose();
			}
			_scaledTiles.Clear();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ClearTileCache();
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Draws the key hints at the bottom of the screen.
		/// The scroll hint only appears when the map is larger than the visible section, the start-position
		/// hint only when that debug overlay can be toggled.
		/// </summary>
		private void DrawHints()
		{
			bool scrollable = Map.WIDTH > ViewColumns || Map.HEIGHT > ViewRows;
			if (!scrollable && !CanToggleStartPositionAreas)
			{
				return;
			}

			int y = this.Height() - HintHeight;
			if (CanToggleStartPositionAreas)
			{
				DrawHint(Translate("A: start position areas"), y);
				y -= HintHeight;
			}

			if (scrollable)
			{
				DrawHint(Translate("Cursor keys: scroll map"), y);
			}
		}

		/// <summary>
		/// Draws a single hint line directly on top of the map.
		/// The text keeps a transparent background and is backed by a grey shadow one pixel down and right,
		/// so it stays readable above both light and dark terrain.
		/// </summary>
		/// <param name="text">The hint text to draw.</param>
		/// <param name="y">The top pixel row of the hint line.</param>
		private void DrawHint(string text, int y)
		{
			this.DrawText(text, HintFont, HintShadowColour, 3, y + 2)
				.DrawText(text, HintFont, HintTextColour, 2, y + 1);
		}

		/// <summary>
		/// Debug overlay: draws a border around each area the area-based starting-position algorithm computed,
		/// so it's possible to visually verify every civilization actually starts inside its own area.
		/// </summary>
		private void DrawStartPositionAreas()
		{
			AreaBasedStartPositionService.MapArea[] areas = AreaBasedStartPositionService.BuildAreas(
					Game.Competition * AreaBasedStartPositionService.AreaOversampleFactor, Map.WIDTH, Map.HEIGHT);
			int tilePixelSize = TilePixelSize;
			int originX = OriginX;
			int originY = OriginY;
			int mapPixelWidth = Map.WIDTH * tilePixelSize;

			int areaIndex = 0;
			foreach (AreaBasedStartPositionService.MapArea area in areas)
			{
				// Offsets are calculated relative to the map, not to the screen, so that the seam check below
				// is not confused by the centering offset of a map that is smaller than the canvas.
				int mapLeft = WrapX(area.X0 - _offsetX) * tilePixelSize;
				int top = originY + ((area.Y0 - _offsetY) * tilePixelSize);
				int width = (area.X1 - area.X0) * tilePixelSize;
				int height = (area.Y1 - area.Y0) * tilePixelSize;

				// Rectangles are drawn with their real (unclipped) bounds so that partially visible areas
				// keep the edges they actually have instead of getting a fake border at the screen edge.
				DrawDashedRectangle(originX + mapLeft, top, width, height);
				DrawAreaLabel(originX + mapLeft, top, areaIndex);

				if (mapLeft + width > mapPixelWidth)
				{
					// The area continues across the map seam, so its remainder reappears on the left side.
					DrawDashedRectangle(originX + mapLeft - mapPixelWidth, top, width, height);
					DrawAreaLabel(originX + mapLeft - mapPixelWidth, top, areaIndex);
				}

				areaIndex++;
			}
		}

		/// <summary>
		/// Draws the area's index number just inside its top-left corner, so overlapping or similarly
		/// shaped areas can be told apart on the debug overlay.
		/// </summary>
		/// <param name="left">Left pixel column of the area rectangle.</param>
		/// <param name="top">Top pixel row of the area rectangle.</param>
		/// <param name="index">Zero-based index of the area, shown as-is.</param>
		private void DrawAreaLabel(int left, int top, int index)
		{
			string text = index.ToString(CultureInfo.InvariantCulture);
			this.DrawText(text, HintFont, HintShadowColour, left + 2, top + 1)
				.DrawText(text, HintFont, StartPositionAreaColour, left + 1, top);
		}

		/// <summary>
		/// Draws a dashed rectangle border, alternating <see cref="DashLength"/> drawn pixels with
		/// <see cref="DashGap"/> untouched pixels, so the map stays visible through the gaps.
		/// Parts outside the screen are skipped.
		/// </summary>
		/// <param name="left">Left pixel column of the rectangle, may be negative.</param>
		/// <param name="top">Top pixel row of the rectangle, may be negative.</param>
		/// <param name="width">Width of the rectangle in pixels.</param>
		/// <param name="height">Height of the rectangle in pixels.</param>
		private void DrawDashedRectangle(int left, int top, int width, int height)
		{
			if (width <= 0 || height <= 0)
			{
				return;
			}

			int right = left + width - 1;
			int bottom = top + height - 1;

			for (int x = left; x <= right; x++)
			{
				if (!IsDash(x - left)) continue;
				SetPixel(x, top);
				SetPixel(x, bottom);
			}

			for (int y = top; y <= bottom; y++)
			{
				if (!IsDash(y - top)) continue;
				SetPixel(left, y);
				SetPixel(right, y);
			}
		}

		/// <summary>
		/// Determines whether the pixel at the given distance from the line start belongs to a dash or to a gap.
		/// </summary>
		/// <param name="offset">Distance from the start of the line in pixels.</param>
		/// <returns><c>true</c> if the pixel should be drawn.</returns>
		private static bool IsDash(int offset) => (offset % (DashLength + DashGap)) < DashLength;

		/// <summary>
		/// Sets a single border pixel, ignoring positions outside the screen.
		/// </summary>
		/// <param name="x">Pixel column.</param>
		/// <param name="y">Pixel row.</param>
		private void SetPixel(int x, int y)
		{
			if (x < 0 || y < 0 || x >= this.Width() || y >= this.Height())
			{
				return;
			}

			Bitmap[x, y] = StartPositionAreaColour;
		}
	}
}
