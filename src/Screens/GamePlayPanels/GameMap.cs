// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Screens.GamePlayPanels
{
	internal partial class GameMap : BaseScreen
	{
		private const int BaseTilePixelSize = 16;

		private IUnit ActiveUnit => Game.Started ? Game.ActiveUnit : null;
		
		private Point _helperDirection = new Point(0, 0);
		private bool _update = true;
		private bool _fullRedraw = false;
		private bool _mapViewEnabled = false;
		private int _x, _y;
		private IUnit _lastUnit;
		private ushort _lastTurn;
		private int _tilePixelSize = BaseTilePixelSize;
		private int _zoomBasisPoints = MapZoomSettings.DefaultBasisPoints;

		private int _tilesX = 15, _tilesY = 12;
		public event EventHandler<int> MapPositionSaved;
		private readonly TerrainEditorState _editorState = new();
		private readonly TerrainEditorDelegate _terrainEditorDelegate = new();
		private IUnit _editorStoredUnit;
		private int _hoveredTileX = 0, _hoveredTileY = 0;

		protected GameMapPositionDelegate _mapPositionDelegate;
		private GamePanMapDelegate _panMapDelegate;
		private GameMapZoomDelegate _zoomDelegate;
		private GameTerrainEditorDelegate _terrainEditorInputDelegate;
		private GameTerrainEditorRenderDelegate _terrainEditorRenderDelegate;
		private GameTerrainEditorSessionDelegate _terrainEditorSessionDelegate;

		internal int X => _x;
		internal int Y => _y;
		internal bool MapViewEnabled => _mapViewEnabled;
		internal int TilePixelSize => _tilePixelSize;
		internal int ZoomBasisPoints => _zoomBasisPoints;
		internal int VisibleTilesX => _tilesX;
		internal int VisibleTilesY => _tilesY;
		internal int HoveredTileX => _hoveredTileX;
		internal int HoveredTileY => _hoveredTileY;
		internal TerrainEditorState EditorState => _editorState;
		internal bool IsTerrainEditorEnabled => TerrainEditorEnabled;
		internal int TerrainBrushSize => _terrainEditorDelegate.GetBrushSize(_editorState.PencilSizeIndex);
		internal int TerrainBrushSizeCount => _terrainEditorDelegate.BrushSizeCount;
		internal string? TerrainCityOwnerText
		{
			get
			{
				Player player = Game.GetPlayer(_editorState.CityOwner);
				if (player == null)
				{
					return null;
				}

				string ownerText = player.Civilization is CivOne.Civilizations.Barbarian ? Translate("Barbarians") : player.TribeNamePlural;
				return ownerText;
			}
		}
		internal string TerrainModeText => _editorState.ShowLandValues
			? Translate("Land values")
			: _editorState.CurrentMode switch
			{
				EditorMode.Terrain => Translate("Terrain"),
				EditorMode.FoundCity => Translate("Found city"),
				EditorMode.SpawnUnit => Translate("Spawn unit"),
				EditorMode.Irrigation => Translate("Irrigation"),
				EditorMode.Road => Translate("Road/Railroad"),
				EditorMode.Mine => Translate("Mine"),
				EditorMode.Fortress => Translate("Fortress"),
				EditorMode.Pollution => Translate("Pollution"),
				EditorMode.Hut => Translate("Hut"),
				EditorMode.Clear => Translate("Clear"),
				_ => Translate("None")
			};

		private static bool DebugTerrainEditorEnabled => Settings.DebugMenu || RuntimeHandler.Runtime?.Settings.Get<bool>("debug") == true;
		private bool TerrainEditorEnabled => DebugTerrainEditorEnabled && _editorState.Enabled;

		private ITile[,] Tiles => Map[_x, _y, _tilesX, _tilesY];

		private int CurrentZoomBasisPoints => Game.Started && Game.CurrentPlayer != null
			? MapZoomSettings.NormalizeBasisPoints(Game.CurrentPlayer.MapZoomBasisPoints)
			: MapZoomSettings.DefaultBasisPoints;

		private static Bytemap ScaleBitmap(Bytemap source, int targetWidth, int targetHeight)
		{
			Bytemap output = new(targetWidth, targetHeight);
			if (source == null || targetWidth <= 0 || targetHeight <= 0)
			{
				return output;
			}

			for (int y = 0; y < targetHeight; y++)
			{
				int sourceY = (y * source.Height) / targetHeight;
				for (int x = 0; x < targetWidth; x++)
				{
					int sourceX = (x * source.Width) / targetWidth;
					output[x, y] = source[sourceX, sourceY];
				}
			}

			return output;
		}

		private void DrawScaledBitmap(IBitmap source, int left, int top, int width, int height)
		{
			using Bytemap scaled = ScaleBitmap(source.Bitmap, width, height);
			this.AddLayer(scaled, left, top);
		}


		private int GetX(ITile tile)
		{
			ITile[,] tiles = Tiles;
			for (int xx = 0; xx < Tiles.GetLength(0); xx++)
			{
				if (tiles[xx, 0].X == tile.X) return xx;
			}
			return -1;
		}

		private int GetY(ITile tile)
		{
			ITile[,] tiles = Tiles;
			for (int yy = 0; yy < Tiles.GetLength(1); yy++)
			{
				if (tiles[0, yy].Y == tile.Y) return yy;
			}
			return -1;
		}

		/// <summary>
		/// Checks whether a world tile coordinate is currently visible inside the map viewport.
		/// </summary>
		/// <param name="x">The world X coordinate of the tile.</param>
		/// <param name="y">The world Y coordinate of the tile.</param>
		/// <returns>
		/// <c>true</c> when the tile is inside the viewport bounds and visible to the human player,
		/// or when reveal-world mode is enabled; otherwise <c>false</c>.
		/// </returns>
		/// <remarks>
		/// This replaces the previous viewport scan (<c>TileList.Any(...)</c>) used in the update path.
		/// The old version iterated over the visible tile collection each tick and created avoidable
		/// overhead in a hot path.
		/// This implementation performs direct boundary checks with X-wrap handling and runs in O(1),
		/// reducing CPU work and avoiding temporary enumeration costs during unit blink/move updates.
		/// </remarks>
		private bool IsTileVisibleInViewport(int x, int y)
		{
			if (y < _y || y >= _y + _tilesY)
			{
				return false;
			}

			int deltaX = x - _x;
			if (deltaX < 0)
			{
				deltaX += Map.WIDTH;
			}

			if (deltaX >= _tilesX)
			{
				return false;
			}

			return Settings.RevealWorld || Human.Visible(x, y);
		}

		private void DrawHelperArrows(int x, int y)
		{
			if (_helperDirection.X == 0 && _helperDirection.Y == 0) return;
			
			if (_helperDirection.X < 0)
			{
				DrawScaledBitmap(Icons.HelperArrow(Direction.North), x - _tilePixelSize, y - _tilePixelSize, _tilePixelSize, _tilePixelSize);
				DrawScaledBitmap(Icons.HelperArrow(Direction.West), x - _tilePixelSize, y, _tilePixelSize, _tilePixelSize);
				DrawScaledBitmap(Icons.HelperArrow(Direction.South), x - _tilePixelSize, y + _tilePixelSize, _tilePixelSize, _tilePixelSize);
			}
			if (_helperDirection.X > 0)
			{
				DrawScaledBitmap(Icons.HelperArrow(Direction.North), x + _tilePixelSize, y - _tilePixelSize, _tilePixelSize, _tilePixelSize);
				DrawScaledBitmap(Icons.HelperArrow(Direction.East), x + _tilePixelSize, y, _tilePixelSize, _tilePixelSize);
				DrawScaledBitmap(Icons.HelperArrow(Direction.South), x + _tilePixelSize, y + _tilePixelSize, _tilePixelSize, _tilePixelSize);
			}
			if (_helperDirection.Y < 0)
			{
				DrawScaledBitmap(Icons.HelperArrow(Direction.West), x - _tilePixelSize, y - _tilePixelSize, _tilePixelSize, _tilePixelSize);
				DrawScaledBitmap(Icons.HelperArrow(Direction.North), x, y - _tilePixelSize, _tilePixelSize, _tilePixelSize);
				DrawScaledBitmap(Icons.HelperArrow(Direction.East), x + _tilePixelSize, y - _tilePixelSize, _tilePixelSize, _tilePixelSize);
			}
			if (_helperDirection.Y > 0)
			{
				DrawScaledBitmap(Icons.HelperArrow(Direction.West), x - _tilePixelSize, y + _tilePixelSize, _tilePixelSize, _tilePixelSize);
				DrawScaledBitmap(Icons.HelperArrow(Direction.South), x, y + _tilePixelSize, _tilePixelSize, _tilePixelSize);
				DrawScaledBitmap(Icons.HelperArrow(Direction.East), x + _tilePixelSize, y + _tilePixelSize, _tilePixelSize, _tilePixelSize);
			}
		}


		internal void SetTerrainEditorEnabled(bool enabled)
			=> _terrainEditorSessionDelegate.SetEnabled(enabled);
		
		public bool MustUpdate(uint gameTick)
		{
			if (!Game.Started)
			{
				_update = false;
				return false;
			}

			_zoomDelegate.SyncZoomState();
			HandleTerrainEditorActiveUnit();

			IUnit unit = ActiveUnit;

			if ((gameTick % 2) == 0 && (_lastTurn != Game.GameTurn || _lastUnit != unit))
			{
				if (_lastUnit != unit && unit != null && Game.Human == unit.Owner && !_mapViewEnabled && ShouldCenter() && !Common.CapsLockActive)
				{
					CenterOnUnit();
				}
				_fullRedraw = true;
				_update = true;
				_lastUnit = unit;
				_lastTurn = Game.GameTurn;
			}

			// Check if the active unit is on the screen and the blink status has changed.
			if (unit == null)
			{
				_update = true;
				return false;
			}

			// O(1) viewport check replaces previous TileList scan in this hot tick path.
			if (IsTileVisibleInViewport(unit.X, unit.Y) && (gameTick % 2) == 0)
			{
				_update = true;
			}
			else if (unit.Moving)
			{
				_update = true;
			}
			else if (unit != _lastUnit && !_mapViewEnabled && ShouldCenter() && Human == unit.Owner && !Common.CapsLockActive)
			{
				CenterOnUnit();
				_update = true;
				_fullRedraw = true;
			}
			else
			{
				_update = (unit != _lastUnit);
			}
			return _update;
		}

		private void HandleTerrainEditorActiveUnit()
		{
			if (!TerrainEditorEnabled || Game.ActiveUnit == null)
			{
				return;
			}
			_editorStoredUnit ??= Game.ActiveUnit;

			Game.ActiveUnit = null;
			_update = true;
			_fullRedraw = true;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			bool renameDialogActive = _mapPositionDelegate.HasRenameDialog;

			if (!Game.Started)
			{
				_update = false;
				return false;
			}

			if (!(_update || _fullRedraw || renameDialogActive)) return false;
			if (!renameDialogActive && Game.MovingUnit == null && (gameTick % 2 == 1)) return false;

			Player? renderPlayer = (Settings.RevealWorld || TerrainEditorEnabled) ? null : Human;

			IUnit activeUnit = ActiveUnit;
			if (Game.MovingUnit != null && !_fullRedraw)
			{
				IUnit movingUnit = Game.MovingUnit;
				ITile tile = movingUnit.Tile;
				int dx = GetX(tile);
				int dy = GetY(tile);
				if (dx < _tilesX && dy < _tilesY)
				{
					dx *= _tilePixelSize; dy *= _tilePixelSize;

					MoveUnit movement = movingUnit.Movement;
					using IBitmap movingArea = Map[movingUnit.X - 1, movingUnit.Y - 1, 3, 3].ToBitmap(player: renderPlayer);
					using Bytemap scaledMovingArea = ScaleBitmap(movingArea.Bitmap, 3 * _tilePixelSize, 3 * _tilePixelSize);
					this.FillRectangle(dx - _tilePixelSize, dy - _tilePixelSize, 3 * _tilePixelSize, 3 * _tilePixelSize, 5)
						.AddLayer(scaledMovingArea, dx - _tilePixelSize, dy - _tilePixelSize);
					// This bitmap comes from the unit sprite cache.
					// Do not dispose it here; disposing would invalidate the cached sprite and break later renders.
					Bytemap unitSource = movingUnit.ToBitmap();
					using Bytemap unitPicture = ScaleBitmap(unitSource, _tilePixelSize, _tilePixelSize);
					this.AddLayer(unitPicture, dx + ((movement.X * _tilePixelSize) / BaseTilePixelSize), dy + ((movement.Y * _tilePixelSize) / BaseTilePixelSize));

					DrawFullCargoUnitWhileMoving(movingUnit, tile, dx, dy, movement, unitPicture);

					_terrainEditorRenderDelegate.DrawLandValuesOverlay();
					_terrainEditorRenderDelegate.DrawSpawnUnitPreview();
					_terrainEditorRenderDelegate.DrawBrushPreview();

					if (renameDialogActive)
					{
						_mapPositionDelegate.DrawRenameDialog(this, gameTick, Width, Height);
					}

					return true;
				}
			}
			else if (_fullRedraw)
			{
				_fullRedraw = false;
				using IBitmap tilesPicture = Tiles.ToBitmap(player: renderPlayer);
				int drawWidth = _tilesX * _tilePixelSize;
				int drawHeight = _tilesY * _tilePixelSize;
				using Bytemap scaledTiles = ScaleBitmap(tilesPicture.Bitmap, drawWidth, drawHeight);
				this.Clear(5)
					.AddLayer(scaledTiles);
			}

			if (!TerrainEditorEnabled && activeUnit != null && Game.CurrentPlayer == Human && !GameTask.Any() && !_mapViewEnabled)
			{
				ITile tile = activeUnit.Tile;
				int dx = GetX(tile);
				int dy = GetY(tile);
				if (dx < _tilesX && dy < _tilesY)
				{
					dx *= _tilePixelSize; dy *= _tilePixelSize;
					
					// blink status
					bool blinkOn = (gameTick % 4) < 2;
					TileSettings blinkState = blinkOn ? TileSettings.BlinkOn : TileSettings.BlinkOff;
					using IBitmap activeTileSource = tile.ToBitmap(blinkState);
					using Bytemap activeTile = ScaleBitmap(activeTileSource.Bitmap, _tilePixelSize, _tilePixelSize);
					this.AddLayer(activeTile, dx, dy);

					DrawHelperArrows(dx, dy);
				}

				_terrainEditorRenderDelegate.DrawLandValuesOverlay();
				_terrainEditorRenderDelegate.DrawSpawnUnitPreview();
				_terrainEditorRenderDelegate.DrawBrushPreview();

				if (renameDialogActive)
				{
					_mapPositionDelegate.DrawRenameDialog(this, gameTick, Width, Height);
				}

				return true;
			}
			
			_update = false;
			_terrainEditorRenderDelegate.DrawLandValuesOverlay();
			_terrainEditorRenderDelegate.DrawSpawnUnitPreview();
			_terrainEditorRenderDelegate.DrawBrushPreview();

			if (renameDialogActive)
			{
				_mapPositionDelegate.DrawRenameDialog(this, gameTick, Width, Height);
			}

			return true;
		}

		private void DrawFullCargoUnitWhileMoving(IUnit movingUnit, ITile tile, int dx, int dy, MoveUnit movement, Bytemap unitPicture)
		{
			if (movingUnit is IBoardable && tile.Units.Any(u => u.Class is UnitClass.Land or UnitClass.Air && (tile.City == null || (tile.City != null && u.Sentry))))
			{
				this.AddLayer(unitPicture, dx + ((movement.X * _tilePixelSize) / BaseTilePixelSize) - 1, dy + ((movement.Y * _tilePixelSize) / BaseTilePixelSize) - 1);
			}
		}

		internal void ForceRefresh()
		{
			_fullRedraw = true;
		}

		///<summary>
		/// Sets the map viewport origin to the specified world tile coordinates, 
		/// adjusting for map wrapping and ensuring the viewport remains within map bounds.
		/// In contrast to <a href="CenterOnPoint"> this method does not attempt to center on the coordinates 
		/// but uses them as the top-left corner of the viewport.
		/// </summary>
		internal void SetViewOrigin(int x, int y)
		{
			_x = x;
			while (_x < 0)
			{
				_x += Map.WIDTH;
			}

			while (_x >= Map.WIDTH)
			{
				_x -= Map.WIDTH;
			}

			_y = y;
			if (_y < 0)
			{
				_y = 0;
			}

			_y = Math.Min(_y, Math.Max(0, Map.HEIGHT - _tilesY));
			_update = true;
			_fullRedraw = true;
		}
		
		internal void CenterOnPoint(int x, int y)
		{
			SetViewOrigin(x - (_tilesX / 2), y - (_tilesY / 2));
		}
		
		private void CenterOnUnit()
		{
			if (Game.ActiveUnit == null) return;
			CenterOnPoint(Game.ActiveUnit.X, Game.ActiveUnit.Y);
		}

		private bool ShouldCenter(int relX = 0, int relY = 0)
		{
			IUnit unit = Game.ActiveUnit;
			if (unit == null) return false;
			int viewRange = 1;
			if (unit.Class == UnitClass.Water)
			{
				viewRange = (unit as BaseUnitSea).Range;
			}
			if (unit.Class == UnitClass.Air)
			{
				viewRange = 2;
			}
			return !Map.QueryMapPart(_x + viewRange, _y + viewRange, (_tilesX - (viewRange * 2)), (_tilesY - (viewRange * 2)))
				.Any(t => t != null && t.X == unit.X + relX && t.Y == unit.Y + relY);
		}

		public bool MoveTo(int relX, int relY) // public for unit testing
		{
			_helperDirection = new Point(0, 0);
			
			if (Game.ActiveUnit == null)
				return false;
			
			return Game.ActiveUnit.MoveTo(relX, relY);
		}

		private void TaskStarted(object sender, TaskEventArgs args)
		{
			if (!(sender is GameTask)) return;
			switch (sender)
			{
				case MoveUnit moveUnit:
					IUnit unit = moveUnit.ActiveUnit;
					if (unit == null || (Human != unit.Owner && !Game.EnemyMoves) || (!Settings.RevealWorld && Human != unit.Owner && !Human.Visible(unit.X, unit.Y)))
					{
						args.Abort();
						return;
					}
					if (!_mapViewEnabled && ShouldCenter(moveUnit.RelX, moveUnit.RelY))
					{
						CenterOnUnit();
					}
					return;
			}
		}

		internal bool ToggleMapView()
		{
			_mapViewEnabled = !_mapViewEnabled;
			_helperDirection = new Point(0, 0);
			_update = true;
			return _mapViewEnabled;
		}

		internal bool CenterOnActiveUnit()
		{
			if (Game.ActiveUnit == null)
			{
				return false;
			}

			CenterOnUnit();
			return true;
		}

		private bool KeyDownActiveUnit(KeyboardEventArgs args)
		{
			if (TerrainEditorEnabled || Game.ActiveUnit == null || Game.ActiveUnit.Moving)
				return false;
			
			if (args.Key == Key.Space)
			{
				Game.ActiveUnit.SkipTurn();
				return true;
			}
			else if (Settings.ArrowHelper)
			{
				switch (args.Key)
				{
					case Key.NumPad1:
					case Key.End:
						return MoveTo(-1, 1);
					case Key.NumPad2:
						return MoveTo(0, 1);
					case Key.NumPad3:
					case Key.PageDown:
						return MoveTo(1, 1);
					case Key.NumPad4:
						return MoveTo(-1, 0);
					case Key.NumPad5:
						GameTask.Enqueue(Show.Empty);
						return true;
					case Key.NumPad6:
						return MoveTo(1, 0);
					case Key.NumPad7:
					case Key.Home:
						return MoveTo(-1, -1);
					case Key.NumPad8:
						return MoveTo(0, -1);
					case Key.NumPad9:
					case Key.PageUp:
						return MoveTo(1, -1);
					case Key.Escape:
						_helperDirection = new Point(0, 0);
						return true;
					case Key.Down:
						_helperDirection.Y++;
						break;
					case Key.Up:
						_helperDirection.Y--;
						break;
					case Key.Left:
						_helperDirection.X--;
						break;
					case Key.Right:
						_helperDirection.X++;
						break;
					default:
						_helperDirection = new Point(0, 0);
						break;
				}

				if (Math.Abs(_helperDirection.X) + Math.Abs(_helperDirection.Y) >= 2)
				{
					int x = 0, y = 0;
					if (_helperDirection.X < 0)
						x = -1;
					else if (_helperDirection.X > 0)
						x = 1;
					
					if (_helperDirection.Y < 0)
						y = -1;
					else if (_helperDirection.Y > 0)
						y = 1;
					
					_helperDirection = new Point(0, 0);
					return MoveTo(x, y);
				}
			}
			else
			{
				switch (args.Key)
				{
					case Key.NumPad1:
					case Key.End:
						return MoveTo(-1, 1);
					case Key.NumPad2:
					case Key.Down:
						return MoveTo(0, 1);
					case Key.NumPad3:
					case Key.PageDown:
						return MoveTo(1, 1);
					case Key.NumPad4:
					case Key.Left:
						return MoveTo(-1, 0);
					case Key.NumPad5:
						GameTask.Enqueue(Show.Empty);
						return true;
					case Key.NumPad6:
					case Key.Right:
						return MoveTo(1, 0);
					case Key.NumPad7:
					case Key.Home:
						return MoveTo(-1, -1);
					case Key.NumPad8:
					case Key.Up:
						return MoveTo(0, -1);
					case Key.NumPad9:
					case Key.PageUp:
						return MoveTo(1, -1);
				}
			}
			
			switch (args.KeyChar)
			{
				case 'B':
					GameTask.Enqueue(Orders.FoundCity(Game.ActiveUnit));
					return true;
				case 'C':
					if (Game.ActiveUnit == null) break;
					CenterOnUnit();
					return true;
				case 'D':
					if (!args.Shift) break;
					Game.DisbandUnit(Game.ActiveUnit);
					return true;
				case 'H':
					Game.ActiveUnit.SetHome();
					return true;
				case 'I':
					GameTask.Enqueue(Orders.BuildIrrigation(Game.ActiveUnit));
					return true;
				case 'M':
					GameTask.Enqueue(Orders.BuildMines(Game.ActiveUnit));
					break;
				case 'P':
					if (args.Modifier == KeyModifier.Shift)
					{
						Game.ActiveUnit.Pillage();
					} else if (args.Modifier == KeyModifier.None)
					{
						GameTask.Enqueue(Orders.ClearPollution(Game.ActiveUnit));
					}
					break;
				case 'R':
					GameTask.Enqueue(Orders.BuildRoad(Game.ActiveUnit));
					break;
				case 'S':
					Game.ActiveUnit.Sentry = true;
					break;
				case 'F':
					if (Game.ActiveUnit is Settlers)
					{
						GameTask.Enqueue(Orders.BuildFortress(Game.ActiveUnit));
						break;
					}
					Game.ActiveUnit.Fortify = true;
					break;
				case 'U':
					if (Game.ActiveUnit is IBoardable)
					{
						return (Game.ActiveUnit as BaseUnitSea).Unload();;
					}
					break;
				case 'W':
					GameTask.Enqueue(Orders.Wait(Game.ActiveUnit));
					break;
			}

			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_mapPositionDelegate.KeyDownRenameDialog(args))
			{
				return true;
			}

			if (Game.CurrentPlayer != Human)
			{
				// Ignore all keypresses if the current player is not human
				return false;
			}

			if (args.Modifier == KeyModifier.Alt && GameMapPositionDelegate.IsZeroKey(args))
			{
				return _mapPositionDelegate.TryOpenMapPositionSlotList();
			}
			
			if (_mapPositionDelegate.TryHandleMapPositionHotkey(args))
			{
				return true;
			}

			if (_mapViewEnabled && _panMapDelegate.KeyDownMapView(args))
			{
				return true;
			}

			if (TerrainEditorEnabled && _terrainEditorInputDelegate.KeyDown(args))
			{
				return true;
			}

			switch (args.KeyChar)
			{
				case 'G':
					GameTask.Enqueue(Show.Goto);
					return true;
				case 'T':
					GameTask.Enqueue(Show.Terrain);
					return true;
			}

			if (Game.ActiveUnit != null)
			{
				return KeyDownActiveUnit(args);
			}
			
			switch (args.Key)
			{
				case Key.Space:
				case Key.Enter:
					GameTask.Enqueue(Turn.End());
					return true;
			}
			return false;
		}
		
		public override bool MouseMove(ScreenEventArgs args)
		{
			if (_terrainEditorInputDelegate.TryGetMapTileCoordinates(args, out int xx, out int yy))
			{
				if (_hoveredTileX != xx || _hoveredTileY != yy)
				{
					_hoveredTileX = xx;
					_hoveredTileY = yy;
					_update = true;
				}
			}
			return false;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_mapPositionDelegate.MouseDownRenameDialog(args))
			{
				return true;
			}

			if (!_terrainEditorInputDelegate.TryGetMapTileCoordinates(args, out int xx, out int yy))
			{
				return false;
			}

			if (TerrainEditorEnabled)
			{
				return _terrainEditorInputDelegate.MouseDown(args, xx, yy);
			}

			_hoveredTileX = xx;
			_hoveredTileY = yy;

			ITile selectedTile = Map[xx, yy];
			if (selectedTile == null)
			{
				return false;
			}

			City city = selectedTile.City;
			
			if ((args.Buttons & MouseButton.Right) > 0)
			{
				if (Game.ActiveUnit != null && (Game.ActiveUnit as BaseUnit).MoveTargets.Any(t => t.X == xx && t.Y == yy))
				{
					int relX = xx - Game.ActiveUnit.X;
					int relY = yy - Game.ActiveUnit.Y;
					if (relX < -1) relX = 1;
					if (relY > 1) relY = -1; 

					MoveTo(relX, relY);
					_update = true;
					return true;
				}

				Common.AddScreen(new Civilopedia(selectedTile));
				return _update;
			}
			if ((args.Buttons & MouseButton.Left) > 0)
			{
				if (city != null && (Human == city.CityOwnerPlayerIndex || Settings.RevealWorld))
				{
					Common.AddScreen(new CityManager(city));
				}
				else if (selectedTile.Units.Any(u => Human == u.Owner))
				{
					GameTask.Enqueue(Show.UnitStack(xx, yy));
				}
				else
				{
					SetViewOrigin(xx - (_tilesX / 2), yy - (_tilesY / 2));
					_update = true;
					_fullRedraw = true;
				}
			}
			return _update;
		}

		public override bool MouseDrag(ScreenEventArgs args)
		{
			if (!TerrainEditorEnabled)
			{
				return false;
			}

			return _terrainEditorInputDelegate.MouseDrag(args);
		}

		protected override void Resize(int width, int height) => _zoomDelegate.Resize(width, height);

		public override bool MouseWheel(ScreenEventArgs args) => _zoomDelegate.MouseWheel(args);

		internal void ResizeMap(int width, int height) => Resize(width, height);
		
		public GameMap()
		{
			GameTask.Started += TaskStarted;

			_x = 0;
			_y = 0;

			_mapPositionDelegate = new(this);
			_panMapDelegate = new(this);
			_zoomDelegate = new(this);
			_terrainEditorInputDelegate = new(this);
			_terrainEditorRenderDelegate = new(this);
			_terrainEditorSessionDelegate = new(this);
			
			Palette = Resources["SP257"].Palette.Copy();
		}
	}
}