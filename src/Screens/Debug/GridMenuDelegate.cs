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
using CivOne.Graphics.Sprites;

namespace CivOne.Screens.Debug
{
	/// <summary>
	/// Reusable input and rendering delegate for a column-major grid dialog.
	/// </summary>
	/// <remarks>
	/// This delegate encapsulates complete grid behavior:
	/// layout calculation, keyboard navigation, mouse hit testing, drawing,
	/// and selection/cancel events. It is designed to be hosted by any screen
	/// that can provide an <see cref="IBitmap"/> target and a canvas height.
	///
	/// Typical usage flow:
	/// 1) Create once with a flat item label list.
	/// 2) Subscribe to <see cref="ItemChecked"/>, <see cref="ItemSelected"/>, and/or <see cref="Cancelled"/>.
	/// 3) Forward input from the host screen to <see cref="KeyDown(KeyboardEventArgs)"/> and <see cref="MouseDown(int, int)"/>.
	/// 4) Call <see cref="Draw(IBitmap, string, int)"/> during render.
	/// 5) If host dimensions or item content assumptions change, call <see cref="Invalidate"/>.
	///
	/// Selection modes:
	/// - <see cref="SelectionMode.CheckUncheck"/>:
	///   activating an item fires <see cref="ItemChecked"/> and keeps the dialog open.
	/// - <see cref="SelectionMode.Select"/>:
	///   activating an item fires <see cref="ItemSelected"/>; host typically closes or switches screen.
	///   You can optionally select an item by default on first draw by passing its global index to the constructor with <c>defaultSelectedIndex</c>.
	/// Notes:
	/// - Item mapping is flat index based and column-major.
	/// - Checked marker rendering is optional and driven by the provided <c>isChecked</c> callback.
	/// - Escape does not destroy screens directly; it only raises <see cref="Cancelled"/> so host decides behavior.
	/// </remarks>
	internal class GridMenuDelegate : BaseInstance
	{
		/// <summary>
		/// Defines how activation behaves when the user confirms the current cell.
		/// </summary>
		public enum SelectionMode
		{
			/// <summary>
			/// Fires <see cref="ItemChecked"/> and keeps dialog active.
			/// </summary>
			CheckUncheck,
			/// <summary>
			/// Fires <see cref="ItemSelected"/>; host usually closes or transitions.
			/// </summary>
			Select
		}

		private const int RowPadding = 1;
		private const int HeaderHeight = 16;
		private const int BottomPadding = 4;
		private const int VerticalDialogMargin = 4;
		private const int MaxGridColumns = 4;

		private readonly string[] _items;
		private readonly Func<int, bool> _isChecked;
		private readonly SelectionMode _mode;
		private readonly byte _fontId;
		private readonly bool _allowCancel;
		private readonly int _defaultSelectedIndex;

		private int _gridRows, _gridCols;
		private int _gridCellWidth, _gridCellHeight;
		private int _gridCellStartX, _gridCellStartY;
		private int _gridX, _gridY;
		private int _gridContentWidth, _gridContentHeight;
		private int _gridRow, _gridCol;

		// Pagination state
		private int _pageOffset = 0;        // First item index of current page
		private int _maxVisibleItems = 1;   // Max items per page (rows * cols)
		private int _pageCount = 1;         // Total number of pages

		private int _lastCanvasHeight, _lastTargetWidth, _lastTargetHeight;
		private bool _layoutDirty = true;
		private bool _defaultSelectionPending;

		/// <summary>Flat index (global) of the currently highlighted item, or -1 if none.</summary>
		public int SelectedIndex
		{
			get
			{
				int globalIdx = _pageOffset + _gridCol * _gridRows + _gridRow;
				return globalIdx < _items.Length ? globalIdx : -1;
			}
		}

		/// <summary>Fired in CheckUncheck mode when the user confirms a cell. Dialog stays open.</summary>
		public event Action<int> ItemChecked;

		/// <summary>Fired in Select mode when the user confirms a cell. Caller should close dialog.</summary>
		public event Action<int> ItemSelected;

		/// <summary>Fired when the user presses Escape.</summary>
		public event EventHandler Cancelled;

		/// <summary>
		/// Marks layout cache dirty so dimensions are recalculated on next <see cref="Draw(IBitmap, string, int)"/>.
		/// Also resets pagination to first page.
		/// </summary>
		public void Invalidate()
		{
			_layoutDirty = true;
			_pageOffset = 0;
			_gridRow = 0;
			_gridCol = 0;
		}

