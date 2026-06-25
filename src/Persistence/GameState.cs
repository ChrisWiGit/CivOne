using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CivOne.Persistence.Game;
using CivOne.Persistence.Model;
using CivOne.Services.GlobalWarming;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Persistence
{
    public enum GameSetting
    {
        // order must be same as in GameStateHandler
        Animations,
        Sound,
        CivilopediaText,
        EndOfTurn,
        InstantAdvice,
        AutoSave,
        EnemyMoves,
        Palace
    }

    [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This collection needs to be settable for deserialization and mapping.")]
    [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "The collection needs to be List<T> for deserialization and mapping.")]
    public class GameState
	{
		public uint GameTurn { get; set; }
        /// <summary>
        /// The player controlled by the human user.
        /// In most situations this is equal to <see cref="CurrentPlayer"/>,
        /// but it can differ when a snapshot is taken during AI turns.
        /// During loading, this is initially null and set to the correct player after all players are loaded, based on the player number stored in the save file. 
        /// </summary>
		public IPlayer? HumanPlayer { get; set; }

        /// <summary>
        /// The game-level random seed (global RNG state source for gameplay logic).
        /// </summary>
        public int RandomSeed { get; set; }

		public int Difficulty { get; set; }

        public List<byte> CivilizationIdentity { get; set; } = [];

        /// <summary>
        /// The player whose turn is currently active.
        /// Usually equal to <see cref="HumanPlayer"/>, but can differ during AI turns.
        /// </summary>
        public IPlayer CurrentPlayer { get; set; } = null!;

		public IPlayer[] Players { get; set; } = [];
        public List<IUnit> Units { get; set; } = [];

        /// <summary>Maps each advance ID to the player number who first discovered it.</summary>
        public Dictionary<byte, byte> AdvanceOrigin { get; set; } = [];

        public List<City> Cities { get; set; } = [];
		public ushort AnthologyTurn { get; set; }

        /// <summary>
        /// The map generation seed used by map/terrain algorithms.
        /// </summary>
		public int TerrainSeed { get; set; }

        public ITile[,] MapTiles { get; set; } = new ITile[0, 0];

        public int MapWidth { get; set; }
        public int MapHeight { get; set; }

        public string[] CityNames { get; set; } = [];

        public List<GameSetting> GameOptions { get; set; } = [];

        /// <summary>Replay events recorded during the game session.</summary>
        public List<ReplayData> ReplayData { get; set; } = [];

		/// <summary>Global peace turn counter from the original save format.</summary>
		public ushort PeaceTurns { get; set; }

		/// <summary>Future-tech counter from the original save format.</summary>
		public ushort PlayerFutureTech { get; set; }

        public int GlobalWarmingCount { get; set; }
        public int PollutedSquaresCount { get; set; }
        public WarmingIndicator WarmingIndicator { get; set; }
	}
}