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
		/// <returns>The arrival year, or <c>null</c> if no ship has been launched.</returns>
		int? ArrivalYear(IPlayerSpaceRace player);

		/// <summary>
		/// Checks whether the player's spaceship has arrived by <paramref name="year"/>.
		/// </summary>
		/// <param name="player">The player whose ship is checked.</param>
		/// <param name="year">The year to check; negative values are BC.</param>
		/// <returns><c>true</c> if a ship was launched and its arrival year is not after <paramref name="year"/>.</returns>
		bool HasArrived(IPlayerSpaceRace player, int year);

		/// <summary>
		/// Checks whether the player's spaceship arrives in <paramref name="turn"/>, i.e. had not arrived in the turn before.
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
