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
using CivOne.Tiles;

namespace CivOne
{
	/// <summary>
	/// Contains map editing related methods used by the GameMap and GamePlay classes.
	/// This can only be in a partial class of Map, since it needs access to internal map methods and properties.
	/// But it only exists for terrain editing related methods.
	/// Do not use it for game logic.
	/// </summary>
	public partial class Map
	{
		internal static int EditorWrapX(int x)
		{
			while (x < 0)
			{
				x += WIDTH;
			}

			while (x >= WIDTH)
			{
				x -= WIDTH;
			}

			return x;
		}

		internal static int EditorClampY(int y)
		{
			if (y < 0)
			{
				return 0;
			}

			if (y >= HEIGHT)
			{
				return HEIGHT - 1;
			}

			return y;
		}

		private static ITile CreateEditorTile(int x, int y, Terrain type, bool special)
		{
			return type switch
			{
				Terrain.Forest => new Forest(x, y, special),
				Terrain.Swamp => new Swamp(x, y, special),
				Terrain.Plains => new Plains(x, y, special),
				Terrain.Tundra => new Tundra(x, y, special),
				Terrain.River => new River(x, y),
				Terrain.Grassland1 => new Grassland(x, y),
				Terrain.Grassland2 => new Grassland(x, y),
				Terrain.Jungle => new Jungle(x, y, special),
				Terrain.Hills => new Hills(x, y, special),
				Terrain.Mountains => new Mountains(x, y, special),
				Terrain.Desert => new Desert(x, y, special),
				Terrain.Arctic => new Arctic(x, y, special),
				Terrain.Ocean => new Ocean(x, y, special),
				_ => throw new ArgumentException($"Invalid terrain type: {type}")
			};
		}

		internal void EditorSetTerrain(int x, int y, Terrain type)
		{
			x = EditorWrapX(x);
			y = EditorClampY(y);

			ITile oldTile = _tiles[x, y];
			if (oldTile == null)
			{
				return;
			}

			bool special = TileIsSpecial(x, y);
			int continentId = oldTile.ContinentId;
			byte landValue = oldTile.LandValue;
			byte visited = oldTile.Visited;
			bool road = oldTile.Road;
			bool railRoad = oldTile.RailRoad;
			bool irrigation = oldTile.Irrigation;
			bool mine = oldTile.Mine;
			bool fortress = oldTile.Fortress;
			bool pollution = oldTile.Pollution;
			bool hut = oldTile.Hut;

			ITile newTile = CreateEditorTile(x, y, type, special);
			newTile.ContinentId = continentId;
			newTile.LandValue = landValue;
			newTile.Road = road;
			newTile.RailRoad = railRoad;
			newTile.Irrigation = irrigation;
			newTile.Mine = mine;
			newTile.Fortress = fortress;
			newTile.Pollution = pollution;
			newTile.Hut = hut;
			for (int i = 0; i < 8; i++)
			{
				if ((visited & (1 << i)) != 0)
				{
					newTile.Visit((byte)i);
				}
			}

			_tiles[x, y] = newTile;
		}

		internal void EditorToggleHut(int x, int y)
		{
			x = EditorWrapX(x);
			y = EditorClampY(y);
			ITile tile = _tiles[x, y];
			if (tile == null)
			{
				return;
			}

			tile.Hut = !tile.Hut;
		}
	}
}
