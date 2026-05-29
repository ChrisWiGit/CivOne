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

namespace CivOne.Screens.GamePlayPanels
{
	internal partial class GameMap
	{
		/// <summary>
		/// Handles map panning controls while map view mode is active.
		/// </summary>
		private class GamePanMapDelegate(GameMap gameMap)
		{
			private readonly GameMap _gameMap = gameMap;

			public bool PanMap(int relX, int relY)
			{
				_gameMap._x += relX;
				while (_gameMap._x < 0)
				{
					_gameMap._x += Map.WIDTH;
				}

				while (_gameMap._x >= Map.WIDTH)
				{
					_gameMap._x -= Map.WIDTH;
				}

				_gameMap._y += relY;
				if (_gameMap._y < 0)
				{
					_gameMap._y = 0;
				}

				_gameMap._y = Math.Min(_gameMap._y, Math.Max(0, Map.HEIGHT - _gameMap._tilesY));
				_gameMap._update = true;
				_gameMap._fullRedraw = true;
				return true;
			}

			public bool KeyDownMapView(KeyboardEventArgs args)
			{
				if (args.KeyChar == 'C' && args.Modifier == KeyModifier.None)
				{
					return _gameMap.CenterOnActiveUnit();
				}

				switch (args.Key)
				{
					case Key.Left:
					case Key.NumPad4:
						return PanMap(-1, 0);
					case Key.Right:
					case Key.NumPad6:
						return PanMap(1, 0);
					case Key.Up:
					case Key.NumPad8:
						return PanMap(0, -1);
					case Key.Down:
					case Key.NumPad2:
						return PanMap(0, 1);
					case Key.Home:
					case Key.NumPad7:
						return PanMap(-1, -1);
					case Key.PageUp:
					case Key.NumPad9:
						return PanMap(1, -1);
					case Key.End:
					case Key.NumPad1:
						return PanMap(-1, 1);
					case Key.PageDown:
					case Key.NumPad3:
						return PanMap(1, 1);
				}

				return false;
			}
		}
	}
}
