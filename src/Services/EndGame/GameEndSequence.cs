namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Everything the end-game flow shows in one turn, prepared by the game from a <see cref="GameEndCheckResult"/>.
	/// A <c>null</c> news text means that reason does not hold this turn.
	/// </summary>
	internal sealed record GameEndSequence
	{
		/// <summary>Gets the conquest news; set when the world is conquered.</summary>
		public string[]? ConquestNews { get; init; }

		/// <summary>Gets the text of the space-victory screen; set when a spaceship arrives this turn.</summary>
		public SpaceVictoryText? SpaceVictoryText { get; init; }

		/// <summary>Gets whether the arriving spaceship is the human player's.</summary>
		public bool SpaceshipWinnerIsHuman { get; init; }

		/// <summary>Gets the human player's palace, the background of the space-victory screen for a rival.</summary>
		public PalaceData? HumanPalace { get; init; }

		/// <summary>Gets the spaceship arrival news.</summary>
		public string[]? SpaceshipNews { get; init; }

		/// <summary>Gets the news of the final year.</summary>
		public string[]? YearEndNews { get; init; }

		/// <summary>Gets the human leader's long tune, played with the news of the final year.</summary>
		public string? YearEndTune { get; init; }
	}
}
