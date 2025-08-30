using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using CivOne.IO;
using CivOne.Services;

namespace CivOne.Persistence.Original.Impl
{
	internal class GameDataQueryAdapter : IGameData
	{
		protected ISaveDataArrayGetAdapter _array;
		protected IArrayGetService _byteArray;
		protected SaveData saveData;
		protected IOriginalGameTime gameTime;

		protected ILogger logger => LoggerProvider.GetLogger();

		public GameDataQueryAdapter(
			SaveData saveData,
			IOriginalGameTime gameTime)
		{
			Debug.Assert(gameTime != null, "gameTime is null");

			_byteArray = ArrayServiceProvider.ProvideGet();
			_array = new SaveDataArrayGetAdapter(saveData, _byteArray);
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
			get => _array.GetArray(nameof(SaveData.LeaderNames), 14, 8);
			set => throw new ReadOnlyException("LeaderNames is read-only.");
		}

		public string[] CivilizationNames
		{
			get => _array.GetArray(nameof(SaveData.CivilizationNames), 12, 8);
			set => throw new ReadOnlyException("CivilizationNames is read-only.");
		}

		public string[] CitizenNames
		{
			get => _array.GetArray(nameof(SaveData.CitizenNames), 11, 8);
			set => throw new ReadOnlyException("CitizenNames is read-only.");
		}

		public string[] CityNames
		{
			get => _array.GetArray(nameof(SaveData.CityNames), 13, 256);
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
			get => [.. GetReplayData()];
			set => throw new ReadOnlyException("ReplayData is read-only.");
		}

		// -- Private Hilfsmethoden (noch zu implementieren) --
		private unsafe byte[][] GetDiscoveredAdvanceIDs()
		{
			byte[] bytes = _array.GetArray(nameof(SaveData.DiscoveredAdvances), 8 * 10);
			byte[][] output = new byte[8][];
			for (int i = 0; i < 8; i++)
				output[i] = bytes.FromBitIds(i * 10, 10).ToArray();
			return output;
		}

		private unsafe CityData[] GetCities()
		{
			SaveData.City[] cities = _array.GetArray<SaveData.City>(nameof(SaveData.Cities), 128);

			List<CityData> output = [];

			for (byte c = 0; c < cities.Length; c++)
			{
				SaveData.City city = cities[c];
				if (city.Status == 0xFF) continue;

				output.Add(new CityData()
				{
					Id = c,
					NameId = city.NameId,
					Buildings = _byteArray.GetBytes<SaveData.City>(city, nameof(SaveData.City.Buildings), 4).FromBitIds(0, 4).ToArray(),
					X = city.X,
					Y = city.Y,
					Status = city.Status,
					ActualSize = city.ActualSize,
					VisibleSize = city.VisibleSize,
					CurrentProduction = city.CurrentProduction,
					Owner = city.Owner,
					Food = city.Food,
					Shields = city.Shields,
					ResourceTiles = _byteArray.GetBytes<SaveData.City>(city, nameof(SaveData.City.ResourceTiles), 6),
					FortifiedUnits = _byteArray.GetBytes<SaveData.City>(city, nameof(SaveData.City.FortifiedUnits), 2).Where(x => x != 0xFF).ToArray(),
					TradingCities = _byteArray.GetBytes<SaveData.City>(city, nameof(SaveData.City.TradingCities), 3).Where(x => x != 0xFF).ToArray()
				});
			}
			return output.ToArray();
		}

		private unsafe UnitData[][] GetUnits()
		{
			SaveData.Unit[] units = _array.GetArray<SaveData.Unit>(nameof(SaveData.Units), 8 * 128);
			UnitData[][] output = new UnitData[8][];

			for (int p = 0; p < 8; p++)
			{
				List<UnitData> unitData = new List<UnitData>();
				for (byte u = 0; u < 128; u++)
				{
					SaveData.Unit unit = units[(p * 128) + u];
					
					if (unit.Type == 0xFF) continue;
					unitData.Add(new UnitData()
					{
						Id = u,
						Status = unit.Status,
						X = unit.X,
						Y = unit.Y,
						TypeId = unit.Type,
						RemainingMoves = unit.RemainingMoves,
						SpecialMoves = unit.SpecialMoves,
						GotoX = unit.GotoX,
						GotoY = unit.GotoY,
						Visibility = unit.Visibility,
						NextUnitId = unit.NextUnitId,
						HomeCityId = unit.HomeCityId
					});
				}
				output[p] = unitData.ToArray();
			}
			return output;
		}

