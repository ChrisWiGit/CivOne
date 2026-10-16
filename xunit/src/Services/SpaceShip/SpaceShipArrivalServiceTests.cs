using CivOne.Enums;
using CivOne.Services;
using CivOne.Services.Calendar;
using CivOne.Services.SpaceShip;
using Xunit;

namespace CivOne.UnitTests.Services.SpaceShip
{
	public class SpaceShipArrivalServiceTests
	{
		// Turn 375 is 1800, turn 376 is 1802 (2-year steps between 1750 and 1850).
		private const ushort Turn1800 = 375;

		[Fact]
		public void NotLaunchedShipHasNoArrival()
		{
			SpaceShipArrivalService service = CreateService(flightTime: 10.0, successRate: 80);
			FakePlayer player = new() { SpaceShipLaunchYear = 0 };

			Assert.Null(service.ArrivalYear(player));
			Assert.False(service.IsInFlight(player));
			Assert.False(service.HasArrived(player, 2000));
			Assert.False(service.ArrivedThisTurn(player, Turn1800));
		}

		[Fact]
		public void LaunchCutsFlightTimeToWholeYears()
		{
			SpaceShipArrivalService service = CreateService(flightTime: 15.7, successRate: 80);
			FakePlayer player = new();

			service.Launch(player, 1800);

			Assert.Equal(1800, player.SpaceShipLaunchYear);
			Assert.Equal(1815, player.SpaceShipEtaYear);
			Assert.Equal(1815, service.ArrivalYear(player));
			Assert.True(service.IsInFlight(player));
		}

		[Fact]
		public void LaunchIgnoresFloatingPointNoiseInFlightTime()
		{
			// 22.0 - 6 * 2.1 - 2 * 0.6 is 8.2 in tenths, but slightly less as a double.
			SpaceShipArrivalService service = CreateService(flightTime: 22.0 - (6 * 2.1) - (2 * 0.6), successRate: 80);
			FakePlayer player = new();

			service.Launch(player, 1800);

			Assert.Equal(1808, player.SpaceShipEtaYear);
		}

		[Fact]
		public void CancelLaunchClearsTheFlight()
		{
			SpaceShipArrivalService service = CreateService(flightTime: 10.0, successRate: 80);
			FakePlayer player = new();
			service.Launch(player, 1800);

			service.CancelLaunch(player);

			Assert.Equal(0, player.SpaceShipLaunchYear);
			Assert.Equal(-1, player.SpaceShipEtaYear);
			Assert.False(service.IsInFlight(player));
			Assert.Null(service.ArrivalYear(player));
		}

		[Fact]
		public void LosingAShipInFlightReturnsItToEarthAndClearsEverything()
		{
			SpaceShipArrivalService service = CreateService(flightTime: 10.0, successRate: 80);
			FakePlayer player = new() { SpaceShipGrid = new[,] { { SpaceShipComponentType.Structural } }, SpaceShipPopulation = 10_000 };
			service.Launch(player, 1800);

			Assert.Equal(SpaceShipLoss.ReturnsToEarth, service.LoseShip(player));
			Assert.False(service.IsInFlight(player));
			Assert.Equal(SpaceShipComponentType.Empty, player.SpaceShipGrid[0, 0]);
			Assert.Equal(0, player.SpaceShipPopulation);
		}

		[Fact]
		public void LosingAShipUnderConstructionCancelsIt()
		{
			SpaceShipArrivalService service = CreateService(flightTime: 10.0, successRate: 80);
			FakePlayer player = new() { SpaceShipGrid = new[,] { { SpaceShipComponentType.Structural } } };

			Assert.Equal(SpaceShipLoss.ConstructionCancelled, service.LoseShip(player));
			Assert.Equal(SpaceShipLoss.None, service.LoseShip(player));
		}

		[Fact]
		public void ArrivalIsDetectedOnlyInTheTurnOfTheArrivalYear()
		{
			// With 1-year steps from turn 376 on, turn 376 is 1801 and turn 377 is 1802.
			CalendarState state = new();
			state.Restore([376, 377, 378]);
			SpaceShipArrivalService service = CreateService(flightTime: 11.0, successRate: 80, state);
			FakePlayer player = new();
			service.Launch(player, 1790);

			Assert.False(service.ArrivedThisTurn(player, Turn1800));
			Assert.True(service.ArrivedThisTurn(player, Turn1800 + 1));
			Assert.False(service.ArrivedThisTurn(player, Turn1800 + 2));
		}

		[Fact]
		public void ArrivalYearSkippedByALargeYearStepIsDetectedInThatTurn()
		{
			// BC turns advance by 20 years: turn 3 is 3940 BC, turn 4 is 3920 BC. An arrival in 3937 BC lies between.
			SpaceShipArrivalService service = CreateService(flightTime: 3.0, successRate: 80);
			FakePlayer player = new();
			service.Launch(player, -3940);

			Assert.False(service.ArrivedThisTurn(player, 3));
			Assert.True(service.ArrivedThisTurn(player, 4));
			Assert.False(service.ArrivedThisTurn(player, 5));
		}

		[Fact]
		public void HasArrivedStaysTrueAfterArrival()
		{
			SpaceShipArrivalService service = CreateService(flightTime: 10.0, successRate: 80);
			FakePlayer player = new();
			service.Launch(player, 1790);

			Assert.False(service.HasArrived(player, 1799));
			Assert.True(service.HasArrived(player, 1800));
			Assert.True(service.HasArrived(player, 1850));
		}

		[Fact]
		public void SuccessRateComesFromSpaceShipService()
		{
			SpaceShipArrivalService service = CreateService(flightTime: 10.0, successRate: 73);

			Assert.Equal(73, service.SuccessRate(new FakePlayer { SpaceShipLaunchYear = 1790 }));
		}

		private static SpaceShipArrivalService CreateService(double flightTime, int successRate, ICalendarState? calendarState = null)
			=> new(new FakeFactory(flightTime, successRate), new GameCalendarService(new TranslationIdentityService(), calendarState ?? new CalendarState()));

		private sealed class FakeFactory(double flightTime, int successRate) : ISpaceShipServiceFactory
		{
			public ISpaceShipService Create(IPlayerSpaceRace player) => new FakeService(flightTime, successRate);
		}

		private sealed class FakeService(double flightTime, int successRate) : ISpaceShipService
		{
			public bool CanAddAnyPart() => false;
			public bool CanAddPart(SpaceShipComponentType partType) => false;
			public bool TryAddPart(SpaceShipComponentType partType) => false;
			public bool CanLaunch() => false;
			public SpaceShipScreenData GetScreenData() => new(0, 0, 0, 0, 0, flightTime, successRate, 0, 0, 0, 0, false);
			public int SuccessRate() => successRate;
		}

		private sealed class FakePlayer : IPlayerSpaceRace
		{
			public SpaceShipComponentType[,] SpaceShipGrid { get; set; } = new SpaceShipComponentType[1, 1];
			public ushort SpaceShipPopulation { get; set; }
			public short SpaceShipLaunchYear { get; set; }
			public short SpaceShipEtaYear { get; set; } = -1;
			public bool HasSpaceFlightAdvance() => false;
			public bool HasPlasticsAdvance() => false;
			public bool HasRoboticsAdvance() => false;
			public bool HasApolloProgram() => false;
		}
	}
}
