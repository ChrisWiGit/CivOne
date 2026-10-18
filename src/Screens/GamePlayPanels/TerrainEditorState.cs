using CivOne.Enums;

namespace CivOne.Screens.GamePlayPanels
{
	/// <summary>
	/// Represents the state of the terrain editor.
	/// Each menu selection in the terrain editor modifies this state, which is then used to determine how to apply changes to the map.
	/// </summary>
	internal enum EditorMode
	{
		None,
		Terrain,
		FoundCity,
		SpawnUnit,
		Irrigation,
		Road,
		Mine,
		Fortress,
		Pollution,
		Hut,
		Clear,
		StartPosition
	}

	internal sealed class TerrainEditorState
	{
		public bool Enabled { get; set; }
		public Terrain SelectedTerrain { get; set; } = Terrain.Grassland1;
		public UnitType SelectedUnitType { get; set; } = UnitType.Settlers;
		public int PencilSizeIndex { get; set; }
		public byte CityOwner { get; set; }
		public Civilization StartPositionCivilization { get; set; }
		public bool ShowLandValues { get; set; }
		public EditorMode CurrentMode { get; set; } = EditorMode.Terrain;
	}
}