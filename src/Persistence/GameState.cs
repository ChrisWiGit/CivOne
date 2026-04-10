using System.Collections.Generic;
using CivOne.Persistence.Model;
using CivOne.Services.GlobalWarming;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Persistence
{
    // 1:1 Abbild von IGameData – reines DTO
    public class GameState2
    {
        public ushort GameTurn { get; set; }
        public ushort HumanPlayer { get; set; }
        public int RandomSeed { get; set; }
        public int Difficulty { get; set; }

        public bool[] ActiveCivilizations { get; set; }
        public byte[] CivilizationIdentity { get; set; }
        public ushort CurrentResearch { get; set; }
        public byte[][] DiscoveredAdvanceIDs { get; set; }

        public string[] LeaderNames { get; set; }
        public string[] CivilizationNames { get; set; }
        public string[] CitizenNames { get; set; }
        public string[] CityNames { get; set; }

        public short[] PlayerGold { get; set; }
        public short[] ResearchProgress { get; set; }
        public ushort[] TaxRate { get; set; }
        public ushort[] ScienceRate { get; set; }
        public ushort[] StartingPositionX { get; set; }
        public ushort[] Government { get; set; }

        public CityData[] Cities { get; set; }
        public UnitData[][] Units { get; set; }

        public ushort[] Wonders { get; set; }
        public bool[][,] TileVisibility { get; set; }
        // public ushort[] AdvanceFirstDiscovery { get; set; }
        public Dictionary<byte, byte> AdvanceFirstDiscovery { get; set; }
        public bool[] GameOptions { get; set; }

        public ushort NextAnthologyTurn { get; set; }
        public ushort OpponentCount { get; set; }
        public int TerrainMasterWord { get; set; }

        public ReplayData[] ReplayData { get; set; }

    }

    public enum GameOptionEnum
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

    /*
		Muss anders konvertiert werden. Wir brauchen einen Zwischenschritt, d.h. eine DTO wo noch unsere internen Typen verwendet werden, da sie einfacher zu handeln sind (z.b. yaml)
		aber die alte art in Binär muss dann nochmal extra in einen andere DTO Klasse umgewandelt werden.

		*/

    public class GameState
	{
		public uint GameTurn { get; set; }
        /// <summary>
        /// The player controlled by the human user.
        /// In most situations this is equal to <see cref="CurrentPlayer"/>,
        /// but it can differ when a snapshot is taken during AI turns.
        /// </summary>
		public IPlayer HumanPlayer { get; set; }

        /// <summary>
        /// The game-level random seed (global RNG state source for gameplay logic).
        /// </summary>
        public int RandomSeed { get; set; }

		public int Difficulty { get; set; }

        public List<byte> CivilizationIdentity { get; set; }

        /// <summary>
        /// The player whose turn is currently active.
        /// Usually equal to <see cref="HumanPlayer"/>, but can differ during AI turns.
        /// </summary>
        public IPlayer CurrentPlayer { get; set; }

		public IPlayer[] Players { get; set; }
        public List<IUnit> Units { get; set; }

        /// <summary>Maps each advance ID to the player number who first discovered it.</summary>
        public Dictionary<byte, byte> AdvanceOrigin { get; set; }

        public List<City> Cities { get; set; }
		public ushort AnthologyTurn { get; set; }

        /// <summary>
        /// The map generation seed used by map/terrain algorithms.
        /// </summary>
		public int TerrainSeed { get; set; }

        public ITile[,] MapTiles { get; set; }

        public int MapWidth { get; set; }
        public int MapHeight { get; set; }

        public string[] CityNames { get; set; }

        public List<GameOptionEnum> GameOptions { get; set; }

        /// <summary>Replay events recorded during the game session.</summary>
        public List<ReplayData> ReplayData { get; set; }

        public int GlobalWarmingCount { get; set; }
        public int PollutedSquaresCount { get; set; }
        public WarmingIndicator WarmingIndicator { get; set; }
	}
}