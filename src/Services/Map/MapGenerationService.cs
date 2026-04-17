// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CivOne.Enums;
using CivOne.Screens;
using CivOne.Tasks;
using CivOne.Tiles;

namespace CivOne.Services.Map
{
	public class MapGenerationService : IMapGenerationService
	{
		public void Generate(CivOne.Map map, int landMass = 1, int temperature = 1, int climate = 1, int age = 1)
		{
			if (map.Ready || map.Tiles != null)
			{
				Log("ERROR: Map is already load{0}/generat{0}", (map.Ready ? "ed" : "ing"));
				return;
			}
			
			if (Settings.Instance.CustomMapSize)
			{
				CustomMapSize customMapSize = new CustomMapSize();
				customMapSize.Closed += (s, a) =>
				{
					Size mapSize = (s as CustomMapSize).MapSize;
					CivOne.Map.SetMapDimensions(mapSize.Width, mapSize.Height);

					map.LandMassInternal = landMass;
					map.TemperatureInternal = temperature;
					map.ClimateInternal = climate;
					map.AgeInternal = age;
					
					Task.Run(() => GenerateThread(map));
				};

				GameTask.Insert(Show.Screen(customMapSize));
				return;
			}
			
			map.LandMassInternal = landMass;
			map.TemperatureInternal = temperature;
			map.ClimateInternal = climate;
			map.AgeInternal = age;
			
			Task.Run(() => GenerateThread(map));
		}

		private void GenerateThread(CivOne.Map map)
		{
			Log("Generating map (Land Mass: {0}, Temperature: {1}, Climate: {2}, Age: {3})", 
				map.LandMassInternal, map.TemperatureInternal, map.ClimateInternal, map.AgeInternal);
			
			map.InitializeForGeneration();
			
			int[,] elevation = GenerateLandMass(map);
			int[,] latitude = TemperatureAdjustments(map);
			MergeElevationAndLatitude(map, elevation, latitude);
			ClimateAdjustments(map);
			AgeAdjustments(map);
			CreateRivers(map);
			
			CalculateContinentSize(map);
			CreatePoles(map);
			PlaceHuts(map);
			CalculateLandValue(map);
			
			map.Ready = true;
			Log("Map: Ready");
		}

		private bool[,] GenerateLandChunk(CivOne.Map map)
		{
			bool[,] stencil = new bool[map.Width, map.Height];

			int x = Common.Random.Next(4, map.Width - 4);
			int y = Common.Random.Next(8, map.Height - 8);
			int pathLength = Common.Random.Next(1, 64);

			for (int i = 0; i < pathLength; i++)
			{
				stencil[x, y] = true;
				stencil[x + 1, y] = true;
				stencil[x, y + 1] = true;
				switch (Common.Random.Next(4))
				{
					case 0: y--; break;
					case 1: x++; break;
					case 2: y++; break;
					default: x--; break;
				}

				if (x < 3 || y < 3 || x > (map.Width - 4) || y > (map.Height - 5)) break;
			}

			return stencil;
		}

		private int[,] GenerateLandMass(CivOne.Map map)
		{
			Log("Map: Stage 1 - Generate land mass");

			int[,] elevation = new int[map.Width, map.Height];
			int landMassSize = (int)(((map.Width * map.Height) / 12.5) * (map.LandMassInternal + 2));

			while ((from int tile in elevation where tile > 0 select 1).Sum() < landMassSize)
			{
				bool[,] chunk = GenerateLandChunk(map);
				for (int y = 0; y < map.Height; y++)
					for (int x = 0; x < map.Width; x++)
					{
						if (chunk[x, y]) elevation[x, y]++;
					}
			}

			for (int y = 0; y < (map.Height - 1); y++)
				for (int x = 0; x < (map.Width - 1); x++)
				{
					if ((elevation[x, y] > 0 && elevation[x + 1, y + 1] > 0) && (elevation[x + 1, y] == 0 && elevation[x, y + 1] == 0))
					{
						elevation[x + 1, y]++;
						elevation[x, y + 1]++;
					}
					else if ((elevation[x, y] == 0 && elevation[x + 1, y + 1] == 0) && (elevation[x + 1, y] > 0 && elevation[x, y + 1] > 0))
					{
						elevation[x + 1, y + 1]++;
					}
				}

			return elevation;
		}

