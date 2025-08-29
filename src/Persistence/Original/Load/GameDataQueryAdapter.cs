using System.Data;
using System.Diagnostics;
using CivOne.IO;
using CivOne.Services;

namespace CivOne.Persistence.Original.Impl
{
	internal class GameDataQueryAdapter : IGameData
	{
		protected ISaveDataArrayGetAdapter _array;
		protected SaveData saveData;
		protected IOriginalGameTime gameTime;

		public GameDataQueryAdapter(SaveData saveData, IOriginalGameTime gameTime)
		{
			Debug.Assert(gameTime != null, "gameTime is null");

			_array = new SaveDataArrayGetAdapter(saveData, ArrayServiceProvider.ProvideGet());
			this.saveData = saveData;
			this.gameTime = gameTime;
		}

		// Implement IGameData properties and methods here, mapping from saveData
		// For example:
		public int Year => gameTime.TurnToYear(saveData.GameTurn);

		public ushort GameTurn
		{
			get => saveData.GameTurn;
			// Setter sind read-only:
			set => throw new ReadOnlyException("GameTurn is read-only.");
		}

		public ushort HumanPlayer
		{
			get => saveData.HumanPlayer;
			set => throw new ReadOnlyException("HumanPlayer is read-only.");
		}

		public ushort RandomSeed
		{
			get => saveData.RandomSeed;
			set => throw new ReadOnlyException("RandomSeed is read-only.");
		}

		public ushort Difficulty
		{
			get => saveData.Difficulty;
			set => throw new ReadOnlyException("Difficulty is read-only.");
		}

		public bool[] ActiveCivilizations
		{
			get
			{
				bool[] output = new bool[8];
				for (int i = 0; i < 8; i++)
					output[i] = (saveData.ActiveCivilizations & (1 << i)) > 0;
				return output;
			}
			set => throw new ReadOnlyException("ActiveCivilizations is read-only.");
		}

		public byte[] CivilizationIdentity
		{
			get
			{
				byte[] output = new byte[8];
				for (int i = 0; i < 8; i++)
					output[i] = ((saveData.CivilizationIdentityFlag & (1 << i)) > 0) ? (byte)1 : (byte)0;
				return output;
			}
			set => throw new ReadOnlyException("CivilizationIdentity is read-only.");
		}

		public ushort CurrentResearch
		{
			get => saveData.CurrentResearch;
			set => throw new ReadOnlyException("CurrentResearch is read-only.");
		}

		public byte[][] DiscoveredAdvanceIDs
		{
			get => GetDiscoveredAdvanceIDs();
			set => throw new ReadOnlyException("DiscoveredAdvanceIDs is read-only.");
		}

		public string[] LeaderNames
		{
			get => GetLeaderNames();
			set => throw new ReadOnlyException("LeaderNames is read-only.");
		}

		public string[] CivilizationNames
		{
			get => GetCivilizationNames();
			set => throw new ReadOnlyException("CivilizationNames is read-only.");
		}

		public string[] CitizenNames
		{
			get => GetCitizenNames();
			set => throw new ReadOnlyException("CitizenNames is read-only.");
		}

		public string[] CityNames
		{
			get => GetCityNames();
			set => throw new ReadOnlyException("CityNames is read-only.");
		}

		public short[] PlayerGold
		{
			get => _array.GetArray<short>(nameof(SaveData.PlayerGold), 8);
			set => throw new ReadOnlyException("PlayerGold is read-only.");
		}

		public short[] ResearchProgress
		{
			get => _array.GetArray<short>(nameof(SaveData.ResearchProgress), 8);
			set => throw new ReadOnlyException("ResearchProgress is read-only.");
		}

		public ushort[] TaxRate
		{
			get => _array.GetArray<ushort>(nameof(SaveData.TaxRate), 8);
			set => throw new ReadOnlyException("TaxRate is read-only.");
		}

		public ushort[] ScienceRate
		{
			get => _array.GetArray<ushort>(nameof(SaveData.ScienceRate), 8);
			set => throw new ReadOnlyException("ScienceRate is read-only.");
		}

		public ushort[] StartingPositionX
		{
			get => _array.GetArray<ushort>(nameof(SaveData.StartingPositionX), 8);
			set => throw new ReadOnlyException("StartingPositionX is read-only.");
		}

