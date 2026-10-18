namespace CivOne.Services.Calendar
{
	/// <summary>
	/// Decides by how many years the calendar advances at the end of a turn.
	/// </summary>
	public interface ICalendarStepRule
	{
		/// <summary>
		/// Gets the number of years the calendar advances from <paramref name="currentYear"/>.
		/// </summary>
		/// <param name="currentYear">The year of the turn that ends; negative values are BC.</param>
		/// <param name="spaceshipInFlight"><c>true</c> if any civilization has a spaceship in flight.</param>
		/// <returns>The step in years, at least 1.</returns>
		int YearStep(int currentYear, bool spaceshipInFlight);
	}
}
