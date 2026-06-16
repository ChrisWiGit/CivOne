// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CivOne.Enums;
using CivOne.IO;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.Units;

namespace CivOne
{
	#pragma warning disable CA1822 // Mark members as static
	internal partial class SaveDataAdapter : IGameData
	{
		private static ICheckedValueSanitizer CVS => ValueSanitizerFactory.GetCheckedValueSanitizer();

		private CityData[] DefaultCityData => [.. Enumerable.Range(0, 128).Select(x => new CityData() { Status = 0xFF, Buildings = new byte[4], ResourceTiles = new byte[6] })];
		private UnitData[][] DefaultUnitData => [.. Enumerable.Repeat(Enumerable.Range(0, 128).Select(id => new UnitData() { Id = (byte)id, TypeId = 0xFF }).ToArray(), 8)];

		private SaveData.UnitType GetUnitType(byte typeId)
		{
			IUnit? unit = Reflect.GetUnits().FirstOrDefault(u => u.Type == (UnitType)typeId);
			SaveData.UnitType output = new();
			if (unit != null)
			{
				SetArray(ref output, nameof(SaveData.UnitType.Name), 12, unit.Name);
				output.ObsoleteTechId = CVS.CheckedUInt16(unit.ObsoleteTech?.Id ?? 0x7F, nameof(SaveDataAdapter), "UnitType.ObsoleteTechId");
				output.TerrainCategory = CVS.CheckedUInt16((int)unit.UnitCategory, nameof(SaveDataAdapter), "UnitType.TerrainCategory");
				output.TotalMoves = CVS.CheckedUInt16(unit.Move, nameof(SaveDataAdapter), "UnitType.TotalMoves");
				output.Attack = CVS.CheckedUInt16(unit.Attack, nameof(SaveDataAdapter), "UnitType.Attack");
				output.Defense = CVS.CheckedUInt16(unit.Defense, nameof(SaveDataAdapter), "UnitType.Defense");
				output.Price = CVS.CheckedUInt16(unit.Price, nameof(SaveDataAdapter), "UnitType.Price");
				switch (unit)
				{
					case BaseUnitAir airUnit:
						output.OutsideMoves = CVS.CheckedUInt16(airUnit.TotalFuel, nameof(SaveDataAdapter), "UnitType.OutsideMoves");
						output.ViewRange = 2;
						break;
					case BaseUnitSea seaUnit:
						output.ViewRange = CVS.CheckedUInt16(seaUnit.Range, nameof(SaveDataAdapter), "UnitType.ViewRange");
						break;
					default:
						output.ViewRange = 1;
						break;
				}
				if (unit is IBoardable boardableUnit)
				{
					output.TransportCapacity = CVS.CheckedUInt16(boardableUnit.Cargo, nameof(SaveDataAdapter), "UnitType.TransportCapacity");
				}
				output.Role = CVS.CheckedUInt16((int)unit.Role, nameof(SaveDataAdapter), "UnitType.Role");
				output.TechId = CVS.CheckedUInt16(unit.RequiredTech?.Id ?? ushort.MaxValue, nameof(SaveDataAdapter), "UnitType.TechId");
			}
			return output;
		}

		private SaveData.UnitType[] DefaultUnitTypes => [.. Enumerable.Range(0, 28).Select(i => GetUnitType((byte)i))];

		private SaveData _saveData;

		public ushort GameTurn
		{
			get => _saveData.GameTurn;
			set
			{
				_saveData.GameTurn = value;
				_saveData.GameYear = CVS.CheckedInt16(Common.TurnToYear(value), nameof(SaveDataAdapter), "GameYear");
			}
		}

		public ushort HumanPlayer
		{
			get => _saveData.HumanPlayer;
			set
			{
				_saveData.HumanPlayer = value;
				_saveData.HumanPlayerBit = CVS.CheckedByte(0x01 << value, nameof(SaveDataAdapter), "HumanPlayerBit");
			}
		}

		public ushort RandomSeed
		{
			get => _saveData.RandomSeed;
			set => _saveData.RandomSeed = value;
		}

		public ushort Difficulty
		{
			get => _saveData.Difficulty;
			set => _saveData.Difficulty = value;
		}

