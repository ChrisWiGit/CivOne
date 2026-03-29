namespace CivOne.Persistence.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CivOne.Advances;
	using CivOne.Civilizations;
	using CivOne.Leaders;
	using CivOne.Persistence.Yaml;
	using CivOne.UnitTests;
	using CivOne.Units;
	using CivOne.Wonders;
	using Xunit;
	using AdvanceId = System.UInt32;

	public class GameSateDtoMapperTest
	{
		private readonly GameStateDtoMapper _testee;
		private readonly MockedIPlayer _player;
		private readonly IPlayerGame _gameInstance;

		public GameSateDtoMapperTest()
		{
			var civsInGame = MockedICivilization.Mock(3);
			string[] classes = this.GetType().Assembly.GetTypes()
				.Where(t => typeof(ILeader).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
				.Select(t => t.Name)
				.ToArray();
			
			CivilizationDto.AllLeaderClassNames = classes;

			_player = new MockedIPlayer()
			{
				Advances = [1, 2, 3],
				Embassies = [4, 5],
				Anarchy = 2,
				Gold = 1234,
				CurrentResearch = new MockedIAdvance() { Id = 1 },
				Government = new MockedIGovernment() { Id = 1 },
				Palace = new MockedIPalace(),
			};

			_gameInstance = new MockGameInstanceForTesting(_player);

			var playerMapper = new PlayerDtoMapper(
				_gameInstance,
				new MockPlayerFactoryForTesting(),
				new CivilizationMapper(civsInGame),
				new PalaceDtoMapper(),
				new CityDtoMapper(new ProductionDtoMapper(new MockedReflect())),
				new UnitDtoMapper(new MockUnitFactoryForTesting()));

			_testee = new GameStateDtoMapper(playerMapper);
			
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
		public void TestGameStateDtoMapper_ToDto()
		{
			// Arrange: Create a GameState with test player
			var gameState = new GameState
			{
				GameTurn = 42,
				HumanPlayer = _player,
				RandomSeed = 12345,
				Difficulty = 3,
				Players = [_player],
				Units = [], // Empty units list
				GameOptions = [GameOptionEnum.Sound, GameOptionEnum.AutoSave],
				AnthologyTurn = 0
			};

			// Act: Convert GameState to GameStateDto
			var dto = _testee.ToDto(gameState);

			// Assert
			Assert.NotNull(dto);
			Assert.Equal(42u, dto.GameTurn);
			Assert.Equal(0u, dto.HumanPlayer); // First player index
			Assert.Equal(12345u, dto.RandomSeed);
			Assert.Equal(DifficultyLevel.Emperor, dto.Difficulty);
			Assert.Single(dto.Players);
			Assert.Contains(GameOptionEnum.Sound, dto.GameOptions);
			Assert.Contains(GameOptionEnum.AutoSave, dto.GameOptions);

			// Save to YAML for manual inspection
			YamlWriter.Of(dto).WithStandard().ToFile("GameSateDtoMapperTest.TestGameStateDtoMapper_ToDto.yaml");
		}

		[Fact]
		public void TestGameStateDtoMapper_RoundTrip()
		{
			// Arrange: Create a GameStateDto
			var playerDto = new PlayerDto
			{
				Id = 0,
				Civilization = new CivilizationDto { 
						LeaderClassName = new MockedILeader().GetType().Name },
				Advances = [1, 2, 3],
				Embassies = [4, 5],
				Anarchy = 2,
				Gold = 1234,
				CurrentResearch = 1,
				Government = 1,
				Palace = new PalaceDto(),
				Cities = [],
				Units = [],
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

			var dto = new GameStateDto
			{
				GameTurn = 50,
				HumanPlayer = 0,
				RandomSeed = 99999,
				Difficulty = DifficultyLevel.Chieftain,
				Players = [playerDto],
				AnthologyTurn = 0,
				GameOptions = [GameOptionEnum.Sound],
				Map = new MapDto()
			};

			// Act: Convert GameStateDto back to GameState
			var gameState = _testee.FromDto(dto);

			// Assert
			Assert.NotNull(gameState);
			Assert.Equal(50u, gameState.GameTurn);
			Assert.Equal(99999, gameState.RandomSeed);
			Assert.Equal(3, gameState.Difficulty);
			Assert.Single(gameState.Players);
			Assert.Contains(GameOptionEnum.Sound, gameState.GameOptions);
		}


		// Mock implementations for testing
		private class MockGameInstanceForTesting : IPlayerGame
		{
			private readonly IPlayer _player;
			public MockGameInstanceForTesting(IPlayer player) => _player = player;

			public bool Started => true;
			public ushort GameTurn => 0;
			public int Difficulty => 3;
			public Player HumanPlayer => (_player as Player) ?? throw new InvalidOperationException("Player must be a Player instance");
			public Player CurrentPlayer => (_player as Player) ?? throw new InvalidOperationException("Player must be a Player instance");
			public IEnumerable<Player> Players => [(_player as Player) ?? throw new InvalidOperationException("Player must be a Player instance")];

			public byte PlayerNumber(Player player) => 0;
			public Player GetPlayer(byte number) => (_player as Player) ?? throw new InvalidOperationException("Player must be a Player instance");
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
			public IPlayer Create(ICivilization civilization, PlayerDto dto) => throw new NotImplementedException();
		}

		private class MockUnitFactoryForTesting : IUnitFactory
		{
			public IUnitRestorable Create(string className, byte player, Guid? HomeCityGuid)
				=> throw new NotImplementedException();
		}

		// [Fact]
		// public void TestMapResourceTiles_OutOfBounds()
		// {
		// 	resourceTiles[0] = new Grassland(-3, -3);

		// 	Assert.Throws<System.ArgumentException>(
		// 		() => _testee.MapResourceTiles([.. resourceTiles]));
		// }

		// [Fact]
		// public void TestMapMapToTiles()
		// {
		// 	bool[][] data = [
		// 		[true, false, true, false, true],
		// 		[false, true, false, true, false],
		// 		[true, false, false, false, true],
		// 		[false, true, false, true, false],
		// 		[true, true, true, true, true]
		// 	];
		// 	int dataTrueCount = data.SelectMany(row => row).Count(b => b);

		// 	Bool2dMap map = new(data);

		// 	var tiles = _testee.MapMapToTiles(_cityTile, map);

		// 	Assert.Equal(dataTrueCount, tiles.Count);
		// 	foreach (var tile in tiles)
		// 	{
		// 		int dx = tile.X;
		// 		int dy = tile.Y;
		// 		Assert.False(dx == 2 && dy == 2);
		// 		Assert.Equal(data[dx][dy], tile.X == dx && tile.Y == dy);
		// 	}
		// }

		// [Fact]
		// public void TestMapMapToTiles_Empty()
		// {
		// 	Bool2dMap map = new(5, 5);
		// 	var tiles = _testee.MapMapToTiles(_cityTile, map);
		// 	Assert.Empty(tiles);
		// }

		// [Fact]
		// public void TestMapResourceTiles_MapMapToTiles()
		// {
		// 	var map = _testee.MapResourceTiles([.. resourceTiles]);
		// 	var tiles = _testee.MapMapToTiles(_cityTile, map);

		// 	Assert.Equal(resourceTiles.Count, tiles.Count);
		// 	foreach (var tile in resourceTiles)
		// 	{
		// 		Assert.Contains(tiles, t => t.X == tile.X && t.Y == tile.Y);
		// 	}

		// 	var map2 = _testee.MapResourceTiles(tiles.ToArray());
		// 	Assert.Equal(map.ToArray(), map2.ToArray());
		// }

		// [Theory]
		// [InlineData(true, false, true, false, true, false, true, false)]
		// [InlineData(false, true, false, true, false, true, false, true)]
		// [InlineData(true, true, true, true, true, true, true, true)]
		// [InlineData(false, false, false, false, false, false, false, false)]
		// public void TestMapStatusFlags(
		// 	bool isRiot,
		// 	bool isCoastal,
		// 	bool celebrationCancelled,
		// 	bool hydroAvailable,
		// 	bool autoBuild,
		// 	bool techStolen,
		// 	bool celebrationOrRapture,
		// 	bool buildingSold)
		// {
		// 	var status = new MockedCityStatus();
		// 	status.IsRiot = isRiot;
		// 	status.IsCoastal = isCoastal;
		// 	status.CelebrationCancelled = celebrationCancelled;
		// 	status.HydroAvailable = hydroAvailable;
		// 	status.AutoBuild = autoBuild;
		// 	status.TechStolen = techStolen;
		// 	status.CelebrationOrRapture = celebrationOrRapture;
		// 	status.BuildingSold = buildingSold;

		// 	var flags = _testee.MapStatusFlags(status);

		// 	Assert.Equal(isRiot, flags.Contains(CityStatusEnum.Riot));
		// 	Assert.Equal(isCoastal, flags.Contains(CityStatusEnum.Coastal));
		// 	Assert.Equal(celebrationCancelled, flags.Contains(CityStatusEnum.CelebrationCancelled));
		// 	Assert.Equal(hydroAvailable, flags.Contains(CityStatusEnum.HydroAvailable));
		// 	Assert.Equal(autoBuild, flags.Contains(CityStatusEnum.AutoBuild));
		// 	Assert.Equal(techStolen, flags.Contains(CityStatusEnum.TechStolen));
		// 	Assert.Equal(celebrationOrRapture, flags.Contains(CityStatusEnum.CelebrationRapture));
		// 	Assert.Equal(buildingSold, flags.Contains(CityStatusEnum.ImprovementSold));

		// 	var status2 = new MockedCityStatus();
		// 	_testee.MapStatusFlags(status2, flags);

		// 	Assert.Equal(isRiot, status2.IsRiot);
		// 	Assert.Equal(isCoastal, status2.IsCoastal);
		// 	Assert.Equal(celebrationCancelled, status2.CelebrationCancelled);
		// 	Assert.Equal(hydroAvailable, status2.HydroAvailable);
		// 	Assert.Equal(autoBuild, status2.AutoBuild);
		// 	Assert.Equal(techStolen, status2.TechStolen);
		// 	Assert.Equal(celebrationOrRapture, status2.CelebrationOrRapture);
		// 	Assert.Equal(buildingSold, status2.BuildingSold);
		// }

		// public class MockedCityStatus : ICityStatus
		// {
		// 	public bool IsRiot { get; set; }
		// 	public bool IsCoastal { get; set; }
		// 	public bool CelebrationCancelled { get; set; }
		// 	public bool HydroAvailable { get; set; }
		// 	public bool AutoBuild { get; set; }
		// 	public bool TechStolen { get; set; }
		// 	public bool CelebrationOrRapture { get; set; }
		// 	public bool BuildingSold { get; set; }
		// }

	}
}


// using System;
// using System.Collections.Generic;
// using System.Linq;
// using CivOne.Civilizations;
// using CivOne.Enums;

// namespace CivOne.Persistence.Model
// {
// 	using PlayerId = System.UInt32;
//     using AdvanceId = System.UInt32;
//     using GovernmentId = System.Byte;
// 	using CityId = System.UInt32;

//     public class PlayerDtoMapper(
// 		IPlayerGame gameInstance,
// 		DtoMapper<CivilizationDto, ICivilization> civilizationMapper,
// 		PalaceDtoMapper palaceMapper,
// 		CityDtoMapper cityMapper
// 		) : DtoMapper<PlayerDto, IPlayer>
// 	{
// 		private readonly DtoMapper<CivilizationDto, ICivilization> 
// 			_civilizationMapper = civilizationMapper;
// 		private readonly PalaceDtoMapper _palaceMapper = palaceMapper;
// 		private readonly CityDtoMapper _cityMapper = cityMapper;

// 		public IPlayer FromDto(PlayerDto dto)
// 		{
// 			if (Player.Game == null) {
// 				// TODO: remove if we can inject the game instance into the player by constructor
// 				Player.Game = gameInstance;
// 			}
// 			return new Player(
// 				civilization: _civilizationMapper.FromDto(dto.Civilization)
// 				// TODO: many more properties to be mapped here
// 			);
// 		}
//         public PlayerDto ToDto(IPlayer player)
//         {
//             return new PlayerDto
//             {
//                 Civilization = _civilizationMapper.ToDto(player.Civilization),

// 				Explored = player.Explored,
// 				Visible = player.Visible,

// 				TribeName = player.TribeName,
// 				TribeNamePlural = player.TribeNamePlural,

// 				Advances = [.. player.Advances.Select(a => (AdvanceId)a)],
// 				Embassies = [.. player.Embassies],

// 				Anarchy = player.Anarchy,
// 				Gold = player.Gold,
// 				CurrentResearch = (AdvanceId)player.CurrentResearch?.Id,
// 				CityNamesSkipped = player.CityNamesSkipped,

// 				Government = (GovernmentId)player.Government?.Id,
// 				LuxuriesRate = player.LuxuriesRate,
// 				TaxesRate = player.TaxesRate,
// 				ScienceRate = player.ScienceRate,
// 				Science = player.Science,
// 				Palace = _palaceMapper.ToDto(player.Palace),

// 				Cities = [.. player.Cities
// 					.Select(_cityMapper.ToDto)]
// 			};
//         }
// 	}
// }


// using System;
// using System.Collections.Generic;
// using System.Linq;
// using CivOne.Civilizations;
// using CivOne.Persistence.Model.Attributes;

// namespace CivOne.Persistence.Model
// {
//     public class GameStateDtoMapper(
//         PlayerDtoMapper playerMapper,
//         UnitDtoMapper unitMapper
//     ) : DtoMapper<GameStateDto, GameState>
//     {
//         public GameState FromDto(GameStateDto dto)
//         {
//             throw new NotImplementedException();
//         }

//         ushort FindHumanPlayerIndex(Player[] players, Player humanPlayer)
//         {
//             for (ushort i = 0; i < players.Length; i++)
//             {
//                 if (players[i] == humanPlayer)
//                 {
//                     return i;
//                 }
//             }
//             throw new Exception("Human player not found in players array");
//         }

//         public GameStateDto ToDto(GameState gameState)
// 		{
// 			var gameStateDto = new GameStateDto
//             {
//                 Difficulty = (DifficultyLevel)gameState.Difficulty,
//                 GameTurn = gameState.GameTurn,
//                 Players = [.. gameState.Players.Select(playerMapper.ToDto)],
//                 HumanPlayer = FindHumanPlayerIndex(gameState.Players, gameState.HumanPlayer),
                
//                 RandomSeed = (uint)gameState.RandomSeed,
//                 AnthologyTurn = gameState.AnthologyTurn,
//                 TerrainSeed = (uint)gameState.TerrainSeed,
//                 // Map = new MapDto(), // TODO: implement map dto and mapper

//                 GameOptions = gameState.GameOptions,
//                 Units = [.. gameState.Units.Select(unitMapper.ToDto)]
// 			};
            
//             foreach (var player in gameStateDto.Players)
//             {
//                 player.Id = (ushort)gameStateDto.Players.IndexOf(player);
//             }
//             return gameStateDto;
// 		}

        // public class GameStateDto
        // {
        //     [Doc("The difficulty level of the game.",
        //         nameof(DifficultyAll))]
        //     public DifficultyLevel Difficulty { get; set; }

        //     public uint GameTurn { get; set; }
        //     public ushort HumanPlayer { get; set; }

        //     public List<PlayerDto> Players { get; set; }

        //     public uint RandomSeed { get; set; }

        //     public uint AnthologyTurn { get; set; }

        //     public uint TerrainSeed { get; set; }

        //     public MapDto Map { get; set; }

        //     [Doc("The game options that are enabled in the game.",
        //         nameof(GameOptionsAll))]
        //     [YamlDotNet.Serialization.YamlMember(typeof(List<string>))]
        //     public List<GameOptionEnum> GameOptions { get; set; }

        //     private static string DifficultyAll { get => string.Join(", ", Enum.GetNames<DifficultyLevel>()); }
        //     private static string GameOptionsAll { get => string.Join(", ", Enum.GetNames<GameOptionEnum>()); }
        // }
//     }
// }