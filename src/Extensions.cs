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
using System.IO;
using System.Linq;
using System.Text;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.Leaders;
using CivOne.Units;

namespace CivOne
{
	internal static class Extensions
	{
		private static Settings Settings => Settings.Instance;

		public static string GetSoundFile(this string input)
		{
			return Directory.GetFiles(Settings.SoundsDirectory).Where(x => Path.GetFileName(x).ToLower() == $"{input.ToLower()}.wav").FirstOrDefault();
		}

		public static byte[] Clear(this byte[] byteArray, byte value = 0)
		{
			for (int i = byteArray.GetUpperBound(0); i >= 0; i--)
				byteArray[i] = value;
			return byteArray;
		}

		public static string ToString(this byte[] bytes, int startIndex, int length)
		{
			StringBuilder output = new StringBuilder();
			for (int i = startIndex; i < startIndex + length; i++)
			{
				if (bytes[i] == 0) break;
				output.Append((char)bytes[i]);
			}
			return output.ToString().Trim();
		}

		public static IEnumerable<byte> FromBitIds(this byte[] bytes, int startIndex, int length)
		{
			byte index = 0;
			for (int i = startIndex; i < startIndex + length; i++)
			for (int b = 0; b < 8; b++)
			{
				if ((bytes[i] & (1 << b)) > 0) yield return index;
				index++;
			}
		}

		public static byte[] ToBitIds(this byte[] bytes, int startIndex, int length, byte[] values)
		{
			foreach (byte value in values)
			{
				int bitNo = value % 8;
				int byteNo = (value - bitNo) / 8;
				if (length <= byteNo) continue;
				bytes[startIndex + byteNo] |= (byte)(1 << bitNo);
			}
			return bytes;
		}

		// TODO move to a save/load specific file?
		private static byte GetId(this City city)
		{
			if (city != null)
			{
				City[] cities = Game.Instance.GetCities();
				for (byte c = 0; c < cities.Length; c++)
					if (cities[c] == city) return c;
			}
			return 0xFF;
		}

		// TODO move to a save/load specific file?
		private static CityData GetCityData(this City city, byte id)
		{
            // TODO fire-eggs fails to take 'fortifyING' into account?
            IUnit[] units = city.Tile?.Units.Where(x => x.Home == city && x.Fortify).Take(2).ToArray();
			
			return new CityData {
				Id = city.GetId(),
				NameId = (byte)city.NameId,
				Status = 0,
				Buildings = city.Buildings.Select(b => b.Id).ToArray(),
				X = city.X,
				Y = city.Y,
				ActualSize = city.Size,
				CurrentProduction = city.CurrentProduction.ProductionId,
				Owner = city.Owner,
				Food = (ushort)city.Food,
				Shields = (ushort)city.Shields,
				ResourceTiles = city.GetResourceTiles(),
                // fire-eggs 20190622 make sure to save fortify/veteran status as per Microprose
				FortifiedUnits = units?.Select(x => (byte)((int)x.Type | 0x40 | (x.Veteran ? 0x80 : 0))).ToArray()
			};
		}

		// TODO move to a save/load specific file?
		public static IEnumerable<CityData> GetCityData(this IEnumerable<City> cityList)
		{
			byte index = 0;
			foreach (City city in cityList)
			{
				yield return city.GetCityData(index++);
			}
		}

		// TODO move to a save/load specific file?
		/// <summary>
		/// Fetch Settler-specific "Status" data from internal state.
		/// </summary>
		/// <param name="unit">which unit to fetch for</param>
		/// <param name="result">the bitfield to modify</param>
		private static void GetSettlerStatus(this IUnit unit, ref byte result)
        {
			switch (unit.order)
			{
				case Order.Road:
					result |= 0b00000010;
					break;
				case Order.Irrigate:
					result |= 0b01000000;
					break;
				case Order.Mines:
					result |= 0b10000000;
					break;
				case Order.Fortress:
					result |= 0b11000000;
					break;
				case Order.ClearPollution:
					result |= 0b10000010;
					break;
			}
		}

		// TODO move to a save/load specific file?
		/// <summary>
		/// Translate internal fields to savefile Status field.
		/// </summary>
		/// <param name="unit">which unit to fetch status for</param>
		/// <returns>the status bitfield</returns>
		private static byte GetUnitStatus(this IUnit unit)
        {
			byte result = 0;
			if (unit.Sentry)
				result |= 0x1;
			if (unit.FortifyActive)
				result |= 0x4;
			if (unit.Fortify)  // TODO not the same as _fortify?
				result |= 0x8;
			if (unit.Veteran)
				result |= 0x20; // fire-eggs 20190710 incorrect hex value

			(unit as Settlers)?.GetSettlerStatus(ref result);

			return result;
		}

