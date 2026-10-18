using System.Linq;
using CivOne.Screens.Reports;
using CivOne.Services;
using CivOne.Services.Score;
using Xunit;

namespace CivOne.UnitTests.Services.Score
{
	public class CivilizationRatingServiceTests
	{
		private readonly CivilizationRatingService _service = new(new LeaderOrderDelegate(new TranslationIdentityService()));

		[Theory]
		[InlineData(500, 0, 10)]
		[InlineData(500, 4, 50)]
		[InlineData(500, 5, 60)]
		[InlineData(49, 0, 0)]
		[InlineData(99, 1, 3)]
		public void RatingIsScoreTimesDifficultyPlusOneOver50(int score, int difficulty, int expected)
		{
			Assert.Equal(expected, _service.Rating(score, difficulty));
		}

		[Theory]
		[InlineData(1, "Dan Quayle")]
		[InlineData(2, "Emperor Nero")]
		[InlineData(4, "Ferdinand Marcos")]
		[InlineData(28, "Simon Bolivar")]
		[InlineData(133, "Emperor Augustus")]
		[InlineData(134, "Solomon the Wise")]
		[InlineData(5_000, "Solomon the Wise")]
		public void FamousPersonFollowsOriginalThresholds(int rating, string expected)
		{
			// Difficulty 49 turns the rating into score * 50 / 50 = score.
			LeaderOrderResult? result = _service.FamousPerson(rating, 49, 0);

			Assert.NotNull(result);
			Assert.Equal(expected, result.Value.SelectedLeaderName);
			Assert.Equal(rating, result.Value.RatingPercent);
		}

		[Fact]
		public void RatingZeroShowsNoWindow()
		{
			Assert.Null(_service.FamousPerson(49, 0, 0));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void ScoreZeroWithoutPalaceUpgradesShowsNoWindow(int palaceUpgradeCount)
		{
			Assert.Null(_service.FamousPerson(0, 4, palaceUpgradeCount));
		}

		[Theory]
		[InlineData(2, "Dan Quayle")]
		[InlineData(4, "Emperor Nero")]
		[InlineData(100, "Solomon the Wise")]
		public void ScoreZeroUsesPalaceUpgradeCount(int palaceUpgradeCount, string expected)
		{
			LeaderOrderResult? result = _service.FamousPerson(0, 4, palaceUpgradeCount);

			Assert.NotNull(result);
			Assert.Equal(expected, result.Value.SelectedLeaderName);
			Assert.Equal(0, result.Value.RatingPercent);
		}

		[Fact]
		public void LeaderOrderHasTwentyPersonsWithOriginalThresholds()
		{
			int[] expected = [1, 2, 4, 6, 9, 13, 17, 22, 28, 34, 41, 49, 57, 66, 76, 86, 97, 109, 121, 134];
			LeaderOrderDelegate leaderOrder = new(new TranslationIdentityService());

			int[] thresholds = [.. leaderOrder.GetLeaderOrder().Select(entry => entry.RatingThreshold).Reverse()];

			Assert.Equal(expected, thresholds);
			Assert.Equal("Solomon the Wise", leaderOrder.GetLeaderOrder()[0].Name);
		}
	}
}
