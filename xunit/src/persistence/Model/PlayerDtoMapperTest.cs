namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.Linq;
	using CivOne.Advances;
	using CivOne.Buildings;
	using CivOne.Civilizations;
	using CivOne.Governments;
	using CivOne.Leaders;
	using CivOne.Enums;
	using CivOne.Persistence.Yaml;
	using CivOne.UnitTests;
	using CivOne.Units;
	using CivOne.Wonders;
	using Xunit;
	using AdvanceId = System.UInt32;

	public class PlayerDtoMapperTest
	{
		private readonly PlayerDto originalDto;
		private readonly PlayerDtoMapper _testee;
		private readonly MockedIPlayer _player;
		private readonly MockedICity _city;
		private readonly MockedIUnit _unit;

		public PlayerDtoMapperTest()
		{
			var civsInGame = MockedICivilization.Mock(3);
			// how to initialize in real game: var civs = Common.Civilizations.Select(c => c.Leader.GetType().Name);
			// * var civsInGame = Common.Civilizations;
			// * var govs = Reflect.GetGovernments();
			// * PlayerDto.AllGovernments = govs.Select(g => $"{g.GetType().Name}:{g.Id}").ToArray();
			string[] classes = [.. this.GetType().Assembly.GetTypes()
				.Where(t => typeof(ILeader).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
				.Select(t => t.Name)];
			
			CivilizationDto.AllLeaderClassNames = classes;

			_city = new MockedICity(1)
			{
				Name = "Rome",
				Owner = 0,
				Size = 7
			};

			_unit = new MockedIUnit
			{
				Owner = 0,
				X = 10,
				Y = 20,
				Goto = new Point(5, 8),
				Veteran = true,
				MovesLeft = 2,
				PartMoves = 1,
				order = Order.Fortify
			};

			_player = new MockedIPlayer()
			{
				Advances = [1, 2, 3],
				Embassies = [4, 5],
				Anarchy = 2,
				Gold = 1234,
				CurrentResearch = new MockedIAdvance() { Id = 1 },
				Government = new MockedIGovernment() { Id = 1 },
				Palace = new MockedIPalace(),
				Cities = [_city],
			};

			originalDto = new PlayerDto
			{
				Id = 0,
				Civilization = new CivilizationDto { LeaderClassName = civsInGame[0].Leader.GetType().Name },
				Advances = [1, 2, 3],
				Embassies = [4, 5],
				Anarchy = 2,
				Gold = 1234,
				CurrentResearch = 1,
				Government = 1,
				Palace = new PalaceDto(),
				Cities = [
					new CityDto
					{
						Name = "Rome",
						Owner = 0,
						Size = 7,
						Location = new MapLocation(10, 10),
						VisibleSizes = [7],
						ResourceTiles = new Bool2dMap(5, 5),
						Specialists = [],
						Buildings = [],
						Wonders = [],
						Status = [],
						TradingCities = [],
						ContinentId = 1
					}
				],
				Units = [
					new UnitDto
					{
						ClassName = "MockedIUnit",
						PlayerId = 0,
						Location = new MapLocation(10, 20),
						Goto = new MapLocation(5, 8),
						Veteran = true,
						MovesLeft = 2,
						PartMoves = 1,
						Order = Order.Fortify
					}
				],
				Explored = new Bool2dMap(5, 5),
				Visible = new Bool2dMap(5, 5),
				TribeName = "Romans",
				TribeNamePlural = "Romans",
				LuxuriesRate = 0,
				TaxesRate = 5,
				ScienceRate = 5,
				Science = 100,
				CityNamesSkipped = 0
			};

			// Setup game instance mock
			var gameInstance = new MockPlayerGameForTesting(_player, [_unit]);
			var yamlReadValueSanitizer = new ValueSanitizer(new NoOpLogger());
			
			_testee = new PlayerDtoMapper(
				gameInstance,
				new FixedPlayerOwnerResolver(0),
				new MockPlayerFactoryForTesting(_player),
				new CivilizationDtoMapper(civsInGame),
				new PalaceDtoMapper(yamlReadValueSanitizer),
				new CityDtoMapper(new ProductionDtoMapper(new MockedReflect()), new TestCityDefinitionResolver(), yamlReadValueSanitizer),
				new UnitDtoMapper(new MockUnitFactoryForTesting(), yamlReadValueSanitizer),
				new TestAdvanceResolver(),
				new TestGovernmentResolver(),
				yamlReadValueSanitizer);
			
			PlayerDto.AllAdvances = ["0(Advance0)", "1(Advance1)", "2(Advance2)", "3(Advance3)"];
			PlayerDto.AllAdvancesInfo = new Dictionary<AdvanceId, string>
							{   { 0, "Advance0" },
								{ 1, "Advance1" },
								{ 2, "Advance2" },
								{ 3, "Advance3" }

							};
			PlayerDto.AllGovernments = ["0(Government0)", "1(Government1)", "42(MockedGovernment42)"];
		}

		[Fact]
		public void TestPlayerDtoMapper_ContractCheck()
		{
			var dtoProperties = GetWritablePropertyNames<PlayerDto>();
			// This guards mapping completeness: when a new writable PlayerDto property is added,
			// the test fails until it is either asserted in roundtrip checks or explicitly excluded.
			var expectedProperties = GetPlayerDtoRoundTripAssertionMap(originalDto, originalDto).Keys
				.Concat(GetExcludedPlayerDtoProperties())
				.ToHashSet();

			Assert.Equal([], dtoProperties.Except(expectedProperties).OrderBy(x => x));
		}

		[Fact]
		public void TestPlayerDtoMapper_RoundTrip()
		{
			var player = _testee.FromDto(originalDto);
			var roundTripDto = _testee.ToDto(player);

			YamlWriter.Of(roundTripDto).WithStandard().ToFile("PlayerDtoMapperTest.yaml");

			Assert.NotNull(roundTripDto);

			var assertions = GetPlayerDtoRoundTripAssertionMap(originalDto, roundTripDto);
			foreach (var assertion in assertions.Values)
			{
				assertion();
			}
		}

		private static Dictionary<string, Action> GetPlayerDtoRoundTripAssertionMap(PlayerDto expected, PlayerDto actual)
			=> new()
			{
				[nameof(PlayerDto.TribeName)] = () => Assert.Equal(expected.TribeName, actual.TribeName),
				[nameof(PlayerDto.TribeNamePlural)] = () => Assert.Equal(expected.TribeNamePlural, actual.TribeNamePlural),
				[nameof(PlayerDto.Anarchy)] = () => Assert.Equal(expected.Anarchy, actual.Anarchy),
				[nameof(PlayerDto.Gold)] = () => Assert.Equal(expected.Gold, actual.Gold),
				[nameof(PlayerDto.CurrentResearch)] = () => Assert.Equal(expected.CurrentResearch, actual.CurrentResearch),
				[nameof(PlayerDto.Government)] = () => Assert.Equal(expected.Government, actual.Government),
				[nameof(PlayerDto.LuxuriesRate)] = () => Assert.Equal(expected.LuxuriesRate, actual.LuxuriesRate),
				[nameof(PlayerDto.TaxesRate)] = () => Assert.Equal(expected.TaxesRate, actual.TaxesRate),
				[nameof(PlayerDto.ScienceRate)] = () => Assert.Equal(expected.ScienceRate, actual.ScienceRate),
				[nameof(PlayerDto.Science)] = () => Assert.Equal(expected.Science, actual.Science),
				[nameof(PlayerDto.CityNamesSkipped)] = () => Assert.Equal(expected.CityNamesSkipped, actual.CityNamesSkipped),
				[nameof(PlayerDto.Advances)] = () => Assert.Equal(expected.Advances, actual.Advances),
				[nameof(PlayerDto.Embassies)] = () => Assert.Equal(expected.Embassies, actual.Embassies),
				[nameof(PlayerDto.Explored)] = () => AssertBool2dMapEqual(expected.Explored, actual.Explored),
				[nameof(PlayerDto.Visible)] = () => AssertBool2dMapEqual(expected.Visible, actual.Visible),
				[nameof(PlayerDto.Civilization)] = () => Assert.Equal(expected.Civilization.LeaderClassName, actual.Civilization.LeaderClassName),
				[nameof(PlayerDto.Palace)] = () => Assert.NotNull(actual.Palace),
				[nameof(PlayerDto.Cities)] = () =>
				{
					Assert.Single(actual.Cities);
					Assert.Equal(expected.Cities[0].Name, actual.Cities[0].Name);
					Assert.Equal(expected.Cities[0].Size, actual.Cities[0].Size);
					Assert.Equal(expected.Cities[0].Owner, actual.Cities[0].Owner);
				},
				[nameof(PlayerDto.Units)] = () =>
				{
					Assert.Single(actual.Units);
					Assert.Equal(expected.Units[0].Location.X, actual.Units[0].Location.X);
					Assert.Equal(expected.Units[0].Location.Y, actual.Units[0].Location.Y);
					Assert.Equal(expected.Units[0].Veteran, actual.Units[0].Veteran);
					Assert.Equal(expected.Units[0].MovesLeft, actual.Units[0].MovesLeft);
					Assert.Equal(expected.Units[0].PartMoves, actual.Units[0].PartMoves);
					Assert.Equal(expected.Units[0].Order, actual.Units[0].Order);
				}
			};

		private static HashSet<string> GetExcludedPlayerDtoProperties() =>
			[nameof(PlayerDto.Id)];

		private static HashSet<string> GetWritablePropertyNames<T>() => typeof(T).GetProperties()
			.Where(p => p.CanRead && p.CanWrite)
			.Select(p => p.Name)
			.ToHashSet();

		private static void AssertBool2dMapEqual(bool[,] expected, bool[,] actual)
		{
			Assert.Equal(expected.GetLength(0), actual.GetLength(0));
			Assert.Equal(expected.GetLength(1), actual.GetLength(1));

			for (var x = 0; x < expected.GetLength(0); x++)
			{
				for (var y = 0; y < expected.GetLength(1); y++)
				{
					Assert.Equal(expected[x, y], actual[x, y]);
				}
			}
		}

		private class MockPlayerGameForTesting : IPlayerGame
		{
			private readonly IPlayer _player;
			private readonly IUnit[] _units;

			public MockPlayerGameForTesting(IPlayer player, IUnit[] units = null)
			{
				_player = player;
				_units = units ?? [];
			}

			public bool Started => true;
			public ushort GameTurn => 0;
			public int Difficulty => 3;
			public Player HumanPlayer => throw new NotImplementedException();
			public Player CurrentPlayer => throw new NotImplementedException();
			public IEnumerable<Player> Players => [(_player as Player) ?? throw new InvalidOperationException("Player must be a Player instance")];

			public byte PlayerNumber(Player player) => 0;
			public Player GetPlayer(byte number) => (_player as Player) ?? throw new InvalidOperationException("Player must be a Player instance");
			public City[] GetCities() => [];
			public IUnit[] GetUnits() => _units;
			public void DisbandUnit(IUnit unit) => throw new NotImplementedException();
			public bool WonderObsolete<T>() where T : IWonder, new() => false;
			public bool WonderBuilt<T>() where T : IWonder => false;
			public IWonder[] BuiltWonders => [];
			public void SetAdvanceOrigin(IAdvance advance, Player player) => throw new NotImplementedException();
		}

		private class MockPlayerFactoryForTesting : IPlayerFactory
		{
			private readonly MockedIPlayer _mockPlayer;

			public MockPlayerFactoryForTesting(MockedIPlayer mockPlayer)
			{
				_mockPlayer = mockPlayer ?? throw new ArgumentNullException(nameof(mockPlayer));
			}

			public IPlayerRestorable Create(ICivilization civilization, PlayerDto dto)
			{
				// Return the mock player which already implements IPlayerRestorable
				return _mockPlayer;
			}
		}

		private class MockUnitDtoMapperForTesting : DtoMapper<UnitDto, IUnit>
		{
			public IUnit FromDto(UnitDto dto) => throw new NotImplementedException();
			public UnitDto ToDto(IUnit domain) => throw new NotImplementedException();
		}

		private class MockUnitFactoryForTesting : IUnitFactory
		{
			public IUnitRestorable Create(string className, byte player, Guid? HomeCityGuid)
				=> throw new NotImplementedException();
		}

		private class FixedPlayerOwnerResolver : IPlayerOwnerResolver
		{
			private readonly byte _ownerId;

			public FixedPlayerOwnerResolver(byte ownerId)
			{
				_ownerId = ownerId;
			}

			public bool TryResolveOwnerId(IPlayer player, out byte ownerId)
			{
				ownerId = _ownerId;
				return true;
			}
		}
		private sealed class TestCityDefinitionResolver : ICityDefinitionResolver
		{
			public IBuilding[] ResolveBuildings(IEnumerable<Building> buildingTypes)
				=> [.. (buildingTypes ?? []).Select(type => new MockedIBuilding { Type = type })];

			public IWonder[] ResolveWonders(IEnumerable<Wonder> wonderTypes)
				=> [.. (wonderTypes ?? []).Select(type => new MockedIWonder { Type = type })];
		}

		private sealed class TestAdvanceResolver : IAdvanceResolver
		{
			public IAdvance ResolveById(uint id)
			{
				return new MockedIAdvance { Id = (byte)id };
			}
		}

		private sealed class TestGovernmentResolver : IGovernmentResolver
		{
			public IGovernment ResolveById(byte id)
			{
				return new MockedIGovernment { Id = id };
			}
		}	}
}

/*
		[Fact]
		public void TestByteArrayArrayFlowStyleYamlTypeConverterSingleRow()
		{
			byte[][] testData = [[1, 2, 3]];

			string yaml = YamlWriter.Of(testData)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.AsString();

			var roundTripped = YamlReader.OfString(yaml)
				.WithTypeConverter(new ByteArrayArrayFlowStyleYamlTypeConverter())
				.As<byte[][]>();

			Assert.Single(roundTripped);
			Assert.Equal(new byte[] { 1, 2, 3 }, roundTripped[0]);
		}

*/