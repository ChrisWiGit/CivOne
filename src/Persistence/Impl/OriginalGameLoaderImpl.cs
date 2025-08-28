using System.Data;
using System.IO;
using CivOne.IO;
using CivOne.Services;
using CivOne.Services.Impl;

namespace CivOne.Persistence.Impl
{
	public class OriginalGameLoaderImpl : IGameLoader
	{
		protected IStreamToSaveDataService _streamToSaveDataService = new StreamToSaveDataService();
		protected IOriginalGameTime _gameTime = new GameTimeImpl();
		protected SaveData saveData;

		public OriginalGameLoaderImpl(IStreamToSaveDataService streamToSaveDataService, IOriginalGameTime gameTime)
		{
			_streamToSaveDataService = streamToSaveDataService ?? _streamToSaveDataService;
			_gameTime = gameTime ?? _gameTime;			
		}

		public OriginalGameLoaderImpl() : this(null, null) { }

		public IGameData Load(Stream stream)
		{
			// Civ original game loading code goes here
			// SaveAdapter.Get.cs: Load()
			saveData = _streamToSaveDataService.StreamToSaveData<SaveData>(stream);

			return new GameDataAdapter(saveData, _gameTime);
		}

		internal class GameDataAdapter(SaveData saveData, IOriginalGameTime gameTime) : IGameData
		{
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
				get => saveData.PlayerGold;
				set => throw new ReadOnlyException("PlayerGold is read-only.");
			}

			public short[] ResearchProgress
			{
				get => saveData.ResearchProgress;
				set => throw new ReadOnlyException("ResearchProgress is read-only.");
			}

			public ushort[] TaxRate
			{
				get => saveData.TaxRate;
				set => throw new ReadOnlyException("TaxRate is read-only.");
			}

			public ushort[] ScienceRate
			{
				get => saveData.ScienceRate;
				set => throw new ReadOnlyException("ScienceRate is read-only.");
			}

			public ushort[] StartingPositionX
			{
				get => saveData.StartingPositionX;
				set => throw new ReadOnlyException("StartingPositionX is read-only.");
			}

			public ushort[] Government
			{
				get => saveData.Government;
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
				get => saveData.Wonders;
				set => throw new ReadOnlyException("Wonders is read-only.");
			}

			public bool[][,] TileVisibility
			{
				get => GetTileVisibility();
				set => throw new ReadOnlyException("TileVisibility is read-only.");
			}

			public ushort[] AdvanceFirstDiscovery
			{
				get => saveData.AdvanceFirstDiscovery;
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

}
