using System.Collections.Generic;
using CivOne.Services.GlobalWarming;
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
        [Fact]
        public void CreateUsesExplicitGameRandomSeedWhenAvailable()
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
            var actual = GameStateHandler.Create(snapshot);

            // Assert
            Assert.Equal(gameRandomSeed, actual.RandomSeed);
            Assert.Equal(terrainMasterWord, actual.TerrainSeed);
        }

        [Fact]
        public void CreateFallsBackToTerrainMasterWordWhenGameRandomSeedMissing()
        {
            // Arrange
            const int terrainMasterWord = 9001;
            var snapshot = new MockedGameSnapshotSource
            {
                GameRandomSeed = null,
                TerrainMasterWord = terrainMasterWord
            };

            // Act
            var actual = GameStateHandler.Create(snapshot);

            // Assert
            Assert.Equal(terrainMasterWord, actual.RandomSeed);
            Assert.Equal(terrainMasterWord, actual.TerrainSeed);
        }

        private sealed class MockedGameSnapshotSource : IGameSnapshotSource
        {
            public int Difficulty { get; set; } = 3;
            public Player CurrentPlayer { get; set; } = null!;
            public Player HumanPlayer { get; set; } = null!;
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
            public (short X, short Y)? HumanLastMapPosition { get; set; }
            public int TerrainMasterWord { get; set; }

            // Must be null: real GlobalWarmingService would require a live Game/Map context.
            public IGlobalWarmingService GlobalWarmingService { get; set; } = null!;

            public (short X, short Y)? GetHumanLastMapPosition() => HumanLastMapPosition;

            public byte PlayerNumber(Player player) => 0;
        }
    }

}
