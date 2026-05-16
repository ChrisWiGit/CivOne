// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Aggregated spaceship part counters used by <see cref="SpaceShipLaunchRules"/> and <see cref="SpaceShipScreenDataFactory"/>.
	/// </summary>
	public readonly record struct SpaceShipPartCounts(
		int Structural,
		int Component,
		int Module,
		int HorizontalStructure,
		int VerticalStructure,
		int StructureNode,
		int CommandModule,
		int LifeSupportModule,
		int HabitationModule,
		int SolarPanelModule,
		int FuelComponent,
		int PropulsionComponent)
	{
		public int StructuralTotal => Structural + HorizontalStructure + VerticalStructure + StructureNode;
		public int ComponentTotal => Component + FuelComponent + PropulsionComponent;
		public int ModuleTotal => Module + CommandModule + LifeSupportModule + HabitationModule + SolarPanelModule;
		public int TotalParts => StructuralTotal + ComponentTotal + ModuleTotal;
		public int DetailedPartCount => HorizontalStructure + VerticalStructure + StructureNode
			+ CommandModule + LifeSupportModule + HabitationModule + SolarPanelModule
			+ FuelComponent + PropulsionComponent;
	}

	/// <summary>
	/// Counts spaceship parts in the grid while handling multi-cell detailed modules.
	/// </summary>
	internal static class SpaceShipPartCounter
	{
		private static bool IsLargeDetailedPart(SpaceShipComponentType type) =>
			type is SpaceShipComponentType.CommandModule
			or SpaceShipComponentType.LifeSupportModule
			or SpaceShipComponentType.HabitationModule
			or SpaceShipComponentType.SolarPanelModule;

		private static bool IsOriginCell(SpaceShipComponentType[,] grid, int x, int y)
		{
			SpaceShipComponentType cell = grid[x, y];
			if (x > 0 && grid[x - 1, y] == cell) return false;
			if (y > 0 && grid[x, y - 1] == cell) return false;
			return true;
		}

		internal static SpaceShipPartCounts Count(SpaceShipComponentType[,] grid)
		{
			int structural = 0;
			int component = 0;
			int module = 0;
			int horizontalStructure = 0;
			int verticalStructure = 0;
			int structureNode = 0;
			int commandModule = 0;
			int lifeSupportModule = 0;
			int habitationModule = 0;
			int solarPanelModule = 0;
			int fuelComponent = 0;
			int propulsionComponent = 0;

			for (int y = 0; y < grid.GetLength(1); y++)
			{
				for (int x = 0; x < grid.GetLength(0); x++)
				{
					SpaceShipComponentType cell = grid[x, y];
					if (IsLargeDetailedPart(cell) && !IsOriginCell(grid, x, y))
					{
						continue;
					}

					switch (cell)
					{
						case SpaceShipComponentType.Structural:
							structural++;
							break;
						case SpaceShipComponentType.Component:
							component++;
							break;
						case SpaceShipComponentType.Module:
							module++;
							break;
						case SpaceShipComponentType.StructureHorizontal:
							horizontalStructure++;
							break;
						case SpaceShipComponentType.StructureVertical:
							verticalStructure++;
							break;
						case SpaceShipComponentType.StructureNode:
							structureNode++;
							break;
						case SpaceShipComponentType.CommandModule:
							commandModule++;
							break;
						case SpaceShipComponentType.LifeSupportModule:
							lifeSupportModule++;
							break;
						case SpaceShipComponentType.HabitationModule:
							habitationModule++;
							break;
						case SpaceShipComponentType.SolarPanelModule:
							solarPanelModule++;
							break;
						case SpaceShipComponentType.FuelComponent:
							fuelComponent++;
							break;
						case SpaceShipComponentType.PropulsionComponent:
							propulsionComponent++;
							break;
					}
				}
			}

			// Special case, but if there are 3 or more life support/habitation modules, 
			// then there must be at least 1 command module, even if it isn't actually present on the grid.
			if (commandModule == 0 && (lifeSupportModule + habitationModule) >= 3)
			{
				commandModule = 1;
			}

			return new SpaceShipPartCounts(
				structural,
				component,
				module,
				horizontalStructure,
				verticalStructure,
				structureNode,
				commandModule,
				lifeSupportModule,
				habitationModule,
				solarPanelModule,
				fuelComponent,
				propulsionComponent);
		}
	}
}