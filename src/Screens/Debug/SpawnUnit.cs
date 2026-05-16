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
using CivOne.Graphics.Sprites;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class SpawnUnit : BaseScreen
    {
		private const int MENU_WIDTH = 136;

		private readonly IUnit[] _units = [.. Reflect.GetUnits().OrderBy(x => x.Name)];

		private readonly Menu _civSelect;
		private GridMenuDelegate _unitSelect;

		private Player _selectedPlayer = null;

		private IUnit _selectedUnit = null;

		public string Value { get; }

		private MouseCursor _cursor = MouseCursor.Pointer;
		public override MouseCursor Cursor => _cursor;

		public event EventHandler Cancel;

		private bool _hasUpdate = false;

		private int _unitX, _unitY;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);
		private readonly int _civMenuHeight;

		private void DrawCivMenuDialog()
		{
			int xx = OffsetX + ((320 - MENU_WIDTH) / 2);
			int yy = OffsetY + ((200 - _civMenuHeight) / 2);

			Picture menuGfx = new Picture(MENU_WIDTH, _civMenuHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();

			this.Clear();
			this.FillRectangle(xx - 1, yy - 1, MENU_WIDTH + 2, _civMenuHeight + 2, 5)
				.AddLayer(menuGfx, xx, yy, dispose: true)
				.DrawText("Spawn Unit...", 0, 15, xx + 8, yy + 3);

			_civSelect.X = xx + 2;
			_civSelect.Y = yy + 11;
			_civSelect.ForceUpdate();
		}

		private int UnitX
		{
			get
			{
				int output = Common.GamePlay.X + _unitX;
				while (output < 0) output += Map.WIDTH;
				while (output >= Map.WIDTH) output -= Map.WIDTH;
				return output;
			}
		}

		private int UnitY
		{
			get
			{
				return Common.GamePlay.Y + _unitY;
			}
		}

		private void CreateUnitGrid()
		{
			Palette = Common.Screens[Common.Screens.Count() - 1].OriginalColours;

			string[] labels = [.. _units.Select(x => x.Name)];
			_unitSelect = new GridMenuDelegate(labels, GridMenuDelegate.SelectionMode.Select, fontId: 0);
			_unitSelect.ItemSelected += UnitSelected;
			_unitSelect.Cancelled += SpawnUnit_Cancel;
		}

		private void CivSelect_Accept(object sender, EventArgs args)
		{
			_selectedPlayer = Game.GetPlayer((byte)_civSelect.ActiveItem);
			Palette = Common.Screens[Common.Screens.Count() - 1].OriginalColours;
			CloseMenus();
		}

		private void UnitSelected(int index)
		{
			if (index < 0 || index >= _units.Length) return;
			_selectedUnit = _units[index];
			_unitSelect = null;
			CloseMenus();
			Refresh();
		}

		private void SpawnUnit_Cancel(object sender, EventArgs args)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
            Destroy();
		}

		private bool ValidTile
		{
			get
			{
				if (_unitX < 0 || _unitY < 0) return false;
				ITile tile = Map[UnitX, UnitY];
				if (tile.Units.Any(x => _selectedPlayer != x.Owner)) return false;
				if (_selectedUnit.Class == UnitClass.Land && tile.City != null)
				{
					return (_selectedPlayer == tile.City.Owner);
				}
				if (_selectedUnit.Class == UnitClass.Land && tile.Type == Terrain.Ocean)
				{
					if (!tile.Units.Any(x => x.Class == UnitClass.Water && x is IBoardable)) return false;
					
					int capacity = tile.Units.Where(x => x.Class == UnitClass.Water && x is IBoardable).Sum(x => (x as IBoardable).Cargo);
					int unitCount = tile.Units.Count(x => x.Class == UnitClass.Land);
					return (unitCount < capacity);
				}
				if (_selectedUnit.Class == UnitClass.Water && tile.Type != Terrain.Ocean)
				{
					return (tile.City != null && _selectedPlayer == tile.City.Owner);
				}
				return true;
			}
		}

		private void SidebarHint()
		{
			int xx = (Settings.RightSideBar ? Width - 80 : 0);
			this.FillRectangle(xx, 153, 79, 1, 15)
				.FillRectangle(xx, 154, 80, 46, 9)
				.FillRectangle(xx + 1, 155, 78, 44, 1)
				.DrawText("Left click:", 1, 15, xx + 3, 157)
				.DrawText("One unit", 1, 15, xx + 8, 164)
				.DrawText("Right click:", 1, 15, xx + 3, 171)
				.DrawText("Multiple units", 1, 15, xx + 8, 178)
				.DrawText("Escape key:", 1, 15, xx + 3, 185)
				.DrawText("Cancel", 1, 15, xx + 8, 192);
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_selectedPlayer != null && _selectedUnit == null && _unitSelect != null)
			{
				bool handled = _unitSelect.KeyDown(args);
				if (handled) Refresh();
				return handled;
			}

			switch (args.Key)
			{
				case Key.Escape:
					Destroy();
					return true;
			}
			return false;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_selectedPlayer != null && _selectedUnit == null && _unitSelect != null)
			{
				bool handled = _unitSelect.MouseDown(args.X, args.Y);
				if (handled) Refresh();
				return handled;
			}

			if (_selectedUnit == null) return false;

			if (ValidTile)
			{
				IUnit unit = Game.CreateUnit(_selectedUnit.Type, UnitX, UnitY, Game.PlayerNumber(_selectedPlayer), false);
				if (unit.Class == UnitClass.Land && Map[UnitX, UnitY].Type == Terrain.Ocean) unit.Sentry = true;

				if (Game.PlayerNumber(_selectedPlayer) < Game.PlayerNumber(Game.CurrentPlayer))
				{
					unit.MovesLeft = 0;
				}

				if (unit.Class == UnitClass.Land && Map[UnitX, UnitY].Hut)
				{
					Map[UnitX, UnitY].Hut = false;
				}

				if (unit.Class == UnitClass.Air)
				{
					(unit as BaseUnitAir).FuelLeft = (unit as BaseUnitAir).TotalFuel;
				}
				
				unit.Explore();
				_hasUpdate = true;
				Common.GamePlay.RefreshMap();
			}
			if ((args.Buttons & MouseButton.Left) > 0 || !ValidTile)
			{
				Destroy();
			}
			return true;
		}

		public override bool MouseMove(ScreenEventArgs args)
		{
			if (_selectedUnit == null) return false;

			if (args.Y < 8 || (Settings.RightSideBar && args.X > (Width - 80)) || (!Settings.RightSideBar && args.X < 80))
			{
				_unitX = -1;
				_unitY = -1;
				_hasUpdate = true;
				return true;
			}

			_unitX = (int)Math.Floor(((double)args.X - (Settings.RightSideBar ? 0 : 80)) / 16);
			_unitY = (int)Math.Floor(((double)args.Y - 8) / 16);
			_hasUpdate = true;
			return true;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				if (_selectedPlayer == null)
				{
					DrawCivMenuDialog();
					if (!_menus.Contains(_civSelect))
					{
						AddMenu(_civSelect);
					}
				}
				else if (_selectedUnit == null)
				{
					CloseMenus();
					if (_unitSelect == null)
					{
						CreateUnitGrid();
					}
					_unitSelect.Draw(this, "Spawn Unit...", CanvasHeight);
				}
				else
				{
					_hasUpdate = true;
				}
				return true;
			}

			if (_selectedPlayer == null && Common.TopScreen.GetType() != typeof(Menu))
			{
				AddMenu(_civSelect);
				return false;
			}
			else if (_selectedPlayer != null && _selectedUnit == null)
			{
				if (_unitSelect == null)
				{
					CreateUnitGrid();
				}
				_unitSelect.Draw(this, "Spawn Unit...", CanvasHeight);
				return false;
			}
			else if (_selectedUnit != null && _hasUpdate)
			{
				int xx = (_unitX * 16) + (Settings.RightSideBar ? 0 : 80);
				int yy = (_unitY * 16) + 8;

				if (xx > Width || yy > Height) return false;

				Bitmap.Clear();
				SidebarHint();
				_cursor = ValidTile ? MouseCursor.Goto : MouseCursor.Pointer;
				if (!ValidTile) return _hasUpdate;
				this.AddLayer(_selectedUnit.ToBitmap(Game.PlayerNumber(_selectedPlayer), false), xx, yy);
				
				return false;
			}
			return true;
		}

		public SpawnUnit()
		{
			Palette = Common.DefaultPalette;

			int fontHeight = Resources.GetFontHeight(0);
			_civMenuHeight = (fontHeight * (Game.Players.Count() + 1)) + 5;

			Picture menuGfx = new Picture(MENU_WIDTH, _civMenuHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();
			IBitmap menuBackground = menuGfx[2, 11, MENU_WIDTH - 4, _civMenuHeight - 11].ColourReplace((7, 11), (22, 3));

			_civSelect = new Menu(Palette, menuBackground)
			{
				X = 0,
				Y = 0,
				MenuWidth = MENU_WIDTH - 4,
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

			_civSelect.Cancel += SpawnUnit_Cancel;
			_civSelect.MissClick += SpawnUnit_Cancel;
			_civSelect.ActiveItem = Game.PlayerNumber(Human);

			DrawCivMenuDialog();
		}
	}
}