		// TODO move to a save/load specific file?
		/// <summary>
		/// Translate internal state data to savefile format.
		/// </summary>
		/// <param name="unit">IUnit object to fetch data for</param>
		/// <param name="id">the unit id</param>
		/// <returns></returns>
		private static UnitData GetUnitData(this IUnit unit, byte id)
		{
			byte gotoX = 0xFF, gotoY = 0;
			if (!unit.Goto.IsEmpty)
			{
				gotoX = (byte)unit.Goto.X;
				gotoY = (byte)unit.Goto.Y;
			}

			// TODO need to save (Settlers.)MovesSkip value to savefile

			return new UnitData {
				Id = id,
				Status = unit.GetUnitStatus(),
				X = (byte)unit.X,
				Y = (byte)unit.Y,
				TypeId = (byte)unit.Type,
				RemainingMoves = (byte)((unit.MovesLeft * 3) + unit.PartMoves),
				SpecialMoves = unit.FuelOrProgress,
				GotoX = gotoX,
				GotoY = gotoY,
				Visibility = 0xFF,
				NextUnitId = 0xFF,
				HomeCityId = unit.Home.GetId()
			};
		}

		private static IEnumerable<IUnit> FilterUnits(this List<IUnit> unitList)
		{
			foreach(City city in Game.Instance.GetCities())
			{
				IUnit[] units = unitList.Where(u => u.X == city.X && u.Y == city.Y && u.Home == city).Take(2).ToArray();
				foreach (IUnit unit in units)
				{
					unitList.Remove(unit);
				}
			}
			return unitList;
		}

		// TODO move to a save/load specific file?
		public static IEnumerable<UnitData> GetUnitData(this IEnumerable<IUnit> unitList)
		{
            // TODO fire-eggs don't do this - only applicable in specific situation which no-one understands!
			// Remove two fortified units in home city (this data is stored in city data)
			IEnumerable<IUnit> filteredUnits = unitList.ToList().FilterUnits();

			byte index = 0;
			List<UnitData> unitDataList = new List<UnitData>();
			foreach (IUnit unit in filteredUnits)
			{
				unitDataList.Add(unit.GetUnitData(index++));
			}

			UnitData[] units = unitDataList.ToArray();
			for (int i = 0; i < units.Length; i++)
			{
				if (!units.Any(u => u.Id != units[i].Id && u.X == units[i].X && u.Y == units[i].Y)) continue;
				units[i].NextUnitId = units.Where(u => u.Id != units[i].Id && u.X == units[i].X && u.Y == units[i].Y).OrderBy(u => u.Id > units[i].Id ? 0 : 1).ThenBy(u => u.Id).First().Id;
			}
			return units;
		}

		public static string YesNo(this bool value) => value ? "Yes" : "No";
		public static string OnOff(this bool value) => value ? "On" : "Off";
		public static string EnabledDisabled(this bool value) => value ? "Enabled" : "Disabled";

		public static string ToText(this AspectRatio aspectRatio)
		{
			switch (aspectRatio)
			{
				case AspectRatio.Auto: return "Automatic";
				case AspectRatio.Fixed: return "Fixed";
				case AspectRatio.Scaled: return "Scaled (blurry)";
				case AspectRatio.ScaledFixed: return "Scaled and fixed (blurry)";
				case AspectRatio.Expand: return "Expand (experimental)";
				default: return null;
			}
		}

		public static string ToText(this GraphicsMode graphicsMode)
		{
			switch (graphicsMode)
			{
				case GraphicsMode.Graphics256: return "256 colors";
				case GraphicsMode.Graphics16: return "16 colors";
				default: return null;
			}
		}

		public static string ToText(this CursorType cursorType)
		{
			switch (cursorType)
			{
				case CursorType.Default: return "Default";
				case CursorType.Builtin: return "Built-in";
				case CursorType.Native: return "Native";
				default: return null;
			}
		}

		public static string ToText(this DestroyAnimation destroyAnimation)
		{
			switch (destroyAnimation)
			{
				case DestroyAnimation.Sprites: return "Sprites (original)";
				case DestroyAnimation.Noise: return "Noise";
				default: return null;
			}
		}

		public static string ToText(this GameOption gameOption)
		{
			switch (gameOption)
			{
				case GameOption.Default: return "Default";
				case GameOption.On: return "On";
				case GameOption.Off: return "Off";
				default: return null;
			}
		}

		public static string ToText(this AggressionLevel aggression)
		{
			switch (aggression)
			{
				case AggressionLevel.Friendly: return "Friendly";
				case AggressionLevel.Normal: return "Normal";
				case AggressionLevel.Aggressive: return "Aggressive";
				default: return null;
			}
		}

