using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Model;

namespace CivOne
{
	/// <summary>
	/// Defines the map-editing operations used by the terrain editor.
	/// Replaces direct Map.Instance access so terrain-editor logic can be unit tested without a live Map.
	/// Read-only queries live in <see cref="IMapQueries"/>.
	/// </summary>
	public interface IMapEditor : IMapQueries
	{
		int EditorWrapX(int x);
		int EditorClampY(int y);
		void EditorSetTerrain(int x, int y, Terrain type);
		void SetStartPosition(Civilization civilization, MapLocation location);
		void RemoveStartPosition(Civilization civilization);
	}
}