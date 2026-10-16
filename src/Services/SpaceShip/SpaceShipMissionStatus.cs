namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Part counts and derived percentages of a spaceship that decide its mission success rate.
	/// Consumed by <see cref="SpaceShipSuccessRateDelegate"/>.
	/// </summary>
	/// <param name="CommandCount">Number of command modules.</param>
	/// <param name="HabitationCount">Number of habitation modules.</param>
	/// <param name="LifeSupportCount">Number of life support modules.</param>
	/// <param name="PropulsionCount">Number of propulsion components.</param>
	/// <param name="FuelCount">Number of fuel components.</param>
	/// <param name="Structural">Number of structural parts.</param>
	/// <param name="Support">Structural support in percent (0-100).</param>
	/// <param name="Energy">Energy supply in percent (0-100).</param>
	/// <param name="FuelPercent">Fuel supply in percent (0-100).</param>
	public readonly record struct SpaceShipMissionStatus(
		int CommandCount,
		int HabitationCount,
		int LifeSupportCount,
		int PropulsionCount,
		int FuelCount,
		int Structural,
		int Support,
		int Energy,
		int FuelPercent);
}