		private int[,] TemperatureAdjustments(CivOne.Map map)
		{
			Log("Map: Stage 2 - Temperature adjustments");

			int[,] latitude = new int[map.Width, map.Height];

			for (int y = 0; y < map.Height; y++)
				for (int x = 0; x < map.Width; x++)
				{
					int l = (int)(((float)y / map.Height) * 50) - 29;
					l += Common.Random.Next(7);
					if (l < 0) l = -l;
					l += 1 - map.TemperatureInternal;

					l = (l / 6) + 1;

					switch (l)
					{
						case 0:
						case 1: latitude[x, y] = 0; break;
						case 2:
						case 3: latitude[x, y] = 1; break;
						case 4:
						case 5: latitude[x, y] = 2; break;
						case 6:
						default: latitude[x, y] = 3; break;
					}
				}

			return latitude;
		}

		private void MergeElevationAndLatitude(CivOne.Map map, int[,] elevation, int[,] latitude)
		{
			Log("Map: Stage 3 - Merge elevation and latitude into the map");

			for (int y = 0; y < map.Height; y++)
				for (int x = 0; x < map.Width; x++)
				{
					bool special = map.TileIsSpecialInternal(x, y);
					switch (elevation[x, y])
					{
						case 0: map.SetTileInternal(x, y, new Ocean(x, y, special)); break;
						case 1:
						{
							switch (latitude[x, y])
							{
								case 0: map.SetTileInternal(x, y, new Desert(x, y, special)); break;
								case 1: map.SetTileInternal(x, y, new Plains(x, y, special)); break;
								case 2: map.SetTileInternal(x, y, new Tundra(x, y, special)); break;
								case 3: map.SetTileInternal(x, y, new Arctic(x, y, special)); break;
							}
						}
						break;
						case 2: map.SetTileInternal(x, y, new Hills(x, y, special)); break;
						default: map.SetTileInternal(x, y, new Mountains(x, y, special)); break;
					}
				}
		}

		private void ClimateAdjustments(CivOne.Map map)
		{
			Log("Map: Stage 4 - Climate adjustments");

			int wetness, latitude;

			for (int y = 0; y < map.Height; y++)
			{
				int yy = (int)(((float)y / map.Height) * 50);

				wetness = 0;
				latitude = Math.Abs(25 - yy);

				for (int x = 0; x < map.Width; x++)
				{
					if (map[x, y].Type == Terrain.Ocean)
					{
						int wy = latitude - 12;
						if (wy < 0) wy = -wy;
						wy += (map.ClimateInternal * 4);

						if (wy > wetness) wetness++;
					}
					else if (wetness > 0)
					{
						bool special = map.TileIsSpecialInternal(x, y);
						int rainfall = Common.Random.Next(7 - (map.ClimateInternal * 2));
						wetness -= rainfall;

						switch (map[x, y].Type)
						{
							case Terrain.Plains: map.SetTileInternal(x, y, new Grassland(x, y)); break;
							case Terrain.Tundra: map.SetTileInternal(x, y, new Arctic(x, y, special)); break;
							case Terrain.Hills: map.SetTileInternal(x, y, new Forest(x, y, special)); break;
							case Terrain.Desert: map.SetTileInternal(x, y, new Plains(x, y, special)); break;
							case Terrain.Mountains: wetness -= 3; break;
						}
					}
				}

				wetness = 0;
				latitude = Math.Abs(25 - yy);

				for (int x = map.Width - 1; x >= 0; x--)
				{
					if (map[x, y].Type == Terrain.Ocean)
					{
						int wy = (latitude / 2) + map.ClimateInternal;
						if (wy > wetness) wetness++;
					}
					else if (wetness > 0)
					{
						bool special = map.TileIsSpecialInternal(x, y);
						int rainfall = Common.Random.Next(7 - (map.ClimateInternal * 2));
						wetness -= rainfall;

						switch (map[x, y].Type)
						{
							case Terrain.Swamp: map.SetTileInternal(x, y, new Forest(x, y, special)); break;
							case Terrain.Plains: new Grassland(x, y); break;
							case Terrain.Grassland1:
							case Terrain.Grassland2: map.SetTileInternal(x, y, new Jungle(x, y, special)); break;
							case Terrain.Hills: map.SetTileInternal(x, y, new Forest(x, y, special)); break;
							case Terrain.Mountains: map.SetTileInternal(x, y, new Forest(x, y, special)); wetness -= 3; break;
							case Terrain.Desert: map.SetTileInternal(x, y, new Plains(x, y, special)); break;
						}
					}
				}
			}
		}