		public static string ToText(this DevelopmentLevel development)
		{
			switch (development)
			{
				case DevelopmentLevel.Perfectionist: return "Perfectionist";
				case DevelopmentLevel.Normal: return "Normal";
				case DevelopmentLevel.Expansionistic: return "Expansionistic";
				default: return null;
			}
		}

		public static string ToText(this MilitarismLevel militarism)
		{
			switch (militarism)
			{
				case MilitarismLevel.Civilized: return "Civilized";
				case MilitarismLevel.Normal: return "Normal";
				case MilitarismLevel.Militaristic: return "Militaristic";
				default: return null;
			}
		}

		public static string[] Traits(this ILeader leader)
		{
			List<string> output = new List<string>();
			if (leader.Aggression != AggressionLevel.Normal) output.Add(leader.Aggression.ToText());
			if (leader.Development != DevelopmentLevel.Normal) output.Add(leader.Development.ToString());
			if (leader.Militarism != MilitarismLevel.Normal) output.Add(leader.Militarism.ToString());
			return output.ToArray();
		}

		public static IAdvance ToInstance(this Advance advance) => Common.Advances.FirstOrDefault(x => x.Id == (byte)advance);
		public static ILeader ToInstance(this Leader leader)
		{
			switch (leader)
			{
				case Leader.Atilla: return new Atilla();
				case Leader.Caesar: return new Caesar();
				case Leader.Hammurabi: return new Hammurabi();
				case Leader.Frederick: return new Frederick();
				case Leader.Ramesses: return new Ramesses();
				case Leader.Lincoln: return new Lincoln();
				case Leader.Alexander: return new Alexander();
				case Leader.Gandhi: return new Gandhi();
				case Leader.Stalin: return new Stalin();
				case Leader.Shaka: return new Shaka();
				case Leader.Napoleon: return new Napoleon();
				case Leader.Montezuma: return new Montezuma();
				case Leader.Mao: return new Mao();
				case Leader.Elizabeth: return new Elizabeth();
				case Leader.Genghis: return new Genghis();
				default: return null;
			}
		}

		public static IBitmap GifToBitmap(this byte[] buffer)
		{
			using (GifFile gifFile = new GifFile(buffer))
			{
				return gifFile.GetBitmap();
			}
		}

		public static Bytemap MatchColours(this IBitmap input, Palette palette, int startIndex, int length)
		{
			Dictionary<int, int> matches = new Dictionary<int, int>();

			Colour[] pal = input.Palette.Entries.ToArray();
			Colour[] cmp = palette.Entries.ToArray();
			for (int i = 0; i < pal.Length; i++)
			{
				if (pal[i].A == 0)
				{
					matches.Add(i, 0);
					continue;
				}

				int entry = 0;
				int mx = 768;
				for (int j = startIndex; j < cmp.Length && j < (startIndex + length); j++)
				{
					int total = Math.Abs((int)pal[i].R - cmp[j].R) + Math.Abs((int)pal[i].G - cmp[j].G) + Math.Abs((int)pal[i].B - cmp[j].B);
					if (total >= mx) continue;
					entry = j;
					mx = total;
				}
				matches.Add(i, entry);
			}
			
			Bytemap output = new Bytemap(input.Width(), input.Height());
			for (int yy = 0; yy < input.Height(); yy++)
			for (int xx = 0; xx < input.Height(); xx++)
			{
				output[xx, yy] = (byte)matches[input.Bitmap[xx, yy]];
			}
			return output;
		}

		public static Picture MakePalette(this IBitmap bitmap, int startIndex, int colourLength)
		{
			Dictionary<byte, int> colourCount = new Dictionary<byte, int>();
			foreach (byte colourIndex in bitmap.Bitmap.ToByteArray())
			{
				if (bitmap.Palette[colourIndex].A == 0) continue; // Do not count transparent
				if (colourCount.ContainsKey(colourIndex))
				{
					colourCount[colourIndex]++;
					continue;
				}
				colourCount.Add(colourIndex, 1);
			}

			Colour[] colours = colourCount.OrderByDescending(x => x.Value).Select(x => bitmap.Palette[x.Key]).Take(colourLength).ToArray();
			Colour[] palette;
			if (Settings.GraphicsMode == GraphicsMode.Graphics256)
			{
				palette = new Colour[256];
				palette[0] = Colour.Transparent;
				Array.Copy(colours, 0, palette, startIndex, Math.Min(colourLength, colours.Length));
			}
			else
			{
				palette = Common.GetPalette16.Entries.ToArray();
			}
			
			Bytemap bytemap = bitmap.MatchColours(palette, startIndex, colourLength);
			return new Picture(bytemap, palette);
		}
	}
}