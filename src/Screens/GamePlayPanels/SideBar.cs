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
using CivOne.Graphics.Sprites;
using CivOne.Screens.PalaceAssets;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Services;
using CivOne.Services.GlobalWarming;

namespace CivOne.Screens.GamePlayPanels
{
	internal class SideBar : BaseScreen
	{
		private const int SideBarWidth = 80;
		private const int MiniMapHeight = 50;
		private const int DemographicsHeight = 39;
		private const int GameInfoOffsetY = MiniMapHeight + DemographicsHeight;
		private const int MiniMapBorder = 1;
		private const int MiniMapTileWidth = SideBarWidth - (MiniMapBorder * 2);
		private const int MiniMapTileHeight = MiniMapHeight - (MiniMapBorder * 2);
		private const int MiniMapViewOffsetX = 30;
		private const int MiniMapViewOffsetY = 18;
		private const int PalaceHotspotBottomY = 62;

		private bool _update = true;
		private int _lastDemographicsSignature;
		private int _lastGameInfoSignature;
		private string _statusInfoText = string.Empty;
		private int _statusInfoFrames;
		private int _miniMapViewOffsetXCurrent = MiniMapViewOffsetX;
		private int _miniMapViewOffsetYCurrent = MiniMapViewOffsetY;
		
		private readonly Picture _miniMap, _demographics;
		private Picture _gameInfo;
		
		private void DrawMiniMap(uint gameTick = 0)
		{
			_miniMap.Clear(5);
			
			if (GamePlay != null)
			{
				int viewWidth = Math.Clamp(GamePlay.VisibleTilesX, 1, MiniMapTileWidth);
				int viewHeight = Math.Clamp(GamePlay.VisibleTilesY, 1, MiniMapTileHeight);
				int dynamicOffsetX = Math.Clamp((MiniMapTileWidth - viewWidth) / 2, 0, MiniMapTileWidth - 1);
				int dynamicOffsetY = Math.Clamp((MiniMapTileHeight - viewHeight) / 2, 0, MiniMapTileHeight - 1);
				_miniMapViewOffsetXCurrent = dynamicOffsetX;
				_miniMapViewOffsetYCurrent = dynamicOffsetY;

				bool editorEnabled = GamePlay.IsTerrainEditorEnabled;
				bool revealWorld = Settings.RevealWorld || editorEnabled;
				IUnit? activeUnit = Game.ActiveUnit;
				ITile[,] tiles = Map[GamePlay.X - dynamicOffsetX, GamePlay.Y - dynamicOffsetY, MiniMapTileWidth, MiniMapTileHeight];
				for (int yy = 0; yy < MiniMapTileHeight; yy++)
				for (int xx = 0; xx < MiniMapTileWidth; xx++)
				{
					ITile tile = tiles[xx, yy];
					if (tile == null) continue;

					// Flash active unit
					if (!editorEnabled && activeUnit != null && Human == activeUnit.Owner && tile.X == activeUnit.X && tile.Y == activeUnit.Y && GamePlay.IsMapViewEnabled != true)
					{
						if (gameTick % 4 <= 1)
						{
							_miniMap[xx + MiniMapBorder, yy + MiniMapBorder] = 15;
						}
						else
						{
							_miniMap[xx + MiniMapBorder, yy + MiniMapBorder] = (byte)(tile.IsOcean ? 1 : 2);
						}
						continue;
					}

					if (revealWorld)
					{
						byte colour = 5;
						switch (tile.Type)
						{
							case Terrain.Ocean: colour = 1; break;
							case Terrain.Forest: colour = 2; break;
							case Terrain.Swamp: colour = 3; break;
							case Terrain.Plains: colour = 6; break;
							case Terrain.Tundra: colour = 7; break;
							case Terrain.River: colour = 9; break;
							case Terrain.Grassland1:
							case Terrain.Grassland2: colour = 10; break;
							case Terrain.Jungle: colour = 11; break;
							case Terrain.Hills: colour = 12; break;
							case Terrain.Mountains: colour = 13; break;
							case Terrain.Desert: colour = 14; break;
							case Terrain.Arctic: colour = 15; break;
						}
						_miniMap[xx + MiniMapBorder, yy + MiniMapBorder] = colour;
					}
					else if (Human.Visible(tile.X, tile.Y))
					{
						if (tile.City != null)
						{
							_miniMap[xx + MiniMapBorder, yy + MiniMapBorder] = Common.ColourLight[tile.City.CityOwnerPlayerIndex];
						}
						else
						{
							if (tile.IsOcean) _miniMap[xx + MiniMapBorder, yy + MiniMapBorder] = 1;
							else _miniMap[xx + MiniMapBorder, yy + MiniMapBorder] = 2;
						}
					}
				}

				int viewX = MiniMapBorder + dynamicOffsetX;
				int viewY = MiniMapBorder + dynamicOffsetY;

				_miniMap.DrawRectangle(viewX, viewY, viewWidth, viewHeight, 15)
					.DrawRectangle3D();
				return;
			}

			_miniMapViewOffsetXCurrent = MiniMapViewOffsetX;
			_miniMapViewOffsetYCurrent = MiniMapViewOffsetY;

			_miniMap.DrawRectangle3D();
		}

