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
using CivOne.Graphics;
using CivOne.Tiles;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class Goto : BaseScreen
	{
		private readonly int _x, _y;

		private bool _update = true;

		public int X { get; private set; }
		public int Y { get; private set; }

		protected override bool HasUpdate(uint gameTick)
		{
			if (_update)
			{
				_update = false;
				return true;
			}
			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			Destroy();
			return true;
		}

		ITile fromCanvas(int x, int y)
		{
			int offsetX = 80;
			if (Settings.RightSideBar) offsetX = 0;
			int offsetY = 8;

			int xx = (int)Math.Floor((double)(x - offsetX) / 16);
			int yy = (int)Math.Floor((double)(y - offsetY) / 16);

			if (xx < 0 || yy < 0)
			{
				return null;
			}

			return Map[_x + xx, _y + yy];
		}
		ITile fromMinimap(int x, int y)
		{
			int offsetX = 1;
			if (Settings.RightSideBar) offsetX = 241;
			int offsetY = 9;

			int xx = (int)(x - offsetX);
			int yy = (int)(y - offsetY);

			if (xx < 0 || yy < 0)
			{
				return null;
			}

			return Map[xx, yy];
		}
		public override bool MouseDown(ScreenEventArgs args)
		{

			ITile tile = fromCanvas(args.X, args.Y);
			if (tile != null)
			{
				X = tile.X;
				Y = tile.Y;
				while (X < 0) X += Map.WIDTH;
				while (X >= Map.WIDTH) X -= Map.WIDTH;
			}

			else
			{
				tile = fromMinimap(args.X, args.Y);
				if (tile != null)
				{
					X = tile.X;
					Y = tile.Y;
					while (X < 0) X += Map.WIDTH;
					while (X >= Map.WIDTH) X -= Map.WIDTH;
				}
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

			Palette = Common.TopScreen.Palette;
		}
	}
}