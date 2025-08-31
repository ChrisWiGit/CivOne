namespace CivOne.Services
{
	public interface ITileConverterService
	{
		int LINEAR_BLOCK_SIZE { get; }
		/// <summary>
		/// Get the index of the location in a 4x4 grid
		/// This is the row-major index of the location in the grid.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		int IndexOfLocation(int x, int y);

		/// <summary>
		/// Determine if the tile at the given coordinates is a "special" tile.
		/// A special tile is one that has a change of having a special 
		/// resource (like a hut, oasis, etc.) placed on it.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		bool HasExtraResourceOnTile(int x, int y);
	}
}