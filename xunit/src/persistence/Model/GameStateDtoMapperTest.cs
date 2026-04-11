namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CivOne;
	using CivOne.Advances;
	using CivOne.Buildings;
	using CivOne.Civilizations;
	using CivOne.Enums;
	using CivOne.Governments;
	using CivOne.Leaders;
	using CivOne.Persistence.Yaml;
	using CivOne.UnitTests;
	using CivOne.Units;
	using CivOne.Wonders;
	using Xunit;
	using AdvanceId = System.UInt32;

	public class GameStateDtoMapperTest
	{
		private readonly GameStateDtoMapper _testee;
		private readonly List<MockedIPlayer> _players;
		private readonly IPlayerGame _gameInstance;
		private readonly GameStateDto _dto;

		public GameStateDtoMapperTest()
		{
			var civsInGame = MockedICivilization.Mock(3);
			CivilizationDto.AllLeaderClassNames = [.. civsInGame.Select(c => c.Leader.GetType().Name).Distinct()];

			// Only Civilization is needed for the Factory to match players.
			// All other values will be set by PlayerDtoMapper.FromDto() from the DTOs.
			_players = [
				new MockedIPlayer() { Civilization = civsInGame[0] },
				new MockedIPlayer() { Civilization = civsInGame[1] }
			];

			_gameInstance = new MockGameInstanceForTesting([.. _players.Cast<IPlayer>()]);
			var yamlReadValueSanitizer = new ValueSanitizer(new NoOpLogger());

			var unitMapper = new UnitDtoMapper(new MockUnitFactoryForTesting(), yamlReadValueSanitizer);
			var mockedMapFactory = new MapDtoTest.MockedIMapFactory();
			var mockedTileMapper = new MapDtoTest.MockedITileDtoMapper(() => mockedMapFactory.CurrentMapTiles);
			var mapMapper = new MapDtoMapper(mockedMapFactory, mockedTileMapper);
			var globalWarmingMapper = new GlobalWarmingDtoMapper(yamlReadValueSanitizer);

			var playerMapper = new PlayerDtoMapper(
				_gameInstance,
				new FixedPlayerOwnerResolver(),
				new MockPlayerFactoryForTesting([.. _players.Cast<IPlayerRestorable>()]),
				new CivilizationDtoMapper(civsInGame),
				new PalaceDtoMapper(yamlReadValueSanitizer),
				new CityDtoMapper(new ProductionDtoMapper(new MockedReflect()), new TestCityDefinitionResolver(), yamlReadValueSanitizer),
				unitMapper,
				new TestAdvanceResolver(),
				new TestGovernmentResolver(),
				yamlReadValueSanitizer);

			_testee = new GameStateDtoMapper(playerMapper, unitMapper, mapMapper, globalWarmingMapper, yamlReadValueSanitizer, new EmptyCityNameCatalog());

			PlayerDto.AllAdvances = ["0(Advance0)", "1(Advance1)", "2(Advance2)", "3(Advance3)"];
			PlayerDto.AllAdvancesInfo = new Dictionary<AdvanceId, string>
			{   { 0, "Advance0" },
				{ 1, "Advance1" },
				{ 2, "Advance2" },
				{ 3, "Advance3" }
			};
			PlayerDto.AllGovernments = ["0(Government0)", "1(Government1)", "2(Government2)"];

			var cityId0 = Guid.NewGuid();
			var cityId1 = Guid.NewGuid();

			var playerDto0 = new PlayerDto
			{
				Id = 0,
				Civilization = new CivilizationDto { LeaderClassName = civsInGame[0].Leader.GetType().Name },
				Advances = [1, 2, 3],
				Embassies = [1],
				Anarchy = 2,
				Gold = 1234,
				CurrentResearch = 1,
				Government = 1,
				Palace = new PalaceDto(),
				Cities = [
					new CityDto {
						Id = cityId0,
						Name = "Rome",
						Owner = 0,
						Size = 5,
						Food = 20,
						Shields = 10,
						Location = new MapLocation(10, 10),
						VisibleSizes = [5, 3],
						CurrentProduction = null,
						ResourceTiles = new Bool2dMap(5, 5),
						Specialists = [],
						Buildings = [],
						Wonders = [],
						Status = [],
						WasInDisorder = false,
						TradingCities = [cityId1],
						ContinentId = 1
					}
				],
				Units = [
					new UnitDto {
						ClassName = "Settlers",
						Location = new MapLocation(11, 11),
						Goto = new MapLocation(11, 11),
						HomeCityGuid = cityId0,
						Busy = false,
						Veteran = false,
						Sentry = false,
						FortifyActive = false,
						Fortify = false,
						FuelOrProgress = 0,
						Fuel = 0,
						WorkProgress = 0,
						Order = Order.None,
						MovesSkip = 0,
						MovesLeft = 1,
						PartMoves = 0,
						PlayerId = 0
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
				FutureTechCount = 4
			};

			var playerDto1 = new PlayerDto
			{
				Id = 1,
				Civilization = new CivilizationDto { LeaderClassName = civsInGame[1].Leader.GetType().Name },
				Advances = [0, 2],
				Embassies = [0],
				Anarchy = 1,
				Gold = 5678,
				CurrentResearch = 2,
				Government = 2,
				Palace = new PalaceDto(),
				Cities = [
					new CityDto {
						Id = cityId1,
						Name = "Alexandria",
						Owner = 1,
						Size = 4,
						Food = 12,
						Shields = 8,
						Location = new MapLocation(20, 20),
						VisibleSizes = [4, 2],
						CurrentProduction = null,
						ResourceTiles = new Bool2dMap(5, 5),
						Specialists = [],
						Buildings = [],
						Wonders = [],
						Status = [],
						WasInDisorder = false,
						TradingCities = [cityId0],
						ContinentId = 2
					}
				],
				Units = [
					new UnitDto {
						ClassName = "Legion",
						Location = new MapLocation(21, 21),
						Goto = new MapLocation(21, 21),
						HomeCityGuid = cityId1,
						Busy = false,
						Veteran = true,
						Sentry = false,
						FortifyActive = false,
						Fortify = false,
						FuelOrProgress = 0,
						Fuel = 0,
						WorkProgress = 0,
						Order = Order.None,
						MovesSkip = 0,
						MovesLeft = 2,
						PartMoves = 0,
						PlayerId = 1
					}
				],
				Explored = new Bool2dMap(5, 5),
				Visible = new Bool2dMap(5, 5),
				TribeName = "Egyptians",
				TribeNamePlural = "Egyptians",
				LuxuriesRate = 0,
				TaxesRate = 5,
				ScienceRate = 5,
				Science = 200,
				CityNamesSkipped = 0,
				FutureTechCount = 9
			};

			_dto = new GameStateDto
			{
				GameTurn = 50,
				HumanPlayer = 0,
				CurrentPlayer = 0,
				GameRandomSeed = 99999,
				PeaceTurns = 11,
				PlayerFutureTech = 4,
				Difficulty = DifficultyLevel.Chieftain,
				Players = [playerDto0, playerDto1],
				AnthologyTurn = 0,
				GameOptions = [GameOptionEnum.Sound],
				AdvanceOrigin = new Dictionary<byte, byte> { [3] = 1, [7] = 0 },
				ReplayData = [new ReplayDataDto { Turn = 5, CivilizationDestroyed = new() { DestroyedId = 1, DestroyedById = 2 } }],
				GlobalWarming = new GlobalWarmingDto
				{
					GlobalWarmingCount = 3,
					PollutedSquaresCount = 7,
					WarmingIndicator = Services.GlobalWarming.WarmingIndicator.Yellow
				},
				Map = new MapDto
				{
					MapSeed = 4242,
					Tiles = new Map2d<TileDto>(new TileDto[,]
					{
						{
							new() { Terrain = Terrain.Plains, Road = true, LandValue = 1 },
							new() { Terrain = Terrain.Grassland1, RailRoad = true, LandValue = 2 }
						},
						{
							new() { Terrain = Terrain.Hills, Mine = true, LandValue = 3 },
							new() { Terrain = Terrain.Forest, Hut = true, LandValue = 4 }
						}
					})
				}
			};
		}

		[Fact]
		public void TestGameStateDtoMapper_ContractCheck()
		{
			var dtoProperties = GetWritablePropertyNames<GameStateDto>();
			// This prevents silent mapper drift: every writable GameStateDto property must
			// be covered by roundtrip assertions, so new fields cannot be forgotten.
			var expectedProperties = GetGameStateDtoRoundTripAssertionMap(_dto, _dto).Keys.ToHashSet();

			Assert.Equal([], dtoProperties.Except(expectedProperties).OrderBy(x => x));
		}

		[Fact]
		public void TestGameStateDtoMapper_RoundTrip()
		{
			var gameState = _testee.FromDto(_dto);
			var roundTripDto = _testee.ToDto(gameState);

			YamlWriter.Of(roundTripDto)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.ToFile("GameStateDtoMapperTest.TestGameStateDtoMapper_ToDto.yaml");

			Assert.NotNull(gameState);
			Assert.Equal(50u, gameState.GameTurn);
			Assert.Equal(99999, gameState.RandomSeed);
			Assert.Equal(0, gameState.Difficulty); // Chieftain = 0
			Assert.Equal(2, gameState.Players.Length);
			Assert.Contains(GameOptionEnum.Sound, gameState.GameOptions);
			Assert.Equal(4242, gameState.TerrainSeed);
			Assert.Equal(2, gameState.MapWidth);
			Assert.Equal(2, gameState.MapHeight);
			Assert.NotNull(gameState.MapTiles);
			Assert.NotNull(gameState.MapTiles[0, 0]);
			Assert.True(gameState.MapTiles[0, 0].Road);
			Assert.Equal(Terrain.Forest, gameState.MapTiles[1, 1].Type);

			Assert.NotNull(gameState.Cities);
			Assert.Equal(2, gameState.Cities.Count);

			{
				var unitsPlayer0 = gameState.Units.Where(u => u.Owner == 0).ToList();
				var unitsPlayer1 = gameState.Units.Where(u => u.Owner == 1).ToList();
				Assert.Single(unitsPlayer0);
				Assert.Single(unitsPlayer1);
			}

			Assert.NotNull(roundTripDto);
			Assert.NotNull(roundTripDto.Players);
			Assert.Equal(2, roundTripDto.Players.Count);
			Assert.Single(roundTripDto.Players[0].Cities);
			Assert.Single(roundTripDto.Players[1].Cities);
			Assert.Single(roundTripDto.Players[0].Cities[0].TradingCities);
			Assert.Single(roundTripDto.Players[1].Cities[0].TradingCities);
			Assert.Equal(_dto.Players[1].Cities[0].Id, roundTripDto.Players[0].Cities[0].TradingCities[0]);
			Assert.Equal(_dto.Players[0].Cities[0].Id, roundTripDto.Players[1].Cities[0].TradingCities[0]);

			var assertions = GetGameStateDtoRoundTripAssertionMap(_dto, roundTripDto);
			foreach (var assertion in assertions.Values)
			{
				assertion();
			}
		}

		private static Dictionary<string, Action> GetGameStateDtoRoundTripAssertionMap(GameStateDto expected, GameStateDto actual)
			=> new()
			{
				[nameof(GameStateDto.Difficulty)] = () => Assert.Equal(expected.Difficulty, actual.Difficulty),
				[nameof(GameStateDto.GameTurn)] = () => Assert.Equal(expected.GameTurn, actual.GameTurn),
				[nameof(GameStateDto.HumanPlayer)] = () => Assert.Equal(expected.HumanPlayer, actual.HumanPlayer),
				[nameof(GameStateDto.CurrentPlayer)] = () => Assert.Equal(expected.CurrentPlayer, actual.CurrentPlayer),
				[nameof(GameStateDto.Players)] = () =>
				{
					Assert.Equal(expected.Players.Count, actual.Players.Count);
					for (int i = 0; i < expected.Players.Count; i++)
					{
						var expectedPlayer = expected.Players[i];
						var actualPlayer = actual.Players.FirstOrDefault(p => p.Id == expectedPlayer.Id)
							?? throw new Exception($"Player with ID {expectedPlayer.Id} not found in actual players");
						Assert.Equal(expectedPlayer.Gold, actualPlayer.Gold);
						Assert.Equal(expectedPlayer.Anarchy, actualPlayer.Anarchy);
						Assert.Equal(expectedPlayer.TribeName, actualPlayer.TribeName);
						Assert.Equal(expectedPlayer.FutureTechCount, actualPlayer.FutureTechCount);
						Assert.Equal(expectedPlayer.Advances.Count, actualPlayer.Advances.Count);
						Assert.Equal(expectedPlayer.Units?.Count ?? 0, actualPlayer.Units?.Count ?? 0);
					}
				},
				[nameof(GameStateDto.GameRandomSeed)] = () => Assert.Equal(expected.GameRandomSeed, actual.GameRandomSeed),
				[nameof(GameStateDto.RandomSeed)] = () => Assert.Equal(expected.RandomSeed, actual.RandomSeed),
				[nameof(GameStateDto.AnthologyTurn)] = () => Assert.Equal(expected.AnthologyTurn, actual.AnthologyTurn),
				[nameof(GameStateDto.PeaceTurns)] = () => Assert.Equal(expected.PeaceTurns, actual.PeaceTurns),
				[nameof(GameStateDto.PlayerFutureTech)] = () => Assert.Equal(expected.PlayerFutureTech, actual.PlayerFutureTech),
				[nameof(GameStateDto.Map)] = () =>
				{
					Assert.NotNull(actual.Map);
					Assert.NotNull(actual.Map.Tiles);
					Assert.Equal(expected.Map.MapSeed, actual.Map.MapSeed);
					Assert.Equal(expected.Map.TerrainSeed, actual.Map.TerrainSeed);
					Assert.Equal(expected.Map.Tiles.Width(), actual.Map.Tiles.Width());
					Assert.Equal(expected.Map.Tiles.Height(), actual.Map.Tiles.Height());
					for (var x = 0; x < expected.Map.Tiles.Width(); x++)
					{
						for (var y = 0; y < expected.Map.Tiles.Height(); y++)
						{
							Assert.Equal(expected.Map.Tiles[x, y].Terrain, actual.Map.Tiles[x, y].Terrain);
							Assert.Equal(expected.Map.Tiles[x, y].Road, actual.Map.Tiles[x, y].Road);
							Assert.Equal(expected.Map.Tiles[x, y].RailRoad, actual.Map.Tiles[x, y].RailRoad);
							Assert.Equal(expected.Map.Tiles[x, y].Mine, actual.Map.Tiles[x, y].Mine);
							Assert.Equal(expected.Map.Tiles[x, y].Hut, actual.Map.Tiles[x, y].Hut);
							Assert.Equal(expected.Map.Tiles[x, y].LandValue, actual.Map.Tiles[x, y].LandValue);
						}
					}
				},
				[nameof(GameStateDto.GameOptions)] = () => Assert.Equal(expected.GameOptions, actual.GameOptions),
				[nameof(GameStateDto.AdvanceOrigin)] = () => Assert.Equal(expected.AdvanceOrigin, actual.AdvanceOrigin),
				[nameof(GameStateDto.ReplayData)] = () => Assert.Equal(
					expected.ReplayData?.Count ?? 0, actual.ReplayData?.Count ?? 0),
				[nameof(GameStateDto.GlobalWarming)] = () =>
				{
					Assert.NotNull(actual.GlobalWarming);
					Assert.Equal(expected.GlobalWarming.GlobalWarmingCount, actual.GlobalWarming.GlobalWarmingCount);
					Assert.Equal(expected.GlobalWarming.PollutedSquaresCount, actual.GlobalWarming.PollutedSquaresCount);
					Assert.Equal(expected.GlobalWarming.WarmingIndicator, actual.GlobalWarming.WarmingIndicator);
				}
			};

		private static HashSet<string> GetWritablePropertyNames<T>() => typeof(T).GetProperties()
			.Where(p => p.CanRead && p.CanWrite)
			.Select(p => p.Name)
			.ToHashSet();


		// Mock implementations for testing
		private class MockGameInstanceForTesting : IPlayerGame
		{
			private readonly List<IPlayer> _players;

			public MockGameInstanceForTesting(List<IPlayer> players)
			{
				_players = players;
			}

			public bool Started => true;
			public ushort GameTurn => 0;
			public int Difficulty => 3;
			public Player HumanPlayer => throw new NotImplementedException();
			public Player CurrentPlayer => throw new NotImplementedException();
			// Return a dummy Player array for Validate to use as fallback
			// In tests, this satisfies the check that gameInstance.Players.Any()
			public IEnumerable<Player> Players => []; // Empty is acceptable for test fallback logic

			public byte PlayerNumber(Player player) => 0;
			public Player GetPlayer(byte number) => throw new NotImplementedException();
			public City[] GetCities() => [];
			public IUnit[] GetUnits() => [];
			public void DisbandUnit(IUnit unit) => throw new NotImplementedException();
			public bool WonderObsolete<T>() where T : IWonder, new() => false;
			public bool WonderBuilt<T>() where T : IWonder => false;
			public IWonder[] BuiltWonders => [];
			public void SetAdvanceOrigin(IAdvance advance, Player player) => throw new NotImplementedException();
		}

		private class MockPlayerFactoryForTesting : IPlayerFactory
		{
			private readonly List<IPlayerRestorable> _players;

			public MockPlayerFactoryForTesting(List<IPlayerRestorable> players)
			{
				_players = players;
			}

			public IPlayerRestorable Create(ICivilization civilization, PlayerDto dto)
			{
				var result = _players.FirstOrDefault(p => p.Civilization.Name == civilization.Name)
					?? throw new Exception("No matching player found for civilization " + civilization.Name);
				return result;
			}
		}

		private class MockUnitFactoryForTesting : IUnitFactory
		{
			public IUnitRestorable Create(string className, byte player, Guid? HomeCityGuid)
				=> new MockedIUnit { Owner = player, Name = className };
		}

		private class FixedPlayerOwnerResolver : IPlayerOwnerResolver
		{
			public bool TryResolveOwnerId(IPlayer player, out byte ownerId)
			{
				ownerId = 0;
				return false;
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
		}

		/// <summary>
		/// Provides an empty city name catalog so that GameStateDtoMapper.FromDto()
		/// does not call Common.AllCityNames, which would trigger Reflect.GetCivilizations()
		/// and instantiate real leader classes that require the graphics Resources subsystem.
		/// </summary>
		private sealed class EmptyCityNameCatalog : ICityNameCatalog
		{
			public IEnumerable<string> GetAllCityNames() => [];
		}
	}
}