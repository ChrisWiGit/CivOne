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
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class PaletteViewerScreen : BaseScreen
	{
		private const int DisplayColorCount = 255;
		private const int GridColumns = 16;
		private const int CellSize = 10;
		private const int MenuWidth = 128;

		private static readonly string[] PaletteResourceNames = ["Standard Palette", "DOCKER", "SP257"];

		private readonly List<string> _paletteNames = [.. PaletteResourceNames];
		private Menu? _menu;
		private Palette _selectedPalette;
		private string _selectedPaletteName;
		private bool _viewerMode;
		private int _hoverIndex = -1;
		private bool _hasUpdate = true;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private static int GridRows => (DisplayColorCount + GridColumns - 1) / GridColumns;
		private static int GridWidth => GridColumns * CellSize;
		private static int GridHeight => GridRows * CellSize;

		private int GridStartX => OffsetX + ((320 - GridWidth) / 2);
		private int GridStartY => OffsetY + 14;

		private void MenuCancel(object? _, EventArgs __)
		{
			Destroy();
		}

		private void SelectPalette(object sender, int selectedIndex)
		{
			if (selectedIndex < 0 || selectedIndex >= _paletteNames.Count)
			{
				return;
			}

			string resourceName = _paletteNames[selectedIndex];

			if (resourceName == "Standard Palette")
			{
				_selectedPalette = Common.DefaultPalette.Copy();
			}
			else
			{
				if (!Resources.Exists(resourceName))
				{
					GameTask.Enqueue(Message.Error(Translate("-- DEBUG: Palette Viewer --"), TranslateFormattedArray("Resource '{0}' not found.\nPlease choose another resource.", resourceName)));
					return;
				}
			}

			_selectedPalette = Resources[resourceName].Palette.Copy();
			_selectedPaletteName = resourceName;
			Palette = _selectedPalette;
			_viewerMode = true;
			_hoverIndex = -1;
			CloseMenus();
			_hasUpdate = true;
			Refresh();
		}

		private void CreateSelectionMenu()
		{
			if (_menu != null)
			{
				return;
			}

			const int fontId = 0;
			int itemHeight = Resources.GetFontHeight(fontId) + 1;
			int menuHeight = (_paletteNames.Count * itemHeight) + 2;

			using Picture background = new Picture(MenuWidth, menuHeight);
			background.Tile(Pattern.PanelGrey).DrawRectangle3D();
			_menu = new Menu(Palette, background)
			{
				X = OffsetX + ((320 - MenuWidth) / 2),
				Y = OffsetY + 88,
				MenuWidth = MenuWidth,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = fontId,
				Indent = 6,
				RowHeight = itemHeight,
				CenterTo320Coordinates = false
			};

			_menu.Cancel += MenuCancel;
			_menu.MissClick += MenuCancel;

			for (int i = 0; i < _paletteNames.Count; i++)
			{
				int index = i;
				_menu.Items.Add(_paletteNames[i]).OnSelect((s, a) => SelectPalette(s, index));
			}

			AddMenu(_menu);
		}

		private int GetHoverIndex(ScreenEventArgs args)
		{
			int localX = args.X - GridStartX;
			int localY = args.Y - GridStartY;

			if (localX < 0 || localY < 0 || localX >= GridWidth || localY >= GridHeight)
			{
				return -1;
			}

			int col = localX / CellSize;
			int row = localY / CellSize;
			int index = (row * GridColumns) + col;
			if (index < 0 || index >= DisplayColorCount)
			{
				return -1;
			}

			return index;
		}

		private void DrawSelection()
		{
			int ox = OffsetX;
			int oy = OffsetY;

			this.Clear()
				.FillRectangle(24 + ox, 54 + oy, 273, 73, 11)
				.FillRectangle(25 + ox, 55 + oy, 271, 71, 15)
				.DrawText(Translate("Palette Viewer"), 0, 5, 114 + ox, 60 + oy)
				.DrawText(Translate("Select resource palette:"), 0, 5, 87 + ox, 72 + oy)
				.DrawText(Translate("ESC: close"), 0, 5, 122 + ox, 114 + oy);

			CreateSelectionMenu();
		}

		private void DrawViewer()
		{
			int ox = OffsetX;
			int oy = OffsetY;

			this.Clear()
				.Tile(Pattern.PanelGrey)
				.DrawText(TranslateFormatted("Palette Viewer: {0}", _selectedPaletteName), 0, 15, 8 + ox, 4 + oy)
				.DrawText(Translate("ESC: close, BACKSPACE: resource menu"), 0, 15, 8 + ox, 192 + oy);

			for (int i = 0; i < DisplayColorCount; i++)
			{
				int col = i % GridColumns;
				int row = i / GridColumns;
				int x = GridStartX + (col * CellSize);
				int y = GridStartY + (row * CellSize);

				this.FillRectangle(x, y, CellSize - 1, CellSize - 1, (byte)i);
			}

			if (_hoverIndex >= 0)
			{
				int hoverCol = _hoverIndex % GridColumns;
				int hoverRow = _hoverIndex / GridColumns;
				int hoverX = GridStartX + (hoverCol * CellSize);
				int hoverY = GridStartY + (hoverRow * CellSize);

				this.DrawRectangle(hoverX - 1, hoverY - 1, CellSize + 1, CellSize + 1, 15);

				Colour colour = _selectedPalette[_hoverIndex];
				this.DrawText(TranslateFormatted("Index: {0}   A:{1} R:{2} G:{3} B:{4}", _hoverIndex, colour.A, colour.R, colour.G, colour.B), 0, 15, 8 + ox, 181 + oy);
			}
			else
			{
				this.DrawText(Translate("Hover a color square to inspect index and A/R/G/B values."), 0, 15, 8 + ox, 181 + oy);
			}
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded() && !_hasUpdate)
			{
				return false;
			}

			if (_viewerMode)
			{
				CloseMenus();
				_menu = null;
				DrawViewer();
			}
			else
			{
				Palette = Common.DefaultPalette;
				DrawSelection();
			}

			_hasUpdate = false;
			return true;
		}

		public override bool MouseMove(ScreenEventArgs args)
		{
			if (!_viewerMode)
			{
				return false;
			}

			int hoverIndex = GetHoverIndex(args);
			if (hoverIndex == _hoverIndex)
			{
				return false;
			}

			_hoverIndex = hoverIndex;
			_hasUpdate = true;
			Refresh();
			return true;
		}

		public override bool MouseDrag(ScreenEventArgs args) => MouseMove(args);

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args.Key == Key.Escape)
			{
				Destroy();
				return true;
			}

			if (_viewerMode && args.Key == Key.Backspace)
			{
				_viewerMode = false;
				_hoverIndex = -1;
				Palette = Common.DefaultPalette;
				_hasUpdate = true;
				Refresh();
				return true;
			}

			return false;
		}

		public PaletteViewerScreen() : base(MouseCursor.Pointer)
		{
			Palette = Common.DefaultPalette;
			_selectedPalette = null!;
			_selectedPaletteName = "Standard Palette";
		}
	}
}