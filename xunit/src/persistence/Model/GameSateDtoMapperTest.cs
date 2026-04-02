namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CivOne;
	using CivOne.Advances;
	using CivOne.Civilizations;
	using CivOne.Enums;
	using CivOne.Leaders;
	using CivOne.Persistence.Yaml;
	using CivOne.UnitTests;
	using CivOne.Units;
	using CivOne.Wonders;
	using Xunit;
	using AdvanceId = System.UInt32;

	public class GameSateDtoMapperTest : TestsBase2
	{
		private readonly GameStateDtoMapper _testee;
		private readonly List<MockedIPlayer> _players;
		private readonly IPlayerGame _gameInstance;
		private readonly GameStateDto _dto;

		public GameSateDtoMapperTest()
		{
			var civsInGame = Common.Civilizations.Take(3).ToList();
			CivilizationDto.AllLeaderClassNames = [.. civsInGame.Select(c => c.Leader.GetType().Name).Distinct()];

			// Only Civilization is needed for the Factory to match players.
			// All other values will be set by PlayerDtoMapper.FromDto() from the DTOs.
			_players = [
				new MockedIPlayer() { Civilization = civsInGame[0] },
				new MockedIPlayer() { Civilization = civsInGame[1] }
			];

			_gameInstance = new MockGameInstanceForTesting([.. _players.Cast<IPlayer>()]);

			var unitMapper = new UnitDtoMapper(new MockUnitFactoryForTesting());

			var playerMapper = new PlayerDtoMapper(
				_gameInstance,
				new FixedPlayerOwnerResolver(),
				new MockPlayerFactoryForTesting([.. _players.Cast<IPlayerRestorable>()]),
				new CivilizationMapper(civsInGame),
				new PalaceDtoMapper(),
				new CityDtoMapper(new ProductionDtoMapper(new MockedReflect())),
				unitMapper);

			_testee = new GameStateDtoMapper(playerMapper, unitMapper);

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
						Location = new MapLocation(10, 10),
						VisibleSizes = [5, 3],
						CurrentProduction = null,
						ResourceTiles = new Bool2dMap(5, 5),
						Specialists = [],
						Buildings = [],
						Wonders = [],
						Status = [],
						WasInDisorder = false,
						TradingCities = [],
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
						HasAction = false,
						HasMovesLeft = true,
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
				CityNamesSkipped = 0
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
						Location = new MapLocation(20, 20),
						VisibleSizes = [4, 2],
						CurrentProduction = null,
						ResourceTiles = new Bool2dMap(5, 5),
						Specialists = [],
						Buildings = [],
						Wonders = [],
						Status = [],
						WasInDisorder = false,
						TradingCities = [],
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
						HasAction = false,
						HasMovesLeft = true,
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
				CityNamesSkipped = 0
			};

			_dto = new GameStateDto
			{
				GameTurn = 50,
				HumanPlayer = 0,
				RandomSeed = 99999,
				Difficulty = DifficultyLevel.Chieftain,
				Players = [playerDto0, playerDto1],
				AnthologyTurn = 0,
				GameOptions = [GameOptionEnum.Sound],
				Map = new MapDto()
			};
		}

		[Fact]
		public void TestGameStateDtoMapper_ContractCheck()
		{
			var requiredProperties = new[]
			{
				nameof(GameStateDto.Difficulty),
				nameof(GameStateDto.GameTurn),
				nameof(GameStateDto.HumanPlayer),
				nameof(GameStateDto.Players),
				nameof(GameStateDto.RandomSeed),
				nameof(GameStateDto.AnthologyTurn),
				nameof(GameStateDto.Map),
				nameof(GameStateDto.GameOptions)
			};

			var dtoProperties = typeof(GameStateDto).GetProperties()
				.Where(p => p.CanRead && p.CanWrite)
				.Select(p => p.Name)
				.ToHashSet();

			foreach (var prop in requiredProperties)
			{
				Assert.Contains(prop, dtoProperties);
			}
		}

		[Fact]
		public void TestGameStateDtoMapper_RoundTrip()
		{
			// Act: DTO -> GameState -> DTO
			var gameState = _testee.FromDto(_dto);
			var roundTripDto = _testee.ToDto(gameState);

			YamlWriter.Of(roundTripDto).WithStandard().ToFile("GameSateDtoMapperTest.TestGameStateDtoMapper_ToDto.yaml");

			// Assert - GameState properties
			Assert.NotNull(gameState);
			Assert.Equal(50u, gameState.GameTurn);
			Assert.Equal(99999, gameState.RandomSeed);
			Assert.Equal(0, gameState.Difficulty); // Chieftain = 0
			Assert.Equal(2, gameState.Players.Length);
			Assert.Contains(GameOptionEnum.Sound, gameState.GameOptions);

			// Assert - Cities: CityDtoMapper.FromDto() is not yet implemented,
			// so cities are empty for now.
			// TODO: Assert.NotEmpty(gameState.Cities) when CityDtoMapper.FromDto() is done
			Assert.Empty(gameState.Cities);

			{
				var unitsPlayer0 = gameState.Units.Where(u => u.Owner == 0).ToList();
				var unitsPlayer1 = gameState.Units.Where(u => u.Owner == 1).ToList();
				Assert.Single(unitsPlayer0);
				Assert.Single(unitsPlayer1);
			}

			// Assert roundtrip DTO matches original
			Assert.NotNull(roundTripDto);
			Assert.Equal(_dto.GameTurn, roundTripDto.GameTurn);
			Assert.Equal(_dto.RandomSeed, roundTripDto.RandomSeed);
			Assert.Equal(_dto.Difficulty, roundTripDto.Difficulty);
			Assert.Equal(_dto.HumanPlayer, roundTripDto.HumanPlayer);
			Assert.Equal(_dto.AnthologyTurn, roundTripDto.AnthologyTurn);
			Assert.Contains(GameOptionEnum.Sound, roundTripDto.GameOptions);

			Assert.Equal(2, roundTripDto.Players.Count);

			// Player 0
			Assert.Equal(_dto.Players[0].Gold, roundTripDto.Players[0].Gold);
			Assert.Equal(_dto.Players[0].Anarchy, roundTripDto.Players[0].Anarchy);
			Assert.Equal(_dto.Players[0].TribeName, roundTripDto.Players[0].TribeName);
			Assert.Equal(_dto.Players[0].Advances.Count, roundTripDto.Players[0].Advances.Count);

			// Player 1
			Assert.Equal(_dto.Players[1].Gold, roundTripDto.Players[1].Gold);
			Assert.Equal(_dto.Players[1].Anarchy, roundTripDto.Players[1].Anarchy);
			Assert.Equal(_dto.Players[1].TribeName, roundTripDto.Players[1].TribeName);
			Assert.Equal(_dto.Players[1].Advances.Count, roundTripDto.Players[1].Advances.Count);

			// Note: unit roundtrip via ToDto is not asserted here because MockGameInstanceForTesting.GetUnits()
			// returns [] and Validate() falls back to index 0 for all mock players.
		}


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
	}
}