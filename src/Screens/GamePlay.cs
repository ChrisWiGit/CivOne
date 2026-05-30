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
using CivOne.Screens.Dialogs;
using CivOne.Screens.GamePlayPanels;
using CivOne.Screens.Reports;
using CivOne.Tasks;
using CivOne.Units;
using CivOne.Services.EndGame;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class GamePlay : BaseScreen
	{
		private readonly MenuBar _menuBar;
		private readonly SideBar _sideBar;
		private readonly GameMap _gameMap;

		private bool Busy => (Game.MovingUnit != null || Human != Game.CurrentPlayer || GameTask.Any());
		
		private GameMenu _gameMenu = null;
		private int _menuX, _menuY;
		private int _menuIndex = -1;
		private uint _lastGameTick;
		private bool _update = true;
		private bool _redraw = false;
		private bool _rightSideBar;
		private static bool DebugMenuEnabled => Settings.DebugMenu || RuntimeHandler.Runtime?.Settings.Get<bool>("debug") == true;

		private bool _shift5 = false;

		public override MouseCursor Cursor => Busy ? MouseCursor.None : MouseCursor.Pointer;

		internal int X => _gameMap.X;
		internal int Y => _gameMap.Y;
		internal bool IsMapViewEnabled => _gameMap.MapViewEnabled;

		internal void CenterOnPoint(int x, int y) => _gameMap.CenterOnPoint(x, y);

		internal void RefreshMap() => _gameMap.ForceRefresh();

		internal Palette MainPalette => OriginalColours.Copy();
		
		private void MenuBarGame(object sender, EventArgs args)
		{
			_menuIndex = 0;
			_gameMenu = new GameMenu("MenuBarGame", Palette.Copy());
			_gameMenu.Items.Add(Translate("Tax Rate")).OnSelect((s, a) => GameTask.Enqueue(Show.TaxRate));
			_gameMenu.Items.Add(Translate("Luxuries Rate")).OnSelect((s, a) => GameTask.Enqueue(Show.LuxuryRate));
			_gameMenu.Items.Add(Translate("FindCity")).OnSelect((s, a) => GameTask.Enqueue(Show.Search));
			_gameMenu.Items.Add(Translate("Options")).OnSelect((s, a) => GameTask.Enqueue(Show.Screen<GameOptions>()));
			_gameMenu.Items.Add(Translate("Save Game")).SetEnabled(Game.GameTurn > 0 && Common.AllowSaveGame).OnSelect((s, a) => GameTask.Enqueue(Show.Screen<SaveGame>()));
			_gameMenu.Items.Add(Translate("REVOLUTION!")).OnSelect((s, a) => GameTask.Enqueue(Show.Screen<Revolution>()));
			_gameMenu.Items.Add(null);
			if (DebugMenuEnabled)
			{
				_gameMenu.Items.Add(Translate("Debug Options")).OnSelect((s, a) => GameTask.Enqueue(Show.Screen<DebugOptions>()));
				_gameMenu.Items.Add(null);
			}
			_gameMenu.Items.Add(Translate("Retire")).OnSelect((s, a) => GameTask.Enqueue(Show.Screen<ConfirmRetire>()));
			_gameMenu.Items.Add(Translate("QUIT to DOS")).OnSelect((s, a) => GameTask.Enqueue(Show.Screen<ConfirmQuit>()));
			
			_menuX = 16;
			_menuY = 8;
			
			_update = true;
		}
		
		private void MenuBarOrders(object sender, EventArgs args)
		{
			if (Game.ActiveUnit == null) return;

			_menuIndex = 1;

			_gameMenu = new GameMenu("MenuBarOrders", Palette);
			_gameMenu.Items.AddRange(Game.ActiveUnit.MenuItems);
			
			_menuX = 72;
			_menuY = 8;
			
			_update = true;
		}
		
		private void MenuBarAdvisors(object sender, EventArgs args)
		{
			_menuIndex = 2;
			_gameMenu = new GameMenu("MenuBarAdvisors", Palette);
			_gameMenu.Items.Add(Translate("City Status (F1)")).OnSelect((s, a) => Common.AddScreen(new CityStatus()));
			_gameMenu.Items.Add(Translate("Military Advisor (F2)")).OnSelect((s, a) => { Common.AddScreen(new MilitaryLosses()); Common.AddScreen(new MilitaryStatus()); });
			_gameMenu.Items.Add(Translate("Intelligence Advisor (F3)")).OnSelect((s, a) => Common.AddScreen(new IntelligenceReport()));
			_gameMenu.Items.Add(Translate("Attitude Advisor (F4)")).OnSelect((s, a) => Common.AddScreen(new AttitudeSurvey()));
			_gameMenu.Items.Add(Translate("Trade Advisor (F5)")).OnSelect((s, a) => Common.AddScreen(new TradeReport()));
			_gameMenu.Items.Add(Translate("Science Advisor (F6)")).OnSelect((s, a) => Common.AddScreen(new ScienceReport()));
			
			_menuX = 112;
			_menuY = 8;
			
			_update = true;
		}
		
		private void MenuBarWorld(object sender, EventArgs args)
		{
			_menuIndex = 3;
			_gameMenu = new GameMenu("MenuBarWorld", Palette);
			_gameMenu.Items.Add(Translate("Wonders of the World (F7)")).OnSelect((s, a) => {
				if (Game.BuiltWonders.Length == 0)
					GameTask.Enqueue(Show.Empty);
				else
					Common.AddScreen(new WorldWonders());
			});
			_gameMenu.Items.Add(Translate("Top 5 Cities (F8)")).OnSelect((s, a) => Common.AddScreen(new TopCities()));
			_gameMenu.Items.Add(Translate("Civilization Score (F9)")).OnSelect((s, a) => Common.AddScreen(new CivilizationScore()));
			_gameMenu.Items.Add(Translate("World Map (F10)")).OnSelect((s, a) => Common.AddScreen(new WorldMap()));
			_gameMenu.Items.Add(Translate("Demographics")).OnSelect((s, a) => Common.AddScreen(new Demographics()));
			
			_gameMenu.Items.Add(Translate("SpaceShips")).OnSelect((s, a) => Common.AddScreen(new SpaceShipCivilizationSelectorDialog())).
				SetEnabled(SpaceShipCivilizationSelectorServicesFactory.CreateDefault().SelectorService.GetCivilizations().Any(c => c.IsEnabled));
			
			_menuX = 144;
			_menuY = 8;
			
			_update = true;
		}
		
		private void MenuBarCivilopedia(object sender, EventArgs args)
		{
			_menuIndex = 4;
			_gameMenu = new GameMenu("MenuBarCivilopedia", Palette);
			_gameMenu.Items.Add(Translate("Complete")).OnSelect((s, a) => Common.AddScreen(new Civilopedia(Civilopedia.Complete)));
			_gameMenu.Items.Add(Translate("Civilization Advances")).OnSelect((s, a) => Common.AddScreen(new Civilopedia(Civilopedia.Advances)));
			_gameMenu.Items.Add(Translate("City Improvements")).OnSelect((s, a) => Common.AddScreen(new Civilopedia(Civilopedia.Improvements)));
			_gameMenu.Items.Add(Translate("Military Units")).OnSelect((s, a) => Common.AddScreen(new Civilopedia(Civilopedia.Units)));
			_gameMenu.Items.Add(Translate("Terrain Types")).OnSelect((s, a) => Common.AddScreen(new Civilopedia(Civilopedia.TerrainType)));
			_gameMenu.Items.Add(Translate("Miscellaneous")).OnSelect((s, a) => Common.AddScreen(new Civilopedia(Civilopedia.Misc)));
			
			_menuX = 182;
			_menuY = 8;
			
			_update = true;
		}
		
		private void DrawLayer(IScreen layer, uint gameTick, int x, int y)
		{
			if (layer == null) return;
			if (!layer.Update(gameTick) && !_redraw) return;
			this.AddLayer(layer, x, y);
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (!Game.Started)
			{
				return false;
			}

			if (Common.TopScreen is GamePlay && !GameTask.Any())
			{
				Game.Update();
			}

			if (gameTick == _lastGameTick)
			{
				_gameMap.MustUpdate(gameTick);
				DrawLayer(_gameMap, gameTick, _rightSideBar ? 0 : 80, 8);
				return true;
			}

			if (_gameMap.MustUpdate(gameTick)) _update = true;
			if (_sideBar.Update(gameTick)) _update = true;
			if (gameTick % (GameTask.Fast ? 6 : 3) == 0) this.Cycle(96, 103).Cycle(104, 111);
			if (!_update && !_redraw) return (gameTick % (GameTask.Fast ? 6 : 3) == 0);
			
			DrawLayer(_menuBar, gameTick, 0, 0);
			DrawLayer(_sideBar, gameTick, _rightSideBar ? (Width - 80) : 0, 8);
			DrawLayer(_gameMap, gameTick, _rightSideBar ? 0 : 80, 8);
			DrawLayer(_gameMenu, gameTick, _menuX, _menuY);
			
			_redraw = false;
			_update = false;
			_lastGameTick = gameTick;
			return true;
		}

		private bool CheckShift56(KeyboardEventArgs args)
		{
			if (!_shift5 && args.Modifier == KeyModifier.Shift && args.KeyChar == '5')
			{
				_shift5 = true;
				return true;
			}
			else if (_shift5 && args.Modifier == KeyModifier.Shift && args.KeyChar == '6')
			{
				_shift5 = false;
				Settings.RevealWorldCheat();
				RefreshMap();
				return true;
			}
			else if (_shift5)
			{
				_shift5 = false;
			}
			return false;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (GameTask.Any()) return true;

			if (args.Key == Key.Tab)
			{
				_gameMap.ToggleMapView();
				_update = true;
				return true;
			}

			if (args[KeyModifier.Control | KeyModifier.Alt, Key.F11] && Game.Started)
			{
				RuntimeHandler.ReturnToCredits();
				return true;
			}

			if (args.Key >= Key.F1 && args.Key <= Key.F12 && args.Modifier != KeyModifier.None)
			{
				// Disallows F1-F12 with modifiers other than Shift (e.g. Ctrl+F1) to prevent conflicts with quick save/load hotkeys
				return true;
			}

			if (CheckShift56(args))
				return true;
			
			if (_gameMenu != null)
			{
				if (args.Key == Key.Left || args.Key == Key.Right)
				{
					int delta = (args.Key == Key.Left) ? -1 : 1;
					SelectMainMenu(delta);
					return true;
				}

				if (!_gameMenu.KeyDown(args))
				{
					_gameMenu = null;
					_redraw = true;
				}
				return true;
			}

			if (_menuBar.KeyDown(args) && _gameMenu != null)
			{
				_gameMenu.KeepOpen = true;
				return true;
			}

			switch (args.Key)
			{
				case Key.F1:
					Common.AddScreen(new CityStatus());
					return true;
				case Key.F2:
					Common.AddScreen(new MilitaryLosses());
					Common.AddScreen(new MilitaryStatus());
					return true;
				case Key.F3:
					Common.AddScreen(new IntelligenceReport());
					return true;
				case Key.F4:
					Common.AddScreen(new AttitudeSurvey());
					return true;
				case Key.F5:
					Common.AddScreen(new TradeReport());
					return true;
				case Key.F6:
					Common.AddScreen(new ScienceReport());
					return true;
				case Key.F7:
					if (Game.BuiltWonders.Length == 0)
						GameTask.Enqueue(Show.Empty);
					else
						Common.AddScreen(new WorldWonders());
					return true;
				case Key.F8:
					Common.AddScreen(new TopCities());
					return true;
				case Key.F9:
					Common.AddScreen(new CivilizationScore());
					return true;
				case Key.F10:
					Common.AddScreen(new WorldMap());
					return true;
				case Key.F12:
					if (DebugMenuEnabled)
					{
						GameTask.Enqueue(Show.Screen<DebugOptions>());
					}
					return true;
				case Key.Plus:
					GameTask.Enqueue(Show.TaxRate);
					return true;
				case Key.Minus:
					GameTask.Enqueue(Show.LuxuryRate);
					return true;
				case Key.Slash:
					GameTask.Enqueue(Show.Search);
					return true;
			}

			if (args.KeyChar == 'C' && args.Modifier == KeyModifier.None && _gameMap.CenterOnActiveUnit())
			{
				_update = true;
				return true;
			}

			return _gameMap.KeyDown(args);
		}

		private void SelectMainMenu(int delta)
		{
			if (_gameMenu == null)
			{
				return;
			}

			int nextIndex = _menuIndex;
			for (int attempts = 0; attempts < 5; attempts++)
			{
				GameMenu previousMenu = _gameMenu;
				int previousIndex = _menuIndex;

				nextIndex = (nextIndex + delta + 5) % 5;
				switch (nextIndex)
				{
					case 0:
						MenuBarGame(this, EventArgs.Empty);
						break;
					case 1:
						MenuBarOrders(this, EventArgs.Empty);
						break;
					case 2:
						MenuBarAdvisors(this, EventArgs.Empty);
						break;
					case 3:
						MenuBarWorld(this, EventArgs.Empty);
						break;
					case 4:
						MenuBarCivilopedia(this, EventArgs.Empty);
						break;
				}

				if (_gameMenu != previousMenu)
				{
					_gameMenu.KeepOpen = true;
					_redraw = true;
					return;
				}

				_menuIndex = previousIndex;
			}
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (Cursor == MouseCursor.None) return true;
			if (_gameMenu != null && _gameMenu.KeepOpen)
			{
				MouseArgsOffset(ref args, _menuX, _menuY);
				_update |= _gameMenu.MouseDown(args);
				return _update;
			}

			if (args.Y < 8)
			{
				return _menuBar.MouseDown(args);
			}
			if (_rightSideBar)
			{
				if (args.X > (Width - 80))
				{
					MouseArgsOffset(ref args, (Width - 80), 8);
					return _sideBar.MouseDown(args);
				}
				else
				{
					MouseArgsOffset(ref args, 0, 8);
					return (_update = _gameMap.MouseDown(args));
				}
			}
			else
			{
				if (args.X < 80)
				{
					MouseArgsOffset(ref args, 0, 8);
					return _sideBar.MouseDown(args);
				}
				else
				{
					MouseArgsOffset(ref args, 80, 8);
					return (_update = _gameMap.MouseDown(args));
				}
			}
		}
		
		public override bool MouseUp(ScreenEventArgs args)
		{
			if (Cursor == MouseCursor.None) return true;
			if (_gameMenu == null) return false;
			if (args.Y < 8)
			{
				_menuBar.MouseDown(args);
				if (!_menuBar.MenuDrag)
				{
					_gameMenu.KeepOpen = true;
					return true;
				}
			}
			
			if (_gameMenu.MouseUp(args))
			{
				_gameMenu = null;
				_redraw = true;
			}
			return true;
		}
		
		public override bool MouseDrag(ScreenEventArgs args)
		{
			if (Cursor == MouseCursor.None) return true;
			if (_gameMenu == null) return false;
			
			MouseArgsOffset(ref args, _menuX, _menuY);
			_update |= _gameMenu.MouseDrag(args);
			return _update;
		}

		public override bool MouseWheel(ScreenEventArgs args)
		{
			if (Cursor == MouseCursor.None)
			{
				return true;
			}

			if (_gameMenu != null)
			{
				return false;
			}

			if (args.Y < 8)
			{
				// Mouse wheel events on the menu bar are ignored to prevent conflicts with scrollable submenus.
				return false;
			}

			if (_rightSideBar)
			{
				if (args.X > (Width - 80))
				{
					// Mouse wheel events on the sidebar are ignored to prevent conflicts with scrollable content in the sidebar.
					return false;
				}

				MouseArgsOffset(ref args, 0, 8);
			}
			else
			{
				if (args.X < 80)
				{
					// Mouse wheel events on the sidebar are ignored to prevent conflicts with scrollable content in the sidebar.
					return false;
				}

				MouseArgsOffset(ref args, 80, 8);
			}

			_update |= _gameMap.MouseWheel(args);
			return _update;
		}
		
		private void Resize(object sender, ResizeEventArgs args)
		{
			this.Clear(5);

			_menuBar.Resize();
			_sideBar.Resize(args.Height - 8);
			_gameMap.ResizeMap(args.Width - 80, args.Height - 8);

			_update = true;
			HasUpdate(0);
		}

		private void CenterMapOnActiveHumanPlayerAsset()
		{
			if (_gameMap.MapViewEnabled)
			{
				return;
			}

			foreach (IUnit unit in Game.GetUnits().OrderByDescending(u => u.MovesLeft))
			{
				if (unit.Owner == Game.PlayerNumber(Game.HumanPlayer))
				{
					_gameMap.CenterOnPoint(unit.X, unit.Y);
					return;
				}
			}

			// if there is no active unit center on random human player city
			foreach (City city in Game.GetCities())
			{
				if (city.Owner == Game.PlayerNumber(Game.HumanPlayer))
				{
					_gameMap.CenterOnPoint(city.X, city.Y);
					return;
				}
			}
		}

		private bool TryRestoreLastLoadedMapPosition()
		{
			if (!Game.TryConsumePendingMapPositionRestore(out int x, out int y))
			{
				return false;
			}

			_gameMap.SetViewOrigin(x, y);
			return true;
		}

		public GamePlay()
		{
			OnResize += Resize;
			
			Palette = Resources["SP257"].Palette;
			
			_rightSideBar = Settings.RightSideBar;

			_menuBar = new MenuBar(Palette);
			_sideBar = new SideBar(Palette, Game.GlobalWarmingService);
			_gameMap = new GameMap();
			_gameMap.MapPositionSaved += GameMapMapPositionSaved;

			if (!TryRestoreLastLoadedMapPosition())
			{
				CenterMapOnActiveHumanPlayerAsset();
			}

			if (Width != 320 || Height != 200)
			{
				Resize(null, new ResizeEventArgs(Width, Height));
			}
			else
			{
				this.Clear(5);
			}
			
			_menuBar.GameSelected += MenuBarGame;
			_menuBar.OrdersSelected += MenuBarOrders;
			_menuBar.AdvisorsSelected += MenuBarAdvisors;
			_menuBar.WorldSelected += MenuBarWorld;
			_menuBar.CivilopediaSelected += MenuBarCivilopedia;

			while (Game.CurrentPlayer != Game.HumanPlayer)
			{
				Game.Instance.Update();
				while (GameTask.Update());
			}
		}

		private void GameMapMapPositionSaved(object sender, int slot)
		{
			_sideBar.ShowMapPositionSavedInfo(slot);
		}
	}
}
