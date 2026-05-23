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

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class SpawnUnit : BaseScreen
    {
		private readonly IUnit[] _units = [.. Reflect.GetUnits().OrderBy(x => x.Name)];

		private readonly CivSelectMenuDelegate _civSelect;
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

		private void DrawCivMenuDialog()
		{
			_civSelect.DrawDialog(this, OffsetX, OffsetY);
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
			Palette = Common.LastScreen.OriginalColours;

			string[] labels = [.. _units.Select(x => x.Name)];
			_unitSelect = new GridMenuDelegate(labels, GridMenuDelegate.SelectionMode.Select, fontId: 0);
			_unitSelect.ItemSelected += UnitSelected;
			_unitSelect.Cancelled += SpawnUnit_Cancel;
		}

		private void OnCivSelected(Player player)
		{
			_selectedPlayer = player;
			Palette = Common.LastScreen.OriginalColours;
			CloseMenus();
		}

		private CivSelectMenuDelegate CreateCivSelectDelegate()
		{
			CivSelectMenuDelegate delegate_ = new(Palette, "Spawn Unit...");
			delegate_.PlayerSelected += OnCivSelected;
			delegate_.Cancelled += SpawnUnit_Cancel;
			return delegate_;
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
				.DrawText(Translate("Left click:"), 1, 15, xx + 3, 157)
				.DrawText(Translate("One unit"), 1, 15, xx + 8, 164)
				.DrawText(Translate("Right click:"), 1, 15, xx + 3, 171)
				.DrawText(Translate("Multiple units"), 1, 15, xx + 8, 178)
				.DrawText(Translate("Escape key:"), 1, 15, xx + 3, 185)
				.DrawText(Translate("Cancel"), 1, 15, xx + 8, 192);
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
			if (TryHandleUnitGridMouseDown(args)) return true;
			if (_selectedUnit == null) return false;

			bool validTile = ValidTile;
			if (validTile)
			{
				SpawnSelectedUnit();
			}

			if ((args.Buttons & MouseButton.Left) > 0 || !validTile)
			{
				Destroy();
			}
			return true;
		}

		private bool TryHandleUnitGridMouseDown(ScreenEventArgs args)
		{
			if (_selectedPlayer == null || _selectedUnit != null || _unitSelect == null) return false;

			bool handled = _unitSelect.MouseDown(args.X, args.Y);
			if (handled) Refresh();
			return handled;
		}

		private void SpawnSelectedUnit()
		{
			IUnit unit = Game.CreateUnit(_selectedUnit.Type, UnitX, UnitY, Game.PlayerNumber(_selectedPlayer), false);
			ApplySpawnRules(unit);
			unit.Explore();
			_hasUpdate = true;
			Common.GamePlay.RefreshMap();
		}

		private void ApplySpawnRules(IUnit unit)
		{
			if (unit.Class == UnitClass.Land && Map[UnitX, UnitY].Type == Terrain.Ocean)
			{
				unit.Sentry = true;
			}

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
				BaseUnitAir airUnit = unit as BaseUnitAir;
				if (airUnit != null)
				{
					airUnit.FuelLeft = airUnit.TotalFuel;
				}
			}
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
				HandleRefreshNeeded();
				return true;
			}

			if (TryEnsureCivMenuVisible()) return false;
			if (TryDrawUnitSelectionGrid()) return false;
			if (TryDrawUnitPreview()) return false;

			return true;
		}

		private void HandleRefreshNeeded()
		{
			if (_selectedPlayer == null)
			{
				DrawCivMenuDialog();
				if (!_menus.Contains(_civSelect.Menu))
				{
					AddMenu(_civSelect.Menu);
				}
				return;
			}

			if (_selectedUnit == null)
			{
				CloseMenus();
				DrawUnitSelectionGrid();
				return;
			}

			_hasUpdate = true;
		}

		private bool TryEnsureCivMenuVisible()
		{
			if (_selectedPlayer != null || _menus.Contains(_civSelect.Menu)) return false;

			AddMenu(_civSelect.Menu);
			return true;
		}

		private bool TryDrawUnitSelectionGrid()
		{
			if (_selectedPlayer == null || _selectedUnit != null) return false;

			DrawUnitSelectionGrid();
			return true;
		}

		private void DrawUnitSelectionGrid()
		{
			if (_unitSelect == null)
			{
				CreateUnitGrid();
			}
			_unitSelect.Draw(this, "Spawn Unit...", CanvasHeight);
		}

		private bool TryDrawUnitPreview()
		{
			if (_selectedUnit == null || !_hasUpdate) return false;

			int xx = (_unitX * 16) + (Settings.RightSideBar ? 0 : 80);
			int yy = (_unitY * 16) + 8;
			if (xx > Width || yy > Height) return true;

			Bitmap.Clear();
			SidebarHint();
			bool validTile = ValidTile;
			_cursor = validTile ? MouseCursor.Goto : MouseCursor.Pointer;
			if (validTile)
			{
				this.AddLayer(_selectedUnit.ToBitmap(Game.PlayerNumber(_selectedPlayer), false), xx, yy);
			}

			return true;
		}

		public SpawnUnit()
		{
			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;
			
			_civSelect = CreateCivSelectDelegate();

			DrawCivMenuDialog();
		}
	}
}