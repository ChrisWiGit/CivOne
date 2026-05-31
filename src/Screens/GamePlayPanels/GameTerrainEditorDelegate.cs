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
using CivOne.Tiles;

namespace CivOne.Screens.GamePlayPanels
{
	internal partial class GameMap
	{
		/// <summary>
		/// Handles terrain editor input orchestration and delegates map mutations.
		/// </summary>
		private sealed class GameTerrainEditorDelegate(GameMap gameMap)
		{
			private readonly GameMap _gameMap = gameMap;

			public bool TryGetMapTileCoordinates(ScreenEventArgs args, out int tileX, out int tileY)
			{
				int x = Math.Clamp(Math.Max(0, args.X) / _gameMap._tilePixelSize, 0, _gameMap._tilesX - 1);
				int y = Math.Clamp(Math.Max(0, args.Y) / _gameMap._tilePixelSize, 0, _gameMap._tilesY - 1);

				tileX = _gameMap._x + x;
				tileY = _gameMap._y + y;
				while (tileX < 0)
				{
					tileX += Map.WIDTH;
				}
				while (tileX >= Map.WIDTH)
				{
					tileX -= Map.WIDTH;
				}

				ITile tile = Map[tileX, tileY];
				return tile != null;
			}

			public bool KeyDown(KeyboardEventArgs args)
			{
				// Alt key combos are menu shortcuts; let them pass through.
				if (args.Alt)
				{
					return false;
				}

				if (!_gameMap._mapViewEnabled)
				{
					switch (args.Key)
					{
						case Key.Left:
							_gameMap._hoveredTileX--;
							if (_gameMap._hoveredTileX < 0)
							{
								_gameMap._hoveredTileX += Map.WIDTH;
							}
							PanViewportIfHoverOutside(Key.Left);
							_gameMap._update = true;
							return true;
						case Key.Right:
							_gameMap._hoveredTileX++;
							if (_gameMap._hoveredTileX >= Map.WIDTH)
							{
								_gameMap._hoveredTileX -= Map.WIDTH;
							}
							PanViewportIfHoverOutside(Key.Right);
							_gameMap._update = true;
							return true;
						case Key.Up:
							_gameMap._hoveredTileY = Math.Max(0, _gameMap._hoveredTileY - 1);
							PanViewportIfHoverOutside(Key.Up);
							_gameMap._update = true;
							return true;
						case Key.Down:
							_gameMap._hoveredTileY = Math.Min(Map.HEIGHT - 1, _gameMap._hoveredTileY + 1);
							PanViewportIfHoverOutside(Key.Down);
							_gameMap._update = true;
							return true;
					}
				}

				if (args.Key == Key.Enter)
				{
					ApplyEditorAt(_gameMap._hoveredTileX, _gameMap._hoveredTileY, rightButton: false);
					_gameMap._update = true;
					_gameMap._fullRedraw = true;
					return true;
				}

				if (args.Key == Key.Backspace && SupportsSecondaryEditorAction())
				{
					ApplyEditorAt(_gameMap._hoveredTileX, _gameMap._hoveredTileY, rightButton: true);
					_gameMap._update = true;
					_gameMap._fullRedraw = true;
					return true;
				}

				if (args.Key == Key.Space)
				{
					return true;
				}

				int brushDelta = GetBrushDelta(args);
				if (brushDelta != 0)
				{
					CycleBrushSize(brushDelta);
					_gameMap._update = true;
					_gameMap._fullRedraw = true;
					return true;
				}

				switch (char.ToLowerInvariant(args.KeyChar))
				{
					case 't':
						OpenTerrainSelector();
						return true;
					case 'a':
						_gameMap._editorState.CurrentMode = EditorMode.Terrain;
						_gameMap._update = true;
						return true;
					case 'y':
						return false;
					case 'u':
						OpenUnitSelector();
						return true;
					case 'i':
						_gameMap._editorState.CurrentMode = EditorMode.Irrigation;
						_gameMap._update = true;
						return true;
					case 'r':
						_gameMap._editorState.CurrentMode = EditorMode.Road;
						_gameMap._update = true;
						return true;
					case 'm':
						_gameMap._editorState.CurrentMode = EditorMode.Mine;
						_gameMap._update = true;
						return true;
					case 'f':
						_gameMap._editorState.CurrentMode = EditorMode.Fortress;
						_gameMap._update = true;
						return true;
					case 'p':
						_gameMap._editorState.CurrentMode = EditorMode.Pollution;
						_gameMap._update = true;
						return true;
					case 'h':
						_gameMap._editorState.CurrentMode = EditorMode.Hut;
						_gameMap._update = true;
						return true;
					case 'c':
						_gameMap._editorState.CurrentMode = EditorMode.Clear;
						_gameMap._update = true;
						return true;
					case 'l':
						_gameMap._editorState.ShowLandValues = !_gameMap._editorState.ShowLandValues;
						_gameMap._update = true;
						_gameMap._fullRedraw = true;
						return true;
				}

				return false;
			}

