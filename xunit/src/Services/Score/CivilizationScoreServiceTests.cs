using System;
using System.Collections.Generic;
using CivOne.Services;
using CivOne.Services.Score;
using Xunit;

namespace CivOne.UnitTests.Services.Score
{
	public class CivilizationScoreServiceTests
	{
		private readonly CivilizationScoreService _service = new();

		[Fact]
		public void CitizenTermIsSizePlusHappyMinusUnhappyAndRedShirts()
		{
			FakeCivilization civilization = new() { Cities = [new(8, 3, 2, 1), new(4, 0, 0, 0)] };

			CivilizationScoreBreakdown score = _service.Calculate(civilization, new FakeGame());

			Assert.Equal((8 + 3 - 3) + 4, score.CitizenScore);
			Assert.Equal(12, score.Total);
		}

		[Fact]
		public void EachWonderAdds20()
		{
			CivilizationScoreBreakdown score = _service.Calculate(new FakeCivilization { WonderCount = 3 }, new FakeGame());

			Assert.Equal(60, score.WonderScore);
			Assert.Equal(60, score.Total);
		}

		[Fact]
		public void EachFutureTechAdds5()
		{
			CivilizationScoreBreakdown score = _service.Calculate(new FakeCivilization { FutureTechCount = 2 }, new FakeGame());

			Assert.Equal(10, score.Total);
		}

		[Fact]
		public void EachPollutedSquareSubtracts10()
		{
			FakeCivilization civilization = new() { WonderCount = 2 };

			CivilizationScoreBreakdown score = _service.Calculate(civilization, new FakeGame { PollutedSquaresCount = 3 });

			Assert.Equal(-30, score.PollutionScore);
			Assert.Equal(10, score.Total);
		}

		[Fact]
		public void ScoreIsFlooredAtZero()
		{
			CivilizationScoreBreakdown score = _service.Calculate(new FakeCivilization(), new FakeGame { PollutedSquaresCount = 5 });

			Assert.Equal(0, score.Total);
		}

		[Fact]
		public void ScoreSaturatesAtShortMaxValue()
		{
			CivilizationScoreBreakdown score = _service.Calculate(new FakeCivilization { WonderCount = 2_000 }, new FakeGame());

			Assert.Equal(short.MaxValue, score.Total);
		}

		[Theory]
		[InlineData(1, 10, 30)]
		[InlineData(1500, 34, 100)]
		[InlineData(1500, 0, 0)]
		[InlineData(-1, 10, 0)]
		[InlineData(-4000, 50, 0)]
		public void PeaceTermIsCappedAndOnlyCountsInAdYears(int year, int peaceTurns, int expected)
		{
			CivilizationScoreBreakdown score = _service.Calculate(new FakeCivilization(), new FakeGame { Year = year, PeaceTurns = peaceTurns });

			Assert.Equal(expected, score.PeaceScore);
			Assert.Equal(expected, score.Total);
		}

		[Fact]
		public void SpaceShipTermAppliesOnlyInArrivalTurn()
		{
			FakeCivilization civilization = new() { SpaceShipPopulation = 40_000, SpaceShipSuccessRate = 75 };

			CivilizationScoreBreakdown before = _service.Calculate(civilization, new FakeGame());
			civilization.SpaceShipArrivedThisTurn = true;
			CivilizationScoreBreakdown arrival = _service.Calculate(civilization, new FakeGame());

			Assert.Equal(150, before.SpaceShipValue);
			Assert.False(before.SpaceShipApplied);
			Assert.Equal(0, before.Total);
			Assert.True(arrival.SpaceShipApplied);
			Assert.Equal(150, arrival.Total);
		}

		[Theory]
		[InlineData(0, 75)]
		[InlineData(9_999, 75)]
		[InlineData(40_000, 0)]
		public void SpaceShipTermIsSkippedWithoutPopulationOrSuccess(int population, int successRate)
		{
			FakeCivilization civilization = new() { SpaceShipPopulation = population, SpaceShipSuccessRate = successRate, SpaceShipArrivedThisTurn = true };

			CivilizationScoreBreakdown score = _service.Calculate(civilization, new FakeGame());

			Assert.Equal(0, score.SpaceShipValue);
			Assert.False(score.SpaceShipApplied);
		}

		[Fact]
		public void ConquestBonusReplacesLowerScore()
		{
			FakeGame game = new() { IsWorldConquered = true, OpponentCount = 6, GameTurn = 300 };

			CivilizationScoreBreakdown score = _service.Calculate(new FakeCivilization { WonderCount = 5 }, game);

			Assert.Equal(400 + 600 + 500, score.Total);
			Assert.True(score.IsBonus);
		}

		[Fact]
		public void ConquestBonusReplacesEqualScore()
		{
			FakeGame game = new() { IsWorldConquered = true, OpponentCount = 1, GameTurn = 550 };

			CivilizationScoreBreakdown score = _service.Calculate(new FakeCivilization { WonderCount = 25 }, game);

			Assert.Equal(500, score.Total);
			Assert.True(score.IsBonus);
		}

		[Fact]
		public void ConquestBonusDoesNotReplaceHigherScore()
		{
			FakeGame game = new() { IsWorldConquered = true, OpponentCount = 1, GameTurn = 550 };

			CivilizationScoreBreakdown score = _service.Calculate(new FakeCivilization { WonderCount = 26 }, game);

			Assert.Equal(520, score.Total);
			Assert.False(score.IsBonus);
		}

		[Fact]
		public void NoConquestBonusWhileRivalsRemain()
		{
			CivilizationScoreBreakdown score = _service.Calculate(new FakeCivilization(), new FakeGame { OpponentCount = 6 });

			Assert.Equal(0, score.Total);
			Assert.False(score.IsBonus);
		}

		[Fact]
		public void NullInputsThrow()
		{
			Assert.Throws<ArgumentNullException>(() => _service.Calculate(null!, new FakeGame()));
			Assert.Throws<ArgumentNullException>(() => _service.Calculate(new FakeCivilization(), null!));
			Assert.Throws<ArgumentNullException>(() => _service.Calculate((Player)null!));
		}

		[Fact]
		public void ScoringLuxuryRateIs40Percent()
		{
			Assert.Equal(4, CivilizationScoreService.ScoringLuxuryRate);
		}

		private sealed class FakeCivilization : IScoredCivilization
		{
			public IReadOnlyList<CityScoreInput> Cities { get; init; } = [];
			public int WonderCount { get; init; }
			public int FutureTechCount { get; init; }
			public int SpaceShipPopulation { get; init; }
			public int SpaceShipSuccessRate { get; init; }
			public bool SpaceShipArrivedThisTurn { get; set; }
		}

		private sealed class FakeGame : IScoreGameState
		{
			public int PollutedSquaresCount { get; init; }
			public int PeaceTurns { get; init; }
			public int Year { get; init; } = 1500;
			public int GameTurn { get; init; } = 300;
			public int OpponentCount { get; init; } = 6;
			public bool IsWorldConquered { get; init; }
		}
	}
}
