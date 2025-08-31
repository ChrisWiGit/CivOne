namespace CivOne.Services
{
	public class TileConverterServiceImpl(int height, int random) : ITileConverterService
	{
		public int LINEAR_BLOCK_SIZE { get { return 4; } }
		/// <summary>
		/// Get the index of the location in a 4x4 grid
		/// This is the row-major index of the location in the grid.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public int IndexOfLocation(int x, int y) => x % LINEAR_BLOCK_SIZE * LINEAR_BLOCK_SIZE + (y % LINEAR_BLOCK_SIZE);

		public bool HasExtraResourceOnTile(int x, int y)
		{
			bool isAntarctica = (y < 2) || (y > (height - 3));

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
			int blockIndex = (weightedBlockX + weightedBlockY + random) % (LINEAR_BLOCK_SIZE * LINEAR_BLOCK_SIZE);

			return IndexOfLocation(x, y) == blockIndex;
			// Original: IndexOfLocation(x, y) == (x / 4 * 13 + y / 4 * 11 + random) % 16;
		}
	}
}