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
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.Services;
using CivOne.Units;

namespace CivOne
{
	internal static class Extensions
	{
		private static Settings Settings => Settings.Instance;
		// CVS = Checked Value Sanitizer. Used for all conversions. Shorthand to clarify intent and reduce verbosity in this save/load specific code.
		private static ICheckedValueSanitizer CVS => ValueSanitizerFactory.GetCheckedValueSanitizer();

		private static string T(this string input) => TranslationServiceFactory.GetCurrent().Translate(input);
		

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
			var cityNameId = CVS.CheckedByte(city.NameId, nameof(Extensions), "CityData.NameId");
			var visibleSize = CVS.CheckedByte(city.VisibleSizeToHumanPlayer, nameof(Extensions), "CityData.VisibleSize");
			var food = CVS.CheckedUInt16(city.Food, nameof(Extensions), "CityData.Food");
			var shields = CVS.CheckedUInt16(city.Shields, nameof(Extensions), "CityData.Shields");
			var baseTrade = CVS.CheckedByte(city.TradeTotal, nameof(Extensions), "CityData.BaseTrade");
			
			return new CityData
			{
				Id = city.GetId(),
				NameId = cityNameId,
				Status = city.Status,
				Buildings = city.Buildings.Select(b => b.Id).ToArray(),
				X = city.X,
				Y = city.Y,
				ActualSize = city.Size,
				VisibleSize = visibleSize,
				CurrentProduction = city.CurrentProduction.ProductionId,
				Owner = city.Owner,
				Food = food,
				Shields = shields,
				// BaseTrade is written solely for compatibility with original CIV1 DOS saves.
				// CivOne itself never reads this value back – trade is recalculated dynamically
				// from resource tiles each turn. Original CIV1 uses this cached value in the
				// trade-route yield formula: (BaseTrade_A + BaseTrade_B + 4) / 8.
				BaseTrade = baseTrade,
				ResourceTiles = city.GetResourceTiles(),
				// fire-eggs 20190622 make sure to save fortify/veteran status as per Microprose
				FortifiedUnits = units?.Select(x => (byte)((int)x.Type | 0x40 | (x.Veteran ? 0x80 : 0))).ToArray(),
				TradingCities = [.. city.TradingCitiesAsCity.Select(c => c.GetId())]
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
				case Order.None:
				case Order.NewCity:
				case Order.Sentry:
				case Order.Fortify:
				case Order.Wait:
				case Order.Skip:
				case Order.Unload:
				case Order.Disband:
					break;
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
				default:
					throw new InvalidOperationException($"Unexpected settler order {unit.order} in GetSettlerStatus");
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
				gotoX = CVS.CheckedByte(unit.Goto.X, nameof(Extensions), "UnitData.GotoX");
				gotoY = CVS.CheckedByte(unit.Goto.Y, nameof(Extensions), "UnitData.GotoY");
			}

			var remainingMoves = (unit.MovesLeft * 3) + unit.PartMoves;
			var unitX = CVS.CheckedByte(unit.X, nameof(Extensions), "UnitData.X");
			var unitY = CVS.CheckedByte(unit.Y, nameof(Extensions), "UnitData.Y");
			var unitTypeId = CVS.CheckedByte((int)unit.Type, nameof(Extensions), "UnitData.TypeId");

			// TODO need to save (Settlers.)MovesSkip value to savefile

			return new UnitData {
				Id = id,
				Status = unit.GetUnitStatus(),
				X = unitX,
				Y = unitY,
				TypeId = unitTypeId,
				RemainingMoves = CVS.CheckedByte(remainingMoves, nameof(Extensions), "UnitData.RemainingMoves"),
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
			unitList.RemoveAll(unit =>
				unit.Home != null &&
				unit.Fortify &&
				Game.Instance.GetCities().Any(city =>
					unit.X == city.X && unit.Y == city.Y && unit.Home == city
				)
			);
			return unitList;
		}

