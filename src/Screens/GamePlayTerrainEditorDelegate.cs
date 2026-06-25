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
using CivOne.Screens.Dialogs;
using CivOne.Screens.GamePlayPanels;
using CivOne.Services;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	internal partial class GamePlay
	{
		/// <summary>
		/// Handles terrain editor menu and hotkey orchestration in gameplay screen.
		/// </summary>
		private sealed class GamePlayTerrainEditorDelegate(GamePlay gamePlay, ITranslationService translationService)
		{
			private enum TerrainMenuAction
			{
				ToggleEditor,
				SelectTile,
				ModeTerrain,
				ModeFoundCity,
				ModeSpawnUnit,
				ModeIrrigation,
				ModeRoad,
				ModeMine,
				ModeFortress,
				ModePollution,
				ModeHut,
				ModeClear,
				ToggleLandValues,
				BrushIncrease,
				BrushDecrease
			}

			private char FOUND_CITY_HOTKEY = 'y';
			private char SPAWN_UNIT_HOTKEY = 'u';
			
			private readonly GamePlay _gamePlay = gamePlay;
			private readonly ITranslationService _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));

			public void OpenOwnerSelectorOverlay(string menuName, EditorMode targetMode)
			{
				Player[] players = [.. Game.Players];
				_gamePlay._gameMenu = new GameMenu(menuName, _gamePlay.Palette);
				for (int i = 0; i < players.Length; i++)
				{
					Player player = players[i];
					byte ownerId = Game.PlayerNumber(player);
					string label = player.Civilization is Civilizations.Barbarian ? 
						_translationService.Translate("Barbarians") : _translationService.Translate(player.TribeNamePlural);
					char hotkey = (i < 9) ? (char)('1' + i) : (char)('A' + (i - 9));

					_gamePlay._gameMenu.Items
						.Add(label)
						.SetShortcut(hotkey.ToString())
						.OnSelect((s, a) =>
						{
							TerrainEditorState state = _gamePlay._gameMap.EditorState;
							state.CityOwner = ownerId;
							state.CurrentMode = targetMode;
							_gamePlay._gameMap.ForceRefresh();
							_gamePlay._update = true;
						});
				}

				_gamePlay._menuIndex = 5;
				_gamePlay._menuX = Math.Max(0, (_gamePlay.Width - _gamePlay._gameMenu.PixelWidth) / 2);
				_gamePlay._menuY = Math.Max(8, (_gamePlay.Height - _gamePlay._gameMenu.PixelHeight) / 2);
				_gamePlay._gameMenu.KeepOpen = true;
				_gamePlay._redraw = true;
				_gamePlay._update = true;
			}

			public void OpenCityOwnerSelector() => OpenOwnerSelectorOverlay("MenuBarTerrainCityOwner", EditorMode.FoundCity);

			public void OpenUnitOwnerSelector() => OpenOwnerSelectorOverlay("MenuBarTerrainUnitOwner", EditorMode.SpawnUnit);

			public void OpenUnitSelector()
			{
				TerrainEditorState state = _gamePlay._gameMap.EditorState;
				Common.AddScreen(new UnitSelectorScreen(state.SelectedUnitType, state.CityOwner, unitType =>
				{
					state.SelectedUnitType = unitType;
					state.CurrentMode = EditorMode.SpawnUnit;
					_gamePlay._gameMap.ForceRefresh();
					_gamePlay._update = true;
				}));
			}

			public void OnUnitSpawnMenuAction(object sender, MenuItemEventArgs<int> args)
			{
				if (IsShiftKeyPressed)
				{
					OpenUnitOwnerSelector();
					return;
				}

				OpenUnitSelector();
				_gamePlay._update = true;
			}

			public string TerrainName(Terrain terrain)
			{
				return terrain switch
				{
					Terrain.Desert => Translate("Desert"),
					Terrain.Plains => Translate("Plains"),
					Terrain.Grassland1 => Translate("Grassland 1"),
					Terrain.Grassland2 => Translate("Grassland 2"),
					Terrain.Forest => Translate("Forest"),
					Terrain.Hills => Translate("Hills"),
					Terrain.Mountains => Translate("Mountains"),
					Terrain.Tundra => Translate("Tundra"),
					Terrain.Arctic => Translate("Arctic"),
					Terrain.Swamp => Translate("Swamp"),
					Terrain.Jungle => Translate("Jungle"),
					Terrain.Ocean => Translate("Ocean"),
					Terrain.River => Translate("River"),
					_ => Translate("Terrain")
				};
			}

			public void OnTerrainMenuAction(object? _, MenuItemEventArgs<int> args)
			{
				TerrainEditorState state = _gamePlay._gameMap.EditorState;
				int brushCount = _gamePlay._gameMap.TerrainBrushSizeCount;
				switch ((TerrainMenuAction)args.Value)
				{
					case TerrainMenuAction.ToggleEditor:
						_gamePlay._gameMap.SetTerrainEditorEnabled(!state.Enabled);
						break;
					case TerrainMenuAction.SelectTile:
						Common.AddScreen(new TerrainSelectorScreen(state.SelectedTerrain, terrain =>
						{
							state.SelectedTerrain = terrain;
							state.CurrentMode = EditorMode.Terrain;
							_gamePlay._gameMap.ForceRefresh();
							_gamePlay._update = true;
						}));
						break;
					case TerrainMenuAction.ModeTerrain:
						state.CurrentMode = EditorMode.Terrain;
						break;
					case TerrainMenuAction.ModeFoundCity:
						if (IsShiftKeyPressed)
						{
							OpenCityOwnerSelector();
							break;
						}

						state.CurrentMode = EditorMode.FoundCity;
						break;
					case TerrainMenuAction.ModeIrrigation:
						state.CurrentMode = EditorMode.Irrigation;
						break;
					case TerrainMenuAction.ModeRoad:
						state.CurrentMode = EditorMode.Road;
						break;
					case TerrainMenuAction.ModeMine:
						state.CurrentMode = EditorMode.Mine;
						break;
					case TerrainMenuAction.ModeFortress:
						state.CurrentMode = EditorMode.Fortress;
						break;
					case TerrainMenuAction.ModePollution:
						state.CurrentMode = EditorMode.Pollution;
						break;
					case TerrainMenuAction.ModeHut:
						state.CurrentMode = EditorMode.Hut;
						break;
					case TerrainMenuAction.ModeClear:
						state.CurrentMode = EditorMode.Clear;
						break;
					case TerrainMenuAction.ToggleLandValues:
						state.ShowLandValues = !state.ShowLandValues;
						break;
					case TerrainMenuAction.BrushIncrease:
						state.PencilSizeIndex = (state.PencilSizeIndex + 1) % brushCount;
						break;
					case TerrainMenuAction.BrushDecrease:
						state.PencilSizeIndex = (state.PencilSizeIndex + brushCount - 1) % brushCount;
						break;
				}

				_gamePlay._gameMap.ForceRefresh();
				_gamePlay._update = true;
			}

			private string Translate(string key)
			{
				return _translationService.Translate(key);
			}
			
			private string TranslateFormatted(string key, params object[] args)
			{
				return _translationService.TranslateFormatted(key, args);
			}


			public void MenuBarTerrain(object? _, EventArgs __)
			{
				if (!DebugMenuEnabled)
				{
					return;
				}

				TerrainEditorState state = _gamePlay._gameMap.EditorState;
				_gamePlay._menuIndex = 5;
				_gamePlay._gameMenu = new GameMenu("MenuBarTerrain", _gamePlay.Palette);

				string editorToggle = state.Enabled ? _translationService.Translate("Editor Off") : _translationService.Translate("Editor On");
				_gamePlay._gameMenu.Items.Add(editorToggle, (int)TerrainMenuAction.ToggleEditor).OnSelect(OnTerrainMenuAction);
				_gamePlay._gameMenu.Items.Add(
					TranslateFormatted("Select {0}...", TerrainName(state.SelectedTerrain)), (int)TerrainMenuAction.SelectTile).SetShortcut("t").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(null);
				_gamePlay._gameMenu.Items.Add(Translate("Paint Terrain"), (int)TerrainMenuAction.ModeTerrain).SetShortcut("a").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items
					.Add(Translate("Found city"), (int)TerrainMenuAction.ModeFoundCity)
					.SetShortcuts(FOUND_CITY_HOTKEY.ToString(), $"^{FOUND_CITY_HOTKEY}")
					.OnSelect(OnTerrainMenuAction)
					.SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items
					.Add(Translate("Spawn Unit"), (int)TerrainMenuAction.ModeSpawnUnit)
					.SetShortcuts(SPAWN_UNIT_HOTKEY.ToString(), $"^{SPAWN_UNIT_HOTKEY}")
					.OnSelect(OnUnitSpawnMenuAction)
					.SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(Translate("Irrigation"), (int)TerrainMenuAction.ModeIrrigation).SetShortcut("i").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(Translate("Road/Railroad"), (int)TerrainMenuAction.ModeRoad).SetShortcut("r").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(Translate("Mine"), (int)TerrainMenuAction.ModeMine).SetShortcut("m").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(Translate("Fortress"), (int)TerrainMenuAction.ModeFortress).SetShortcut("f").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(Translate("Pollution"), (int)TerrainMenuAction.ModePollution).SetShortcut("p").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(Translate("Hut"), (int)TerrainMenuAction.ModeHut).SetShortcut("h").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(Translate("Clear Improvements"), (int)TerrainMenuAction.ModeClear).SetShortcut("c").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(null);
				_gamePlay._gameMenu.Items.Add(Translate("Toggle Land Values"), (int)TerrainMenuAction.ToggleLandValues).SetShortcut("l").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(Translate("Brush Size +"), (int)TerrainMenuAction.BrushIncrease).SetShortcut("+").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);
				_gamePlay._gameMenu.Items.Add(Translate("Brush Size -"), (int)TerrainMenuAction.BrushDecrease).SetShortcut("-").OnSelect(OnTerrainMenuAction).SetEnabled(state.Enabled);

				const int enabledEditorMenuX = 216;
				const int disabledEditorMenuX = 236;
				_gamePlay._menuX = state.Enabled ? enabledEditorMenuX : disabledEditorMenuX;
				_gamePlay._menuY = 8;
				_gamePlay._update = true;
			}

			public bool HandleTerrainMenuHotkeys(KeyboardEventArgs args)
			{
				if (_gamePlay._menuIndex != 5)
				{
					return false;
				}

				char key = args.Key switch
				{
					Key.Character => char.ToUpperInvariant(args.KeyChar),
					Key.Plus => '+',
					Key.Minus => '-',
					_ => '\0'
				};

				if (key == '\0')
				{
					return false;
				}

				if (_gamePlay._gameMenu == null)
				{
					return false;
				}

				foreach (MenuItem<int> item in _gamePlay._gameMenu.Items)
				{
					if (item?.Enabled != true || string.IsNullOrEmpty(item.Shortcut))
					{
						continue;
					}

					if (char.ToUpperInvariant(item.Shortcut[0]) != char.ToUpperInvariant(key))
					{
						continue;
					}

					item.Select();
					return true;
				}

				return false;
			}

			public bool HandleTerrainEditorYU(KeyboardEventArgs args)
			{
				if (args.Key == Key.Character && char.ToLowerInvariant(args.KeyChar) == FOUND_CITY_HOTKEY)
				{
					if (IsShiftKeyPressed)
					{
						OpenCityOwnerSelector();
					}
					else
					{
						TerrainEditorState state = _gamePlay._gameMap.EditorState;
						state.CurrentMode = EditorMode.FoundCity;
						_gamePlay._gameMap.ForceRefresh();
					}

					return true;
				}

				if (args.Key == Key.Character && char.ToLowerInvariant(args.KeyChar) == SPAWN_UNIT_HOTKEY)
				{
					if (IsShiftKeyPressed)
					{
						OpenUnitOwnerSelector();
					}
					else
					{
						OpenUnitSelector();
					}

					return true;
				}

				return false;
			}

			public bool HandleTerrainEditorKeyDown(KeyboardEventArgs args)
			{
				if (!_gamePlay._gameMap.IsTerrainEditorEnabled)
				{
					return false;
				}

				// Alt opens menubar menus and F12 is reserved for debug options.
				if (args.Alt || args.Key == Key.F12)
				{
					return false;
				}

				if (HandleTerrainEditorYU(args))
				{
					_gamePlay._update = true;
					return true;
				}

				if (_gamePlay._gameMap.KeyDown(args))
				{
					_gamePlay._update = true;
					return true;
				}

				if (args.Key == Key.Character || args.Key == Key.Enter || args.Key == Key.Space
					|| (args.Key >= Key.F1 && args.Key <= Key.F11)
					|| args.Key == Key.Plus || args.Key == Key.Minus || args.Key == Key.Slash)
				{
					return true;
				}

				return false;
			}

			public bool HandleTerrainEditorMouseMove(ScreenEventArgs args)
			{
				if (!_gamePlay._gameMap.IsTerrainEditorEnabled)
				{
					return false;
				}

				_gamePlay._gameMap.MouseMove(args);
				_gamePlay._update = true;
				return true;
			}
		}
	}
}