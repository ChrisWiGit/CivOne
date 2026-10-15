using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Services;
using CivOne.Services.Calendar;
using CivOne.Services.EndGame;
using CivOne.Services.SpaceShip;
using Xunit;

namespace CivOne.UnitTests.Services.EndGame
{
	public class GameEndCheckServiceTests
	{
		// From 1850 every turn is one year: turn 400 is 1850, so year Y is turn Y - 1450.
		private static ushort TurnOf(int year) => (ushort)(year - 1450);

		[Theory]
		[InlineData(0, 2080, 2100)]
		[InlineData(1, 2060, 2080)]
		[InlineData(2, 2040, 2060)]
		[InlineData(3, 2020, 2040)]
		[InlineData(4, 2000, 2020)]
		[InlineData(5, 1980, 2000)]
		public void WarningAndEndComeOnlyInTheirExactYear(int difficulty, int warningYear, int endYear)
		{
			GameEndCheckService testee = CreateService();
			FakePlayer[] players = [new(), new()];

			Assert.True(testee.Check(players, TurnOf(warningYear), difficulty, false).RetirementWarning);
			Assert.False(testee.Check(players, TurnOf(warningYear + 1), difficulty, false).RetirementWarning);
			Assert.False(testee.Check(players, TurnOf(warningYear), difficulty, false).EndsGame);

			Assert.True(testee.Check(players, TurnOf(endYear), difficulty, false).YearEnd);
			Assert.False(testee.Check(players, TurnOf(endYear - 1), difficulty, false).EndsGame);
			Assert.False(testee.Check(players, TurnOf(endYear + 1), difficulty, false).EndsGame);
		}

		[Fact]
		public void SpaceshipArrivesOnlyInItsArrivalYear()
		{
			GameEndCheckService testee = CreateService();
			FakePlayer[] players = [new(), new() { EtaYear = 1900 }];

			Assert.False(testee.Check(players, TurnOf(1899), 0, false).HasSpaceshipWinner);
			Assert.Equal(1, testee.Check(players, TurnOf(1900), 0, false).SpaceshipWinnerSlot);
			Assert.False(testee.Check(players, TurnOf(1901), 0, false).HasSpaceshipWinner);
		}

		[Fact]
		public void HighestSlotWinsWhenSeveralShipsArriveInTheSameYear()
		{
			GameEndCheckService testee = CreateService();
			FakePlayer[] players = [new(), new() { EtaYear = 1900 }, new(), new() { EtaYear = 1900 }];

			Assert.Equal(3, testee.Check(players, TurnOf(1900), 0, false).SpaceshipWinnerSlot);
		}

		[Fact]
		public void BarbarianSlotIsIgnored()
		{
			GameEndCheckService testee = CreateService();
			FakePlayer[] players = [new() { EtaYear = 1900 }, new()];

			Assert.False(testee.Check(players, TurnOf(1900), 0, false).EndsGame);
		}

		[Fact]
		public void ConquestClearsTheSpaceshipWinnerButNotTheYearEnd()
		{
			GameEndCheckService testee = CreateService();
			FakePlayer[] players = [new(), new() { EtaYear = 2100 }];

			GameEndCheckResult result = testee.Check(players, TurnOf(2100), 0, true);

			Assert.True(result.Conquest);
			Assert.False(result.HasSpaceshipWinner);
			Assert.True(result.YearEnd);
			Assert.True(result.EndsGame);
		}

		private static GameEndCheckService CreateService()
		{
			IGameCalendarService calendar = new GameCalendarService(new TranslationIdentityService(), new CalendarState());
			return new(new SpaceShipArrivalService(new NoSpaceShipServiceFactory(), calendar), calendar);
		}

		private sealed class NoSpaceShipServiceFactory : ISpaceShipServiceFactory
		{
			public ISpaceShipService Create(IPlayerSpaceRace player) => throw new System.NotSupportedException();
		}

		private sealed class FakePlayer : IPlayerSpaceRace
		{
			public short EtaYear { init => SpaceShipEtaYear = value; }
			public SpaceShipComponentType[,] SpaceShipGrid { get; set; } = new SpaceShipComponentType[1, 1];
			public ushort SpaceShipPopulation { get; set; }
			public short SpaceShipLaunchYear { get; set; } = 1800;
			public short SpaceShipEtaYear { get; set; } = -1;
			public bool HasSpaceFlightAdvance() => false;
			public bool HasPlasticsAdvance() => false;
			public bool HasRoboticsAdvance() => false;
			public bool HasApolloProgram() => false;
		}
	}
}
