namespace CivOne.Services.Persistence
{
	public class GameSnapshot
	{
		public int GameTurn { get; set; }
		public ushort HumanPlayer { get; set; }
		public int RandomSeed { get; set; }
		public ushort Difficulty { get; set; }

		public bool[] ActiveCivilizations { get; set; }
		public byte[] CivilizationIdentity { get; set; }

		public byte CurrentResearch { get; set; }
		public byte[][] DiscoveredAdvanceIDs { get; set; }

		public string[] LeaderNames { get; set; }
		public string[] CivilizationNames { get; set; }
		public string[] CitizenNames { get; set; }

		public ushort[] PlayerGold { get; set; }
		public ushort[] ResearchProgress { get; set; }

		// usw. – 1:1 das, was IGameData bekommt
	}
}