		private bool IsValidCell(int row, int col)
		{
			if (row < 0 || row >= _gridRows || col < 0 || col >= _gridCols)
			{
				return false;
			}
			int globalIdx = _pageOffset + col * _gridRows + row;
			return globalIdx < _items.Length;
		}

		private int FindFirstValidRowInColumn(int col)
		{
			for (int row = 0; row < _gridRows; row++)
			{
				if (IsValidCell(row, col))
				{
					return row;
				}
			}
			return -1;
		}

		private int FindNearestValidRowInColumn(int col, int preferredRow)
		{
			if (col < 0 || col >= _gridCols)
			{
				return -1;
			}
			int clampedRow = Math.Max(0, Math.Min(_gridRows - 1, preferredRow));
			if (IsValidCell(clampedRow, col))
			{
				return clampedRow;
			}

			for (int offset = 1; offset < _gridRows; offset++)
			{
				int up = clampedRow - offset;
				if (up >= 0 && IsValidCell(up, col))
				{
					return up;
				}

				int down = clampedRow + offset;
				if (down < _gridRows && IsValidCell(down, col))
				{
					return down;
				}
			}

			return -1;
		}

		private void NextPage()
		{
			if (_pageCount <= 1)
			{
				return; // Single page, no pagination
			}
			_pageOffset = (_pageOffset + _maxVisibleItems) % _items.Length;
			_gridRow = 0;
			_gridCol = 0;
			EnsureSelectionValid();
		}

		private void PreviousPage(bool goToLast = false)
		{
			if (_pageCount <= 1)
			{
				return; // Single page, no pagination
			}
			_pageOffset = (_pageOffset - _maxVisibleItems + _items.Length) % _items.Length;
			if (goToLast)
			{
				// Find the last valid cell on this page (column-major order)
				_gridRow = 0;
				_gridCol = 0;
				for (int col = _gridCols - 1; col >= 0; col--)
				{
					for (int row = _gridRows - 1; row >= 0; row--)
					{
						if (IsValidCell(row, col))
						{
							_gridRow = row;
							_gridCol = col;
							return;
						}
					}
				}
			}
			else
			{
				_gridRow = 0;
				_gridCol = 0;
				EnsureSelectionValid();
			}
		}

		private int GetCurrentPageNumber()
		{
			return (_pageOffset / _maxVisibleItems) + 1;
		}

		private void GoToFirstPageFirstItem()
		{
			_pageOffset = 0;
			_gridRow = 0;
			_gridCol = 0;
			EnsureSelectionValid();
		}

		private void GoToLastPageFirstItem()
		{
			if (_pageCount <= 1)
			{
				GoToFirstPageFirstItem();
				return;
			}

			_pageOffset = (_pageCount - 1) * _maxVisibleItems;
			_gridRow = 0;
			_gridCol = 0;
			EnsureSelectionValid();
		}

