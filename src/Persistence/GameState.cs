namespace CivOne.Persistence
{
    // 1:1 Abbild von IGameData – reines DTO
    public class GameState
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
        public ushort[] AdvanceFirstDiscovery { get; set; }
        public bool[] GameOptions { get; set; }

        public ushort NextAnthologyTurn { get; set; }
        public ushort OpponentCount { get; set; }

        public ReplayData[] ReplayData { get; set; }
    }
}