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
using CivOne.Screens.GamePlayPanels;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Screens
{
	[Modal, ScreenResizeable]
	internal class Goto : BaseScreen
	{
		private readonly int _x, _y;
		private readonly GotoDelegate _delegate;

		private bool _update = true;
		private bool? _lastBlinkOn;

		public int X { get; private set; }
		public int Y { get; private set; }

		protected override bool HasUpdate(uint gameTick)
		{
			bool blinkOn = (gameTick % 4) < 2;
			if (_update || _lastBlinkOn != blinkOn)
			{
				_lastBlinkOn = blinkOn;
				_update = false;
				_delegate.Render(gameTick);
				return true;
			}
			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			Destroy();
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_lastBlinkOn = null;
			_update = true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_delegate.TrySelect(args, out Point destination))
			{
				X = destination.X;
				Y = destination.Y;
			}

			Destroy();
			return true;
		}

		internal Goto(int x, int y) : base(MouseCursor.Goto)
		{
			_x = x;
			_y = y;
			X = -1;
			Y = -1;
			_delegate = new GotoDelegate(this, _x, _y);

			Palette = CurrentGamePlay.Palette;
		}

		private static GamePlay CurrentGamePlay => Common.Screens.OfType<GamePlay>().First();

		private sealed class GotoDelegate(Goto gotoScreen, int originX, int originY)
		{
			private readonly Goto _gotoScreen = gotoScreen;
			private readonly int _originX = originX;
			private readonly int _originY = originY;

			public void Render(uint gameTick)
			{
				GamePlay gamePlay = Goto.CurrentGamePlay;
				_gotoScreen.Clear(5).AddLayer(gamePlay.Bitmap);
				DrawBlinkingActiveUnit(gameTick, gamePlay);
			}

			public bool TrySelect(ScreenEventArgs args, out Point destination)
			{
				if (TrySelectFromCanvas(args.X, args.Y, out destination))
				{
					return true;
				}

				return TrySelectFromMinimap(args.X, args.Y, out destination);
			}

			private bool TrySelectFromCanvas(int x, int y, out Point destination)
			{
				GamePlay gamePlay = Goto.CurrentGamePlay;
				int offsetX = Settings.RightSideBar ? 0 : 80;
				int offsetY = 8;
				int tilePixelSize = Math.Max(1, gamePlay.TilePixelSize);

				int xx = (int)Math.Floor((double)(x - offsetX) / tilePixelSize);
				int yy = (int)Math.Floor((double)(y - offsetY) / tilePixelSize);

				if (xx < 0 || yy < 0 || xx >= gamePlay.VisibleTilesX || yy >= gamePlay.VisibleTilesY)
				{
					destination = default;
					return false;
				}

				ITile? tile = Map[_originX + xx, _originY + yy];
				if (tile == null)
				{
					destination = default;
					return false;
				}

				destination = Normalize(tile.X, tile.Y);
				return true;
			}

			private static bool TrySelectFromMinimap(int x, int y, out Point destination)
			{
				int offsetX = Settings.RightSideBar ? 241 : 1;
				int offsetY = 9;

				int xx = x - offsetX;
				int yy = y - offsetY;
				if (xx < 0 || yy < 0)
				{
					destination = default;
					return false;
				}

				ITile? tile = Map[xx, yy];
				if (tile == null)
				{
					destination = default;
					return false;
				}

				destination = Normalize(tile.X, tile.Y);
				return true;
			}

			private static Point Normalize(int x, int y)
			{
				while (x < 0)
				{
					x += Map.WIDTH;
				}

				while (x >= Map.WIDTH)
				{
					x -= Map.WIDTH;
				}

				return new Point(x, y);
			}

			private void DrawBlinkingActiveUnit(uint gameTick, GamePlay gamePlay)
			{
				if (Game.ActiveUnit is not IUnit activeUnit || activeUnit.Moving)
				{
					return;
				}

				ITile tile = activeUnit.Tile;
				int localX = tile.X - gamePlay.X;
				if (localX < 0)
				{
					localX += Map.WIDTH;
				}

				if (localX < 0 || localX >= gamePlay.VisibleTilesX)
				{
					return;
				}

				int localY = tile.Y - gamePlay.Y;
				if (localY < 0 || localY >= gamePlay.VisibleTilesY)
				{
					return;
				}

				bool blinkOn = (gameTick % 4) < 2;
				TileSettings blinkState = blinkOn ? TileSettings.BlinkOn : TileSettings.BlinkOff;
				int mapOffsetX = Settings.RightSideBar ? 0 : 80;
				int mapOffsetY = 8;
				using IBitmap activeTile = tile.ToBitmap(blinkState, pixelSize: gamePlay.TilePixelSize);
				_gotoScreen.AddLayer(activeTile.Bitmap, mapOffsetX + (localX * gamePlay.TilePixelSize), mapOffsetY + (localY * gamePlay.TilePixelSize));
			}
		}
	}
}