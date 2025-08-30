using System;
using System.IO;

namespace CivOne.Persistence.Original.Load
{
	public class OriginalMapLoaderImpl(IMapFactory mapFactory) : IMapLoader
	{
		public IMap Load(Stream stream)
		{
			// Implementiere das Laden der Karte aus dem Stream
			throw new NotImplementedException();
		}


		private void LoadMap(Bytemap bitmap)
		{
			_tiles = new ITile[WIDTH, HEIGHT];
			
			for (int x = 0; x < WIDTH; x++)
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
		
		public void LoadMap(string filename, int randomSeed)
		{
			Log("Map: Loading {0} - Random seed: {1}", filename, randomSeed);
			_terrainMasterWord = randomSeed;
			
			using (Bytemap bitmap = Resources[filename].Bitmap)
			{
				_tiles = new ITile[WIDTH, HEIGHT];
				
				LoadMap(bitmap);
				// PlaceHuts();
				// CalculateLandValue();
				
				// Load improvement layer
				for (int x = 0; x < WIDTH; x++)
				for (int y = 0; y < HEIGHT; y++)
				{
					byte b = bitmap[x, y + (HEIGHT * 2)];
					// 0x01 = CITY ?
					_tiles[x, y].Irrigation = (b & 0x02) > 0;
					_tiles[x, y].Mine = (b & 0x04) > 0;
					_tiles[x, y].Road = (b & 0x08) > 0;
				}
				
				// Load improvement layer 2
				for (int x = 0; x < WIDTH; x++)
				for (int y = 0; y < HEIGHT; y++)
				{
					byte b = bitmap[x, y + (HEIGHT * 3)];
					_tiles[x, y].RailRoad = (b & 0x01) > 0;
				}
				
				// Remove huts
				for (int x = 0; x < WIDTH; x++)
				for (int y = 0; y < HEIGHT; y++)
				{
					if (!_tiles[x, y].Hut) continue;
					byte b = bitmap[x + (WIDTH * 2), y];
					_tiles[x, y].Hut = (b == 0);
				}
			}
			
			Ready = true;
			Log("Map: Ready");
		}
	}
}