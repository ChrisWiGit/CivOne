using CivOne.Screens.Reports;

namespace CivOne.Services.Score
{
	/// <summary>
	/// Turns a civilization score into the rating and the Famous Person shown at the end of the game.
	/// </summary>
	internal interface ICivilizationRatingService
	{
		/// <summary>
		/// Calculates the civilization rating in percent.
		/// </summary>
		/// <param name="score">The civilization score.</param>
		/// <param name="difficulty">The game difficulty, 0 for Chief.</param>
		/// <returns>The rating, <c>score * (difficulty + 1) / 50</c>.</returns>
		int Rating(int score, int difficulty);

		/// <summary>
		/// Selects the Famous Person the civilization exceeds.
		/// </summary>
		/// <param name="score">The civilization score.</param>
		/// <param name="difficulty">The game difficulty, 0 for Chief.</param>
		/// <param name="palaceUpgradeCount">The palace build counter (<see cref="IPalaceData.BuildCount"/>), used when <paramref name="score"/> is 0.</param>
		/// <returns>The selected person, or <c>null</c> if no window is shown.</returns>
		LeaderOrderResult? FamousPerson(int score, int difficulty, int palaceUpgradeCount);
	}
}
