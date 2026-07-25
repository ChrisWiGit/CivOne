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
	/// Defines the map-editing operations used by the terrain editor.
	/// Replaces direct Map.Instance access so terrain-editor logic can be unit tested without a live Map.
	/// </summary>
	public interface IMapEditor : IMapTiles
	{
		int EditorWrapX(int x);
		int EditorClampY(int y);
		void EditorSetTerrain(int x, int y, Terrain type);
		void SetStartPosition(Civilization civilization, MapLocation location);
		void RemoveStartPosition(Civilization civilization);
		bool TryGetStartPosition(ICivilization civilization, out MapLocation? location);
		bool FixedStartPositions { get; }
		IEnumerable<ITile> ContinentTiles(int continentId);
		bool TileIsType(ITile tile, params Terrain[] terrain);
	}
}
