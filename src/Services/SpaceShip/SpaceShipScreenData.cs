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