		public ushort[] Government
		{
			get => _array.GetArray<ushort>(nameof(SaveData.Government), 8);
			set => throw new ReadOnlyException("Government is read-only.");
		}

		public CityData[] Cities
		{
			get => GetCities();
			set => throw new ReadOnlyException("Cities is read-only.");
		}

		public UnitData[][] Units
		{
			get => GetUnits();
			set => throw new ReadOnlyException("Units is read-only.");
		}

		public ushort[] Wonders
		{
			get => _array.GetArray<ushort>(nameof(SaveData.Wonders), 22);
			set => throw new ReadOnlyException("Wonders is read-only.");
		}

		public bool[][,] TileVisibility
		{
			get => GetTileVisibility();
			set => throw new ReadOnlyException("TileVisibility is read-only.");
		}

		public ushort[] AdvanceFirstDiscovery
		{
			get => _array.GetArray<ushort>(nameof(SaveData.AdvanceFirstDiscovery), 72);
			set => throw new ReadOnlyException("AdvanceFirstDiscovery is read-only.");
		}

		public bool[] GameOptions
		{
			get
			{
				bool[] output = new bool[8];
				for (int i = 0; i < 8; i++)
					output[i] = (saveData.GameOptions & (1 << i)) > 0;
				return output;
			}
			set => throw new ReadOnlyException("GameOptions is read-only.");
		}

		public ushort NextAnthologyTurn
		{
			get => saveData.NextAnthologyTurn;
			set => throw new ReadOnlyException("NextAnthologyTurn is read-only.");
		}

		public ushort OpponentCount
		{
			get => saveData.OpponentCount;
			set => throw new ReadOnlyException("OpponentCount is read-only.");
		}

		public ReplayData[] ReplayData
		{
			get => GetReplayData();
			set => throw new ReadOnlyException("ReplayData is read-only.");
		}

		// -- Private Hilfsmethoden (noch zu implementieren) --
		private byte[][] GetDiscoveredAdvanceIDs()
		{
			// TODO: Implementiere Mapping anhand von SaveData.DiscoveredAdvances und AdvancesCount
			return new byte[8][]; // Platzhalter
		}

		private string[] GetLeaderNames()
		{
			// TODO: Implementiere Mapping anhand von SaveData.LeaderNames
			return new string[8]; // Platzhalter
		}

		private string[] GetCivilizationNames()
		{
			// TODO: Implementiere Mapping anhand von SaveData.CivilizationNames
			return new string[8]; // Platzhalter
		}

		private string[] GetCitizenNames()
		{
			// TODO: Implementiere Mapping anhand von SaveData.CitizensName
			return new string[8]; // Platzhalter
		}

		private string[] GetCityNames()
		{
			// TODO: Implementiere Mapping anhand von SaveData.CityNames
			return new string[256]; // Platzhalter
		}

		private CityData[] GetCities()
		{
			// TODO: Implementiere Mapping, wie es in SaveDataAdapter.cs in GetCities() zu finden ist.
			return new CityData[0]; // Platzhalter
		}

		private UnitData[][] GetUnits()
		{
			// TODO: Implementiere Mapping, wie es in SaveDataAdapter.cs in GetUnits() zu finden ist.
			return new UnitData[0][]; // Platzhalter
		}

		private bool[][,] GetTileVisibility()
		{
			// TODO: Implementiere Mapping anhand von SaveData.MapVisibility
			return new bool[8][,]; // Platzhalter
		}

		private ReplayData[] GetReplayData()
		{
			// TODO: Implementiere Mapping anhand von SaveData.ReplayData
			return new ReplayData[0]; // Platzhalter
		}

		public void Dispose()
		{
			// TODO: Falls Ressourcen freigegeben werden müssen.
		}

		public byte[] GetBytes()
		{
			// TODO: Falls benötigt, Implementierung analog zu SaveDataAdapter.GetBytes()
			throw new ReadOnlyException("GetBytes is not implemented.");
		}

		public bool ValidMapSize(int width, int height)
		{
			return (width == 80 && height == 50);
		}

		public bool ValidData => true; // TODO: Implementiere Validierungslogik, falls benötigt
	}
}