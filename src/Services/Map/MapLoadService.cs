// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.IO;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.Enums;
using CivOne.Tiles;

namespace CivOne.Services.Map
{
	public class MapLoadService : IMapLoadService
	{
		private static Resources Resources => Resources.Instance;

		public void Load(CivOne.Map map, string filename, int randomSeed)
		{
			ValidateBinaryMapDimensions(map);
			Log("Map: Loading {0} - Random seed: {1}", filename, randomSeed);
			map.TerrainMasterWordInternal = randomSeed;
			
			using (Bytemap bitmap = Resources[filename].Bitmap)
			{
				map.InitializeForLoadService();
				
				LoadTerrainLayer(map, bitmap);
				PlaceHuts(map);
				CalculateLandValue(map);
				
				LoadImprovementLayer(map, bitmap);
				LoadImprovementLayer2(map, bitmap);
				LoadHutRemovalLayer(map, bitmap);
			}
			
			map.Ready = true;
			Log("Map: Ready");
		}

		private void LoadTerrainLayer(CivOne.Map map, Bytemap bitmap)
		{
			for (int x = 0; x < map.Width; x++)
			for (int y = 0; y < map.Height; y++)
			{
				ITile tile;
				bool special = map.TileIsSpecialInternal(x, y);
				switch (bitmap[x, y])
				{
					case 2: tile = new Forest(x, y, special); break;
					case 3: tile = new Swamp(x, y, special); break;
					case 6: tile = new Plains(x, y, special); break;
					case 7: tile = new Tundra(x, y, special); break;
					case 9: tile = new River(x, y); break;
					case 10: tile = new Grassland(x, y); break;
					case 11: tile = new Jungle(x, y, special); break;
					case 12: tile = new Hills(x, y, special); break;
					case 13: tile = new Mountains(x, y, special); break;
					case 14: tile = new Desert(x, y, special); break;
					case 15: tile = new Arctic(x, y, special); break;
					default: tile = new Ocean(x, y, special); break;
				}
				map.SetTileInternal(x, y, tile);
			}
		}

		private void LoadImprovementLayer(CivOne.Map map, Bytemap bitmap)
		{
			for (int x = 0; x < map.Width; x++)
			for (int y = 0; y < map.Height; y++)
			{
				byte b = bitmap[x, y + (map.Height * 2)];
				// 0x01 = CITY ?
				map[x, y].Irrigation = (b & 0x02) > 0;
				map[x, y].Mine = (b & 0x04) > 0;
				map[x, y].Road = (b & 0x08) > 0;
			}
		}

		private void LoadImprovementLayer2(CivOne.Map map, Bytemap bitmap)
		{
			for (int x = 0; x < map.Width; x++)
			for (int y = 0; y < map.Height; y++)
			{
				byte b = bitmap[x, y + (map.Height * 3)];
				map[x, y].RailRoad = (b & 0x01) > 0;
			}
		}

		private void LoadHutRemovalLayer(CivOne.Map map, Bytemap bitmap)
		{
			for (int x = 0; x < map.Width; x++)
			for (int y = 0; y < map.Height; y++)
			{
				if (!map[x, y].Hut) continue;
				byte b = bitmap[x + (map.Width * 2), y];
				map[x, y].Hut = (b == 0);
			}
		}

		private void PlaceHuts(CivOne.Map map)
		{
			Log("Map: Placing goody huts");
			
			for (int y = 0; y < map.Height; y++)
			for (int x = 0; x < map.Width; x++)
			{
				if (map[x, y].Type == Terrain.Ocean) continue;
				map[x, y].Hut = TileHasHut(map, x, y);
			}
		}

		private bool TileHasHut(CivOne.Map map, int x, int y)
		{
			if (y < 2 || y > (map.Height - 3)) return false;
			return ModGrid(x, y) == ((x / 4) * 13 + (y / 4) * 11 + map.TerrainMasterWord + 8) % 32;
		}

		private void CalculateLandValue(CivOne.Map map)
		{
			Log("Map: Calculating land value");
			
			for (int y = 2; y < map.Height - 2; y++)
			for (int x = 2; x < map.Width - 2; x++)
			{
				map[x, y].LandValue = 0;
				
				if (!TileIsType(map[x, y], Terrain.Plains, Terrain.Grassland1, Terrain.Grassland2, Terrain.River)) continue;
				
				int landValue = 0;
				for (int yy = -2; yy <= 2; yy++)
				for (int xx = -2; xx <= 2; xx++)
				{
					if (Math.Abs(xx) == 2 && Math.Abs(yy) == 2) continue;
					
					int val = 0;
					
					ITile tile = map[x + xx, y + yy];
					if (tile.Special && TileIsType(tile, Terrain.Grassland1, Terrain.Grassland2, Terrain.River))
					{
						val += 2;
						if (tile.Type == Terrain.River)
							val += (new River()).LandScore;
						else
							val += (new Grassland()).LandScore;
					}
					else
					{
						val += tile.LandScore;
					}
					
					if (Math.Abs(xx) <= 1 && Math.Abs(yy) <= 1 && (xx != 0 || yy != 0)) val *= 2;
					
					if (xx == 0 && yy == -1) val *= 2;
					
					landValue += val;
				}
				
				if (!map[x, y].Special && TileIsType(map[x, y], Terrain.Grassland1, Terrain.River)) landValue -= 16;
				
				landValue -= 120;
				bool negative = (landValue < 0);
				landValue = Math.Abs(landValue);
				landValue /= 8;
				if (negative) landValue = 1 - landValue;
				
				if (landValue < 1) landValue = 1;
				if (landValue > 15) landValue = 15;
				
				landValue /= 2;
				landValue += 8;
				map[x, y].LandValue = (byte)landValue;
			}
		}

		private int ModGrid(int x, int y) => (x % 4) * 4 + (y % 4);

		private static void ValidateBinaryMapDimensions(CivOne.Map map)
		{
			ArgumentNullException.ThrowIfNull(map);

			if (map.Width == CivOne.Map.BinaryFormatWidth && map.Height == CivOne.Map.BinaryFormatHeight)
			{
				return;
			}

			throw new ArgumentException(
				$"Binary map format only supports {CivOne.Map.BinaryFormatWidth}x{CivOne.Map.BinaryFormatHeight}. Current map size is {map.Width}x{map.Height}.",
				nameof(map));
		}

		private static bool TileIsType(ITile tile, params Terrain[] terrain)
		{
			foreach (var t in terrain)
				if (tile.Type == t) return true;
			return false;
		}

		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);
	}
}
