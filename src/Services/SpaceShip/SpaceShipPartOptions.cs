// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Enums;

namespace CivOne.Services.SpaceShip
{
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