        // TODO fire-eggs: is bit order compatible with CivDOS?
		public bool[] ActiveCivilizations // TODO fire-eggs duplicated code
		{
			get
			{
				bool[] output = new bool[8];
				for (int i = 0; i < 8; i++)
					output[i] = (_saveData.ActiveCivilizations & (1 << i)) > 0;
				return output;
			}
			set
			{
				ushort setValue = 0;
				for (int i = 0; i < value.Length; i++)
					setValue |= CVS.CheckedUInt16(value[i] ? (1 << i) : 0, nameof(SaveDataAdapter), "ActiveCivilizations.BitMask");
				_saveData.ActiveCivilizations = setValue;
			}
		}

        // TODO fire-eggs: is bit order compatible with CivDOS?
		public byte[] CivilizationIdentity // TODO fire-eggs duplicated code
        {
			get
			{
				byte[] output = new byte[8];
				for (int i = 0; i < 8; i++)
					output[i] = CVS.CheckedByte(((_saveData.CivilizationIdentityFlag & (1 << i)) > 0) ? 1 : 0, nameof(SaveDataAdapter), "CivilizationIdentity.ReadFlag");
				return output;
			}
			set
			{
				ushort setValue = 0;
                for (int i = 0; i < value.Length; i++)
                    setValue |= CVS.CheckedUInt16(value[i] << i, nameof(SaveDataAdapter), "CivilizationIdentity.BitMask");
				_saveData.CivilizationIdentityFlag = setValue;
			}
		}

		public ushort CurrentResearch
		{
			get => _saveData.CurrentResearch;
			set => _saveData.CurrentResearch = value;
		}

		public byte[][] DiscoveredAdvanceIDs
		{
			get => GetDiscoveredAdvanceIDs();
			set
			{
				SetDiscoveredAdvanceIDs(value);
				SetArray(nameof(SaveData.AdvancesCount), value.Select(x => CVS.CheckedUInt16(x.Length, nameof(SaveDataAdapter), "AdvancesCount")).ToArray());
			}
		}

		public string[] LeaderNames
		{
			get => GetLeaderNames();
			set => SetArray(nameof(SaveData.LeaderNames), 14, value);
		}

		public string[] CivilizationNames
		{
			get => GetCivilizationNames();
			set => SetCivilizationNames(value);
		}

		public string[] CitizenNames
		{
			get => GetCitizenNames();
			set => SetCitizenNames(value);
		}

		public string[] CityNames
		{
			get => GetCityNames();
			set => SetCityNames(value);
		}

		public short[] PlayerGold
		{
			get => GetPlayerGold();
			set => SetPlayerGold(value);
		}

		public short[] ResearchProgress
		{
			get => GetResearchProgress();
			set => SetResearchProgress(value);
		}

		public ushort[] TaxRate
		{
			get => GetTaxRate();
			set => SetTaxRate(value);
		}

		public ushort[] ScienceRate
		{
			get => GetScienceRate();
			set => SetScienceRate(value);
		}

		public ushort[] HumanContactTurns
		{
			get => GetHumanContactTurns();
			set => SetHumanContactTurns(value);
		}

		public ushort[] StartingPositionX
		{
			get => GetStartingPositionX();
			set => SetStartingPositionX(value);
		}

		public ushort[] Government
		{
			get => GetGovernment();
			set => SetGovernment(value);
		}

		public CityData[] Cities
		{
			get => GetCities();
			set
			{
				SetCities(value);
				SetCityX([.. Enumerable.Range(0, 256).Select(i => value.Any(x => x.NameId == i)
					? CVS.CheckedByte(value.First(x => x.NameId == i).X, nameof(SaveDataAdapter), "CityX")
					: (byte)0xFF)]);
				SetCityY([.. Enumerable.Range(0, 256).Select(i => value.Any(x => x.NameId == i)
					? CVS.CheckedByte(value.First(x => x.NameId == i).Y, nameof(SaveDataAdapter), "CityY")
					: (byte)0xFF)]);
				SetCityCount([.. Enumerable.Range(0, 8).Select(i => CVS.CheckedUInt16(value.Count(c => c.Owner == i), nameof(SaveDataAdapter), "CityCount"))]);
				SetTotalCitySize([.. Enumerable.Range(0, 8).Select(i => CVS.CheckedUInt16(value.Sum(c => c.ActualSize), nameof(SaveDataAdapter), "TotalCitySize"))]);
			}
		}

