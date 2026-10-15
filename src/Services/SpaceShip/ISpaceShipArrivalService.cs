namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Decides when a launched spaceship reaches Alpha Centauri and how likely the mission succeeds.
	/// Read by the space victory check and the civilization score.
	/// </summary>
	public interface ISpaceShipArrivalService
	{
		/// <summary>
		/// Gets the year in which the player's spaceship arrives.
		/// </summary>
		/// <param name="player">The player whose ship is checked.</param>
		/// <returns>The arrival year, or <c>null</c> if no ship is in flight.</returns>
		int? ArrivalYear(IPlayerSpaceRace player);

		/// <summary>
		/// Launches the player's spaceship and stores its arrival year.
		/// Like the original game, the flight time is cut to whole years: <c>year + flightTenths / 10</c>.
		/// </summary>
		/// <param name="player">The launching player.</param>
		/// <param name="year">The launch year.</param>
		void Launch(IPlayerSpaceRace player, int year);

		/// <summary>
		/// Takes the player's spaceship back to the ground, e.g. through the debug screen.
		/// </summary>
		/// <param name="player">The player whose launch is cancelled.</param>
		void CancelLaunch(IPlayerSpaceRace player);

		/// <summary>
		/// Loses the player's whole spaceship program, like the original game when the capital is captured or the
		/// civilization is destroyed: a ship in flight returns to Earth, a ship under construction is cancelled.
		/// All parts are gone and the ship has to be built again.
		/// </summary>
		/// <param name="player">The player who loses the program.</param>
		/// <returns>What was lost.</returns>
		SpaceShipLoss LoseShip(IPlayerSpaceRace player);

		/// <summary>
		/// Checks whether the player's spaceship is in flight.
		/// The calendar advances by 1 year between 1750 and 1850 while any ship is in flight.
		/// </summary>
		/// <param name="player">The player whose ship is checked.</param>
		/// <returns><c>true</c> if a ship was launched and has an arrival year.</returns>
		bool IsInFlight(IPlayerSpaceRace player);

		/// <summary>
		/// Checks whether the player's spaceship has arrived by <paramref name="year"/>.
		/// </summary>
		/// <param name="player">The player whose ship is checked.</param>
		/// <param name="year">The year to check; negative values are BC.</param>
		/// <returns><c>true</c> if a ship was launched and its arrival year is not after <paramref name="year"/>.</returns>
		bool HasArrived(IPlayerSpaceRace player, int year);

		/// <summary>
		/// Checks whether the player's spaceship arrives in <paramref name="turn"/>: its arrival year is the year of that turn.
		/// </summary>
		/// <param name="player">The player whose ship is checked.</param>
		/// <param name="turn">The game turn to check.</param>
		/// <returns><c>true</c> only in the turn of the arrival.</returns>
		bool ArrivedThisTurn(IPlayerSpaceRace player, ushort turn);

		/// <summary>
		/// Gets the live mission success rate of the player's spaceship.
		/// </summary>
		/// <param name="player">The player whose ship is checked.</param>
		/// <returns>The success rate in percent (0-100).</returns>
		int SuccessRate(IPlayerSpaceRace player);
	}
}
