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
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.IO;
using CivOne.Tiles;
using CivOne.Units;

using static CivOne.Enums.Direction;

namespace CivOne.Screens
{
	internal class DestroyUnit : BaseScreen
	{
		private struct RenderTile
		{
			public bool Visible;
			public int X, Y;
			public ITile Tile;
			public IBitmap Image => Tile.ToBitmap();
			public Point Position => new Point(X * 16, Y * 16);
		}

		private readonly DestroyAnimation _animation;

		private const int NOISE_COUNT = 8;

		private readonly IUnit _unit;
		private readonly bool _stack;
		private int _x, _y;
		
		private int _noiseCounter = NOISE_COUNT + 2;
		private readonly byte[,] _noiseMap;

		private Picture _gameMap, _overlay = null;

		private IBitmap[] _destroySprites = null;
		
		private IEnumerable<RenderTile> RenderTiles
		{
			get
			{
				for (int x = 0; x < 15; x++)
				for (int y = 0; y < 12; y++)
				{
					int tx = _x + x;
					int ty = _y + y;
					while (tx >= Map.WIDTH) tx -= Map.WIDTH;
					
					if (ty < 0 || ty >= Map.HEIGHT) continue;

					yield return new RenderTile
					{
						Visible = Human.Visible(tx, ty),
						X = x,
						Y = y,
						Tile = Map[tx, ty]
					};
				}
			}
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			int cx = Settings.RightSideBar ? 0 : 80;
			int cy = 8;

			if (_overlay == null || _animation == DestroyAnimation.Sprites)
			{
				_overlay = new Picture(Bitmap, Palette);

				int xx = _unit.X - _x;
				int yy = _unit.Y - _y;
				while (xx < 0) xx += Map.WIDTH;
				while (xx >= Map.WIDTH) xx -= Map.WIDTH;

				_overlay.AddLayer(_unit.ToBitmap(), cx + (xx * 16), cy + (yy * 16));
				if (_unit.Tile.Units.Length > 1 && !_unit.Tile.Fortress && _unit.Tile.City == null)
					_overlay.AddLayer(_unit.ToBitmap(), cx + (xx * 16) - 1, cy + (yy * 16) - 1);
				
				if (_animation == DestroyAnimation.Sprites)
				{
					int step = 8 - _noiseCounter--;
					if (step >= 0 && step < 8)
					{
						_overlay.AddLayer(_destroySprites[step], cx + (xx * 16), cy + (yy * 16));
					}
				}
			}

			if (_animation == DestroyAnimation.Noise)
			{
				_overlay.ApplyNoise(_noiseMap, --_noiseCounter);
			}

			this.AddLayer(_gameMap, cx, cy)
				.AddLayer(_overlay, 0, 0);

			if (_noiseCounter == 0)
			{
				IUnit[] units;
				if (_unit.Tile.Units.Length > 1 && _unit.Tile.City == null && !_unit.Tile.Fortress && _stack)
				{
					units = _unit.Tile.Units;
				}
				else
				{
					units = new IUnit[] { _unit };
				}
				foreach (IUnit unit in units)
					Game.DisbandUnit(unit);
				Common.GamePlay.RefreshMap();
				// CW: Okay, this is wild.
				// If a city/unit is attacked and destroyed, its icon is displayed right after the destruction animation.
				// When a city defended itself successfully it looks like as if the unit is within the city.
				// I think this is a race condition, when drawing the unit icon another time right after the destruction animation.
				// It helped to increase the gameTick by 1, so that inner workings work differently.
				// It may break again in the future.
				uint doNotDrawUnitAfterDestruction = gameTick + 1;
				Common.GamePlay.Update(doNotDrawUnitAfterDestruction);
				Destroy();
			}

			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			return false;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			return false;
		}

		private Picture GameMap
		{
			get
			{
				Picture output = new Picture(240, 192);

				RenderTile[] renderTiles = RenderTiles.ToArray();
				foreach (RenderTile t in renderTiles)
				{
					if (!Settings.RevealWorld && !t.Visible)
					{
						output.FillRectangle(t.X * 16, t.Y * 16, 16, 16, 5);
						continue;
					}
					output.AddLayer(t.Image, t.Position);
					if (Settings.RevealWorld) continue;
					
					if (!Human.Visible(t.Tile, West)) output.AddLayer(MapTile.Fog[West], t.Position);
					if (!Human.Visible(t.Tile, North)) output.AddLayer(MapTile.Fog[North], t.Position);
					if (!Human.Visible(t.Tile, East)) output.AddLayer(MapTile.Fog[East], t.Position);
					if (!Human.Visible(t.Tile, South)) output.AddLayer(MapTile.Fog[South], t.Position);
				}

				foreach (RenderTile t in renderTiles)
				{
					if (!Settings.RevealWorld && !t.Visible) continue;

					if (t.Tile.City != null) continue;
					
					if (_unit != Game.ActiveUnit && t.Tile.Units.Any(x => x == _unit))
					{
						// Unit is attacked, it is not in a city or fortress, destroy them all
						if (t.Tile.City == null && !t.Tile.Fortress) continue;
					}

					IUnit[] units = t.Tile.Units.Where(u => u != _unit).ToArray();
					if (t.Tile.Type == Terrain.Ocean)
					{
						// Always show naval units first at sea
						units = units.OrderBy(u => (u.Class == UnitClass.Water) ? 1 : 0).ToArray();
					}
					if (units.Length == 0) continue;
					
					IUnit drawUnit = units.FirstOrDefault(u => u == _unit);
					drawUnit = units[0];

					if (t.Tile.IsOcean && drawUnit.Class != UnitClass.Water && drawUnit.Sentry)
					{
						// Do not draw sentried land units at sea
						continue;
					}
					
					output.AddLayer(drawUnit.ToBitmap(), t.Position);
					if (units.Length == 1) continue;
					output.AddLayer(drawUnit.ToBitmap(), t.Position.X - 1, t.Position.Y - 1);
				}

				foreach (RenderTile t in renderTiles.Reverse())
				{
					if (!Settings.RevealWorld && !t.Visible) continue;

					City city = t.Tile.City;
					if (city == null) continue;
					
					output.AddLayer(Icons.City(city), t.Position);
					
					if (t.Y == 11) continue;
					int labelX = (t.X == 0) ? t.Position.X : t.Position.X - 8;
					int labelY = t.Position.Y + 16;
					output.DrawText(city.Name, 0, 5, labelX, labelY + 1, TextAlign.Left);
					output.DrawText(city.Name, 0, 11, labelX, labelY, TextAlign.Left);
				}

				return output;
			}
		}

		internal DestroyUnit(IUnit unit, bool stack)
		{
			_unit = unit;
			_stack = stack;

			_x = Common.GamePlay.X;
			_y = Common.GamePlay.Y;

			Palette = Common.DefaultPalette;
			_gameMap = GameMap;
			_animation = Settings.DestroyAnimation;
			if (!Resources.Exists("SP257"))
				_animation = DestroyAnimation.Noise;

			switch (_animation)
			{
				case DestroyAnimation.Sprites:
					_destroySprites = new IBitmap[8];
					for (int i = 0; i < 8; i++)
					{
						_destroySprites[i] = Resources["SP257"][16 * i, 96, 16, 16] .ColourReplace(9, 0);
					}
					break;
				case DestroyAnimation.Noise:
					_noiseMap = new byte[320, 200];
					for (int x = 0; x < 320; x++)
					for (int y = 0; y < 200; y++)
					{
						_noiseMap[x, y] = (byte)Common.Random.Next(1, NOISE_COUNT);
					}
					break;
			}
		}
	}
}