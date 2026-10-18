namespace CivOne.Enums
{
	/// <summary>
	/// Spaceship component grid cell type.
	/// Legacy values (Structural/Component/Module) are kept for save and gameplay compatibility.
	/// </summary>
	public enum SpaceShipComponentType
	{
		Empty = 0,
		Structural = 1,      // SSStructural (Cost 8)
		Component = 2,       // SSComponent (Cost 16)
		Module = 3,          // SSModule (Cost 32)

		StructureHorizontal = 4,
		StructureVertical = 5,
		StructureNode = 6,

		CommandModule = 7,
		LifeSupportModule = 8,
		HabitationModule = 9,
		SolarPanelModule = 10,

		FuelComponent = 11,
		PropulsionComponent = 12
	}
}