namespace CivOne.Services
{
	/// <summary>
	/// Converts between game turns and calendar years, formats years and advances the calendar.
	/// </summary>
	public interface IGameCalendarService
	{
		/// <summary>
		/// Converts a calendar year to the last game turn whose year is not after it.
		/// </summary>
		/// <param name="year">The year; negative values are BC.</param>
		/// <returns>The game turn.</returns>
		ushort YearToTurn(int year);

		/// <summary>
		/// Converts a game turn to its calendar year, also for past turns.
		/// </summary>
		/// <param name="turn">The game turn.</param>
		/// <returns>The year; negative values are BC.</returns>
		int TurnToYear(ushort turn);

		/// <summary>
		/// Formats a game turn as a year string, e.g. "4000 BC" or "1 AD".
		/// </summary>
		/// <param name="turn">The game turn.</param>
		/// <param name="zeroAd"><c>true</c> shows 1 AD as 0 AD.</param>
		/// <returns>The formatted year.</returns>
		string FormatYear(ushort turn, bool zeroAd = false);

		/// <summary>
		/// Formats a calendar year, e.g. "4000 BC" or "1813 AD".
		/// Use it for stored years, such as a spaceship's arrival year: a year in the future may not belong to any turn yet.
		/// </summary>
		/// <param name="year">The year; negative values are BC.</param>
		/// <returns>The formatted year.</returns>
		string FormatCalendarYear(int year);

		/// <summary>
		/// Gets the era label ("BC" or "AD") of a game turn.
		/// </summary>
		/// <param name="turn">The game turn.</param>
		/// <returns>The translated era label.</returns>
		string FormatEra(ushort turn);

		/// <summary>
		/// Records the year step of the turn change to <paramref name="newTurn"/>.
		/// Call it once per turn change, after the turn counter was increased.
		/// </summary>
		/// <param name="newTurn">The turn that begins.</param>
		/// <param name="spaceshipInFlight"><c>true</c> if any civilization has a spaceship in flight.</param>
		void AdvanceTurn(ushort newTurn, bool spaceshipInFlight);
	}
}
