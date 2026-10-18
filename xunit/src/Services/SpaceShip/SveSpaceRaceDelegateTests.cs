using CivOne.Enums;
using CivOne.Services.SpaceShip;
using Xunit;

namespace CivOne.UnitTests.Services.SpaceShip
{
	public class SveSpaceRaceDelegateTests
	{
		private readonly SveSpaceRaceDelegate _testee = new(new SpaceShipArrivalService(new FakeFactory()));

		[Fact]
		public void FlagsMarkShipsInFlightPartsAndTheScoringLatch()
		{
			FakePlayer[] players = [new(), new() { LaunchYear = 1800, EtaYear = 1812 }, new(), new() { WithParts = true }];

			ushort flags = _testee.BuildFlags(players, scoringCompleted: true);

			Assert.Equal((ushort)((1 << 1) | 0x100 | (1 << 3 + 8)), flags);
			Assert.True(_testee.IsScoringCompleted(flags));
			Assert.False(_testee.IsScoringCompleted((ushort)(1 << 1)));
		}

		[Fact]
		public void ArrivalYearsRoundTripOnlyForShipsInFlight()
		{
			FakePlayer[] players = [new(), new() { LaunchYear = 1800, EtaYear = 1812 }, new()];
			ushort flags = _testee.BuildFlags(players, scoringCompleted: false);
			short[] years = _testee.BuildArrivalYears(players);
			Assert.Equal([-1, 1812, -1, -1, -1, -1, -1, -1], years);

			FakePlayer[] loaded = [new(), new() { LaunchYear = 1800 }, new() { LaunchYear = 1700 }];
			years[2] = 1750;
			_testee.RestoreArrivalYears(loaded, flags, years);

			Assert.Equal(1812, loaded[1].SpaceShipEtaYear);
			Assert.Equal(-1, loaded[2].SpaceShipEtaYear);
		}

		private sealed class FakePlayer : IPlayerSpaceRace
		{
			public short LaunchYear { get; init; }
			public short EtaYear { get; init; } = -1;
			public bool WithParts { get; init; }

			private SpaceShipComponentType[,]? _grid;
			public SpaceShipComponentType[,] SpaceShipGrid
			{
				get => _grid ??= WithParts ? new[,] { { SpaceShipComponentType.Structural } } : new SpaceShipComponentType[1, 1];
				set => _grid = value;
			}
			public ushort SpaceShipPopulation { get; set; }
			private short? _launchYear;
			public short SpaceShipLaunchYear { get => _launchYear ?? LaunchYear; set => _launchYear = value; }
			private short? _etaYear;
			public short SpaceShipEtaYear { get => _etaYear ?? EtaYear; set => _etaYear = value; }
			public bool HasSpaceFlightAdvance() => false;
			public bool HasPlasticsAdvance() => false;
			public bool HasRoboticsAdvance() => false;
			public bool HasApolloProgram() => false;
		}

		private sealed class FakeFactory : ISpaceShipServiceFactory
		{
			public ISpaceShipService Create(IPlayerSpaceRace player) => throw new System.NotSupportedException();
		}
	}
}