		public UnitData[][] Units
		{
			get => GetUnits();
			set
			{
				SetUnits(value);
				SetUnitCount([.. value.Select(p => CVS.CheckedUInt16(p.Length, nameof(SaveDataAdapter), "UnitCount"))]);
				SetUnitsActive(value);
				SetSettlerCount([.. value.Select(p => CVS.CheckedUInt16(p.Count(u => u.TypeId == (byte)UnitType.Settlers), nameof(SaveDataAdapter), "SettlerCount"))]);
			}
		}

		public ushort[] Wonders
		{
			get => GetWonders();
			set => SetWonders(value);
		}

		public bool[][,] TileVisibility
		{
			get => GetTileVisibility();
			set => SetTileVisibility(value);
		}

		public ushort[] AdvanceFirstDiscovery
		{
			get => GetAdvanceFirstDiscovery();
			set => SetAdvanceFirstDiscovery(value);
		}

        // TODO fire-eggs: is bit order compatible with CivDOS?
		public bool[] GameOptions // TODO fire-eggs duplicated code
        {
			get
			{
				bool[] output = new bool[8];
				for (int i = 0; i < output.Length; i++)
					output[i] = ((_saveData.GameOptions & (1 << i)) > 0);
				return output;
			}
			set
			{
				ushort setValue = 0;
				for (int i = 0; i < value.Length; i++)
					setValue |= CVS.CheckedUInt16(value[i] ? 1 << i : 0, nameof(SaveDataAdapter), "GameOptions.BitMask");
				_saveData.GameOptions = setValue;
			}
		}

		public ushort NextAnthologyTurn
		{
			get => _saveData.NextAnthologyTurn;
			set => _saveData.NextAnthologyTurn = value;
		}

		public ushort OpponentCount
		{
			get => _saveData.OpponentCount;
			set => _saveData.OpponentCount = value;
		}

		public ushort GlobalWarmingCount
		{
			get => _saveData.GlobalWarming;
			set => _saveData.GlobalWarming = value;
		}

		public ushort PollutedSquaresCount
		{
			get => _saveData.PollutionSquares;
			set => _saveData.PollutionSquares = value;
		}

		public ushort WarmingIndicator
		{
			get => _saveData.PollutionEffect;
			set => _saveData.PollutionEffect = value;
		}

		public ushort PeaceTurns
		{
			get => _saveData.PeaceTurns;
			set => _saveData.PeaceTurns = value;
		}

		public ushort PlayerFutureTech
		{
			get => _saveData.PlayerFutureTech;
			set => _saveData.PlayerFutureTech = value;
		}

		public ReplayData[] ReplayData
		{
			get => [.. GetReplayData()];
			set => SetReplayData(value);
		}

		public bool ValidData { get; private set; }

		public byte[] GetBytes()
		{
			if (!ValidData) return Array.Empty<byte>();

			byte[] output = new byte[Marshal.SizeOf<SaveData>()];

			IntPtr buffer = Marshal.AllocHGlobal(output.Length);
			Marshal.StructureToPtr(_saveData, buffer, false);
			Marshal.Copy(buffer, output, 0, output.Length);
			Marshal.FreeHGlobal(buffer);
			return output;
		}

		public bool ValidMapSize(int width, int height) => (width == 80 && height == 50);

		public static SaveDataAdapter Load(byte[] input) => new(input);

		private SaveDataAdapter(byte[] input)
		{
			int expectedSize = Marshal.SizeOf<SaveData>();
			if (input.Length != expectedSize)
			{
				RuntimeHandler.Runtime.Log($"SaveDataAdapter: Invalid file size {input.Length} (expected {expectedSize})");

				ValidData = false;
				return;
			}

			IntPtr dataPtr = Marshal.AllocHGlobal(expectedSize);
			Marshal.Copy(input, 0, dataPtr, input.Length);
			_saveData = Marshal.PtrToStructure<SaveData>(dataPtr);
			Marshal.FreeHGlobal(dataPtr);

			ValidData = true;
		}

		internal SaveDataAdapter()
		{
			_saveData = new SaveData();
			SetCities(DefaultCityData);
			SetUnitTypes(DefaultUnitTypes);
			SetUnits(DefaultUnitData);
			SetWonders([.. Enumerable.Repeat(ushort.MaxValue, 22)]);
			SetCityX([.. Enumerable.Repeat((byte)0xFF, 256)]);
			SetCityY([.. Enumerable.Repeat((byte)0xFF, 256)]);
			ValidData = true;
		}

		void IDisposable.Dispose()
		{
		}
	}
}