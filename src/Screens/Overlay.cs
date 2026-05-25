// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class Overlay : BaseScreen
	{
		private struct HelpLabel
		{
			public int X, Y;
			public int PointX, PointY;
			public string Text;
			public HelpLabel(string text, int x, int y, int pointX, int pointY)
			{
				Text = text;
				X = x;
				Y = y;
				PointX = pointX;
				PointY = pointY;
			}
		}

		private bool _update = true;
		private bool _interfaceHelp = false;
		private bool _showTerrain = false;

		private int _x, _y;

		private bool _closing = false;

		private IEnumerable<HelpLabel> HelpLabels
		{
			get
			{
				IUnit startUnit = Game.GetUnits().First(x => Game.Human == x.Owner);
				IUnit activeUnit = Game.ActiveUnit;
				GamePlay gamePlay = (GamePlay)Common.Screens.First(x => x is GamePlay);
				gamePlay.Update(0);

				IUnit focusUnit = (activeUnit != null && activeUnit.Owner == Game.PlayerNumber(Human)) ? activeUnit : startUnit;
				int mapOffsetX = Settings.RightSideBar ? 0 : 80;
				const int mapOffsetY = 8;
				int mapWidth = Math.Max(0, gamePlay.Width() - 80);
				int mapHeight = Math.Max(0, gamePlay.Height() - 8);
				int mapCenterX = mapOffsetX + (mapWidth / 2);
				int mapWindowPointX = Settings.RightSideBar
					? mapOffsetX + Math.Max(8, mapWidth - 8)
					: Math.Max(8, mapOffsetX - 32);
				int mapWindowPointY = mapOffsetY + 24;
				int referenceCenterX = Settings.RightSideBar ? 120 : 200;
				int labelShiftX = mapCenterX - referenceCenterX;
				int tilesX = Math.Max(1, (int)Math.Ceiling((double)mapWidth / 16));
				int tilesY = Math.Max(1, (int)Math.Ceiling((double)mapHeight / 16));
				int mapX = gamePlay.X;
				int mapY = gamePlay.Y;

				int activeRelX = focusUnit.X - mapX;
				while (activeRelX < 0) activeRelX += Map.WIDTH;
				while (activeRelX >= Map.WIDTH) activeRelX -= Map.WIDTH;
				int activeRelY = focusUnit.Y - mapY;
				int activePointX = mapOffsetX + (activeRelX * 16) + 8;
				int activePointY = mapOffsetY + (activeRelY * 16) + 8;
				if (Settings.RightSideBar)
				{
					yield return new HelpLabel(Translate("Map Window"), 148 + labelShiftX, 24, mapWindowPointX, mapWindowPointY);
					yield return new HelpLabel(Translate("Menu Bar"), 61 + labelShiftX, 16, 160 + labelShiftX, 6);
					yield return new HelpLabel(Translate("Active Unit"), 158 + labelShiftX, 170, activePointX, activePointY);
				}
				else
				{
					yield return new HelpLabel(Translate("Map Window"), 88 + labelShiftX, 24, mapWindowPointX, mapWindowPointY);
					yield return new HelpLabel(Translate("Menu Bar"), 201 + labelShiftX, 16, 160 + labelShiftX, 6);
					yield return new HelpLabel(Translate("Active Unit"), 88 + labelShiftX, 170, activePointX, activePointY);
				}

				int labelCenterBaseX = mapCenterX - 30;
				
				for (int yy = -1; yy <= 1; yy++)
				for (int xx = -1; xx <= 1; xx++)
				{
					if (xx == 0 && yy == 0) continue;
					string text = string.Empty;
					ITile tile = focusUnit.Tile[xx, yy];
					switch (tile.Type)
					{
						case Terrain.Desert: text = (tile.Special ? Translate("Oasis") : Translate("Desert")); break;
						case Terrain.Plains: text = (tile.Special ? Translate("Horses") : Translate("Plains")); break;
						case Terrain.Forest: text = (tile.Special ? Translate("Game") : Translate("Desert")); break;
						case Terrain.Hills: text = (tile.Special ? Translate("Coal") : Translate("Hills")); break;
						case Terrain.Mountains: text = (tile.Special ? Translate("Gold") : Translate("Mountains")); break;
						case Terrain.Tundra: text = (tile.Special ? Translate("Game") : Translate("Tundra")); break;
						case Terrain.Arctic: text = (tile.Special ? Translate("Seals") : Translate("Arctic")); break;
						case Terrain.Swamp: text = (tile.Special ? Translate("Oil") : Translate("Swamp")); break;
						case Terrain.Jungle: text = (tile.Special ? Translate("Gems") : Translate("Jungle")); break;
						case Terrain.Ocean: text = (tile.Special ? Translate("Fish") : Translate("Ocean")); break;
						case Terrain.River: text = Translate("River"); break;
						case Terrain.Grassland1:
						case Terrain.Grassland2: text = Translate("Grassland"); break;
					}
					if (tile.Hut) text = Translate("Village");

					int relX = tile.X - mapX;
					while (relX < 0) relX += Map.WIDTH;
					while (relX >= Map.WIDTH) relX -= Map.WIDTH;

					int relY = tile.Y - mapY;
					if (relX < 0 || relY < 0 || relX >= tilesX || relY >= tilesY)
					{
						continue;
					}

					int pointX = mapOffsetX + (relX * 16) + 8;
					int pointY = mapOffsetY + (relY * 16) + 8;
					yield return new HelpLabel(text, labelCenterBaseX + (65 * xx), 100 + (49 * yy), pointX, pointY);
				}
			}
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_closing)
			{
				if (!HandleScreenFadeOut()) Destroy();
				return true;
			}

			if (_update)
			{
				this.Clear();

				if (_interfaceHelp)
				{
					foreach (HelpLabel helpLabel in HelpLabels)
					{
						Size textSize = Resources.GetTextSize(0, helpLabel.Text);

						int ww = textSize.Width + 11, hh = textSize.Height + 9;
						Picture label = new Picture(textSize.Width + 11, textSize.Height + 9)
							.Tile(Pattern.PanelGrey)
							.DrawRectangle()
							.DrawRectangle3D(1, 1, ww - 2, hh - 2)
							.DrawText(helpLabel.Text, 0, 15, 5, 5)
							.As<Picture>();

						this.DrawLine(helpLabel.PointX, helpLabel.PointY, helpLabel.X + 5, helpLabel.Y + 6, 15)
							.AddLayer(label, helpLabel.X, helpLabel.Y);
					}
				}

				if (_showTerrain)
				{
					int cx = Settings.RightSideBar ? 0 : 80;
					int cy = 8;

					this.AddLayer(Map[_x, _y, 15, 12].ToBitmap(TileSettings.Terrain, Human), cx, cy, dispose: true);
				}

				_update = false;
				return true;
			}
			return false;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_interfaceHelp)
			{
				_closing = true;
				return true;
			}
			Destroy();
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_interfaceHelp)
			{
				_closing = true;
				return true;
			}
			Destroy();
			return true;
		}

		public static Overlay Empty
		{
			get
			{
				return new Overlay();
			}
		}

		public static Overlay InterfaceHelp
		{
			get
			{
				return new Overlay()
				{
					_interfaceHelp = true
				};
			}
		}

		public static Overlay TerrainView(int x, int y)
		{
			return new Overlay()
			{
				_showTerrain = true,
				_x = x,
				_y = y
			};
		}

		private Overlay() : base(MouseCursor.Pointer)
		{	
			Palette = Common.TopScreen.Palette.Copy();
		}
	}
}