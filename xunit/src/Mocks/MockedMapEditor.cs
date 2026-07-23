using System.Collections.Generic;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Model;
using CivOne.Tiles;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Test double for <see cref="IMapEditor"/>. Tracks tiles and start positions in-memory
	/// instead of touching the real <see cref="Map"/> singleton.
	/// </summary>
	sealed class MockedMapEditor : IMapEditor
	{
		private readonly Dictionary<(int X, int Y), ITile> _tiles = [];
		private readonly Dictionary<Civilization, MapLocation> _startPositions = [];

		public List<(int X, int Y, Terrain Type)> EditorSetTerrainCalls { get; } = [];
		public List<(Civilization Civilization, MapLocation Location)> SetStartPositionCalls { get; } = [];
		public List<Civilization> RemoveStartPositionCalls { get; } = [];

		public ITile this[int x, int y] => _tiles.TryGetValue((x, y), out ITile? tile) ? tile : null!;

		public int Width => throw new System.NotImplementedException();
		public int Height => throw new System.NotImplementedException();

		public int EditorWrapX(int x) => x;
		public int EditorClampY(int y) => y;

		public void EditorSetTerrain(int x, int y, Terrain type) => EditorSetTerrainCalls.Add((x, y, type));

		public void SetStartPosition(Civilization civilization, MapLocation location)
		{
			SetStartPositionCalls.Add((civilization, location));
			_startPositions[civilization] = location;
		}

		public void RemoveStartPosition(Civilization civilization)
		{
			RemoveStartPositionCalls.Add(civilization);
			_startPositions.Remove(civilization);
		}

		public bool TryGetStartPosition(ICivilization civilization, out MapLocation? location)
			=> _startPositions.TryGetValue((Civilization)civilization.Id, out location);

		public void SetTile(int x, int y, ITile tile) => _tiles[(x, y)] = tile;

		public void SeedStartPosition(Civilization civilization, MapLocation location) => _startPositions[civilization] = location;
	}
}
