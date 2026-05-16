// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Services;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class SetPlayerAdvances : BaseScreen
	{
		private readonly IAdvanceManagementService _advanceService;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);
		private readonly int _playerMenuWidth;
		private readonly int _playerMenuHeight;

		private readonly Menu _civSelect;

		// Grid state
		private IAdvance[][] _advanceGrid;
		private int _gridRows;
		private int _gridCols;
		private int _gridRow = 0;
		private int _gridCol = 0;
		private int _gridContentWidth;
		private int _gridContentHeight;
		private int _gridX;
		private int _gridY;
		private int _gridCellWidth;
		private int _gridCellHeight;
		private int _gridCellStartX;
		private int _gridCellStartY;

		private Player _selectedPlayer = null;

		public string Value { get; private set; }

		public event EventHandler Cancel;

		private bool IsValidGridCell(int row, int col)
		{
			if (_advanceGrid == null)
				return false;
			if (row < 0 || row >= _gridRows || col < 0 || col >= _gridCols)
				return false;
			return _advanceGrid[row][col] != null;
		}

		private int FindFirstValidRowInColumn(int col)
		{
			for (int row = 0; row < _gridRows; row++)
			{
				if (IsValidGridCell(row, col))
					return row;
			}
			return -1;
		}

		private void EnsureSelectionValid()
		{
			if (IsValidGridCell(_gridRow, _gridCol))
				return;

			_gridRow = 0;
			_gridCol = 0;
			if (IsValidGridCell(_gridRow, _gridCol))
				return;

			for (int col = 0; col < _gridCols; col++)
			{
				int row = FindFirstValidRowInColumn(col);
				if (row >= 0)
				{
					_gridCol = col;
					_gridRow = row;
					return;
				}
			}
		}

		private void MoveVerticalSelection(int delta)
		{
			for (int i = 0; i < _gridRows; i++)
			{
				_gridRow = (_gridRow + delta + _gridRows) % _gridRows;
				if (IsValidGridCell(_gridRow, _gridCol))
					return;
			}
		}

		private void MoveHorizontalSelection(int delta)
		{
			for (int i = 0; i < _gridCols; i++)
			{
				_gridCol = (_gridCol + delta + _gridCols) % _gridCols;
				if (IsValidGridCell(_gridRow, _gridCol))
					return;

				int firstRow = FindFirstValidRowInColumn(_gridCol);
				if (firstRow >= 0)
				{
					_gridRow = firstRow;
					return;
				}
			}
		}

		private bool TryGetGridCellFromPoint(int x, int y, out int row, out int col)
		{
			row = -1;
			col = -1;

			if (_selectedPlayer == null || _advanceGrid == null)
				return false;

			int relativeX = x - _gridCellStartX;
			int relativeY = y - _gridCellStartY;
			if (relativeX < 0 || relativeY < 0)
				return false;

			int hitCol = relativeX / _gridCellWidth;
			int hitRow = relativeY / _gridCellHeight;
			if (!IsValidGridCell(hitRow, hitCol))
				return false;

			col = hitCol;
			row = hitRow;
			return true;
		}

		private void DrawPlayerMenuDialog()
		{
			int xx = OffsetX + ((320 - _playerMenuWidth) / 2);
			int yy = OffsetY + ((200 - _playerMenuHeight) / 2);

			Picture menuGfx = new Picture(_playerMenuWidth, _playerMenuHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();

			this.Clear();
			this.FillRectangle(xx - 1, yy - 1, _playerMenuWidth + 2, _playerMenuHeight + 2, 5)
				.AddLayer(menuGfx, xx, yy, dispose: true)
				.DrawText("Set Player Advances...", 0, 15, xx + 8, yy + 3);

			_civSelect.X = xx + 2;
			_civSelect.Y = yy + 11;
		}

		private void InitializeGrid()
		{
			IAdvance[] advances = _advanceService.GetAllAdvances();
			const int rowPadding = 1;
			const int headerHeight = 16;
			const int bottomPadding = 4;
			int fontHeight = Resources.GetFontHeight(1);
			int maxAdvanceWidth = advances.Max(a => Resources.GetTextSize(1, $"* {a.Name}").Width);

			// Calculate grid dimensions
			int contentWidth = Math.Max(200, Width - OffsetX * 2 - 20);
			int contentHeight = Math.Max(150, Height - OffsetY * 2 - 50);
			
			int cellWidth = maxAdvanceWidth + 8;
			_gridCols = Math.Max(3, Math.Min(10, contentWidth / cellWidth));
			int cellHeight = fontHeight + rowPadding;
			_gridRows = (advances.Length + _gridCols - 1) / _gridCols;

			// Build 2D grid (column-major: top-to-bottom, left-to-right)
			_advanceGrid = new IAdvance[_gridRows][];
			for (int row = 0; row < _gridRows; row++)
			{
				_advanceGrid[row] = new IAdvance[_gridCols];
				for (int col = 0; col < _gridCols; col++)
				{
					int index = col * _gridRows + row;
					if (index < advances.Length)
						_advanceGrid[row][col] = advances[index];
				}
			}

			_gridContentWidth = cellWidth * _gridCols + 8;
			_gridContentHeight = headerHeight + (cellHeight * _gridRows) + bottomPadding;
			_gridX = OffsetX + ((320 - _gridContentWidth) / 2);
			_gridY = OffsetY + ((200 - _gridContentHeight) / 2);
			_gridCellWidth = cellWidth;
			_gridCellHeight = cellHeight;
			_gridCellStartX = _gridX + 4;
			_gridCellStartY = _gridY + headerHeight;

			_gridRow = 0;
			_gridCol = 0;
			EnsureSelectionValid();
		}

		private void RenderAdvancesGrid()
		{
			if (_advanceGrid == null)
				InitializeGrid();

			if (_advanceGrid == null || _selectedPlayer == null)
				return;

			const byte fontId = 1;
			int cellWidth = _gridCellWidth;
			int cellHeight = _gridCellHeight;

			Picture gridGfx = new Picture(_gridContentWidth, _gridContentHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();

			this.Clear();
			this.FillRectangle(_gridX - 1, _gridY - 1, _gridContentWidth + 2, _gridContentHeight + 2, 5)
				.AddLayer(gridGfx, _gridX, _gridY)
				.DrawText($"Set Advances: {_selectedPlayer.TribeNamePlural}", 0, 15, _gridX + 8, _gridY + 3);

			// Render grid cells
			int cellStartX = _gridCellStartX;
			int cellStartY = _gridCellStartY;

			for (int row = 0; row < _gridRows; row++)
			{
				for (int col = 0; col < _gridCols; col++)
				{
					IAdvance advance = _advanceGrid[row][col];
					if (advance == null) continue;

					int x = cellStartX + col * cellWidth;
					int y = cellStartY + row * cellHeight;

					// Highlight current selection
					if (row == _gridRow && col == _gridCol)
						this.DrawRectangle(x - 2, y - 1, cellWidth + 2, cellHeight, 11);

					string prefix = _selectedPlayer.HasAdvance(advance) ? "*" : " ";
					string text = $"{prefix} {advance.Name}";
					this.DrawText(text, fontId, 5, x, y);
				}
			}
		}

		private void CivSelect_Accept(object sender, EventArgs args)
		{
			_selectedPlayer = Game.GetPlayer((byte)_civSelect.ActiveItem);
			_advanceGrid = null;
			CloseMenus();
			Refresh();
		}

		private void OnCancel(object sender, EventArgs args)
		{
			Cancel?.Invoke(this, null);
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			// Draw the appropriate dialog based on state
			if (_selectedPlayer == null)
			{
				// State 1: Civ selection
				if (RefreshNeeded())
					DrawPlayerMenuDialog();

				if (Common.TopScreen.GetType() != typeof(Menu))
				{
					AddMenu(_civSelect);
					return false;
				}
			}
			else
			{
				// State 2: Grid display
				if (RefreshNeeded())
					RenderAdvancesGrid();
			}

			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_selectedPlayer == null)
				return false; // Menu handles input

			if (_advanceGrid == null)
				return false;

			switch (args.Key)
			{
				case Key.Up:
				case Key.NumPad8:
					MoveVerticalSelection(-1);
					Refresh();
					return true;

				case Key.Down:
				case Key.NumPad2:
					MoveVerticalSelection(1);
					Refresh();
					return true;

				case Key.Left:
				case Key.NumPad4:
					MoveHorizontalSelection(-1);
					Refresh();
					return true;

				case Key.Right:
				case Key.NumPad6:
					MoveHorizontalSelection(1);
					Refresh();
					return true;

				case Key.Enter:
				case Key.Space:
				case Key.NumPad5:
					EnsureSelectionValid();
					IAdvance advance = _advanceGrid[_gridRow][_gridCol];
					if (advance != null)
					{
						_advanceService.ToggleAdvance(Game.PlayerNumber(_selectedPlayer), advance);
						Refresh();
					}
					return true;

				case Key.Escape:
					_selectedPlayer = null;
					Refresh();
					return true;
			}

			return false;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_selectedPlayer == null)
				return false;

			if (!TryGetGridCellFromPoint(args.X, args.Y, out int row, out int col))
				return false;

			_gridRow = row;
			_gridCol = col;
			IAdvance advance = _advanceGrid[_gridRow][_gridCol];
			if (advance == null)
				return false;

			_advanceService.ToggleAdvance(Game.PlayerNumber(_selectedPlayer), advance);
			Refresh();
			return true;
		}

		public SetPlayerAdvances() : base(MouseCursor.Pointer)
		{
			_advanceService = new AdvanceManagementService();
			Palette sourcePalette = Common.Screens.LastOrDefault()?.OriginalColours ?? Common.DefaultPalette;
			Palette = sourcePalette;
			int fontHeight = Resources.GetFontHeight(0);
			_playerMenuHeight = (fontHeight * (Game.Players.Count() + 2)) + 5;
			_playerMenuWidth = 136;

			Picture menuGfx = new Picture(_playerMenuWidth, _playerMenuHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();
			IBitmap menuBackground = menuGfx[2, 11, _playerMenuWidth - 4, _playerMenuHeight - 11].ColourReplace((7, 11), (22, 3));

			_civSelect = new Menu(Palette, menuBackground)
			{
				X = 0,
				Y = 0,
				MenuWidth = _playerMenuWidth - 4,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = 0,
				Indent = 8
			};

			foreach (Player player in Game.Players)
			{
				_civSelect.Items.Add(player.TribeNamePlural).OnSelect(CivSelect_Accept);
			}

			_civSelect.Cancel += OnCancel;
			_civSelect.MissClick += OnCancel;
			_civSelect.ActiveItem = Game.PlayerNumber(Human);

			DrawPlayerMenuDialog();
		}
	}
}