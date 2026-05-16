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
	///
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

		private readonly string[] _items;
		private readonly Func<int, bool> _isChecked;
		private readonly SelectionMode _mode;
		private readonly byte _fontId;

		private int _gridRows, _gridCols;
		private int _gridCellWidth, _gridCellHeight;
		private int _gridCellStartX, _gridCellStartY;
		private int _gridX, _gridY;
		private int _gridContentWidth, _gridContentHeight;
		private int _gridRow, _gridCol;

		private int _lastCanvasHeight, _lastTargetWidth, _lastTargetHeight;
		private bool _layoutDirty = true;

		/// <summary>Flat index of the currently highlighted item, or -1 if none.</summary>
		public int SelectedIndex
		{
			get
			{
				int idx = _gridCol * _gridRows + _gridRow;
				return idx < _items.Length ? idx : -1;
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
		/// </summary>
		public void Invalidate() => _layoutDirty = true;

		private bool IsValidCell(int row, int col)
		{
			if (row < 0 || row >= _gridRows || col < 0 || col >= _gridCols)
				return false;
			return col * _gridRows + row < _items.Length;
		}

		private int FindFirstValidRowInColumn(int col)
		{
			for (int row = 0; row < _gridRows; row++)
				if (IsValidCell(row, col)) return row;
			return -1;
		}

		private void EnsureSelectionValid()
		{
			if (IsValidCell(_gridRow, _gridCol)) return;
			_gridRow = 0;
			_gridCol = 0;
			if (IsValidCell(_gridRow, _gridCol)) return;
			for (int col = 0; col < _gridCols; col++)
			{
				int row = FindFirstValidRowInColumn(col);
				if (row >= 0) { _gridCol = col; _gridRow = row; return; }
			}
		}

		private void MoveLinear(int delta)
		{
			EnsureSelectionValid();
			int total = _gridRows * _gridCols;
			int index = _gridCol * _gridRows + _gridRow;
			for (int i = 0; i < total; i++)
			{
				index = (index + delta + total) % total;
				int col = index / _gridRows;
				int row = index % _gridRows;
				if (!IsValidCell(row, col)) continue;
				_gridRow = row;
				_gridCol = col;
				return;
			}
		}

		private void MoveHorizontal(int delta)
		{
			for (int i = 0; i < _gridCols; i++)
			{
				_gridCol = (_gridCol + delta + _gridCols) % _gridCols;
				if (IsValidCell(_gridRow, _gridCol)) return;
				int firstRow = FindFirstValidRowInColumn(_gridCol);
				if (firstRow >= 0) { _gridRow = firstRow; return; }
			}
		}

		private void ComputeLayout(int canvasHeight, int targetWidth, int targetHeight)
		{
			if (!_layoutDirty
				&& _lastCanvasHeight == canvasHeight
				&& _lastTargetWidth == targetWidth
				&& _lastTargetHeight == targetHeight)
				return;

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
				if (w > maxLabelWidth) maxLabelWidth = w;
			}

			int availableHeight = Math.Max(1, canvasHeight - HeaderHeight - BottomPadding - (VerticalDialogMargin * 2));
			int maxRowsByHeight = Math.Max(1, availableHeight / cellHeight);
			int minCols = (_items.Length + maxRowsByHeight - 1) / maxRowsByHeight;
			_gridCols = Math.Max(1, minCols);
			_gridRows = (_items.Length + _gridCols - 1) / _gridCols;

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

			Picture gridGfx = new Picture(_gridContentWidth, _gridContentHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();

			target.Clear();
			target.FillRectangle(_gridX - 1, _gridY - 1, _gridContentWidth + 2, _gridContentHeight + 2, 5)
				.AddLayer(gridGfx, _gridX, _gridY)
				.DrawText(title, 0, 15, _gridX + 8, _gridY + 3);

			for (int row = 0; row < _gridRows; row++)
			{
				for (int col = 0; col < _gridCols; col++)
				{
					if (!IsValidCell(row, col)) continue;
					int idx = col * _gridRows + row;
					int x = _gridCellStartX + col * _gridCellWidth;
					int y = _gridCellStartY + row * _gridCellHeight;

					if (row == _gridRow && col == _gridCol)
						target.DrawRectangle(x - 2, y - 1, _gridCellWidth + 2, _gridCellHeight, 11);

					string prefix = _isChecked != null ? (_isChecked(idx) ? "*" : " ") : string.Empty;
					string raw = _isChecked != null ? $"{prefix} {_items[idx]}" : _items[idx];
					string text = TruncateTextToWidth(_fontId, raw, _gridCellWidth - 2);
					target.DrawText(text, _fontId, 5, x, y);
				}
			}
		}

		/// <summary>
		/// Handles keyboard input for the grid.
		/// Returns true if the key was consumed.
		/// Navigation keys move the cursor; Enter/Space/NumPad5 activates; Escape fires Cancelled.
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

				case Key.Enter:
				case Key.Space:
				case Key.NumPad5:
					ActivateSelected();
					return true;

				case Key.Escape:
					Cancelled?.Invoke(this, EventArgs.Empty);
					return true;
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
			if (relX < 0 || relY < 0) return false;

			int hitCol = relX / _gridCellWidth;
			int hitRow = relY / _gridCellHeight;
			if (!IsValidCell(hitRow, hitCol)) return false;

			_gridRow = hitRow;
			_gridCol = hitCol;
			ActivateSelected();
			return true;
		}

		private void ActivateSelected()
		{
			EnsureSelectionValid();
			int idx = SelectedIndex;
			if (idx < 0) return;

			if (_mode == SelectionMode.CheckUncheck)
				ItemChecked?.Invoke(idx);
			else
				ItemSelected?.Invoke(idx);
		}

		private static string TruncateTextToWidth(byte fontId, string text, int maxWidth)
		{
			if (maxWidth <= 0) return string.Empty;
			if (Resources.GetTextSize(fontId, text).Width <= maxWidth) return text;

			const string suffix = "...";
			int suffixWidth = Resources.GetTextSize(fontId, suffix).Width;
			if (suffixWidth > maxWidth) return string.Empty;

			int length = text.Length;
			while (length > 0)
			{
				length--;
				string candidate = text[..length] + suffix;
				if (Resources.GetTextSize(fontId, candidate).Width <= maxWidth) return candidate;
			}
			return suffix;
		}

		/// <param name="items">Flat list of item labels.</param>
		/// <param name="mode">CheckUncheck keeps the dialog open and fires ItemChecked; Select closes via ItemSelected.</param>
		/// <param name="isChecked">Optional: returns whether item at given index is checked (renders '*' prefix). Pass null for no check indicator.</param>
		/// <param name="fontId">Font to use for rendering.</param>
		public GridMenuDelegate(string[] items, SelectionMode mode, Func<int, bool> isChecked = null, byte fontId = 1)
		{
			_items = items;
			_mode = mode;
			_isChecked = isChecked;
			_fontId = fontId;
		}
	}
}
