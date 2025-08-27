// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CivOne.IO;

namespace CivOne
{
	internal partial class SaveDataAdapter
	{
		private void SetArray<T>(ref T structure, string fieldName, params byte[] values) where T : struct
		{
			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
			Marshal.StructureToPtr(structure, ptr, false);
			IntPtr offset = IntPtr.Add(ptr, (int)Marshal.OffsetOf<T>(fieldName));
			Marshal.Copy(values, 0, offset, values.Length);
			structure = Marshal.PtrToStructure<T>(ptr);
			Marshal.FreeHGlobal(ptr);
		}

		private void SetArray(string fieldName, params byte[] values) => SetArray(ref _saveData, fieldName, values);

		private void SetArray<T>(string fieldName, params T[] values) where T : struct
		{
			int itemSize = Marshal.SizeOf<T>();
			byte[] bytes = new byte[values.Length * itemSize];
			for (int i = 0; i < values.Length; i++)
			{
				T value = values[i];
				IntPtr ptr = Marshal.AllocHGlobal(itemSize);
				Marshal.StructureToPtr(value, ptr, false);
				Marshal.Copy(ptr, bytes, (i * itemSize), itemSize);
				Marshal.FreeHGlobal(ptr);
			}
			SetArray(fieldName, bytes);
		}
		
		private void SetArray(string fieldName, params short[] values)
		{
			byte[] bytes = new byte[values.Length * 2];
			Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
			SetArray(fieldName, bytes);
		}

		private void SetArray(string fieldName, params ushort[] values)
		{
			byte[] bytes = new byte[values.Length * 2];
			Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
			SetArray(fieldName, bytes);
		}

		private void SetArray<T>(ref T structure, string fieldName, int itemLength, params string[] values) where T : struct
		{
			byte[] bytes = new byte[itemLength * values.Length];
			for (int i = 0; i < values.Length; i++)
			for (int c = 0; c < itemLength; c++)
				bytes[(i * itemLength) + c] = (c >= values[i].Length) ? (byte)0 : (byte)values[i][c];
			SetArray(ref structure, fieldName, bytes);
		}

		private void SetArray(string fieldName, int itemLength, params string[] values) => SetArray(ref _saveData, fieldName, itemLength, values);

		private unsafe void SetDiscoveredAdvanceIDs(byte[][] input)
		{
			byte[] bytes = new byte[8 * 10];
			for (int p = 0; p < 8; p++)
			{
				if (p >= input.Length) continue;
				bytes = bytes.ToBitIds(p * 10, 10, input[p]);
			}
			SetArray(nameof(SaveData.DiscoveredAdvances), bytes);
		}

		private unsafe void SetAdvancesCount(ushort[] values) => SetArray(nameof(SaveData.AdvancesCount), values);

		private unsafe void SetLeaderNames(string[] values) => SetArray(nameof(SaveData.LeaderNames), 14, values);

		private unsafe void SetCivilizationNames(string[] values) => SetArray(nameof(SaveData.CivilizationNames), 12, values);

		private unsafe void SetCitizenNames(string[] values) => SetArray(nameof(SaveData.CitizensName), 11, values);

		private unsafe void SetCityNames(string[] values) => SetArray(nameof(SaveData.CityNames), 13, values);

		private unsafe void SetPlayerGold(short[] values) => SetArray(nameof(SaveData.PlayerGold), values);

		private unsafe void SetResearchProgress(short[] values) => SetArray(nameof(SaveData.ResearchProgress), values);

		private unsafe void SetUnitsActive(UnitData[][] unitData)
		{
			ushort[] data = new ushort[8 * 28];
			for (int p = 0; p < unitData.Length; p++)
			for (int i = 0; i < 28; i++)
			{
				data[(p * 28) + i] = (ushort)unitData[p].Count(u => u.TypeId == i);
			}
			SetArray(nameof(SaveData.UnitsActive), data);
		}

		private unsafe void SetTaxRate(ushort[] values) => SetArray(nameof(SaveData.TaxRate), values);

		private unsafe void SetScienceRate(ushort[] values) => SetArray(nameof(SaveData.ScienceRate), values);

		private unsafe void SetStartingPositionX(ushort[] values) => SetArray(nameof(SaveData.StartingPositionX), values);

		private unsafe void SetGovernment(ushort[] values) => SetArray(nameof(SaveData.Government), values);

		private unsafe void SetCityCount(ushort[] values) => SetArray(nameof(SaveData.CityCount), values);

		private unsafe void SetSettlerCount(ushort[] values) => SetArray(nameof(SaveData.SettlerCount), values);

		private unsafe void SetUnitCount(ushort[] values) => SetArray(nameof(SaveData.UnitCount), values);

		private unsafe void SetTotalCitySize(ushort[] values) => SetArray(nameof(SaveData.TotalCitySize), values);

		byte[] CopyArrayMax(byte[] source, int maxSourceLength, int maxDestLength, byte fillValue = 0)
		{
			byte[] dest = new byte[maxDestLength];
			Array.Fill(dest, fillValue);

			if (source == null || source.Length == 0)
			{
				return dest;
			}

			int length = Math.Min(source.Length, maxSourceLength);
			Array.Copy(source, dest, length);

			return dest;
		}

