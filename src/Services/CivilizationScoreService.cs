using System;
using System.Linq;
using CivOne.Screens.Services;

namespace CivOne.Services
{
	internal interface ICivilizationScoreService
	{
		int TotalScore(Player player);
		int RatingPercent(int totalScore, int topLeaderThreshold);
	}

	internal static class CivilizationScoreServiceFactory
	{
		public static ICivilizationScoreService CreateDefault() => new CivilizationScoreService();
	}

	internal sealed class CivilizationScoreService : ICivilizationScoreService
	{
		internal const int ScoringLuxuryRate = 0;

		private const int HappyCitizenScoreWeight = 2;
		private const int CityScoreWeight = 3;
		private const int AdvanceScoreWeight = 10;
		private const int WonderScoreWeight = 50;
		private const int GoldPerScorePoint = 25;

		/// <summary>
		/// Calculates the absolute civilization score for a player.
		///
		/// The total is the sum of citizen score, city count score, population score, advance score, wonder score, and gold score.
		///
		/// Gold is converted into score using <c>GoldPerScorePoint</c>.
		///
		/// Happy citizens are weighted with <c>HappyCitizenScoreWeight</c>.
		///
		/// Unhappy and red-shirt citizens do not contribute.
		/// </summary>
		/// <param name="player">The player whose current game state is evaluated.</param>
		/// <returns>The calculated total civilization score.</returns>
		public int TotalScore(Player player)
		{
			ArgumentNullException.ThrowIfNull(player);

			int cityCount = player.Cities.Length;
			int populationScore = player.Population;
			int cityScore = cityCount * CityScoreWeight;
			int advanceScore = player.Advances.Length * AdvanceScoreWeight;
			int goldScore = Math.Max(0, player.Gold / GoldPerScorePoint);
			int wonderCount = player.Cities.Sum(city => city.Wonders.Length);
			int wonderScore = wonderCount * WonderScoreWeight;
			int citizenScore = player.Cities
				.Select(city => city.GetCitizenTypes(ScoringLuxuryRate))
				.Sum(CityScore);

			return citizenScore + cityScore + populationScore + advanceScore + wonderScore + goldScore;
		}

		/// <summary>
		/// Converts an absolute score into a rating percentage against the top leader threshold.
		///
		/// The calculation is <c>(totalScore / topLeaderThreshold) * 100</c> with midpoint rounding away from zero.
		///
		/// Returns <c>0</c> when the threshold is zero or negative.
		/// </summary>
		/// <param name="totalScore">The player's calculated total score.</param>
		/// <param name="topLeaderThreshold">The score threshold of the top leader rank.</param>
		/// <returns>The rating percentage used for leader ranking.</returns>
		public int RatingPercent(int totalScore, int topLeaderThreshold)
		{
			if (topLeaderThreshold <= 0)
			{
				return 0;
			}

			double scoreRatio = (double)totalScore / topLeaderThreshold;
			return (int)Math.Round(scoreRatio * 100, MidpointRounding.AwayFromZero);
		}

		/// <summary>
		/// Calculates the citizen contribution for a single city.
		///
		/// Happy citizens count double.
		///
		/// Unhappy and red-shirt citizens do not contribute.
		/// </summary>
		/// <param name="citizens">The citizen state of one city.</param>
		/// <returns>The score contribution of the city citizens.</returns>
		private static int CityScore(CitizenTypes citizens)
		{
			return HappyCitizenScoreWeight * citizens.happy + (citizens.Citizens.Length - citizens.unhappy - citizens.redShirt);
		}
	}
}