			public bool MouseDown(ScreenEventArgs args, int x, int y)
			{
				_gameMap._hoveredTileX = x;
				_gameMap._hoveredTileY = y;

				if ((args.Buttons & MouseButton.Left) > 0)
				{
					ApplyEditorAt(x, y, rightButton: false);
					return true;
				}

				if ((args.Buttons & MouseButton.Right) > 0)
				{
					if (SupportsSecondaryEditorAction())
					{
						ApplyEditorAt(x, y, rightButton: true);
					}
					return true;
				}

				return false;
			}

			public bool MouseDrag(ScreenEventArgs args)
			{
				if (!TryGetMapTileCoordinates(args, out int x, out int y))
				{
					return false;
				}

				_gameMap._hoveredTileX = x;
				_gameMap._hoveredTileY = y;

				if ((args.Buttons & MouseButton.Left) > 0)
				{
					if (_gameMap._editorState.CurrentMode == EditorMode.SpawnUnit)
					{
						_gameMap._update = true;
						return true;
					}

					ApplyEditorAt(x, y, rightButton: false);
					return true;
				}

				if ((args.Buttons & MouseButton.Right) > 0 && SupportsSecondaryEditorAction())
				{
					ApplyEditorAt(x, y, rightButton: true);
					return true;
				}

				return false;
			}

			private static int GetBrushDelta(KeyboardEventArgs args)
			{
				if (args.Key == Key.Character && args.KeyChar == ']')
				{
					return 1;
				}

				if (args.Key == Key.Character && args.KeyChar == '[')
				{
					return -1;
				}

				if (args.Key == Key.Plus)
				{
					return 1;
				}

				if (args.Key == Key.Minus)
				{
					return -1;
				}

				return 0;
			}

			private void CycleBrushSize(int delta)
			{
				int count = _gameMap._terrainEditorDelegate.BrushSizeCount;
				_gameMap._editorState.PencilSizeIndex = (_gameMap._editorState.PencilSizeIndex + delta + count) % count;
			}

			private bool SupportsSecondaryEditorAction()
			{
				if (_gameMap._editorState.ShowLandValues)
				{
					return true;
				}

				return _gameMap._editorState.CurrentMode switch
				{
					EditorMode.FoundCity => true,
					EditorMode.SpawnUnit => true,
					EditorMode.Irrigation => true,
					EditorMode.Road => true,
					EditorMode.Mine => true,
					EditorMode.Fortress => true,
					EditorMode.Pollution => true,
					EditorMode.Hut => true,
					EditorMode.Clear => true,
					_ => false
				};
			}

			private void OpenTerrainSelector()
			{
				Common.AddScreen(new TerrainSelectorScreen(_gameMap._editorState.SelectedTerrain, terrain =>
				{
					_gameMap._editorState.SelectedTerrain = terrain;
					_gameMap._editorState.CurrentMode = EditorMode.Terrain;
				}));
			}

			private void OpenUnitSelector()
			{
				Common.AddScreen(new UnitSelectorScreen(_gameMap._editorState.SelectedUnitType, _gameMap._editorState.CityOwner, unitType =>
				{
					_gameMap._editorState.SelectedUnitType = unitType;
					_gameMap._editorState.CurrentMode = EditorMode.SpawnUnit;
					_gameMap._update = true;
					_gameMap._fullRedraw = true;
				}));
			}

