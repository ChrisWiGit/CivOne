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
	using CivOne.Persistence.Factories;
	using CivOne.Persistence.Game;
	using CivOne.Persistence.Resolver;
	using CivOne.Persistence.Mapper;
	using CivOne.Services.SpaceShip;

	public class PlayerDtoMapperTest
	{
		private readonly PlayerDto originalDto;
		private readonly PlayerDtoMapper _testee;
		private readonly MockedIPlayer _player;
		private readonly MockedICity _city;
		private readonly MockedIUnit _unit;
		private readonly Guid _playerGuid = Guid.NewGuid();

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
				CityOwnerPlayerIndex = 0,
				Size = 7
			};

			_unit = new MockedIUnit
			{
				Owner = 0,
				X = 10,
				Y = 20,
				GotoDestination = new Point(5, 8),
				Veteran = true,
				MovesLeft = 2,
				PartMoves = 1,
				order = Order.Fortify
			};

			_player = new MockedIPlayer()
			{
				PlayerGuid = _playerGuid,
				Advances = [1, 2, 3],
				Embassies = [4, 5],
				Diplomacy = [0, 1, 2, 3, 4, 5, 6, 7],
				Anarchy = 2,
				Gold = 1234,
				CurrentResearch = new MockedIAdvance() { Id = 1 },
				FutureTechCount = 6,
				HumanContactTurn = 12,
				MapPositions = [(11, 22), (33, 44), (-1, -1), (-1, -1), (-1, -1), (-1, -1), (-1, -1), (-1, -1), (-1, -1)],
				MapPositionNames = ["Capital", "Front", "", "", "", "", "", "", ""],
				MapZoomBasisPoints = 750,
				UnitsLost = [.. Enumerable.Range(0, 28).Select(i => (ushort)i)],
				UnitsDestroyedBy = [.. Enumerable.Range(0, 8).Select(i => (ushort)i)],
				EpicRanking = 11,
				MilitaryPower = 222,
				CivilizationScore = 333,
				Government = new MockedIGovernment() { Id = 1 },
				Palace = new MockedIPalace(),
				Cities = [_city],
			};

			originalDto = new PlayerDto
			{
				Id = 0,
				PlayerGuid = _playerGuid,
				Civilization = new CivilizationDto { LeaderClassName = civsInGame[0].Leader.GetType().Name },
				Advances = [1, 2, 3],
				Embassies = [4, 5],
				Diplomacy =
				[
					new DiplomacyEntryDto { TargetPlayerId = 0, TargetPlayerGuid = _playerGuid, RawFlags = 0, Decoded = new DiplomacyDecodedDto() },
					new DiplomacyEntryDto { TargetPlayerId = 1, RawFlags = 1, Decoded = new DiplomacyDecodedDto() },
					new DiplomacyEntryDto { TargetPlayerId = 2, RawFlags = 2, Decoded = new DiplomacyDecodedDto() },
					new DiplomacyEntryDto { TargetPlayerId = 3, RawFlags = 3, Decoded = new DiplomacyDecodedDto() },
					new DiplomacyEntryDto { TargetPlayerId = 4, RawFlags = 4, Decoded = new DiplomacyDecodedDto() },
					new DiplomacyEntryDto { TargetPlayerId = 5, RawFlags = 5, Decoded = new DiplomacyDecodedDto() },
					new DiplomacyEntryDto { TargetPlayerId = 6, RawFlags = 6, Decoded = new DiplomacyDecodedDto() },
					new DiplomacyEntryDto { TargetPlayerId = 7, RawFlags = 7, Decoded = new DiplomacyDecodedDto() }
				],
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
				CityNamesSkipped = 0,
				FutureTechCount = 6,
				HumanContactTurn = 12,
				StartX = 33,
				MapPositions =
				[
					new MapPositionDto { X = 11, Y = 22, Name = "Capital" },
					new MapPositionDto { X = 33, Y = 44, Name = "Front" },
					new MapPositionDto { X = -1, Y = -1, Name = "" },
					new MapPositionDto { X = -1, Y = -1, Name = "" },
					new MapPositionDto { X = -1, Y = -1, Name = "" },
					new MapPositionDto { X = -1, Y = -1, Name = "" },
					new MapPositionDto { X = -1, Y = -1, Name = "" },
					new MapPositionDto { X = -1, Y = -1, Name = "" },
					new MapPositionDto { X = -1, Y = -1, Name = "" }
				],
				MapZoomBasisPoints = 750,
				SpaceShip = new SpaceShipDto
				{
					Grid = new SpaceShipGridMap2D(new SpaceShipComponentType[SpaceShipSlotBlueprintFactoryProvider.CanonicalGridWidth, SpaceShipSlotBlueprintFactoryProvider.CanonicalGridHeight]),
					Population = 0,
					LaunchYear = 0
				},
				UnitsLost = [.. Enumerable.Range(0, 28).Select(i => (long)i)],
				UnitsDestroyedBy = [.. Enumerable.Range(0, 8).Select(i => (long)i)],
				EpicRanking = 11,
				MilitaryPower = 222,
				CivilizationScore = 333
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
		public void TestPlayerDtoMapperContractCheck()
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
		public void TestPlayerDtoMapperRoundTrip()
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

		[Fact]
		public void TestPlayerDtoMapperFromDtoExpandsAllAdvancesSentinel()
		{
			var dto = new PlayerDto
			{
				Civilization = originalDto.Civilization,
				Advances = [-1],
				Embassies = [],
				Diplomacy = [],
				CurrentResearch = 1,
				Government = 1,
				Cities = [],
				Units = [],
				Explored = new Bool2dMap(5, 5),
				Visible = new Bool2dMap(5, 5)
			};

			var actual = _testee.FromDto(dto);

			Assert.Equal([0, 1, 2, 3], actual.Advances);
		}

		[Fact]
		public void TestPlayerDtoMapperFromDtoMapZoomBasisPointsDefaultAndClamp()
		{
			var defaultZoomDto = new PlayerDto
			{
				Civilization = originalDto.Civilization,
				CurrentResearch = 1,
				Government = 1,
				Advances = [],
				Embassies = [],
				Diplomacy = [],
				Cities = [],
				Units = [],
				Explored = new Bool2dMap(5, 5),
				Visible = new Bool2dMap(5, 5),
				MapZoomBasisPoints = 0
			};

			var defaultZoomPlayer = _testee.FromDto(defaultZoomDto);
			Assert.Equal(1000, defaultZoomPlayer.MapZoomBasisPoints);

			var clampedZoomDto = new PlayerDto
			{
				Civilization = originalDto.Civilization,
				CurrentResearch = 1,
				Government = 1,
				Advances = [],
				Embassies = [],
				Diplomacy = [],
				Cities = [],
				Units = [],
				Explored = new Bool2dMap(5, 5),
				Visible = new Bool2dMap(5, 5),
				MapZoomBasisPoints = 120
			};

			var clampedZoomPlayer = _testee.FromDto(clampedZoomDto);
			Assert.Equal(125, clampedZoomPlayer.MapZoomBasisPoints);
		}

		private static Dictionary<string, Action> GetPlayerDtoRoundTripAssertionMap(PlayerDto expected, PlayerDto actual)
			=> new()
			{
				[nameof(PlayerDto.PlayerGuid)] = () => Assert.Equal(expected.PlayerGuid, actual.PlayerGuid),
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
				[nameof(PlayerDto.FutureTechCount)] = () => Assert.Equal(expected.FutureTechCount, actual.FutureTechCount),
				[nameof(PlayerDto.HumanContactTurn)] = () => Assert.Equal(expected.HumanContactTurn, actual.HumanContactTurn),
				[nameof(PlayerDto.StartX)] = () => Assert.Equal(expected.StartX, actual.StartX),
				[nameof(PlayerDto.LastMapPosition)] = () =>
				{
					Assert.Equal(expected.LastMapPosition?.X, actual.LastMapPosition?.X);
					Assert.Equal(expected.LastMapPosition?.Y, actual.LastMapPosition?.Y);
					Assert.Equal(expected.LastMapPosition?.Name, actual.LastMapPosition?.Name);
				},
				[nameof(PlayerDto.MapZoomBasisPoints)] = () => Assert.Equal(expected.MapZoomBasisPoints, actual.MapZoomBasisPoints),
				[nameof(PlayerDto.MapPositions)] = () =>
				{
					Assert.NotNull(actual.MapPositions);
					Assert.Equal(expected.MapPositions?.Count ?? -1, actual.MapPositions.Count);
					for (var i = 0; i < expected.MapPositions?.Count; i++)
					{
						Assert.Equal(expected.MapPositions[i].X, actual.MapPositions[i].X);
						Assert.Equal(expected.MapPositions[i].Y, actual.MapPositions[i].Y);
						Assert.Equal(expected.MapPositions[i].Name, actual.MapPositions[i].Name);
					}
				},
				[nameof(PlayerDto.UnitsLost)] = () => Assert.Equal(expected.UnitsLost, actual.UnitsLost),
				[nameof(PlayerDto.UnitsDestroyedBy)] = () => Assert.Equal(expected.UnitsDestroyedBy, actual.UnitsDestroyedBy),
				[nameof(PlayerDto.EpicRanking)] = () => Assert.Equal(expected.EpicRanking, actual.EpicRanking),
				[nameof(PlayerDto.MilitaryPower)] = () => Assert.Equal(expected.MilitaryPower, actual.MilitaryPower),
				[nameof(PlayerDto.CivilizationScore)] = () => Assert.Equal(expected.CivilizationScore, actual.CivilizationScore),
				[nameof(PlayerDto.Advances)] = () => Assert.Equal(expected.Advances, actual.Advances),
				[nameof(PlayerDto.Embassies)] = () => Assert.Equal(expected.Embassies, actual.Embassies),
				[nameof(PlayerDto.Diplomacy)] = () =>
				{
					Assert.NotNull(actual.Diplomacy);
					Assert.Equal(expected.Diplomacy.Count, actual.Diplomacy.Count);
					for (var i = 0; i < expected.Diplomacy.Count; i++)
					{
						Assert.Equal(expected.Diplomacy[i].TargetPlayerId, actual.Diplomacy[i].TargetPlayerId);
						Assert.Equal(expected.Diplomacy[i].TargetPlayerGuid, actual.Diplomacy[i].TargetPlayerGuid);
						Assert.Equal(expected.Diplomacy[i].RawFlags, actual.Diplomacy[i].RawFlags);
						Assert.NotNull(actual.Diplomacy[i].Decoded);
					}
				},
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
					Assert.Equal(expected.Cities[0].Food, actual.Cities[0].Food);
					Assert.Equal(expected.Cities[0].Shields, actual.Cities[0].Shields);
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
				},
				[nameof(PlayerDto.SpaceShip)] = () => AssertSpaceShipEqual(expected.SpaceShip, actual.SpaceShip)
			};

		private static HashSet<string> GetExcludedPlayerDtoProperties() =>
			[nameof(PlayerDto.Id), nameof(PlayerDto.UnitsDestroyedByByPlayerGuid)];

		private static HashSet<string> GetWritablePropertyNames<T>() => typeof(T)
			.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
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

		private static void AssertSpaceShipEqual(SpaceShipDto? expected, SpaceShipDto? actual)
		{
			if (expected == null)
			{
				Assert.Null(actual);
				return;
			}

			Assert.NotNull(actual);
			Assert.Equal(expected.Population, actual.Population);
			Assert.Equal(expected.LaunchYear, actual.LaunchYear);

			var expectedGrid = expected.Grid?.ToArray();
			var actualGrid = actual.Grid?.ToArray();
			Assert.NotNull(expectedGrid);
			Assert.NotNull(actualGrid);
			Assert.Equal(expectedGrid.GetLength(0), actualGrid.GetLength(0));
			Assert.Equal(expectedGrid.GetLength(1), actualGrid.GetLength(1));

			for (var x = 0; x < expectedGrid.GetLength(0); x++)
			{
				for (var y = 0; y < expectedGrid.GetLength(1); y++)
				{
					Assert.Equal(expectedGrid[x, y], actualGrid[x, y]);
				}
			}
		}

		private sealed class MockPlayerGameForTesting : IPlayerGame
		{
			private readonly IPlayer _player;
			private readonly IUnit[] _units;

			public MockPlayerGameForTesting(IPlayer player, IUnit[]? units = null)
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
			public void DisbandUnit(IUnit? unit) => throw new NotImplementedException();
			public bool WonderObsolete<T>() where T : IWonder, new() => false;
			public bool WonderBuilt<T>() where T : IWonder => false;
			public IWonder[] BuiltWonders => [];
			public void SetAdvanceOrigin(IAdvance advance, Player player) => throw new NotImplementedException();
		}

		private sealed class MockPlayerFactoryForTesting : IPlayerFactory
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

		private sealed class MockUnitDtoMapperForTesting : IDtoMapper<UnitDto, IUnit>
		{
			public IUnit FromDto(UnitDto dto) => throw new NotImplementedException();
			public UnitDto ToDto(IUnit domain) => throw new NotImplementedException();
		}

		private sealed class MockUnitFactoryForTesting : IUnitFactory
		{
			public IUnitRestorable Create(string className, byte player, Guid? HomeCityGuid)
				=> throw new NotImplementedException();
		}

		private sealed class FixedPlayerOwnerResolver : IPlayerOwnerResolver
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

			public IEnumerable<byte> ResolveAllIds()
			{
				return [0, 1, 2, 3];
			}
		}

		private sealed class TestGovernmentResolver : IGovernmentResolver
		{
			public IGovernment ResolveById(byte id)
			{
				return new MockedIGovernment { Id = id };
			}
		}
	}
}