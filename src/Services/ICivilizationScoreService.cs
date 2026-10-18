using CivOne.Services.Score;

namespace CivOne.Services
{
	/// <summary>
	/// Calculates the civilization score of the original game.
	/// </summary>
	internal interface ICivilizationScoreService
	{
		/// <summary>
		/// Calculates the score with its parts from explicit inputs.
		/// </summary>
		/// <param name="civilization">The per-player state.</param>
		/// <param name="game">The global game state.</param>
		/// <returns>The score with its parts.</returns>
		CivilizationScoreBreakdown Calculate(IScoredCivilization civilization, IScoreGameState game);

		/// <summary>
		/// Calculates the score with its parts for a player of the running game.
		/// </summary>
		/// <param name="player">The player to score.</param>
		/// <returns>The score with its parts.</returns>
		CivilizationScoreBreakdown Calculate(Player player);

		/// <summary>
		/// Calculates the final score for a player of the running game.
		/// </summary>
		/// <param name="player">The player to score.</param>
		/// <returns>The total score, or the world-conquest bonus when it replaced the score.</returns>
		int TotalScore(Player player);

	}
}