		private unsafe void SetCities(CityData[] values)
		{
			byte INVALID = 0xFF;
			SaveData.City[] cities = GetArray<SaveData.City>(nameof(SaveData.Cities), 128);

			Debug.Assert(values.Length <= 128, $"CityData array length {values.Length} exceeds 128");

			for (int i = 0; i < Math.Min(values.Length, 128); i++)
			{
				CityData data = values[i];

				byte[] fortifiedUnits = CopyArrayMax(data.FortifiedUnits, 2, 2, INVALID);
				byte[] tradingCities = CopyArrayMax(data.TradingCities, 3, 3, INVALID);

				SetArray(ref cities[i], nameof(SaveData.City.Buildings), new byte[4].ToBitIds(0, 4, data.Buildings));
				cities[i].X = data.X;
				cities[i].Y = data.Y;
				cities[i].Status = data.Status;
				cities[i].ActualSize = data.ActualSize;
				cities[i].VisibleSize = data.ActualSize;
				cities[i].CurrentProduction = data.CurrentProduction;
				cities[i].BaseTrade = data.BaseTrade;
				cities[i].Owner = data.Owner;
				cities[i].Food = data.Food;
				cities[i].Shields = data.Shields;
				SetArray(ref cities[i], nameof(SaveData.City.ResourceTiles), data.ResourceTiles);
				cities[i].NameId = data.NameId;
				SetArray(ref cities[i], nameof(SaveData.City.TradingCities), tradingCities);
				SetArray(ref cities[i], nameof(SaveData.City.FortifiedUnits), fortifiedUnits);
			}
			SetArray(nameof(SaveData.Cities), cities);
		}

		private unsafe void SetUnitTypes(SaveData.UnitType[] types) => SetArray(nameof(SaveData.UnitTypes), types);

		private unsafe void SetUnits(UnitData[][] values)
		{
			SaveData.Unit[] units = GetArray<SaveData.Unit>(nameof(SaveData.Units), 8 * 128);

			for (int p = 0; p < new[] { values.Length, 8 }.Min(); p++)
			for (int u = 0; u < new[] { values[p].Length, 128 }.Min(); u++)
			{
				UnitData data = values[p][u];

				int i = (p * 128) + u;
				units[i].Status = data.Status;
				units[i].X = data.X;
				units[i].Y = data.Y;
				units[i].Type = data.TypeId;
				units[i].RemainingMoves = data.RemainingMoves;
				units[i].SpecialMoves = data.SpecialMoves;
				units[i].GotoX = data.GotoX;
				units[i].GotoY = data.GotoY;
				units[i].Visibility = data.Visibility;
				units[i].NextUnitId = data.NextUnitId;
				units[i].HomeCityId = data.HomeCityId;
			}

			SetArray(nameof(SaveData.Units), units);
		}

		private unsafe void SetWonders(ushort[] values) => SetArray<ushort>(nameof(SaveData.Wonders), values);

		private unsafe void SetTileVisibility(bool[][,] values)
		{
			byte[] bytes = new byte[80 * 50];
			for (int p = 0; p < values.Length; p++)
			{
				for (int y = 0; y < 50; y++)
				for (int x = 0; x < 80; x++)
				{
					bytes[(x * 50) + y] |= (byte)(values[p][x, y] ? (1 << p) : 0);
				}
			}
			SetArray(nameof(SaveData.MapVisibility), bytes);
		}

		private unsafe void SetAdvanceFirstDiscovery(ushort[] values) => SetArray(nameof(SaveData.AdvanceFirstDiscovery), values);

		private unsafe void SetCityX(byte[] values) => SetArray(nameof(SaveData.CityX), values);
		
		private unsafe void SetCityY(byte[] values) => SetArray(nameof(SaveData.CityY), values);

		private unsafe void SetReplayData(ReplayData[] values)
		{
			List<byte> output = new List<byte>();
			foreach (ReplayData value in values)
			{
				byte entryId;
				byte[] data;
				switch (value)
				{
					case ReplayData.CivilizationDestroyed civDestroyed:
						entryId = 0xD0;
						// CW: 0-7 range handled in CivilizationDestroyed constructor.
						data = [(byte)civDestroyed.DestroyedId,
								(byte)civDestroyed.DestroyedById ];
						break;
					default:
						continue;
				}

                output.Add((byte)(entryId | ((byte)(value.Turn & 0x0F00)) >> 8));
				output.Add((byte)(value.Turn & 0xFF));
				output.AddRange(data);
			}

            var outArr = output.ToArray();
			_saveData.ReplayLength = (ushort)output.Count;
            fixed (byte* p = _saveData.ReplayData)
            {
                for (int i = 0; i < output.Count; i++)
                    p[i] = outArr[i];
            }

            //SetArray(nameof(SaveData.ReplayData), output.ToArray());
		}
	}
}