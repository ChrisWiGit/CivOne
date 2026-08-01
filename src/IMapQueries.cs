// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Model;
using CivOne.Tiles;

namespace CivOne
{
	/// <summary>
	/// Read-only map queries that go beyond plain tile access.
	/// Kept separate from <see cref="IMapEditor"/> so consumers that only read the map (e.g. starting-position
	/// services) do not depend on map-mutating operations they never call.
	/// </summary>
	public interface IMapQueries : IMapTiles
	{
		/// <summary>
		/// Whether the map defines fixed starting positions for civilizations.
		/// </summary>
		bool FixedStartPositions { get; }

		/// <summary>
		/// Looks up the starting position stored on the map for the given civilization.
		/// </summary>
		/// <param name="civilization">The civilization to look up.</param>
		/// <param name="location">The stored starting position, or <see langword="null"/> if the map has none.</param>
		/// <returns>True if the map has a starting position for the civilization; otherwise, false.</returns>
		bool TryGetStartPosition(ICivilization civilization, out MapLocation? location);

		/// <summary>
		/// Returns every tile belonging to the given continent.
		/// </summary>
		/// <param name="continentId">The continent identifier to collect tiles for.</param>
		/// <returns>The tiles of that continent.</returns>
		IEnumerable<ITile> ContinentTiles(int continentId);

		/// <summary>
		/// Checks whether a tile has one of the given terrain types.
		/// </summary>
		/// <param name="tile">The tile to check.</param>
		/// <param name="terrain">The accepted terrain types.</param>
		/// <returns>True if the tile matches one of the terrain types; otherwise, false.</returns>
		bool TileIsType(ITile tile, params Terrain[] terrain);
	}
}
