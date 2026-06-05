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
using CivOne.Services.Random;
using CivOne.Tiles;
using CivOne.Units;
using static System.Diagnostics.Debug;

using static CivOne.Enums.Direction;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class DestroyUnit : BaseScreen
	{
		private interface IAnimation
		{
			bool IsFinished { get; }
			bool RebuildOverlayEachFrame { get; }
			void DrawAnimation(Picture overlay, int cx, int cy);
		}

		private sealed class SpriteAnimation : IAnimation
		{
			private readonly IUnit _unit;
			private readonly int _x;
			private readonly int _y;
			private readonly IBitmap[] _destroySprites;
			private int _frameCounter = NOISE_COUNT + SPRITE_DELAY_FRAMES;

			public bool IsFinished => _frameCounter == 0;
			public bool RebuildOverlayEachFrame => true;

			internal SpriteAnimation(IUnit unit, int x, int y, IBitmap[] destroySprites)
			{
				_unit = unit;
				_x = x;
				_y = y;
				_destroySprites = destroySprites;
			}

			public void DrawAnimation(Picture overlay, int cx, int cy)
			{
				(int xx, int yy) = GetUnitOffset(_unit, _x, _y);
				overlay.AddLayer(_unit.ToBitmap(), cx + (xx * 16), cy + (yy * 16));
				if (_unit.Tile.Units.Length > 1 && !_unit.Tile.Fortress && _unit.Tile.City == null)
				{
					overlay.AddLayer(_unit.ToBitmap(), cx + (xx * 16) - 1, cy + (yy * 16) - 1);
				}

				if (IsFinished)
				{
					return;
				}

				int frameIndex = NOISE_COUNT + SPRITE_DELAY_FRAMES - _frameCounter;
				if (frameIndex >= SPRITE_DELAY_FRAMES)
				{
					int spriteIndex = frameIndex - SPRITE_DELAY_FRAMES;
					if (spriteIndex < NOISE_COUNT)
					{
						overlay.AddLayer(_destroySprites[spriteIndex], cx + (xx * 16), cy + (yy * 16));
					}
				}

				_frameCounter--;
			}
		}

		private sealed class NoiseAnimation : IAnimation
		{
			private readonly IUnit _unit;
			private readonly int _x;
			private readonly int _y;
			private readonly byte[,] _noiseMap;
			private int _frameCounter = NOISE_COUNT + SPRITE_DELAY_FRAMES;

			public bool IsFinished => _frameCounter == 0;
			public bool RebuildOverlayEachFrame => false;

			internal NoiseAnimation(IUnit unit, int x, int y, IRandomService random)
			{
				_unit = unit;
				_x = x;
				_y = y;
				_noiseMap = new byte[320, 200];
				for (int nx = 0; nx < 320; nx++)
					for (int ny = 0; ny < 200; ny++)
					{
						_noiseMap[nx, ny] = (byte)random.Next(1, NOISE_COUNT + 1);
					}
			}

			public void DrawAnimation(Picture overlay, int cx, int cy)
			{
				if (_frameCounter == NOISE_COUNT + SPRITE_DELAY_FRAMES)
				{
					(int xx, int yy) = GetUnitOffset(_unit, _x, _y);
					overlay.AddLayer(_unit.ToBitmap(), cx + (xx * 16), cy + (yy * 16));
					if (_unit.Tile.Units.Length > 1 && !_unit.Tile.Fortress && _unit.Tile.City == null)
					{
						overlay.AddLayer(_unit.ToBitmap(), cx + (xx * 16) - 1, cy + (yy * 16) - 1);
					}
				}

				if (IsFinished)
				{
					return;
				}

				overlay.ApplyNoise(_noiseMap, --_frameCounter);
			}
		}

		private readonly struct RenderTile
		{
			public bool Visible { get; }
			public int X { get; }
			public int Y { get; }
			public ITile Tile { get; }

			public IBitmap Image => Tile.ToBitmap();
			public Point Position => new(X * 16, Y * 16);

			public RenderTile(bool visible, int x, int y, ITile tile)
			{
				Visible = visible;
				X = x;
				Y = y;
				Tile = tile;
			}
		}

		private readonly IAnimation _animation;

		private const int NOISE_COUNT = 8;
		private const int SPRITE_DELAY_FRAMES = 2;

		private readonly IUnit _unit;
		private readonly bool _stack;
		private readonly int _x, _y;

		private readonly Picture _gameMap;
		private Picture? _overlay;

		private readonly GamePlay _gamePlayDI;

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

						yield return new RenderTile(
							visible: Human.Visible(tx, ty),
							x,
							y,
							tile: Map[tx, ty]);
					}
			}
		}

		protected override bool HasUpdate(uint gameTick)
		{
			int cx = Settings.RightSideBar ? 0 : 80;
			int cy = 8;

			if (_overlay == null || _animation.RebuildOverlayEachFrame)
			{
				_overlay = new Picture(Bitmap, Palette);
			}

			_animation.DrawAnimation(_overlay, cx, cy);

			this.AddLayer(_gameMap, cx, cy)
				.AddLayer(_overlay, 0, 0);

			if (_animation.IsFinished)
			{
				DrawDestructionResult(gameTick);
			}

			return true;
		}

		private void DrawDestructionResult(uint gameTick)
		{
			IUnit[] units;
			if (_unit.Tile.Units.Length > 1 && _unit.Tile.City == null && !_unit.Tile.Fortress && _stack)
			{
				units = _unit.Tile.Units;
			}
			else
			{
				units = [_unit];
			}
			foreach (IUnit unit in units)
				Game.DisbandUnit(unit);

			_gamePlayDI.RefreshMap();
			// CW: Okay, this is wild.
			// If a city/unit is attacked and destroyed, its icon is displayed right after the destruction animation.
			// When a city defended itself successfully it looks like as if the unit is within the city.
			// I think this is a race condition, when drawing the unit icon another time right after the destruction animation.
			// It helped to increase the gameTick by 1, so that inner workings work differently.
			// It may break again in the future.
			uint doNotDrawUnitAfterDestruction = gameTick + 1;

			_gamePlayDI.Update(doNotDrawUnitAfterDestruction);

			Destroy();
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
				Picture output = new(240, 192);

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
						units = [.. units.OrderBy(u => (u.Class == UnitClass.Water) ? 1 : 0)];
					}

					if (units.Length == 0) continue;

					IUnit drawUnit = units[0];

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
			Assert(Common.GamePlay != null, "GamePlay is null in DestroyUnit constructor");
			_gamePlayDI = Common.GamePlay!;

			IRandomService random = RandomServiceFactory.Create();

			_unit = unit;
			_stack = stack;

			_x = _gamePlayDI.X;
			_y = _gamePlayDI.Y;

			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			_gameMap = GameMap;
			DestroyAnimation animationType = Settings.DestroyAnimation;
			if (!Resources.Exists("SP257"))
			{
				animationType = DestroyAnimation.Noise;
			}

			switch (animationType)
			{
				case DestroyAnimation.Sprites:
					IBitmap[] destroySprites = new IBitmap[8];
					for (int i = 0; i < 8; i++)
					{
						destroySprites[i] = Resources["SP257"][16 * i, 96, 16, 16].ColourReplace(9, 0);
					}
					_animation = new SpriteAnimation(_unit, _x, _y, destroySprites);
					break;
				case DestroyAnimation.Noise:
					_animation = new NoiseAnimation(_unit, _x, _y, random);
					break;
				default:
					_animation = new NoiseAnimation(_unit, _x, _y, random);
					break;
			}
		}

		private static (int X, int Y) GetUnitOffset(IUnit unit, int x, int y)
		{
			int xx = unit.X - x;
			int yy = unit.Y - y;

			while (xx < 0) xx += Map.WIDTH;
			while (xx >= Map.WIDTH) xx -= Map.WIDTH;

			return (xx, yy);
		}
	}
}