using System.Linq;
using CivOne.Enums;

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Helper for resolving concrete part choices for generic part families used by <see cref="CivOne.Screens.SpaceShipPartSelectorDialog"/>.
	/// </summary>
	public static class SpaceShipPartOptions
	{
		public static SpaceShipComponentType[] GetOptions(SpaceShipComponentType genericType) => genericType switch
		{
			SpaceShipComponentType.Component =>
			[
				SpaceShipComponentType.FuelComponent,
				SpaceShipComponentType.PropulsionComponent
			],
			SpaceShipComponentType.Module =>
			[
				SpaceShipComponentType.SolarPanelModule,
				SpaceShipComponentType.LifeSupportModule,
				SpaceShipComponentType.HabitationModule
			],
			_ => []
		};

		public static bool HasAnyAvailable(ISpaceShipService service, SpaceShipComponentType genericType)
		{
			if (service == null)
			{
				return false;
			}

			return GetOptions(genericType).Any(service.CanAddPart);
		}
	}
}