		private void AgeAdjustments(CivOne.Map map)
		{
			Log("Map: Stage 5 - Age adjustments");

			int x = 0;
			int y = 0;
			int ageRepeat = (int)(((float)800 * (1 + map.AgeInternal) / (CivOne.Map.BinaryFormatWidth * CivOne.Map.BinaryFormatHeight)) * (map.Width * map.Height));
			for (int i = 0; i < ageRepeat; i++)
			{
				if (i % 2 == 0)
				{
					x = Common.Random.Next(map.Width);
					y = Common.Random.Next(map.Height);
				}
				else
				{
					switch (Common.Random.Next(8))
					{
						case 0: { x--; y--; break; }
						case 1: { y--; break; }
						case 2: { x++; y--; break; }
						case 3: { x--; break; }
						case 4: { x++; break; }
						case 5: { x--; y++; break; }
						case 6: { y++; break; }
						default: { x++; y++; break; }
					}
					if (x < 0) x = 1;
					if (y < 0) y = 1;
					if (x >= map.Width) x = map.Width - 2;
					if (y >= map.Height) y = map.Height - 2;
				}

				bool special = map.TileIsSpecialInternal(x, y);
				switch (map[x, y].Type)
				{
					case Terrain.Forest: map.SetTileInternal(x, y, new Jungle(x, y, special)); break;
					case Terrain.Swamp: map.SetTileInternal(x, y, new Grassland(x, y)); break;
					case Terrain.Plains: map.SetTileInternal(x, y, new Hills(x, y, special)); break;
					case Terrain.Tundra: map.SetTileInternal(x, y, new Hills(x, y, special)); break;
					case Terrain.River: map.SetTileInternal(x, y, new Forest(x, y, special)); break;
					case Terrain.Grassland1:
					case Terrain.Grassland2: map.SetTileInternal(x, y, new Forest(x, y, special)); break;
					case Terrain.Jungle: map.SetTileInternal(x, y, new Swamp(x, y, special)); break;
					case Terrain.Hills: map.SetTileInternal(x, y, new Mountains(x, y, special)); break;
					case Terrain.Mountains:
						if ((x == 0 || map[x - 1, y - 1].Type != Terrain.Ocean) &&
							(y == 0 || map[x + 1, y - 1].Type != Terrain.Ocean) &&
							(x == (map.Width - 1) || map[x + 1, y + 1].Type != Terrain.Ocean) &&
							(y == (map.Height - 1) || map[x - 1, y + 1].Type != Terrain.Ocean))
							map.SetTileInternal(x, y, new Ocean(x, y, special));
						break;
					case Terrain.Desert: map.SetTileInternal(x, y, new Plains(x, y, special)); break;
					case Terrain.Arctic: map.SetTileInternal(x, y, new Mountains(x, y, special)); break;
				}
			}
		}

