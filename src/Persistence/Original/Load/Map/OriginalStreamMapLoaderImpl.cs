using System;
using System.Diagnostics;
using System.IO;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Services;
using CivOne.Tiles;

namespace CivOne.Persistence.Original.Load
{
	public class OriginalStreamMapLoaderImpl(string _filePath, int _randomSeed) : IStreamMapLoader
	{
		protected static readonly ILoggerService logger = LoggerProvider.GetLogger();

		protected IHutGeneratorService HutGeneratorService => MapServiceProvider.GetHutProvider(_randomSeed);
		protected ILandValueCalculatorService LandValueCalculator => MapServiceProvider.GetLandValueCalculator();

		protected ITileConverterService TileConverterService => MapServiceProvider.GetTileConverterService(_randomSeed);

		// TODO: Must be by DI
		private static Resources Resources = Resources.Instance;


		public IMap Load(Stream stream)
		{
			var tiles = LoadMap(_filePath ,stream);
			Map map = new(_randomSeed, tiles);

			return map;
		}


		protected void LoadMap(Bytemap bitmap, ITile[,] _tiles)
		{
			for (int x = 0; x < Map.WIDTH; x++)
				for (int y = 0; y < Map.HEIGHT; y++)
				{
					ITile tile;
					bool special = TileConverterService.HasExtraResourceOnTile(x, y);
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

		protected ITile[,] LoadMap(string filePath, Stream stream)
		{
			ITile[,] tiles = new ITile[Map.WIDTH, Map.HEIGHT];

			// Remove existing map if any and load new one
			using (Bytemap bitmap = Resources.Reset(filePath, stream).Bitmap)
			{
				LoadMap(bitmap, tiles);
				HutGeneratorService.PlaceHuts(tiles);
				LandValueCalculator.CalculateLandValue(tiles);

				// Load improvement layer
				for (int x = 0; x < Map.WIDTH; x++)
				{
					for (int y = 0; y < Map.HEIGHT; y++)
					{
						byte b = bitmap[x, y + (Map.HEIGHT * 2)];
						// 0x01 = CITY ?
						tiles[x, y].Irrigation = (b & 0x02) > 0;
						tiles[x, y].Mine = (b & 0x04) > 0;
						tiles[x, y].Road = (b & 0x08) > 0;
					}
				}
				// Load improvement layer 2
				for (int x = 0; x < Map.WIDTH; x++)
				{
					for (int y = 0; y < Map.HEIGHT; y++)
					{
						byte b = bitmap[x, y + (Map.HEIGHT * 3)];
						tiles[x, y].RailRoad = (b & 0x01) > 0;
					}
				}
				// Remove huts
				for (int x = 0; x < Map.WIDTH; x++)
				{
					for (int y = 0; y < Map.HEIGHT; y++)
					{
						if (!tiles[x, y].Hut) continue;
						byte b = bitmap[x + (Map.WIDTH * 2), y];
						tiles[x, y].Hut = b == 0;
					}
				}
			}

			logger.Log("Map: Ready");

			return tiles;
		}
	}
}