		private unsafe bool[][,] GetTileVisibility()
		{
			byte[] bytes = _array.GetArray(nameof(SaveData.MapVisibility), 80 * 50);
			bool[][,] output = new bool[8][,];
			for (int p = 0; p < 8; p++)
			{
				output[p] = new bool[80, 50];
				for (int i = 0; i < (80 * 50); i++)
				{
					int y = (i % 50), x = (i - y) / 50;
					output[p][x, y] = ((bytes[i] & (1 << p)) > 0);
				}
			}
			return output;
		}

		//CW: Warning. Autogenerated by CoPilot AI from JCivEdit Sources MemoryMapReplay.java
		/// <summary>
		/// Parses replay data from the original Civilization save format.
		/// 
		/// Event Types (from Java reference MemoryMapReplay.java):
		/// - 0x1: City Built/Destroyed (5 bytes) - CityId=0xFF indicates destruction
		/// - 0x2: War Declared (2 bytes) - Packed: (declarer<<4)|target
		/// - 0x3: Peace Made (2 bytes) - Packed: (maker<<4)|with  
		/// - 0x5: Advance Discovered (3 bytes) - Civ, Advance
		/// - 0x6: Unit First Built (3 bytes) - Civ, UnitType
		/// - 0x8: Government Change (3 bytes) - Civ, GovernmentType
		/// - 0x9: City Captured (5 bytes) - Civ, CityNameId, X, Y
		/// - 0xA: Wonder Built (3 bytes) - Civ, WonderType
		/// - 0xB: Replay Summary (4 bytes) - CityCount, Population (2 bytes)
		/// - 0xC: Civilization Rankings (5 bytes) - Packed civilization rankings
		/// - 0xD: Civilization Destroyed (3 bytes) - DestroyedCiv, DestroyerCiv
		/// </summary>
		private unsafe List<ReplayData> GetReplayData()
		{
			ushort replayLength = saveData.ReplayLength;
			byte[] bytes = _array.GetArray(nameof(SaveData.ReplayData), 4096);

			// Debug output: display complete bytes in hex format with 8 per line
			for (int i = 0; i < replayLength; i += 8)
			{
				logger.Log(string.Join(" ", bytes.Skip(i).Take(8).Select(b => b.ToString("X2"))));
			}

			List<ReplayData> output = new List<ReplayData>();
			for (int i = 0; i < replayLength; i++)
			{
				byte entryCode = (byte)((bytes[i] & 0xF0) >> 4);
				int turn = bytes[i + 1] + ((bytes[i] & 0x0F) << 8);

				//CW: Warning. Autogenerated by CoPilot AI from JCivEdit Sources MemoryMapReplay.java
				// Major overhaul necessary.

				switch (entryCode)
				{
					case 0x1: // City Built/Destroyed
							  // Format: EntryHeader(2) + OwnerId(1) + CityNameId(1) + X(1) + Y(1)
							  // CityId=0xFF indicates city destruction
						byte cityOwnerId = bytes[i + 2];
						byte cityNameId = bytes[i + 3];
						byte cityX = bytes[i + 4];
						byte cityY = bytes[i + 5];

						if (cityOwnerId == 0xFF)
						{
							// City Destroyed: CityDestroyed(turn, cityId, cityNameId, x, y)
							// Note: When destroyed, cityOwnerId=0xFF, so we use 0 as cityId
							output.Add(new ReplayData.CityDestroyed(turn, 0, cityNameId, cityX, cityY));
							logger.Log($"City destroyed: City {cityNameId} at ({cityX},{cityY}) turn {turn}");
						}
						else
						{
							// City Built: CityBuilt(turn, ownerId, cityId, cityNameId, x, y) 
							// Note: We use cityNameId as cityId for consistency
							output.Add(new ReplayData.CityBuilt(turn, cityOwnerId, cityNameId, cityNameId, cityX, cityY));
							logger.Log($"City built: Owner {cityOwnerId}, City {cityNameId} at ({cityX},{cityY}) turn {turn}");
						}
						i += 5;
						continue;

					case 0x2: // War Declared
							  // Format: EntryHeader(2) + PackedCivs(1) - (declarer<<4)|target
						byte warData = bytes[i + 2];
						byte warDeclarer = (byte)((warData >> 4) & 0xF);
						byte warTarget = (byte)(warData & 0xF);
						logger.Log($"War declared: Civ {warDeclarer} vs Civ {warTarget} at turn {turn}");
						i += 2;
						continue;

					case 0x3: // Peace Made
							  // Format: EntryHeader(2) + PackedCivs(1) - (maker<<4)|with
						byte peaceData = bytes[i + 2];
						byte peaceMaker = (byte)((peaceData >> 4) & 0xF);
						byte peaceWith = (byte)(peaceData & 0xF);
						logger.Log($"Peace made: Civ {peaceMaker} with Civ {peaceWith} at turn {turn}");
						i += 2;
						continue;

					case 0x5: // Advance Discovered
							  // Format: EntryHeader(2) + CivId(1) + AdvanceId(1)
						byte advanceCiv = bytes[i + 2];
						byte advanceId = bytes[i + 3];
						logger.Log($"Advance discovered: Civ {advanceCiv}, Advance {advanceId} at turn {turn}");
						i += 3;
						continue;

					case 0x6: // Unit First Built
							  // Format: EntryHeader(2) + CivId(1) + UnitTypeId(1)
						byte unitCiv = bytes[i + 2];
						byte unitType = bytes[i + 3];
						logger.Log($"Unit first built: Civ {unitCiv}, Unit {unitType} at turn {turn}");
						i += 3;
						continue;

					case 0x8: // Government Change
							  // Format: EntryHeader(2) + CivId(1) + GovernmentTypeId(1)
						byte govCiv = bytes[i + 2];
						byte govType = bytes[i + 3];
						logger.Log($"Government change: Civ {govCiv}, Government {govType} at turn {turn}");
						i += 3;
						continue;

					case 0x9: // City Captured
							  // Format: EntryHeader(2) + CivId(1) + CityNameId(1) + X(1) + Y(1)
						byte captureCiv = bytes[i + 2];
						byte captureCity = bytes[i + 3];
						byte captureX = bytes[i + 4];
						byte captureY = bytes[i + 5];
						logger.Log($"City captured: Civ {captureCiv}, City {captureCity} at ({captureX},{captureY}) turn {turn}");
						i += 5;
						continue;

					case 0xA: // Wonder Built
							  // Format: EntryHeader(2) + CivId(1) + WonderTypeId(1)
						byte wonderCiv = bytes[i + 2];
						byte wonderType = bytes[i + 3];
						logger.Log($"Wonder built: Civ {wonderCiv}, Wonder {wonderType} at turn {turn}");
						i += 3;
						continue;

					case 0xB: // Replay Summary
							  // Format: EntryHeader(2) + CityCount(1) + PopulationHigh(1) + PopulationLow(1)
						byte cityCount = bytes[i + 2];
						int population = (((bytes[i + 3] << 8) & 0xFF00) + bytes[i + 4]) * 10000;
						logger.Log($"Replay summary: Cities {cityCount}, Population {population} at turn {turn}");
						i += 4;
						continue;

					case 0xC: // Civilization Rankings
							  // Format: EntryHeader(2) + PackedRankings(3) - 4 bits per civ ranking
						logger.Log($"Civilization rankings at turn {turn}");
						for (int rank = 0; rank < 8; rank++)
						{
							int byteIndex = rank / 2 + 2;
							int shift = (rank % 2 == 0) ? 4 : 0;
							int civId = (bytes[i + byteIndex] >> shift) & 0xF;
							logger.Log($"  Rank {rank}: Civ {civId}");
						}
						i += 5;
						continue;

					case 0xD: // Civilization Destroyed
							  // Format: EntryHeader(2) + DestroyedCivId(1) + DestroyerCivId(1)
						logger.Log($"i = {i}, byte[i]={bytes[i]}, byte[i+1]={bytes[i + 1]}, byte[i+2]={bytes[i + 2]}, byte[i+3]={bytes[i + 3]}");
						logger.Log($"Civilization destroyed: {bytes[i + 2]} by {bytes[i + 3]} at turn {turn}");

						output.Add(new ReplayData.CivilizationDestroyed(turn, bytes[i + 2], bytes[i + 3]));
						i += 3;
						continue;

					default:
						// Unknown entry type - stop parsing to avoid corruption
						logger.Log($"Unknown replay entry type: 0x{entryCode:X} at position {i}");
						break;
				}
			}

            return output;
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