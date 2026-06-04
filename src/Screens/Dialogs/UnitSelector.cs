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
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Units;

namespace CivOne.Screens.Dialogs
{
	[Modal, ScreenResizeable]
	internal sealed class UnitSelectorScreen : BaseScreen
	{
		private sealed class UnitSelectorItem
		{
			public UnitType UnitType { get; init; }
			public string Label { get; init; }
			public char Hotkey { get; init; }
		}

		private readonly Action<UnitType> _onSelected;
		private readonly UnitSelectorItem[] _items;
		private readonly byte _previewOwner;
		private int _selectedIndex;

		private const int CellWidth = 52;
		private const int CellHeight = 32;
		private const int GridColumns = 6;

		private int GridRows => (int)Math.Ceiling((double)_items.Length / GridColumns);
		private int GridWidth => GridColumns * CellWidth;
		private int GridHeight => GridRows * CellHeight;
		private int GridX => Math.Max(0, (Width - GridWidth) / 2);
		private int GridY => Math.Max(0, (Height - GridHeight) / 2) + 6;

		private static char ResolveHotkey(int index)
		{
			if (index < 9)
			{
				return (char)('1' + index);
			}

			int alphaIndex = index - 9;
			if (alphaIndex < 26)
			{
				return (char)('A' + alphaIndex);
			}

			return '\0';
		}

		private static UnitSelectorItem[] CreateItems()
		{
			IUnit[] units = [.. Reflect.GetUnits().OrderBy(x => x.TranslatedName)];
			UnitSelectorItem[] items = new UnitSelectorItem[units.Length];
			for (int i = 0; i < units.Length; i++)
			{
				items[i] = new UnitSelectorItem
				{
					UnitType = units[i].Type,
					Label = units[i].TranslatedName,
					Hotkey = ResolveHotkey(i)
				};
			}

			return items;
		}

		private bool TrySelectByHotkey(char keyChar)
		{
			char normalized = char.ToUpperInvariant(keyChar);
			for (int i = 0; i < _items.Length; i++)
			{
				if (_items[i].Hotkey == '\0' || _items[i].Hotkey != normalized)
				{
					continue;
				}

				_selectedIndex = i;
				SelectCurrentAndClose();
				return true;
			}

			return false;
		}

		private void SelectCurrentAndClose()
		{
			_onSelected?.Invoke(_items[_selectedIndex].UnitType);
			Destroy();
		}

		private int GetIndexFromPoint(int x, int y)
		{
			if (x < GridX || y < GridY)
			{
				return -1;
			}

			int col = (x - GridX) / CellWidth;
			int row = (y - GridY) / CellHeight;
			if (col < 0 || col >= GridColumns || row < 0 || row >= GridRows)
			{
				return -1;
			}

			int index = row * GridColumns + col;
			if (index < 0 || index >= _items.Length)
			{
				return -1;
			}

			return index;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			this.Clear(5);
			this.DrawText(Translate("Select unit..."), 0, 15, GridX, GridY - 10);
			for (int i = 0; i < _items.Length; i++)
			{
				int row = i / GridColumns;
				int col = i % GridColumns;
				int left = GridX + (col * CellWidth);
				int top = GridY + (row * CellHeight);
				byte borderColour = i == _selectedIndex ? (byte)11 : (byte)15;

				this.FillRectangle(left, top, CellWidth - 2, CellHeight - 2, 5);
				this.DrawRectangle(left, top, CellWidth - 2, CellHeight - 2, borderColour);

				// Do not wrap this in using/dispose: ToBitmap can return/shared underlying buffers.
				// Disposing here can invalidate later map rendering and trigger ObjectDisposedException.
				var preview = Game.CreateUnit(_items[i].UnitType).ToBitmap(_previewOwner, false);
				this.AddLayer(preview, left + 2, top + 2);

				if (_items[i].Hotkey != '\0')
				{
					this.DrawText(_items[i].Hotkey.ToString(), 0, 14, left + 17, top + 2);
				}

				this.DrawText(_items[i].Label, 1, 15, left + 2, top + 20);
			}

			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args[Key.Escape])
			{
				Destroy();
				return true;
			}

			if (args.Key == Key.Character && TrySelectByHotkey(args.KeyChar))
			{
				return true;
			}

			switch (args.Key)
			{
				case Key.Left:
					_selectedIndex = (_selectedIndex <= 0) ? _items.Length - 1 : _selectedIndex - 1;
					Refresh();
					return true;
				case Key.Right:
					_selectedIndex = (_selectedIndex + 1) % _items.Length;
					Refresh();
					return true;
				case Key.Up:
					_selectedIndex -= GridColumns;
					while (_selectedIndex < 0)
					{
						_selectedIndex += _items.Length;
					}
					Refresh();
					return true;
				case Key.Down:
					_selectedIndex = (_selectedIndex + GridColumns) % _items.Length;
					Refresh();
					return true;
				case Key.Enter:
				case Key.Space:
				case Key.NumPad5:
					SelectCurrentAndClose();
					return true;
			}

			return false;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			int index = GetIndexFromPoint(args.X, args.Y);
			if (index < 0)
			{
				return false;
			}

			_selectedIndex = index;
			SelectCurrentAndClose();
			return true;
		}

		public UnitSelectorScreen(UnitType selectedUnitType, byte previewOwner, Action<UnitType> onSelected) : base(MouseCursor.Pointer)
		{
			_onSelected = onSelected;
			_previewOwner = previewOwner;
			_items = CreateItems();
			_selectedIndex = Array.FindIndex(_items, x => x.UnitType == selectedUnitType);
			if (_selectedIndex < 0)
			{
				_selectedIndex = 0;
			}

			Palette = Common.TopScreen?.Palette.Copy() ?? Common.DefaultPalette.Copy();
		}
	}
}
