namespace CivOne
{
	/// <summary>
	/// The sizes a game is built around.
	/// Defined here rather than on the game class, because the API assembly cannot reference the game but
	/// still has to validate player and civilization ids (see <see cref="ReplayData"/>).
	/// </summary>
	public static class PlayerLimits
	{
		/// <summary>
		/// The number of player slots a game has, including the barbarian player at slot 0.
		/// This bounds the tile "visited" bitmask width and the player colour palette size.
		/// </summary>
		public const int MaxPlayers = 32;

		/// <summary>
		/// The highest valid player slot.
		/// </summary>
		public const int MaxPlayerIndex = MaxPlayers - 1;

		/// <summary>
		/// The highest valid civilization Id.
		/// Includes classic and extended civilizations.
		/// Barbarians remain 0; regular civilizations are currently in range 1-32.
		/// </summary>
		public const int MaxCivilizationId = 32;
	}
}
