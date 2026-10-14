namespace CivOne.Services.Historian
{
	/// <summary>
	/// Decides which palace the Historian shows for a player.
	/// </summary>
	internal interface IHistorianPalaceService
	{
		/// <summary>
		/// Gets the palace to show for <paramref name="player"/>.
		/// </summary>
		/// <param name="player">The player of the ranking row.</param>
		/// <returns>The real palace for the human player, a generated one for AI players.</returns>
		IPalaceData PalaceFor(Player player);

		/// <summary>
		/// Sums <c>Size + Happy - (Unhappy + RedShirt)</c> over the player's cities at the player's real luxury rate.
		/// </summary>
		/// <param name="player">The player to read.</param>
		/// <returns>The city sum; the original's per-turn score accumulation.</returns>
		int CitySum(Player player);

		/// <summary>
		/// Converts a city sum into the build count of an AI player's mini palace, as the original does.
		/// </summary>
		/// <param name="citySum">The city sum.</param>
		/// <returns>The build count; 0 for sums of 5 or less.</returns>
		int PalaceLevel(int citySum);
	}
}
