namespace CivOne.Services.Palace
{
	/// <summary>
	/// The player and game state a palace upgrade trigger reads.
	/// </summary>
	public interface IPlayerGameState
	{
		/// <summary>
		/// Gets the player's civilization score.
		/// </summary>
		int CivilizationScore { get; }

		/// <summary>
		/// Gets the player's palace.
		/// </summary>
		IPalaceData Palace { get; }

		/// <summary>
		/// Gets whether the player is the human player.
		/// </summary>
		bool IsHuman { get; }

		/// <summary>
		/// Gets the game difficulty, 0 for Chief.
		/// </summary>
		int Difficulty { get; }

		/// <summary>
		/// Gets whether the game option "Palace" is on.
		/// </summary>
		bool PalaceOptionEnabled { get; }

		/// <summary>
		/// Gets whether the player owns a city with a palace.
		/// </summary>
		bool HasPalaceCity { get; }
	}
}
