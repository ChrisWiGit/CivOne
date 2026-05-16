// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Snapshot of derived spaceship metrics for UI rendering and launch readiness display.
	/// Created by <see cref="ISpaceShipScreenDataFactory"/>.
	/// </summary>
	public record SpaceShipScreenData(
		int Population,
		int SupportPercent,
		int EnergyPercent,
		int MassTons,
		int FuelPercent,
		double FlightTimeYears,
		int SuccessProbabilityPercent,
		int StructuralCount,
		int ComponentCount,
		int ModuleCount,
		int TotalParts,
		bool CanLaunch);
}