		// TODO move to a save/load specific file?
		public static IEnumerable<UnitData> GetUnitData(this IEnumerable<IUnit> unitList)
		{
			// CW: See REMARKS.md. Currently we do this always. This is not the same as in the original game.
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

		public static string YesNo(this bool value) => value ?  T("Yes") : T("No");
		public static string OnOff(this bool value) => value ? T("On") : T("Off");
		public static string EnabledDisabled(this bool value) => value ? T("Enabled") : T("Disabled");

		public static string ToText(this AspectRatio aspectRatio)
		{
			switch (aspectRatio)
			{
				case AspectRatio.Auto: return T("Automatic");
				case AspectRatio.Fixed: return T("Fixed");
				case AspectRatio.Scaled: return T("Scaled (blurry)");
				case AspectRatio.ScaledFixed: return T("Scaled and fixed (blurry)");
				case AspectRatio.Expand: return T("Expand");
				default: return null;
			}
		}

		public static string ToText(this GraphicsMode graphicsMode)
		{
			switch (graphicsMode)
			{
				case GraphicsMode.Graphics256: return T("256 colors");
				case GraphicsMode.Graphics16: return T("16 colors");
				default: return null;
			}
		}

		public static string ToText(this SimulateInternationalFont simulateInternationalFont)
		{
			switch (simulateInternationalFont)
			{
				case SimulateInternationalFont.Auto: return T("Auto");
				case SimulateInternationalFont.Yes: return T("Yes");
				case SimulateInternationalFont.No: return T("No");
				default: return null;
			}
		}

		public static string ToText(this CursorType cursorType)
		{
			switch (cursorType)
			{
				case CursorType.Default: return T("Default");
				case CursorType.Builtin: return T("Built-in");
				case CursorType.Native: return T("Native");
				default: return null;
			}
		}

		public static string ToText(this DestroyAnimation destroyAnimation)
		{
			switch (destroyAnimation)
			{
				case DestroyAnimation.Sprites: return T("Sprites (original)");
				case DestroyAnimation.Noise: return T("Noise");
				default: return null;
			}
		}

		public static string ToText(this GameOption gameOption)
		{
			switch (gameOption)
			{
				case GameOption.Default: return T("Default");
				case GameOption.On: return T("On");
				case GameOption.Off: return T("Off");
				default: return null;
			}
		}

		public static string ToText(this AggressionLevel aggression)
		{
			switch (aggression)
			{
				case AggressionLevel.Friendly: return T("Friendly");
				case AggressionLevel.Normal: return T("Normal");
				case AggressionLevel.Aggressive: return T("Aggressive");
				default: return null;
			}
		}

		public static string ToText(this DevelopmentLevel development)
		{
			switch (development)
			{
				case DevelopmentLevel.Perfectionist: return T("Perfectionist");
				case DevelopmentLevel.Normal: return T("Normal");
				case DevelopmentLevel.Expansionistic: return T("Expansionistic");
				default: return null;
			}
		}

		public static string ToText(this MilitarismLevel militarism)
		{
			switch (militarism)
			{
				case MilitarismLevel.Civilized: return T("Civilized");
				case MilitarismLevel.Normal: return T("Normal");
				case MilitarismLevel.Militaristic: return T("Militaristic");
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
			// Anti-Refactor-Notice:
			// Safe despite returning from inside a using-block: GifFile.GetBitmap() returns
			// a new Picture(_pixels, _palette), and that Picture constructor performs a deep
			// copy of both the Bytemap (Bytemap.Copy) and the Palette (Palette.Copy).
			// The returned IBitmap therefore does not share buffers with the GifFile instance
			// and stays valid after Dispose(). Additionally GifFile.Dispose() is currently a
			// no-op, so disposal does not invalidate any state either way.
			using GifFile gifFile = new(buffer);
			return gifFile.GetBitmap();
		}

		/// <summary>
		/// Match the colours in the input bitmap to the closest possible match in the provided palette, 
		/// and return a bytemap with the remapped colour indices. This is used for loading external bitmaps as game assets, 
		/// to ensure they are displayed with the correct colours regardless of their original palette. 
		/// The startIndex and length parameters allow specifying a subset of the palette to match against, 
		/// which is useful when the palette contains reserved colours that should not be used for matching (e.g. transparency or UI colours).
		/// </summary>
		/// <param name="input">The input bitmap to match colours from.</param>
		/// <param name="palette">The target palette to match colours against.</param>
		/// <param name="startIndex">The starting index in the palette to begin matching.</param>
		/// <param name="length">The number of colours in the palette to consider for matching.</param>
		/// <returns>A bytemap with the remapped colour indices.</returns>
		public static Bytemap MatchColours(this IBitmap input, Palette palette, int startIndex, int length)
		{
			Dictionary<int, int> matches = [];

			Colour[] pal = [.. input.Palette.Entries];
			Colour[] cmp = [.. palette.Entries];
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
					// Simple colour distance calculation (Manhattan distance in RGB space). 
					int total = Math.Abs(pal[i].R - cmp[j].R) + Math.Abs(pal[i].G - cmp[j].G) + Math.Abs(pal[i].B - cmp[j].B);
					if (total >= mx) continue;
					entry = j;
					mx = total;
				}
				matches.Add(i, entry);
			}
			
			Bytemap output = new(input.Width(), input.Height());
			for (int yy = 0; yy < input.Height(); yy++)
			{
				for (int xx = 0; xx < input.Width(); xx++)
				{
					output[xx, yy] = (byte)matches[input.Bitmap[xx, yy]];
				}
			}
			return output;
		}

		public static Picture MakePalette(this IBitmap bitmap, int startIndex, int colourLength)
		{
			Dictionary<byte, int> colourCount = [];
			foreach (byte colourIndex in bitmap.Bitmap.ToByteArray())
			{
				if (bitmap.Palette[colourIndex].A == 0) continue; // Do not count transparent
				if (colourCount.TryGetValue(colourIndex, out int value))
				{
					colourCount[colourIndex] = ++value;
					continue;
				}
				colourCount.Add(colourIndex, 1);
			}

			Colour[] colours = [.. colourCount.OrderByDescending(x => x.Value).Select(x => bitmap.Palette[x.Key]).Take(colourLength)];
			Colour[] palette;
			if (Settings.GraphicsMode == GraphicsMode.Graphics256)
			{
				palette = new Colour[256];
				palette[0] = Colour.Transparent;
				Array.Copy(colours, 0, palette, startIndex, Math.Min(colourLength, colours.Length));
			}
			else
			{
				palette = [.. Common.GetPalette16.Entries];
			}
			
			Bytemap bytemap = bitmap.MatchColours(palette, startIndex, colourLength);
			return new Picture(bytemap, palette);
		}
	}
}