		private void DrawDemographics()
		{
			_demographics.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.FillRectangle(3, 2, 74, 11, 11)
				.FillRectangle(3, 13, 74, 1, 2);
			if (Human.Population > 0)
			{
				string population = Common.NumberSeperator(Human.Population);
				_demographics.DrawText($"{population}#", 0, 5, 2, 15, TextAlign.Left);
			}
			_demographics.DrawText(Game.GameYear, 0, 5, 2, 23, TextAlign.Left);


			int width = Resources.GetTextSize(0, Game.GameYear).Width;
			int stage = (int)Math.Floor((double)Human.Science / Human.ScienceCost * 4);
			_demographics.AddLayer(Icons.Lamp(stage)!, 4 + width, 22);

			DrawPollutionSun(width);

			_demographics.DrawText($"{Human.Gold}$ {Human.LuxuriesRate}.{Human.TaxesRate}.{Human.ScienceRate}", 0, 5, 2, 31, TextAlign.Left);
			
			DrawPreviewPalace(_demographics);
		}

		private void DrawPreviewPalace(IBitmap targetLayer)
		{
			IBitmap palacePreview = _palaceRenderer.RenderPalace(Human.Palace);
			int palacePreviewX = (SideBarWidth - palacePreview.Width()) / 2;
			targetLayer.AddLayer(palacePreview, palacePreviewX, 1);
		}

		private void DrawPollutionSun(int width)
		{
			if (_globalWarmingService.WarmingIndicator == WarmingIndicator.None)
			{
				return;
			}
			_demographics.AddLayer(Icons.Sun((int)_globalWarmingService.WarmingIndicator - 1)!, 4 + 10 + width, 24);
		}

		private int GetDemographicsSignature()
		{
			HashCode hash = new();
			hash.Add(Human.Population);
			hash.Add(Game.GameYear);
			hash.Add(Human.Gold);
			hash.Add(Human.LuxuriesRate);
			hash.Add(Human.TaxesRate);
			hash.Add(Human.ScienceRate);
			hash.Add(Human.Science);
			hash.Add(Human.ScienceCost);
			hash.Add((int)_globalWarmingService.WarmingIndicator);

			for (int i = 0; i < 7; i++)
			{
				hash.Add(Human.Palace.GetPalaceLevel(i));
				hash.Add((int)Human.Palace.GetPalaceStyle(i));
			}

			return hash.ToHashCode();
		}

