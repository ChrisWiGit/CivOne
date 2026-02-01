using System.Collections.Generic;
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

    /*
		Muss anders konvertiert werden. Wir brauchen einen Zwischenschritt, d.h. eine DTO wo noch unsere internen Typen verwendet werden, da sie einfacher zu handeln sind (z.b. yaml)
		aber die alte art in Binär muss dann nochmal extra in einen andere DTO Klasse umgewandelt werden.

		*/

    public class GameState
	{
		public int Difficulty { get; set; }
		public Player CurrentPlayer { get; set; }
		public Player HumanPlayer { get; set; }

		public Player[] Players { get; set; }
		public List<City> Cities { get; set; }
		public List<IUnit> Units { get; set; }

		public Dictionary<byte, byte> AdvanceOrigin { get; set; }

		public ushort GameTurn { get; set; }
		public ushort AnthologyTurn { get; set; }

		public string[] CityNames { get; set; }
		public int TerrainMasterWord { get; set; }

		public List<ReplayData> ReplayData { get; set; }

	}
}