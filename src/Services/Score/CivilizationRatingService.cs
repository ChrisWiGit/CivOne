using CivOne.Screens.Reports;

namespace CivOne.Services.Score
{
	/// <summary>
	/// Default <see cref="ICivilizationRatingService"/> following the original game.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Rating = <c>score * (D + 1) / 50</c>, integer division.
	/// With a score of 0 the original picks the person from the palace counter instead:
	/// <c>PalaceBuildCount / 2 - 1</c>.
	/// </para>
	/// <para>
	/// Deliberate deviation: CivOneX derives the palace counter from the palace levels and caps it,
	/// so the score-0 fallback can pick a lower person than the original (deviation 10).
	/// See <c>docs/plan-civilizationScore.impl.md</c>, §15.1.
	/// </para>
	/// </remarks>
	/// <param name="leaderOrder">The Famous Persons list; <c>null</c> uses the translated default.</param>
	internal sealed class CivilizationRatingService(LeaderOrderDelegate? leaderOrder = null) : ICivilizationRatingService
	{
		private const int RatingDivisor = 50;

		private LeaderOrderDelegate? _leaderOrder = leaderOrder;

		private LeaderOrderDelegate LeaderOrder => _leaderOrder ??= new();

		/// <inheritdoc />
		public int Rating(int score, int difficulty) => score * (difficulty + 1) / RatingDivisor;

		/// <inheritdoc />
		public LeaderOrderResult? FamousPerson(int score, int difficulty, int palaceUpgradeCount)
		{
			if (score == 0)
			{
				return LeaderOrder.SelectByRank((palaceUpgradeCount / 2) - 1, 0);
			}

			return LeaderOrder.Calculate(Rating(score, difficulty));
		}
	}
}
