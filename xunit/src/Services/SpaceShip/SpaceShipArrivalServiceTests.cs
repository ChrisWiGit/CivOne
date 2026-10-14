using CivOne.Enums;
using CivOne.Services;
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
			Assert.False(service.HasArrived(player, 2000));
			Assert.False(service.ArrivedThisTurn(player, Turn1800));
		}

		[Fact]
		public void ArrivalYearRoundsFlightTimeUp()
		{
			SpaceShipArrivalService service = CreateService(flightTime: 10.2, successRate: 80);

			Assert.Equal(1801, service.ArrivalYear(new FakePlayer { SpaceShipLaunchYear = 1790 }));
		}

		[Fact]
		public void ArrivalBetweenTwoYearStepsIsDetectedInTheFollowingTurn()
		{
			SpaceShipArrivalService service = CreateService(flightTime: 10.2, successRate: 80);
			FakePlayer player = new() { SpaceShipLaunchYear = 1790 };

			Assert.False(service.ArrivedThisTurn(player, Turn1800));
			Assert.True(service.ArrivedThisTurn(player, Turn1800 + 1));
			Assert.False(service.ArrivedThisTurn(player, Turn1800 + 2));
		}

		[Fact]
		public void HasArrivedStaysTrueAfterArrival()
		{
			SpaceShipArrivalService service = CreateService(flightTime: 10.0, successRate: 80);
			FakePlayer player = new() { SpaceShipLaunchYear = 1790 };

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

		private static SpaceShipArrivalService CreateService(double flightTime, int successRate)
			=> new(new FakeFactory(flightTime, successRate), new GameCalendarService(new TranslationIdentityService()));

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
			public bool HasSpaceFlightAdvance() => false;
			public bool HasPlasticsAdvance() => false;
			public bool HasRoboticsAdvance() => false;
			public bool HasApolloProgram() => false;
		}
	}
}