		private void DrawGameInfo(uint gameTick = 0)
		{
			IUnit? unit = Game.ActiveUnit;

			_gameInfo.Tile(Pattern.PanelGrey)
				.DrawRectangle3D();

			bool isInEditorMode = DrawTerrainEditorInfo();
			if (isInEditorMode)
			{
				return;
			}

			if (Game.CurrentPlayer != Human || (unit != null && Human != unit.Owner) || (GameTask.Any() && !GameTask.Is<Show>() && !GameTask.Is<Message>()))
			{
				_gameInfo.FillRectangle(2, _gameInfo.Height - 8, 6, 6, (byte)((gameTick % 4 < 2) ? 15 : 8));
				return;
			}

			if (unit != null)
			{
				int yy = 2;
				_gameInfo.DrawText(Human.TribeName, 0, 5, 4, 2, TextAlign.Left);
				_gameInfo.DrawText(unit.TranslatedName, 0, 5, 4, yy += 8, TextAlign.Left);

				if (unit.Veteran)
				{
					_gameInfo.DrawText(Translate("Veteran"), 0, 5, 8, yy += 8, TextAlign.Left);
				}

				if (unit is BaseUnitAir airUnit)
				{
					_gameInfo.DrawText(TranslateFormatted("Moves: {0}({1})", unit.MovesLeft, airUnit.FuelLeft), 0, 5, 4, yy += 8, TextAlign.Left);
				}
				else if (unit.PartMoves > 0)
				{
					_gameInfo.DrawText(TranslateFormatted("Moves: {0}.{1}", unit.MovesLeft, unit.PartMoves), 0, 5, 4, yy += 8, TextAlign.Left);
				}
				else
				{
					_gameInfo.DrawText(TranslateFormatted("Moves: {0}", unit.MovesLeft), 0, 5, 4, yy += 8, TextAlign.Left);
				}
				_gameInfo.DrawText(unit.Home == null ? Translate("NONE") : unit.Home.Name, 0, 5, 4, yy += 8, TextAlign.Left);
				_gameInfo.DrawText($"({Map[unit.X, unit.Y].TranslatedName})", 0, 5, 4, yy += 8, TextAlign.Left);

				if (Map[unit.X, unit.Y].RailRoad)
					_gameInfo.DrawText(Translate("(RailRoad)"), 0, 5, 4, yy += 8, TextAlign.Left);
				else if (Map[unit.X, unit.Y].Road)
					_gameInfo.DrawText(Translate("(Road)"), 0, 5, 4, yy += 8, TextAlign.Left);
				if (Map[unit.X, unit.Y].Irrigation)
					_gameInfo.DrawText(Translate("(Irrigation)"), 0, 5, 4, yy += 8, TextAlign.Left);
				else if (Map[unit.X, unit.Y].Mine)
					_gameInfo.DrawText(Translate("(Mining)"), 0, 5, 4, yy += 8, TextAlign.Left);

				yy += 11;

				IUnit[] units = Map[unit.X, unit.Y].Units.Where(u => u != unit).Take(8).ToArray();
				for (int i = 0; i < units.Length; i++)
				{
					int ix = 7 + (i % 4 * 16);
					int iy = yy + ((i - (i % 4)) / 4 * 16);
					_gameInfo.AddLayer(units[i].ToBitmap(), ix, iy);
				}
			}
			else
			{
				if (gameTick % 4 < 2)
					_gameInfo.DrawText(Translate("End of Turn"), 0, 5, 4, 26, TextAlign.Left);
				_gameInfo.DrawText(Translate("Press Enter"), 0, 5, 4, 42, TextAlign.Left);
				_gameInfo.DrawText(Translate("to continue"), 0, 5, 4, 50, TextAlign.Left);
			}
		}