		private void EnsureSelectionValid()
		{
			if (IsValidCell(_gridRow, _gridCol))
			{
				return;
			}
			_gridRow = 0;
			_gridCol = 0;
			if (IsValidCell(_gridRow, _gridCol))
			{
				return;
			}
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

		private bool IsLinearBoundaryPageTransition(int delta, int index, int nextIndex)
		{
			if (_pageCount <= 1)
			{
				return false;
			}

			if (delta > 0)
			{
				return nextIndex <= index;
			}

			if (delta < 0)
			{
				return nextIndex >= index;
			}

			return false;
		}

		private void HandleLinearBoundaryTransition(int delta)
		{
			bool hasPreviousPage = _pageOffset > 0;
			bool hasNextPage = _pageOffset + _maxVisibleItems < _items.Length;

			if (delta > 0)
			{
				if (hasNextPage)
				{
					NextPage();
				}
				return;
			}

			if (delta < 0 && hasPreviousPage)
			{
				PreviousPage(goToLast: true);
			}
		}

		private void TryMoveLinearWithinPage(int delta, int total, int index)
		{
			for (int i = 0; i < total; i++)
			{
				index = (index + delta + total) % total;
				int col = index / _gridRows;
				int row = index % _gridRows;
				if (!IsValidCell(row, col))
				{
					continue;
				}
				_gridRow = row;
				_gridCol = col;
				return;
			}
		}

		private void MoveLinear(int delta)
		{
			EnsureSelectionValid();
			int total = _gridRows * _gridCols;
			int index = _gridCol * _gridRows + _gridRow;
			int nextIndex = (index + delta + total) % total;

			if (IsLinearBoundaryPageTransition(delta, index, nextIndex))
			{
				HandleLinearBoundaryTransition(delta);
				return;
			}

			TryMoveLinearWithinPage(delta, total, index);
		}

		private bool TryPageHorizontally(int delta, int currentRow)
		{
			if (_pageCount <= 1)
			{
				return false;
			}

			if (delta > 0 && _gridCol == _gridCols - 1)
			{
				return TryPageRightKeepRow(currentRow);
			}

			if (delta < 0 && _gridCol == 0)
			{
				return TryPageLeftKeepRow(currentRow);
			}

			return false;
		}

		private bool TryPageRightKeepRow(int preferredRow)
		{
			bool hasNextPage = _pageOffset + _maxVisibleItems < _items.Length;
			if (!hasNextPage)
			{
				return true;
			}

			_pageOffset += _maxVisibleItems;
			_gridCol = 0;
			ApplyPagedColumnSelection(preferredRow);
			return true;
		}

		private bool TryPageLeftKeepRow(int preferredRow)
		{
			bool hasPreviousPage = _pageOffset > 0;
			if (!hasPreviousPage)
			{
				return true;
			}

			_pageOffset -= _maxVisibleItems;
			_gridCol = _gridCols - 1;
			ApplyPagedColumnSelection(preferredRow);
			return true;
		}

		private void ApplyPagedColumnSelection(int preferredRow)
		{
			int row = FindNearestValidRowInColumn(_gridCol, preferredRow);
			if (row >= 0)
			{
				_gridRow = row;
				return;
			}

			EnsureSelectionValid();
		}

		private void TryMoveHorizontalWithinPage(int delta)
		{
			for (int i = 0; i < _gridCols; i++)
			{
				_gridCol = (_gridCol + delta + _gridCols) % _gridCols;
				if (IsValidCell(_gridRow, _gridCol))
				{
					return;
				}

				int firstRow = FindFirstValidRowInColumn(_gridCol);
				if (firstRow >= 0)
				{
					_gridRow = firstRow;
					return;
				}
			}
		}

		private void MoveHorizontal(int delta)
		{
			int currentRow = _gridRow;

			if (TryPageHorizontally(delta, currentRow))
			{
				return;
			}

			TryMoveHorizontalWithinPage(delta);
		}

		private void ComputeLayout(int canvasHeight, int targetWidth, int targetHeight)
		{
			if (!_layoutDirty
				&& _lastCanvasHeight == canvasHeight
				&& _lastTargetWidth == targetWidth
				&& _lastTargetHeight == targetHeight)
			{
				return;
			}

			_lastCanvasHeight = canvasHeight;
			_lastTargetWidth = targetWidth;
			_lastTargetHeight = targetHeight;
			_layoutDirty = false;

			int fontHeight = Resources.GetFontHeight(_fontId);
			int cellHeight = fontHeight + RowPadding;

			int maxLabelWidth = 0;
			for (int i = 0; i < _items.Length; i++)
			{
				string label = _isChecked != null ? $"* {_items[i]}" : _items[i];
				int w = Resources.GetTextSize(_fontId, label).Width;
				if (w > maxLabelWidth)
				{
					maxLabelWidth = w;
				}
			}

			int availableHeight = Math.Max(1, canvasHeight - HeaderHeight - BottomPadding - (VerticalDialogMargin * 2));
			int maxRowsByHeight = Math.Max(1, availableHeight / cellHeight);

			// Rows are fixed by canvas height; columns are capped at MaxGridColumns.
			// Pagination activates when all items don't fit in MaxGridColumns * maxRows.
			_gridRows = maxRowsByHeight;
			int idealCols = (_items.Length + _gridRows - 1) / _gridRows;
			_gridCols = Math.Max(1, Math.Min(MaxGridColumns, idealCols));

			// Calculate pagination: max items per page = fixed rows × capped columns
			_maxVisibleItems = Math.Max(1, _gridRows * _gridCols);
			_pageCount = (_items.Length + _maxVisibleItems - 1) / _maxVisibleItems;

			// Ensure page offset is valid
			if (_pageOffset >= _items.Length)
			{
				_pageOffset = 0;
			}

			if (_defaultSelectionPending)
			{
				int pageIndex = _defaultSelectedIndex / _maxVisibleItems;
				_pageOffset = pageIndex * _maxVisibleItems;
				int localIndex = _defaultSelectedIndex - _pageOffset;
				_gridCol = localIndex / _gridRows;
				_gridRow = localIndex % _gridRows;
				_defaultSelectionPending = false;
			}

			int usableGridWidth = Math.Max(64, targetWidth - 16);
			int desiredCellWidth = maxLabelWidth + 8;
			int cellWidth = Math.Min(desiredCellWidth, Math.Max(8, usableGridWidth / _gridCols));

			_gridContentWidth = cellWidth * _gridCols + 8;
			_gridContentHeight = HeaderHeight + (cellHeight * _gridRows) + BottomPadding;
			_gridContentHeight = Math.Min(_gridContentHeight, canvasHeight - (VerticalDialogMargin * 2));

			_gridX = Math.Max(0, (targetWidth - _gridContentWidth) / 2);
			_gridY = Math.Max(0, (targetHeight - _gridContentHeight) / 2);
			_gridCellWidth = cellWidth;
			_gridCellHeight = cellHeight;
			_gridCellStartX = _gridX + 4;
			_gridCellStartY = _gridY + HeaderHeight;

			EnsureSelectionValid();
		}

		/// <summary>
		/// Draws the dialog frame and all visible grid cells to the target.
		/// </summary>
		/// <param name="target">Drawing target, typically current screen.</param>
		/// <param name="title">Dialog title shown in header row.</param>
		/// <param name="canvasHeight">
		/// Effective runtime canvas height used to determine how many rows fit without clipping.
		/// </param>
		public void Draw(IBitmap target, string title, int canvasHeight)
		{
			ComputeLayout(canvasHeight, target.Bitmap.Width, target.Bitmap.Height);

			int minTitleContentWidth = Resources.GetTextSize(0, title).Width + 16;
			if (_gridContentWidth < minTitleContentWidth)
			{
				_gridContentWidth = minTitleContentWidth;
				_gridX = Math.Max(0, (target.Bitmap.Width - _gridContentWidth) / 2);
				_gridCellStartX = _gridX + 4;
			}

			Picture gridGfx = new Picture(_gridContentWidth, _gridContentHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();

			target.Clear();
			target.FillRectangle(_gridX - 1, _gridY - 1, _gridContentWidth + 2, _gridContentHeight + 2, 5)
				.AddLayer(gridGfx, _gridX, _gridY);

			// Draw title and optional page indicator
			string headerText = title;
			if (_pageCount > 1)
			{
				int currentPage = GetCurrentPageNumber();
				headerText = $"{title} [{currentPage}/{_pageCount}]";
			}
			target.DrawText(headerText, 0, 15, _gridX + 8, _gridY + 3);

			DrawGrid(target);
		}

		private void DrawGrid(IBitmap target)
		{
			for (int row = 0; row < _gridRows; row++)
			{
				for (int col = 0; col < _gridCols; col++)
				{
					DrawGridCell(target, row, col);
				}
			}
		}

		private void DrawGridCell(IBitmap target, int row, int col)
		{
			if (!IsValidCell(row, col))
			{
				return;
			}

			int globalIdx = _pageOffset + col * _gridRows + row;
			int x = _gridCellStartX + col * _gridCellWidth;
			int y = _gridCellStartY + row * _gridCellHeight;

			DrawSelectionRectangle(target, row, col, x, y);
			string text = GetGridCellText(globalIdx);
			target.DrawText(text, _fontId, 5, x, y);
		}

		private string GetGridCellText(int globalIdx)
		{
			string raw = _items[globalIdx];

			if (_isChecked != null)
			{
				string checkLabel = _isChecked(globalIdx) ? "*" : " ";
				raw = $"{checkLabel} {raw}";
			}

			return TruncateTextToWidth(_fontId, raw, _gridCellWidth - 2);
		}

		private void DrawSelectionRectangle(IBitmap target, int row, int col, int x, int y)
		{
			if (row == _gridRow && col == _gridCol)
			{
				target.DrawRectangle(x - 2, y - 1, _gridCellWidth + 2, _gridCellHeight, 11);
			}
		}

		/// <summary>
		/// Handles keyboard input for the grid.
		/// Returns true if the key was consumed.
		/// Navigation keys move the cursor; Enter/Space/NumPad5 activates; Escape fires Cancelled.
		/// Page Up/Down navigate pages; arrow keys navigate cells with auto-paging.
		/// </summary>
		public bool KeyDown(KeyboardEventArgs args)
		{
			switch (args.Key)
			{
				case Key.Up:
				case Key.NumPad8:
					MoveLinear(-1);
					return true;

				case Key.Down:
				case Key.NumPad2:
					MoveLinear(1);
					return true;

				case Key.Left:
				case Key.NumPad4:
					MoveHorizontal(-1);
					return true;

				case Key.Right:
				case Key.NumPad6:
					MoveHorizontal(1);
					return true;

				case Key.PageUp:
					PreviousPage();
					return true;

				case Key.PageDown:
					NextPage();
					return true;

				case Key.Home:
					GoToFirstPageFirstItem();
					return true;

				case Key.End:
					GoToLastPageFirstItem();
					return true;

				case Key.Enter:
				case Key.Space:
				case Key.NumPad5:
					ActivateSelected();
					return true;

				case Key.Escape:
					if (_allowCancel)
					{
						Cancelled?.Invoke(this, EventArgs.Empty);
					}
					return _allowCancel;
			}
			return false;
		}

		/// <summary>
		/// Handles a mouse click at the given coordinates.
		/// Returns true if a valid cell was hit.
		/// </summary>
		public bool MouseDown(int x, int y)
		{
			int relX = x - _gridCellStartX;
			int relY = y - _gridCellStartY;
			if (relX < 0 || relY < 0)
			{
				return false;
			}

			int hitCol = relX / _gridCellWidth;
			int hitRow = relY / _gridCellHeight;
			if (!IsValidCell(hitRow, hitCol))
			{
				return false;
			}

			_gridRow = hitRow;
			_gridCol = hitCol;
			ActivateSelected();
			return true;
		}

		private void ActivateSelected()
		{
			EnsureSelectionValid();
			int idx = SelectedIndex;
			if (idx < 0)
			{
				return;
			}

			if (_mode == SelectionMode.CheckUncheck)
			{
				ItemChecked?.Invoke(idx);
			}
			else
			{
				ItemSelected?.Invoke(idx);
			}
		}

		private static string TruncateTextToWidth(byte fontId, string text, int maxWidth)
		{
			if (maxWidth <= 0)
			{
				return string.Empty;
			}
			if (Resources.GetTextSize(fontId, text).Width <= maxWidth)
			{
				return text;
			}

			const string suffix = "...";
			int suffixWidth = Resources.GetTextSize(fontId, suffix).Width;
			if (suffixWidth > maxWidth)
			{
				return string.Empty;
			}

			int length = text.Length;
			while (length > 0)
			{
				length--;
				string candidate = text[..length] + suffix;
				if (Resources.GetTextSize(fontId, candidate).Width <= maxWidth)
				{
					return candidate;
				}
			}
			return suffix;
		}

		/// <param name="items">Flat list of item labels.</param>
		/// <param name="mode">CheckUncheck keeps the dialog open and fires ItemChecked; Select closes via ItemSelected.</param>
		/// <param name="isChecked">Optional: returns whether item at given index is checked (renders '*' prefix). Pass null for no check indicator.</param>
		/// <param name="fontId">Font to use for rendering.</param>
		/// <param name="allowCancel">If true, pressing Escape fires the <see cref="Cancelled"/> event. Default is true.</param>
		/// <param name="defaultSelectedIndex">Optional initial selected item index for Select mode. Ignored for CheckUncheck mode.</param>
		public GridMenuDelegate(string[] items, SelectionMode mode, Func<int, bool> isChecked = null,
				byte fontId = 1, bool allowCancel = true, int defaultSelectedIndex = -1)
		{
			_items = items;
			_mode = mode;
			_isChecked = isChecked;
			_fontId = fontId;
			_allowCancel = allowCancel;
			_defaultSelectedIndex = defaultSelectedIndex;
			_defaultSelectionPending = _mode == SelectionMode.Select
				&& _defaultSelectedIndex >= 0
				&& _defaultSelectedIndex < _items.Length;
		}
	}
}
