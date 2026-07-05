// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.IO;
using System.Threading.Tasks;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.Tiles;

namespace CivOne
{
	public partial class Map
	{
		private void LoadMap(Bytemap bitmap)
		{
			#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
			_tiles = new ITile[WIDTH, HEIGHT];
			#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
			
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					ITile tile;
					bool special = TileIsSpecial(x, y);
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
					_tiles[x, y] = tile;
				}
			}
		}
		
		public void LoadMap(string filename, int randomSeed)
		{
			UseDefaultMapSize();
			Log("Map: Loading {0} - Random seed: {1}", filename, randomSeed);
			_terrainMasterWord = randomSeed;
			
			using (Bytemap bitmap = _mapResourceProvider.GetPicture(filename).Bitmap)
			{
				#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
				_tiles = new ITile[WIDTH, HEIGHT];
				#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
				
				LoadMap(bitmap);
				PlaceHuts();
				CalculateLandValue();
				
				// Load improvement layer
				for (int x = 0; x < WIDTH; x++)
				{
					for (int y = 0; y < HEIGHT; y++)
					{
						byte b = bitmap[x, y + (HEIGHT * 2)];
						// 0x01 = CITY ?
						_tiles[x, y].Irrigation = (b & 0x02) > 0;
						_tiles[x, y].Mine = (b & 0x04) > 0;
						_tiles[x, y].Road = (b & 0x08) > 0;
					}
				}
				
				// Load improvement layer 2
				for (int x = 0; x < WIDTH; x++)
				{
					for (int y = 0; y < HEIGHT; y++)
					{
						byte b = bitmap[x, y + (HEIGHT * 3)];
						_tiles[x, y].RailRoad = (b & 0x01) > 0;
					}
				}
				
				// Remove huts
				for (int x = 0; x < WIDTH; x++)
				{
					for (int y = 0; y < HEIGHT; y++)
					{
						if (!_tiles[x, y].Hut) continue;
						byte b = bitmap[x + (WIDTH * 2), y];
						_tiles[x, y].Hut = (b == 0);
					}
				}
			}
			
			SetReady(true);
			Log("Map: Ready");
		}

		public ushort SaveMap(string filename)
		{
			Log($"Map: Saving {filename} - Random seed: {_terrainMasterWord}");

			Picture sp299 = _mapResourceProvider.GetPicture("SP299");
			using Bytemap bitmap = sp299.Bitmap;

			SaveTerrainLayer(bitmap);
			SaveImprovementLayer(bitmap);
			SaveImprovementLayer2(bitmap);
			SaveExploredLayer(bitmap);
			SaveAreaSegmentationLayer(bitmap);

			using Picture picture = new(bitmap, sp299.Palette);
			using PicFile picFile = new(picture);
			// fire-eggs 20190710 removing this allows JCivEd to load the .MAP file as a .PIC
			//	HasPalette256 = false
			_mapPersistenceService.WriteAllBytes(filename, picFile.GetBytes());
			return (ushort)_terrainMasterWord;
		}

		private void SaveTerrainLayer(Bytemap bitmap)
		{
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					bitmap[x, y] = _tiles[x, y].Type switch
					{
						Terrain.Forest => (byte)2,
						Terrain.Swamp => (byte)3,
						Terrain.Plains => (byte)6,
						Terrain.Tundra => (byte)7,
						Terrain.River => (byte)9,
						Terrain.Grassland1 or Terrain.Grassland2 => (byte)10,
						Terrain.Jungle => (byte)11,
						Terrain.Hills => (byte)12,
						Terrain.Mountains => (byte)13,
						Terrain.Desert => (byte)14,
						Terrain.Arctic => (byte)15,
						_ => (byte)1,
					};
				}
			}
		}

		private void SaveImprovementLayer(Bytemap bitmap)
		{
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					byte b = 0;
					if (_tiles[x, y].City != null) b |= 0x01;
					if (_tiles[x, y].Irrigation) b |= 0x02;
					if (_tiles[x, y].Mine) b |= 0x04;
					if (_tiles[x, y].Road) b |= 0x08;

					bitmap[x, y + (HEIGHT * 2)] = b;
					bitmap[x + (WIDTH * 1), y + (HEIGHT * 2)] = b; // Visibility layer
				}
			}
		}

		private void SaveImprovementLayer2(Bytemap bitmap)
		{
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					byte b = 0;
					if (_tiles[x, y].RailRoad) b |= 0x01;

					bitmap[x, y + (HEIGHT * 3)] = b;
					bitmap[x + (WIDTH * 1), y + (HEIGHT * 3)] = b; // Visibility layer
				}
			}
		}

		private void SaveExploredLayer(Bytemap bitmap)
		{
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					bitmap[x + (WIDTH * 2), y] = _tiles[x, y].Visited;
				}
			}
		}

		private void SaveAreaSegmentationLayer(Bytemap bitmap)
		{
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					// ContinentId is int internally; the original Civ1 MAP format stores it as a byte.
					// IDs above 255 are truncated — continent IDs are always recalculated on load anyway.
					bitmap[x, y + HEIGHT] = (byte)_tiles[x, y].ContinentId;
					bitmap[x + WIDTH, y + HEIGHT] = 0;
				}
			}
		}
		
		protected void RunEarthMapThread()
		{
			Log("Map: Loading MAP.PIC");
			
			using (Bytemap bitmap = _mapResourceProvider.GetPicture("MAP").Bitmap)
			{
				LoadMap(bitmap);
			}
			
			CreatePoles();
			PlaceHuts();
			CalculateLandValue();
			
			SetReady(true);
			Log("Map: Ready");
		}

		public void LoadEarthMapInThread()
		{
			if (Ready || _tiles != null)
			{
				Log("ERROR: Map is already load{0}/generat{0}", Ready ? "ed" : "ing");
				return;
			}

			UseDefaultMapSize();

			_landMass = null;
			_landMassValue = -1;
			_temperature = null;
			_temperatureValue = -1;
			_climate = null;
			_climateValue = -1;
			_age = null;
			_ageValue = -1;
			FixedStartPositions = true;

			TaskRunEarthMapGeneration();
		}
		
		protected virtual void TaskRunEarthMapGeneration()
		{
			Log("Map: Starting Earth map generation thread");
			Task.Run(RunEarthMapThread);
		}
	}
}