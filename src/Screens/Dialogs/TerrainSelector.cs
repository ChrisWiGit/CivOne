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
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Tiles;

namespace CivOne.Screens.Dialogs
{
	[Modal, ScreenResizeable]
	internal sealed class TerrainSelectorScreen : BaseScreen
	{
		private sealed class TerrainSelectorItem
		{
			public Terrain Terrain { get; init; }
			public string Label { get; init; } = string.Empty;
			public char Hotkey { get; init; }
		}

		private readonly Action<Terrain> _onSelected;
		private readonly TerrainSelectorItem[] _items;
		private int _selectedIndex;

		private const int CellWidth = 72;
		private const int CellHeight = 42;
		private const int GridColumns = 4;

		private int GridRows => (int)Math.Ceiling((double)_items.Length / GridColumns);
		private static int GridWidth => GridColumns * CellWidth;
		private int GridHeight => GridRows * CellHeight;
		private int GridX => Math.Max(0, (Width - GridWidth) / 2);
		private int GridY => Math.Max(0, (Height - GridHeight) / 2) + 6;

		private static TerrainSelectorItem[] CreateItems(Func<string, string> translate)
		{
			return
			[
				new() { Terrain = Terrain.Desert, Label = translate("Desert"), Hotkey = '1' },
				new() { Terrain = Terrain.Plains, Label = translate("Plains"), Hotkey = '2' },
				new() { Terrain = Terrain.Grassland1, Label = translate("Grassland 1"), Hotkey = '3' },
				new() { Terrain = Terrain.Grassland2, Label = translate("Grassland 2"), Hotkey = '4' },
				new() { Terrain = Terrain.Forest, Label = translate("Forest"), Hotkey = '5' },
				new() { Terrain = Terrain.Hills, Label = translate("Hills"), Hotkey = '6' },
				new() { Terrain = Terrain.Mountains, Label = translate("Mountains"), Hotkey = '7' },
				new() { Terrain = Terrain.Tundra, Label = translate("Tundra"), Hotkey = '8' },
				new() { Terrain = Terrain.Arctic, Label = translate("Arctic"), Hotkey = '9' },
				new() { Terrain = Terrain.Swamp, Label = translate("Swamp"), Hotkey = 'A' },
				new() { Terrain = Terrain.Jungle, Label = translate("Jungle"), Hotkey = 'B' },
				new() { Terrain = Terrain.Ocean, Label = translate("Ocean"), Hotkey = 'C' },
				new() { Terrain = Terrain.River, Label = translate("River"), Hotkey = 'D' }
			];
		}

		private static ITile CreateFallbackTile(Terrain terrain)
		{
			return terrain switch
			{
				Terrain.Desert => new Desert(),
				Terrain.Plains => new Plains(),
				Terrain.Grassland1 => new Grassland(),
				Terrain.Grassland2 => new Grassland(),
				Terrain.Forest => new Forest(),
				Terrain.Hills => new Hills(),
				Terrain.Mountains => new Mountains(),
				Terrain.Tundra => new Tundra(),
				Terrain.Arctic => new Arctic(),
				Terrain.Swamp => new Swamp(),
				Terrain.Jungle => new Jungle(),
				Terrain.Ocean => new Ocean(),
				Terrain.River => new River(),
				_ => new Grassland()
			};
		}

		private static ITile ResolvePreviewTile(Terrain terrain)
		{
			ITile? tile = Map.Instance.AllTiles().FirstOrDefault(t => t?.Type == terrain);
			return tile ?? CreateFallbackTile(terrain);
		}

		private void SelectCurrentAndClose()
		{
			_onSelected?.Invoke(_items[_selectedIndex].Terrain);
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

		private bool TrySelectByHotkey(char keyChar)
		{
			char normalized = char.ToUpperInvariant(keyChar);
			for (int i = 0; i < _items.Length; i++)
			{
				if (_items[i].Hotkey != normalized)
				{
					continue;
				}

				_selectedIndex = i;
				SelectCurrentAndClose();
				return true;
			}

			return false;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			this.Clear(5);
			this.DrawText(Translate("Select terrain..."), 0, 15, GridX, GridY - 10);
			for (int i = 0; i < _items.Length; i++)
			{
				int row = i / GridColumns;
				int col = i % GridColumns;
				int left = GridX + (col * CellWidth);
				int top = GridY + (row * CellHeight);
				byte borderColour = i == _selectedIndex ? (byte)11 : (byte)15;
				this.FillRectangle(left, top, CellWidth - 2, CellHeight - 2, 5);
				this.DrawRectangle(left, top, CellWidth - 2, CellHeight - 2, borderColour);

				ITile tile = ResolvePreviewTile(_items[i].Terrain);
				using IBitmap tileBitmap = tile.ToBitmap(TileSettings.Terrain);
				this.AddLayer(tileBitmap, left + 4, top + 4);
				this.DrawText(_items[i].Hotkey.ToString(), 0, 14, left + 24, top + 6);
				this.DrawText(_items[i].Label, 0, 15, left + 4, top + 22);
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

		public TerrainSelectorScreen(Terrain selectedTerrain, Action<Terrain> onSelected) : base(MouseCursor.Pointer)
		{
			_onSelected = onSelected;
			_items = CreateItems(Translate);
			_selectedIndex = Array.FindIndex(_items, x => x.Terrain == selectedTerrain);
			if (_selectedIndex < 0)
			{
				_selectedIndex = 0;
			}

			Palette = Common.TopScreen?.Palette.Copy() ?? Common.DefaultPalette.Copy();
		}
	}
}