		private bool DrawTerrainEditorInfo()
		{
			if ((GamePlay?.IsTerrainEditorEnabled) != true)
			{
				return false;
			}

			const int fontId = 1;
			const byte colorId = 5;
			const int yStart = 2;
			int brushSize = GamePlay.TerrainEditorBrushSize;
			int fontHeight = Resources.GetFontHeight(fontId) + 1;
			bool spawnMode = GamePlay.IsTerrainEditorSpawnMode;
			ITile hoveredTile = Map[GamePlay.HoveredTileX, GamePlay.HoveredTileY];
			IUnit[] hoveredUnits = hoveredTile?.Units ?? [];
			string? hoveredUnitOwnerText = null;
			string? hoveredUnitStackText = null;

			if (spawnMode && hoveredUnits.Length > 0)
			{
				IUnit firstUnit = hoveredUnits[0];
				Player? owner = Game.GetPlayer(firstUnit.Owner);
				if (owner != null)
				{
					hoveredUnitOwnerText = owner.Civilization is CivOne.Civilizations.Barbarian
						? Translate("Barbarians")
						: owner.TribeNamePlural;
				}

				hoveredUnitStackText = $"{firstUnit.TranslatedName} : {hoveredUnits.Length}";
			}

			_gameInfo.DrawText(Translate("Editor active"), fontId, colorId, 2, yStart, TextAlign.Left);
			_gameInfo.DrawText(GamePlay.TerrainEditorModeText, fontId, colorId, 2, yStart + fontHeight, TextAlign.Left);
			_gameInfo.DrawText(TranslateFormatted("Brush {0}x{0}", brushSize), fontId, colorId, 2, yStart + 2 * fontHeight, TextAlign.Left);

			if (spawnMode)
			{
				if (!string.IsNullOrEmpty(hoveredUnitOwnerText))
				{
					_gameInfo.DrawText(hoveredUnitOwnerText, fontId, colorId, 2, yStart + 3 * fontHeight, TextAlign.Left);
				}

				if (!string.IsNullOrEmpty(hoveredUnitStackText))
				{
					_gameInfo.DrawText(hoveredUnitStackText, fontId, colorId, 2, yStart + 4 * fontHeight, TextAlign.Left);
				}
			}
			else if (!string.IsNullOrEmpty(GamePlay.TerrainEditorCityOwnerText))
			{
				_gameInfo.DrawText(GamePlay.TerrainEditorCityOwnerText, fontId, colorId, 2, yStart + 3 * fontHeight, TextAlign.Left);
			}
			_gameInfo.DrawText($"{GamePlay.HoveredTileX},{GamePlay.HoveredTileY}", fontId, colorId, 2, yStart + 5 * fontHeight, TextAlign.Left);
			
			return true;
		}

		private int GetGameInfoSignature(uint gameTick)
		{
			IUnit? unit = Game.ActiveUnit;
			bool hasBlockingTask = GameTask.Any() && !GameTask.Is<Show>() && !GameTask.Is<Message>();
			int blinkPhase = (unit == null || hasBlockingTask) ? (int)(gameTick % 4) : -1;
			bool editorEnabled = GamePlay?.IsTerrainEditorEnabled == true;
			bool spawnMode = editorEnabled && GamePlay!.IsTerrainEditorSpawnMode;
			string editorMode = editorEnabled ? GamePlay!.TerrainEditorModeText : string.Empty;
			int editorBrush = editorEnabled ? GamePlay!.TerrainEditorBrushSize : -1;
			string editorOwner = editorEnabled ? GamePlay!.TerrainEditorCityOwnerText ?? string.Empty : string.Empty;
			int hoveredTileX = editorEnabled ? GamePlay!.HoveredTileX : -1;
			int hoveredTileY = editorEnabled ? GamePlay!.HoveredTileY : -1;
			ITile? hoveredTile = editorEnabled ? Map[hoveredTileX, hoveredTileY] : null;
			IUnit[] hoveredUnits = hoveredTile?.Units ?? [];
			byte hoveredOwner = hoveredUnits.Length > 0 ? hoveredUnits[0].Owner : byte.MaxValue;
			UnitType hoveredType = hoveredUnits.Length > 0 ? hoveredUnits[0].Type : (UnitType)(-1);
			int hoveredCount = hoveredUnits.Length;

			return HashCode.Combine(
				HashCode.Combine(
					Game.CurrentPlayer,
					unit,
					unit == null ? -1 : unit.MovesLeft,
					unit == null ? -1 : unit.PartMoves,
					hasBlockingTask,
					blinkPhase,
					_statusInfoText,
					_statusInfoFrames),
				HashCode.Combine(
					HashCode.Combine(editorEnabled, spawnMode, editorMode, editorBrush, editorOwner),
					HashCode.Combine(hoveredTileX, hoveredTileY, hoveredOwner, hoveredType, hoveredCount)));
		}

		internal void ShowMapPositionSavedInfo(int slot)
		{
			_statusInfoText = TranslateFormatted("Map position {0} saved", slot);
			_statusInfoFrames = 20;
			_update = true;
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_statusInfoFrames > 0)
			{
				_statusInfoFrames--;
				_update = true;
			}

