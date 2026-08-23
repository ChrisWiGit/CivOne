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
using CivOne.Enums;

namespace CivOne
{
	public interface IPalaceData
	{
		int PalaceLeft { get; }
		int PalaceRight { get; }

		PalaceStyle GetPalaceStyle(int index);
		byte GetPalaceLevel(int index);
		byte GetGardenLevel(int index);
		int UpgradeCount { get; }
		bool CanUpgrade { get; }
		bool IsSlotUnlocked(int index);
	}

	public class PalaceData : IPalaceData
	{
		readonly byte[] PalaceStyle = new byte[7];
		readonly byte[] PalaceLevel = new byte[7];
		readonly byte[] GardenLevel = new byte[3];

		public int PalaceLeft
		{
			get
			{
				for (int i = 0; i < 3; i++)
				{
					if (PalaceLevel[i] > 0) return i;
				}
				return 2;
			}
		}

		public int PalaceRight
		{
			get
			{
				for (int i = 6; i > 3; i--)
				{
					if (PalaceLevel[i] > 0) return i;
				}
				return 4;
			}
		}

		public PalaceStyle GetPalaceStyle(int index)
		{
			if (index < 0 || index > 6) throw new InvalidOperationException("Invalid palace index");
			return (PalaceStyle)PalaceStyle[index];
		}

		public byte GetPalaceLevel(int index)
		{
			if (index < 0 || index > 6) throw new InvalidOperationException("Invalid palace index");
			return PalaceLevel[index];
		}

		public byte GetGardenLevel(int index)
		{
			if (index < 0 || index > 2) throw new InvalidOperationException("Invalid garden index");
			return GardenLevel[index];
		}

		public int UpgradeCount => PalaceLevel.Sum(x => x) + GardenLevel.Sum(x => x);

		public bool CanUpgrade
		{
			get
			{
				for (int i = 0; i < 7; i++)
				{
					if (IsSlotUnlocked(i) && PalaceLevel[i] < 4)
					{
						return true;
					}
				}

				return GardenLevel.Any(x => x < 3);
			}
		}

		public bool IsSlotUnlocked(int index)
		{
			return index switch
			{
				3 => true,
				2 => true,
				4 => true,
				1 => PalaceLevel[2] > 0,
				5 => PalaceLevel[4] > 0,
				0 => PalaceLevel[1] > 0,
				6 => PalaceLevel[5] > 0,
				_ => false
			};
		}

		public void SetPalace(int index, byte style, byte level)
		{
			if (index < 0 || index > 6)
				throw new InvalidOperationException($"Invalid palace index: {index}");
			if (style < 0 || style > 3)
				throw new InvalidOperationException($"Invalid palace style: {style}");
			if (level < 0 || level > 4)
				throw new InvalidOperationException($"Invalid palace level: {level}");

			if (level == 0 || style == 0)
			{
				PalaceStyle[index] = 0;
				PalaceLevel[index] = 0;
				return;
			}
			PalaceStyle[index] = style;
			PalaceLevel[index] = level;
		}

		public void SetGarden(int index, byte level)
		{
			if (index < 0 || index > 2) throw new InvalidOperationException("Invalid garden index");
			if (level < 0 || level > 3) throw new InvalidOperationException("Invalid garden level");

			GardenLevel[index] = level;
		}
	}
}