		private void CreateRivers(CivOne.Map map)
		{
			Log("Map: Stage 6 - Create rivers");

			int rivers = 0;
			for (int i = 0; i < 256 && rivers < ((map.ClimateInternal + map.LandMassInternal) * 2) + 6; i++)
			{
				ITile[,] tilesBackup = (ITile[,])map.Tiles.Clone();

				int riverLength = 0;
				int varA = Common.Random.Next(4) * 2;
				bool nearOcean = false;

				ITile tile = null;
				while (tile == null)
				{
					int x = Common.Random.Next(map.Width);
					int y = Common.Random.Next(map.Height);
					if (map[x, y].Type == Terrain.Hills) tile = map[x, y];
				}
				do
				{
					map.SetTileInternal(tile.X, tile.Y, new River(tile.X, tile.Y));
					int varB = varA;
					int varC = Common.Random.Next(2);
					varA = (((varC - riverLength % 2) * 2 + varA) & 0x07);
					varB = 7 - varB;

					riverLength++;

					nearOcean = NearOcean(map, tile.X, tile.Y);
					switch (varA)
					{
						case 0:
						case 1: tile = map[tile.X, tile.Y - 1]; break;
						case 2:
						case 3: tile = map[tile.X + 1, tile.Y]; break;
						case 4:
						case 5: tile = map[tile.X, tile.Y + 1]; break;
						case 6:
						case 7: tile = map[tile.X - 1, tile.Y]; break;
					}
				}
				while (!nearOcean && (tile.GetType() != typeof(Ocean) && tile.GetType() != typeof(River) && tile.GetType() != typeof(Mountains)));

				if ((nearOcean || tile.Type == Terrain.River) && riverLength > 5)
				{
					rivers++;
					ITile[,] mapPart = map[tile.X - 3, tile.Y - 3, 7, 7];
					for (int x = 0; x < 7; x++)
						for (int y = 0; y < 7; y++)
						{
							if (mapPart[x, y] == null) continue;
							int xx = mapPart[x, y].X, yy = mapPart[x, y].Y;
							if (map[xx, yy].Type == Terrain.Forest)
								map.SetTileInternal(xx, yy, new Jungle(xx, yy, map.TileIsSpecialInternal(x, y)));
						}
				}
				else
				{
					// Restore from backup
					for (int y = 0; y < map.Height; y++)
						for (int x = 0; x < map.Width; x++)
							map.SetTileInternal(x, y, tilesBackup[x, y]);
				}
			}
		}

		private bool NearOcean(CivOne.Map map, int x, int y)
		{
			for (int relY = -1; relY <= 1; relY++)
			for (int relX = -1; relX <= 1; relX++)
			{
				if (Math.Abs(relX) == Math.Abs(relY)) continue;
				if (map[x + relX, y + relY] is Ocean) return true;
			}
			return false;
		}

		private void CalculateContinentSize(CivOne.Map map)
		{
			Log("Map: Calculate continent/ocean sizes and give continents a number in size order");

			for (int y = 0; y < map.Height; y++)
				for (int x = 0; x < map.Width; x++)
					map[x, y].ContinentId = 0;

			// TODO: Implement continent calculation if needed
		}

		private void CreatePoles(CivOne.Map map)
		{
			Log("Map: Creating poles");

			int poleHeight = Math.Min(4, Math.Max(1, map.Height / 2));
			int topMaxY = poleHeight - 1;
			int bottomMinY = map.Height - poleHeight;

			for (int x = 0; x < map.Width; x++)
			{
				map.SetTileInternal(x, 0, new Arctic(x, 0, false));
				map.SetTileInternal(x, map.Height - 1, new Arctic(x, map.Height - 1, false));
			}

			for (int i = 0; i < (map.Width / 4); i++)
			for (int y = 0; y < map.Height; y++)
			{
				if (y > topMaxY && y < bottomMinY)
				{
					continue;
				}

				int x = Common.Random.Next(map.Width);
				map.SetTileInternal(x, y, new Tundra(x, y, false));
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

		private static bool TileIsType(ITile tile, params Terrain[] terrain)
		{
			foreach (var t in terrain)
				if (tile.Type == t) return true;
			return false;
		}

		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);
	}
}
