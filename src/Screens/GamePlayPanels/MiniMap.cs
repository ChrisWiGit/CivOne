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
using CivOne.Graphics;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Persistence.Game;

namespace CivOne.Screens.GamePlayPanels
{
	internal class MiniMapWrapper(
			ISettings settings,
			IMapTilesRect map,
			IPlayer human,
			Palette palette,
			int miniMapViewOffsetX,
			int miniMapViewOffsetY,
			int MiniMapTileWidth,
			int MiniMapTileHeight,
			int SideBarWidth,
			int MiniMapHeight
			) : IDisposable
	{
		public const int MiniMapBorder = 1;

		public int MiniMapViewOffsetXCurrent { get; private set; }
		public int MiniMapViewOffsetYCurrent { get; private set; }
		
		private readonly Picture _miniMap = new(SideBarWidth, MiniMapHeight, palette);

		public IBitmap MiniMap => _miniMap;
		public void DrawMiniMap(GamePlay? gamePlay, IUnit? activeUnit, uint gameTick = 0)
		{
			_miniMap.Clear(5);
			
			if (gamePlay != null)
			{
				DrawMiniMapInternal(gamePlay, activeUnit, gameTick);
				return;
			}

			MiniMapViewOffsetXCurrent = miniMapViewOffsetX;
			MiniMapViewOffsetYCurrent = miniMapViewOffsetY;

			_miniMap.DrawRectangle3D();
		}

		private void DrawMiniMapInternal(GamePlay gamePlay, IUnit? activeUnit, uint gameTick)
		{
			int viewWidth = Math.Clamp(gamePlay.VisibleTilesX, 1, MiniMapTileWidth);
			int viewHeight = Math.Clamp(gamePlay.VisibleTilesY, 1, MiniMapTileHeight);
			int dynamicOffsetX = Math.Clamp((MiniMapTileWidth - viewWidth) / 2, 0, MiniMapTileWidth - 1);
			int dynamicOffsetY = Math.Clamp((MiniMapTileHeight - viewHeight) / 2, 0, MiniMapTileHeight - 1);
			MiniMapViewOffsetXCurrent = dynamicOffsetX;
			MiniMapViewOffsetYCurrent = dynamicOffsetY;

			bool editorEnabled = gamePlay.IsTerrainEditorEnabled;
			bool revealWorld = settings.RevealWorld || editorEnabled;

			ITile[,] tiles = map[gamePlay.X - dynamicOffsetX, gamePlay.Y - dynamicOffsetY, MiniMapTileWidth, MiniMapTileHeight];
			for (int yy = 0; yy < MiniMapTileHeight; yy++)
				for (int xx = 0; xx < MiniMapTileWidth; xx++)
				{
					ITile tile = tiles[xx, yy];
					if (tile == null) continue;

					// Flash active unit
					if (!editorEnabled && activeUnit != null && human == activeUnit.Player && tile.X == activeUnit.X && tile.Y == activeUnit.Y && gamePlay.IsMapViewEnabled != true)
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
					else if (human.Visible[tile.X, tile.Y])
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

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_miniMap.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}