using CivOne.Tiles;

namespace CivOne.Services
{
	public class HutGeneratorServiceImpl(int _randomSeed) : IHutGeneratorService
	{
		private readonly ILoggerService logger = LoggerProvider.GetLogger();
		private readonly ITileConverterService _tileConverterService = MapServiceProvider.GetTileConverterService(_randomSeed);

		private int LINEAR_BLOCK_SIZE => _tileConverterService.LINEAR_BLOCK_SIZE;

		public void PlaceHuts(ITile[,] _tiles)
		{
			logger.Log("Map: Placing goody huts");

			for (int y = 0; y < Map.HEIGHT; y++)
			{
				for (int x = 0; x < Map.WIDTH; x++)
				{
					if (_tiles[x, y].Type == Enums.Terrain.Ocean)
					{
						continue;
					}
					_tiles[x, y].Hut = TileHasHut(x, y);
				}
			}
		}

		private bool TileHasHut(int x, int y)
		{
			bool isAntarctica = (y < 2) || (y > (Map.HEIGHT - 3));
			if (isAntarctica)
			{
				return false;
			}

			int pseudoRandomXFactor = 13;
			int pseudoRandomYFactor = 11;

			int blockX = x / LINEAR_BLOCK_SIZE;
			int blockY = y / LINEAR_BLOCK_SIZE;
			int weightedBlockX = blockX * pseudoRandomXFactor;
			int weightedBlockY = blockY * pseudoRandomYFactor;
			int offsetY = 8;

			int blockIndex = (weightedBlockX + weightedBlockY + _randomSeed + offsetY) % (LINEAR_BLOCK_SIZE * LINEAR_BLOCK_SIZE * 2);

			return _tileConverterService.IndexOfLocation(x, y) == blockIndex;
			// Original: IndexOfLocation(x, y) ==  ((x / 4) * 13 + (y / 4) * 11 + _randomSeed + 8) % 32;
		}
	}
}