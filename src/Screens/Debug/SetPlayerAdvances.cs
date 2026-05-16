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

		private CivSelectMenuDelegate _civSelectDelegate;

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

		public string Value { get; }

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
			MoveLinearSelection(delta);
		}

		private void MoveLinearSelection(int delta)
		{
			if (_advanceGrid == null || _gridRows <= 0 || _gridCols <= 0)
				return;

			EnsureSelectionValid();

			int totalCells = _gridRows * _gridCols;
			int index = _gridCol * _gridRows + _gridRow;

			for (int i = 0; i < totalCells; i++)
			{
				index = (index + delta + totalCells) % totalCells;
				int col = index / _gridRows;
				int row = index % _gridRows;

				if (!IsValidGridCell(row, col))
					continue;

				_gridRow = row;
				_gridCol = col;
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
			_civSelectDelegate.DrawDialog(this, OffsetX, OffsetY);
		}

		private void InitializeGrid()
		{
			IAdvance[] advances = _advanceService.GetAllAdvances();
			const int maxVisibleRows = 28;
			const int rowPadding = 1;
			const int headerHeight = 16;
			const int bottomPadding = 4;
			const int verticalDialogMargin = 4;
			int fontHeight = Resources.GetFontHeight(1);
			int maxAdvanceWidth = advances.Max(a => Resources.GetTextSize(1, $"* {a.Name}").Width);
			int cellHeight = fontHeight + rowPadding;

			// Calculate grid dimensions
			int availableHeight = Math.Max(1, CanvasHeight - headerHeight - bottomPadding - (verticalDialogMargin * 2));
			int maxRowsByHeight = Math.Max(1, Math.Min(maxVisibleRows, availableHeight / cellHeight));
			int minColsForMaxRows = (advances.Length + maxRowsByHeight - 1) / maxRowsByHeight;
			_gridCols = Math.Max(1, minColsForMaxRows);
			_gridRows = (advances.Length + _gridCols - 1) / _gridCols;

			int dialogMaxWidth = Math.Max(64, Width - 8);
			int usableGridWidth = Math.Max(64, dialogMaxWidth - 8);

			int desiredCellWidth = maxAdvanceWidth + 8;
			int cellWidth = Math.Min(desiredCellWidth, Math.Max(8, usableGridWidth / _gridCols));

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
			_gridContentHeight = Math.Min(_gridContentHeight, CanvasHeight - (verticalDialogMargin * 2));
			_gridX = Math.Max(0, (Width - _gridContentWidth) / 2);
			_gridY = Math.Max(0, (Height - _gridContentHeight) / 2);
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
				.DrawText($"Set Advances: {_selectedPlayer.TribeNamePlural} (Help: Alt+H)", 0, 15, _gridX + 8, _gridY + 3);
			
			DrawAdvancesGrid(fontId, cellWidth, cellHeight);
		}

		private void DrawAdvancesGrid(byte fontId, int cellWidth, int cellHeight)
		{
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
					string text = TruncateTextToWidth(fontId, $"{prefix} {advance.Name}", cellWidth - 2);
					this.DrawText(text, fontId, 5, x, y);
				}
			}
		}

		private static string TruncateTextToWidth(byte fontId, string text, int maxWidth)
		{
			if (maxWidth <= 0)
				return string.Empty;

			if (Resources.GetTextSize(fontId, text).Width <= maxWidth)
				return text;

			const string suffix = "...";
			int suffixWidth = Resources.GetTextSize(fontId, suffix).Width;
			if (suffixWidth > maxWidth)
				return string.Empty;

			int length = text.Length;
			while (length > 0)
			{
				length--;
				string candidate = text.Substring(0, length) + suffix;
				if (Resources.GetTextSize(fontId, candidate).Width <= maxWidth)
					return candidate;
			}

			return suffix;
		}

		private CivSelectMenuDelegate CreateCivSelectDelegate()
		{
			var delegate_ = new CivSelectMenuDelegate(Palette, "Set Player Advances...");
			delegate_.PlayerSelected += OnCivSelected;
			delegate_.Cancelled += OnCancel;
			return delegate_;
		}

		private void OnCivSelected(Player player)
		{
			_selectedPlayer = player;
			_advanceGrid = null;
			CloseMenus();
			Refresh();
		}

		private void OnCancel(object sender, EventArgs args)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			Destroy();
		}

		private bool TryOpenCivilopediaForSelectedAdvance()
		{
			EnsureSelectionValid();
			IAdvance advance = _advanceGrid[_gridRow][_gridCol];
			if (advance == null)
				return false;

			Common.AddScreen(new CivOne.Screens.Civilopedia(advance));
			return true;
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
					AddMenu(_civSelectDelegate.Menu);
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

			if (args.Alt && args.Key == Key.Character && (args.KeyChar == 'h' || args.KeyChar == 'H'))
				return TryOpenCivilopediaForSelectedAdvance();

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
					_civSelectDelegate = CreateCivSelectDelegate();
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
			Palette = Common.Screens.LastOrDefault()?.OriginalColours ?? Common.DefaultPalette;

			_civSelectDelegate = CreateCivSelectDelegate();

			DrawPlayerMenuDialog();
		}
	}
}