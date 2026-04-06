// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Tiles;

namespace CivOne
{
	public partial class Map
	{
		/// <summary>
		/// Prepares the map for loading from a YAML save file.
		/// Allocates the tile grid and sets the terrain seed so that
		/// <see cref="TileIsSpecial"/> works correctly during tile restoration.
		/// Does NOT call PlaceHuts() or CalculateLandValue() – those values
		/// come pre-serialized in the YAML (Hut = TileCodec bit 10, LandValue = LandValues array).
		/// </summary>
		internal void InitializeForYamlLoad(int width, int height, int terrainSeed)
		{
			_terrainMasterWord = terrainSeed;
			_tiles = new ITile[width, height];
			Ready = false;
		}

		/// <summary>
		/// Marks the map as ready after all tiles have been restored from YAML.
		/// Call this after <see cref="InitializeForYamlLoad"/> and after all tile data
		/// has been set via <see cref="CivOne.Persistence.Model.RuntimeTileDtoMapper"/>.
		/// </summary>
		internal void FinalizeYamlLoad()
		{
			Ready = true;
			Log("Map: Ready (loaded from YAML)");
		}

		/// <summary>
		/// Internal accessor for the special-resource flag during YAML tile restoration.
		/// Delegates to the private <c>TileIsSpecial</c> method. Requires
		/// <see cref="InitializeForYamlLoad"/> to have been called first so that
		/// <c>_terrainMasterWord</c> is set correctly.
		/// </summary>
		internal bool TileIsSpecialInternal(int x, int y) => TileIsSpecial(x, y);

		/// <summary>
		/// Directly writes a tile into the map grid during YAML load.
		/// Only intended for use by <see cref="CivOne.Persistence.Model.RuntimeTileDtoMapper"/>.
		/// </summary>
		internal void SetTileInternal(int x, int y, ITile tile) => _tiles[x, y] = tile;
	}
}