			if (!_update && gameTick % 2 != 0)
			{
				return false;
			}
			if (!(Common.TopScreen is GamePlay))
				gameTick = 0;

			DrawMiniMap(gameTick);

			int demographicsSignature = GetDemographicsSignature();
			if (_update || demographicsSignature != _lastDemographicsSignature)
			{
				DrawDemographics();
				_lastDemographicsSignature = demographicsSignature;
			}

			int gameInfoSignature = GetGameInfoSignature(gameTick);
			if (_update || gameInfoSignature != _lastGameInfoSignature)
			{
				DrawGameInfo(gameTick);
				if (_statusInfoFrames > 0 && !string.IsNullOrWhiteSpace(_statusInfoText))
				{
					_gameInfo.DrawText(_statusInfoText, 0, 5, 2, _gameInfo.Height - 8, TextAlign.Left);
				}
				_lastGameInfoSignature = gameInfoSignature;
			}

			this.AddLayer(_miniMap, 0, 0)
				.AddLayer(_demographics, 0, MiniMapHeight)
				.AddLayer(_gameInfo, 0, GameInfoOffsetY);

			_update = false;
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (args.Y <= MiniMapHeight)
			{
				if (args.X < MiniMapBorder || args.Y < MiniMapBorder || args.X > (SideBarWidth - MiniMapBorder) || args.Y > (MiniMapHeight - MiniMapBorder)) return true;
				
				int xx = args.X - MiniMapBorder + GamePlay!.X - _miniMapViewOffsetXCurrent;
				int yy = args.Y - MiniMapBorder + GamePlay!.Y - _miniMapViewOffsetYCurrent;

				GamePlay.CenterOnPoint(xx, yy);
			}
			if (args.Y > MiniMapHeight && args.Y < PalaceHotspotBottomY)
			{
				Log("Sidebar: Palace View");
				Common.AddScreen(new PalaceView(false, PalaceSpriteProviderFactory.GetInstance()));
			}
			else if (args.Y >= PalaceHotspotBottomY)
			{
				if (Game.CurrentPlayer == Human && Game.ActiveUnit == null)
				{
					GameTask.Enqueue(Turn.End());
				}
			}
			return true;
		}

		#pragma warning disable CA1822 // This method may be static
		private GamePlay? GamePlay
		{
			get
			{
				IScreen? mapScreen = Common.Screens.FirstOrDefault(s => s is GamePlay);
				if (mapScreen is GamePlay gamePlay)
					return gamePlay;

				return null;
			}
		}
		#pragma warning restore CA1822
		
		public void Resize(int height)
		{
			Bitmap = new Bytemap(SideBarWidth, height);
			_gameInfo?.Dispose();
			_gameInfo = new Picture(SideBarWidth, height - GameInfoOffsetY, Palette);
			_lastDemographicsSignature = int.MinValue;
			_lastGameInfoSignature = int.MinValue;
			_update = true;
		}

		private readonly IGlobalWarmingService _globalWarmingService;
		private readonly IPreviewPalaceRenderer _palaceRenderer = PreviewPalaceRendererFactory.GetInstance();

		public SideBar(Palette palette, IGlobalWarmingService globalWarmingService) : base(SideBarWidth, 192)
		{
			_globalWarmingService = globalWarmingService;
			_lastDemographicsSignature = int.MinValue;
			_lastGameInfoSignature = int.MinValue;

			_miniMap = new Picture(SideBarWidth, MiniMapHeight, palette);
			_demographics = new Picture(SideBarWidth, DemographicsHeight, palette);
			_gameInfo = new Picture(SideBarWidth, 192 - GameInfoOffsetY, palette);

			DrawMiniMap();
			DrawDemographics();
			_lastDemographicsSignature = GetDemographicsSignature();
			DrawGameInfo();
			_lastGameInfoSignature = GetGameInfoSignature(0);

			Palette = palette.Copy();
			this.AddLayer(_miniMap, 0, 0)
				.AddLayer(_demographics, 0, MiniMapHeight)
				.AddLayer(_gameInfo, 0, GameInfoOffsetY);
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_miniMap.Dispose();
			_demographics.Dispose();
			_gameInfo.Dispose();
			base.Dispose(disposing);
		}
	}
}