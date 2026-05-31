// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;

namespace CivOne.Screens.GamePlayPanels
{
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
		Clear
	}

	internal sealed class TerrainEditorState
	{
		public bool Enabled { get; set; }
		public Terrain SelectedTerrain { get; set; } = Terrain.Grassland1;
		public UnitType SelectedUnitType { get; set; } = UnitType.Settlers;
		public int PencilSizeIndex { get; set; }
		public byte CityOwner { get; set; }
		public bool ShowLandValues { get; set; }
		public EditorMode CurrentMode { get; set; } = EditorMode.Terrain;
	}
}
