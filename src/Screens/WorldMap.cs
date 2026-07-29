// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Services.StartPositions;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Screens
{
	/// <summary>
	/// Overview map screen.
	/// Each map tile is drawn as a 4x4 pixel block, so only <c>canvas width / 4</c> by <c>canvas height / 4</c>
	/// tiles fit on screen at once.
	/// Maps larger than that can be scrolled with the cursor keys, Page Up/Down and the mouse wheel.
	/// </summary>
	[ScreenResizeable]
	internal class WorldMap : BaseScreen
	{
		/// <summary>Pixel size of a single map tile on the overview map.</summary>
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

		/// <summary>Number of pixels drawn per dash of an area border.</summary>
		private const int DashLength = 2;

		/// <summary>Number of pixels left untouched between two dashes of an area border.</summary>
		private const int DashGap = 2;

		private bool _update = true;
		private bool _showStartPositionAreas;

		/// <summary>Map column shown at the left edge of the screen.</summary>
		private int _offsetX;

		/// <summary>Map row shown at the top edge of the screen.</summary>
		private int _offsetY;

		private static bool CanToggleStartPositionAreas => Settings.DebugMenu && Settings.StartPositionAlgorithm == Settings.StartPositionAlgorithmType.AreaBased;

		private int ViewColumns => Math.Max(1, this.Width() / TileSize);
		private int ViewRows => Math.Max(1, this.Height() / TileSize);

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
			int offsetX = WrapX(_offsetX + stepX);
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
			_offsetX = Settings.RevealWorld ? 0 : WrapX(Human.StartX - (ViewColumns / 2));

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

			int columns = Math.Min(ViewColumns, Map.WIDTH);
			int rows = Math.Min(ViewRows, Map.HEIGHT - _offsetY);

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

				int dx = column * TileSize;
				int dy = row * TileSize;

				this.AddLayer(Resources.WorldMapTiles[xx, yy, TileSize, TileSize], dx, dy);

				if (city is { Size: > 0 })
				{
					this.FillRectangle(dx, dy, 4, 4, Common.ColourLight[city.CityOwnerPlayerIndex]);
				}
				else if (tile.Units is { Length: > 0 } units)
				{
					this.FillRectangle(dx + 1, dy + 1, 3, 3, 5)
						.FillRectangle(dx, dy, 3, 3, Common.ColourLight[units[0].Owner]);
				}
			}

			if (_showStartPositionAreas && CanToggleStartPositionAreas)
			{
				DrawStartPositionAreas();
			}

			DrawHints();
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
			int mapPixelWidth = Map.WIDTH * TileSize;

			foreach (AreaBasedStartPositionService.MapArea area in areas)
			{
				int left = WrapX(area.X0 - _offsetX) * TileSize;
				int top = (area.Y0 - _offsetY) * TileSize;
				int width = (area.X1 - area.X0) * TileSize;
				int height = (area.Y1 - area.Y0) * TileSize;

				// Rectangles are drawn with their real (unclipped) bounds so that partially visible areas
				// keep the edges they actually have instead of getting a fake border at the screen edge.
				DrawDashedRectangle(left, top, width, height);

				if (left + width > mapPixelWidth)
				{
					// The area continues across the map seam, so its remainder reappears on the left side.
					DrawDashedRectangle(left - mapPixelWidth, top, width, height);
				}
			}
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
