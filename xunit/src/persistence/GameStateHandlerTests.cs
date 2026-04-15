using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Services.GlobalWarming;
using CivOne.src;
using CivOne.Tiles;
using CivOne.Units;
using Xunit;

namespace CivOne.UnitTests.Persistence
{
	/// <summary>
    /// Verifies both seed mapping paths in GameStateHandler.Create(...):
    /// 1) explicit game RNG seed when available and
    /// 2) documented legacy fallback to TerrainMasterWord when GameRandomSeed is missing.
    /// This guards the migration contract described in INTERNALS.md
    /// (section "Seed Semantics (legacy vs. new YAML model)").
	/// </summary>
    public class GameStateHandlerTests
    {
        private readonly GameStateHandler _testee;

        public GameStateHandlerTests()
        {
            _testee = new GameStateHandler();
        }

        [Fact]
        public void Create_UsesExplicitGameRandomSeed_WhenAvailable()
        {
            // Arrange
            const int gameRandomSeed = 1337;
            const int terrainMasterWord = 4242;
            var snapshot = new MockedGameSnapshotSource
            {
                GameRandomSeed = gameRandomSeed,
                TerrainMasterWord = terrainMasterWord
            };

            // Act
            var actual = _testee.Create(snapshot);

            // Assert
            Assert.Equal(gameRandomSeed, actual.RandomSeed);
            Assert.Equal(terrainMasterWord, actual.TerrainSeed);
        }

        [Fact]
        public void Create_FallsBackToTerrainMasterWord_WhenGameRandomSeedMissing()
        {
            // Arrange
            const int terrainMasterWord = 9001;
            var snapshot = new MockedGameSnapshotSource
            {
                GameRandomSeed = null,
                TerrainMasterWord = terrainMasterWord
            };

            // Act
            var actual = _testee.Create(snapshot);

            // Assert
            Assert.Equal(terrainMasterWord, actual.RandomSeed);
            Assert.Equal(terrainMasterWord, actual.TerrainSeed);
        }

        private sealed class MockedGameSnapshotSource : IGameSnapshotSource
        {
            public int Difficulty { get; set; } = 3;
            public Player CurrentPlayer { get; set; }
            public Player HumanPlayer { get; set; }
            public Player[] Players { get; set; } = [];
            public List<City> Cities { get; set; } = [];
            public List<IUnit> Units { get; set; } = [];
            public Dictionary<byte, byte> AdvanceOrigin { get; set; } = [];
            public ushort GameTurn { get; set; } = 1;
            public ushort AnthologyTurn { get; set; }
			public ushort PeaceTurns { get; set; }
			public ushort PlayerFutureTech { get; set; }
            public string[] CityNames { get; set; } = [];
            public List<ReplayData> ReplayData { get; set; } = [];
            public ITile[,] MapTiles { get; set; } = new ITile[2, 3];
            public bool Animations { get; set; }
            public bool Sound { get; set; }
            public bool CivilopediaText { get; set; }
            public bool EndOfTurn { get; set; }
            public bool InstantAdvice { get; set; }
            public bool AutoSave { get; set; }
            public bool EnemyMoves { get; set; }
            public bool Palace { get; set; }
            public int? GameRandomSeed { get; set; }
            public int TerrainMasterWord { get; set; }

            // Must be null: real GlobalWarmingService would require a live Game/Map context.
            public IGlobalWarmingService GlobalWarmingService { get; set; } = null;

            public byte PlayerNumber(Player player) => 0;
        }
    }

    public class GameStateHandlerLegacyCityExportTests : TestsBase
    {
        private readonly GameStateHandler _testee;

        public GameStateHandlerLegacyCityExportTests()
        {
            _testee = new GameStateHandler();
        }

        [Fact]
        public void CreateOld_UsesHumanVisibleSize_ForEnemyCityExport()
        {
            // Arrange
            var otherPlayer = Game.Instance.Players.First(player => player != playa && player.Civilization is not Barbarian);
            var city = Game.Instance.AddCity(otherPlayer, 3, 52, 14);
            city.Size = 7;
            city.VisibleSizeToHumanPlayer = 3;
            var snapshot = new RuntimeGameSnapshotSource(Game.Instance);

            // Act
            var actual = _testee.CreateOld(snapshot);

            // Assert
            var actualCity = Assert.Single(actual.Cities);
            Assert.Equal(city.Size, actualCity.ActualSize);
            Assert.Equal((byte)city.VisibleSizeToHumanPlayer, actualCity.VisibleSize);
        }

        private sealed class RuntimeGameSnapshotSource : IGameSnapshotSource
        {
            private readonly Game _game;

            public RuntimeGameSnapshotSource(Game game)
            {
                _game = game;
            }

            public int Difficulty => _game.Difficulty;
            public Player CurrentPlayer => _game.CurrentPlayer;
            public Player HumanPlayer => _game.HumanPlayer;
            public Player[] Players => [.. _game.Players];
            public List<City> Cities => [.. _game.GetCities()];
            public List<IUnit> Units => [.. _game.GetUnits()];
            public Dictionary<byte, byte> AdvanceOrigin => new() { [0] = 0 };
            public ushort GameTurn => Game.Instance.GameTurn;
            public ushort AnthologyTurn => 0;
            public ushort PeaceTurns => 0;
            public ushort PlayerFutureTech => 0;
            public string[] CityNames => _game.CityNames;
            public List<ReplayData> ReplayData => [];
            public ITile[,] MapTiles => GetMapTiles();
            public bool Animations => _game.Animations;
            public bool Sound => _game.Sound;
            public bool CivilopediaText => _game.CivilopediaText;
            public bool EndOfTurn => _game.EndOfTurn;
            public bool InstantAdvice => _game.InstantAdvice;
            public bool AutoSave => _game.AutoSave;
            public bool EnemyMoves => _game.EnemyMoves;
            public bool Palace => _game.Palace;
            public int? GameRandomSeed => null;
            public int TerrainMasterWord => 1;
            public IGlobalWarmingService GlobalWarmingService => null;

            public byte PlayerNumber(Player player) => _game.PlayerNumber(player);

            private static ITile[,] GetMapTiles()
            {
                var output = new ITile[Map.WIDTH, Map.HEIGHT];
                for (var x = 0; x < Map.WIDTH; x++)
                for (var y = 0; y < Map.HEIGHT; y++)
                {
                    output[x, y] = Map.Instance[x, y];
                }

                return output;
            }
        }
    }
}
