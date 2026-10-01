using CivOne.Enums;

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Maps slot blueprint symbols to their concrete SpaceShipComponentType.
	/// The slot symbol (from SpaceShipSlotBlueprint.SlotMap) is now the
	/// authoritative source for which specific part type to place.
	/// </summary>
	public static class SpaceShipComponentTypeMapper
	{
		/// <summary>
		/// Returns the concrete SpaceShipComponentType for a slot symbol from
		/// SpaceShipSlotBlueprint.SlotMap. Returns Empty for unknown symbols.
		/// </summary>
		public static SpaceShipComponentType FromSlotSymbol(char symbol) => symbol switch
		{
			'=' => SpaceShipComponentType.StructureHorizontal,
			'|' => SpaceShipComponentType.StructureVertical,
			'#' => SpaceShipComponentType.StructureNode,
			'F' => SpaceShipComponentType.FuelComponent,
			'P' => SpaceShipComponentType.PropulsionComponent,
			'C' => SpaceShipComponentType.CommandModule,
			'L' => SpaceShipComponentType.LifeSupportModule,
			'H' => SpaceShipComponentType.HabitationModule,
			'S' => SpaceShipComponentType.SolarPanelModule,
			_ => SpaceShipComponentType.Empty
		};
	}
}