			private void PanViewportIfHoverOutside(Key direction)
			{
				int relativeX = _gameMap._hoveredTileX - _gameMap._x;
				if (relativeX < 0)
				{
					relativeX += Map.WIDTH;
				}

				bool outsideX = relativeX < 0 || relativeX >= _gameMap._tilesX;
				bool outsideY = _gameMap._hoveredTileY < _gameMap._y || _gameMap._hoveredTileY >= (_gameMap._y + _gameMap._tilesY);
				if (!outsideX && !outsideY)
				{
					return;
				}

				switch (direction)
				{
					case Key.Left:
						_gameMap.SetViewOrigin(_gameMap._x - 1, _gameMap._y);
						break;
					case Key.Right:
						_gameMap.SetViewOrigin(_gameMap._x + 1, _gameMap._y);
						break;
					case Key.Up:
						_gameMap.SetViewOrigin(_gameMap._x, _gameMap._y - 1);
						break;
					case Key.Down:
						_gameMap.SetViewOrigin(_gameMap._x, _gameMap._y + 1);
						break;
				}
			}

			private void ApplyEditorAt(int x, int y, bool rightButton)
			{
				if (_gameMap._editorState.ShowLandValues)
				{
					_gameMap._terrainEditorDelegate.AdjustLandValue(x, y, _gameMap._editorState.PencilSizeIndex, rightButton ? -1 : 1);
				}
				else
				{
					switch (_gameMap._editorState.CurrentMode)
					{
						case EditorMode.Terrain:
							_gameMap._terrainEditorDelegate.ApplyBrush(x, y, _gameMap._editorState.PencilSizeIndex, _gameMap._editorState.SelectedTerrain);
							break;
						case EditorMode.FoundCity:
							_gameMap._terrainEditorDelegate.EditCitySingleTile(x, y, _gameMap._editorState.CityOwner, shrink: rightButton);
							break;
						case EditorMode.SpawnUnit:
							if (rightButton)
							{
								_gameMap._terrainEditorDelegate.RemoveUnit(x, y, _gameMap._editorState.CityOwner, _gameMap._editorState.SelectedUnitType);
							}
							else
							{
								_gameMap._terrainEditorDelegate.SpawnUnit(x, y, _gameMap._editorState.CityOwner, _gameMap._editorState.SelectedUnitType);
							}
							break;
						case EditorMode.Irrigation:
								if (rightButton)
								{
									_gameMap._terrainEditorDelegate.RemoveIrrigation(x, y, _gameMap._editorState.PencilSizeIndex);
								}
								else
								{
									_gameMap._terrainEditorDelegate.SetIrrigation(x, y, _gameMap._editorState.PencilSizeIndex);
								}
							break;
						case EditorMode.Road:
								if (rightButton)
								{
									_gameMap._terrainEditorDelegate.RemoveRoad(x, y, _gameMap._editorState.PencilSizeIndex);
								}
								else
								{
									_gameMap._terrainEditorDelegate.AddRoad(x, y, _gameMap._editorState.PencilSizeIndex);
								}
							break;
						case EditorMode.Mine:
								if (rightButton)
								{
									_gameMap._terrainEditorDelegate.RemoveMine(x, y, _gameMap._editorState.PencilSizeIndex);
								}
								else
								{
									_gameMap._terrainEditorDelegate.SetMine(x, y, _gameMap._editorState.PencilSizeIndex);
								}
							break;
						case EditorMode.Fortress:
								_gameMap._terrainEditorDelegate.SetFortress(x, y, _gameMap._editorState.PencilSizeIndex, enabled: !rightButton);
							break;
						case EditorMode.Pollution:
								_gameMap._terrainEditorDelegate.SetPollution(x, y, _gameMap._editorState.PencilSizeIndex, enabled: !rightButton);
							break;
						case EditorMode.Hut:
								_gameMap._terrainEditorDelegate.SetHut(x, y, _gameMap._editorState.PencilSizeIndex, enabled: !rightButton);
							break;
						case EditorMode.Clear:
							_gameMap._terrainEditorDelegate.ClearImprovements(x, y, _gameMap._editorState.PencilSizeIndex);
							break;
					}
				}

				_gameMap._update = true;
				_gameMap._fullRedraw = true;
			}
		}
	}
}
