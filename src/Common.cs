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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Services;
using CivOne.Screens;
using CivOne.Wonders;
using System.Threading;
using System.Globalization;

namespace CivOne
{
	internal class Common
	{
		private static Resources Resources => Resources.Instance;
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		public static Random? Random; // = new Random((int)DateTime.Now.Ticks);

		/// <summary>
		/// True if Caps Lock is currently active. 
		/// It does not track the state of the Shift key, as Shift is properly tracked via KeyboardEventArgs.Modifier, 
		/// but Caps Lock is not a modifier in the same sense and thus requires separate tracking.
		/// </summary>
		internal static bool CapsLockActive;
		internal static bool ShiftKeyHeld;

		public static IAdvance[] Advances = Reflect.GetAdvances().ToArray();
		public static IBuilding[] Buildings = Reflect.GetBuildings().ToArray();
		public static IWonder[] Wonders = Reflect.GetWonders().ToArray();
		public static ICivilization[] Civilizations => Reflect.GetCivilizations().ToArray();
		public static byte[] ColourLight = new byte[] { 12, 15, 10, 9, 14, 11, 13, 7 };
		public static byte[] ColourDark = new byte[] { 4, 7, 2, 1, 10, 3, 4, 8 };
		
		internal static IEnumerable<string> AllCityNames => Civilizations.Select(x => x.CityNames).SelectMany(x => x);

		private static List<IScreen> _screens = new List<IScreen>();
		private static readonly Lock _attributeCacheSync = new();
		private static readonly Dictionary<(Type ObjectType, Type AttributeType), bool> _attributeCache = [];
		/// <summary>
		/// Returns a snapshot of all currently active screens in stack order (bottom to top).
		/// </summary>
		/// <remarks>
		/// For new code, prefer IScreenQueryService.Screens obtained from ScreenServiceFactory.CreateQueryService().
		/// This provides better testability and loose coupling.
		/// </remarks>
		internal static IScreen[] Screens => _screens.ToArray();

		/// <summary>
		/// Returns the screen at the bottom of the stack, or <c>null</c> if the stack is empty.
		/// </summary>
		/// <remarks>
		/// For new code, prefer IScreenQueryService.LastScreen obtained from ScreenServiceFactory.CreateQueryService().
		/// This provides better testability and loose coupling.
		/// </remarks>
		internal static IScreen? LastScreen => _screens.LastOrDefault();

		internal static bool HasAttribute<T>(object checkObject) where T : Attribute
		{
			if (checkObject == null)
				return false;

			Type objectType = checkObject.GetType();
			Type attributeType = typeof(T);
			return HasAttribute(objectType, attributeType);
		}

		private static bool HasAttribute(Type objectType, Type attributeType)
		{
			lock (_attributeCacheSync)
			{
				if (_attributeCache.TryGetValue((objectType, attributeType), out bool isDefined))
				{
					return isDefined;
				}

				isDefined = Attribute.IsDefined(objectType, attributeType);
				_attributeCache[(objectType, attributeType)] = isDefined;
				return isDefined;
			}
		}

		/// <summary>
		/// Returns the topmost active screen, favouring modal screens if any are present.
		/// Returns <c>null</c> if the stack is empty.
		/// </summary>
		/// <remarks>
		/// For new code, prefer IScreenQueryService.TopScreen obtained from ScreenServiceFactory.CreateQueryService().
		/// This provides better testability and loose coupling.
		/// </remarks>
		public static IScreen? TopScreen
		{
			get
			{
				IScreen[] screens = [.. _screens];
				for (int i = screens.Length - 1; i >= 0; i--)
				{
					if (HasAttribute<Modal>(screens[i]))
					{
						return screens[i];
					}
				}

				return screens.LastOrDefault();
			}
		}

		public static MouseCursor MouseCursor
		{
			get
			{
				IScreen? topScreen = TopScreen;
				if (topScreen == null)
					return MouseCursor.None;

				return topScreen.Cursor;
			}
		}


		/// <summary>
		/// Gets a <b>copy</b> of the default palette.
		/// You must not call Copy() on the returned palette, as it is already a copy.
		/// 
		/// You should use "using" on the returned palette, to ensure it is disposed properly after use, to avoid memory leaks.
		/// </summary>
		/// <example>
		/// <code>
		/// using Palette palette = Common.DefaultPalette
		/// </code>
		/// </example>
		public static Palette DefaultPalette
		{
			get
			{
				GamePlay? gamePlay = GamePlay;
				if (gamePlay != null)
					return gamePlay.MainPalette.Copy();
				return Resources["SP257"].Palette.Copy();
			}
		}

		public static GamePlay? GamePlay => (GamePlay?)_screens.FirstOrDefault(x => x is GamePlay);

        internal static void SetRandomSeed(ushort seed) => Random = new Random(seed == ushort.MaxValue ? -1 : seed);
        internal static void SetRandomSeed() => SetRandomSeed(ushort.MaxValue);
		
		/// <summary>
		/// Adds a screen to the top of the screen stack and makes it active.
		/// </summary>
		/// <param name="screen">The screen to add.</param>
		/// <remarks>
		/// For new code, prefer IScreenCommandService.AddScreen obtained from ScreenServiceFactory.CreateCommandService().
		/// This provides better testability and loose coupling.
		/// </remarks>
		internal static void AddScreen(IScreen screen) => _screens.Add(screen);
		
		/// <summary>
		/// Removes a screen from the screen stack and disposes it.
		/// </summary>
		/// <param name="screen">The screen to remove and dispose.</param>
		/// <remarks>
		/// For new code, prefer IScreenCommandService.DestroyScreen obtained from ScreenServiceFactory.CreateCommandService().
		/// This provides better testability and loose coupling.
		/// </remarks>
		internal static void DestroyScreen(IScreen screen)
		{
			screen?.Dispose();

			if (screen != null)
			{
				_screens.Remove(screen);
			}
		}
		
		internal static bool HasScreenType<T>() where T : IScreen => _screens.Any(x => x is T);
		
		internal static string? CaptureFilename
		{
			get
			{
				for (int i = 1; i < 99999; i++)
				{
					string filename = Path.Combine(Settings.Instance.CaptureDirectory, $"capture{i:00000}.gif");
					if (File.Exists(filename)) continue;
					return filename;
				}
				
				Log("Error: Capture folder is full.");
				return null;
			}
		}
		
		private static bool _reloadSettings;
		internal static bool ReloadSettings
		{
			get
			{
				if (_reloadSettings)
				{
					_reloadSettings = false;
					return true;
				}
				return false;
			}
			set
			{
				_reloadSettings = value;
			}
		}

		internal static string NumberSeperator(int number)
		{
			string input = number.ToString(CultureInfo.InvariantCulture);
			input = input.PadLeft(3 - (input.Length % 3) + input.Length, '0');
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < input.Length; i++)
			{
				if (sb.Length > 0 && i % 3 == 0) sb.Append(',');
				sb.Append(input[i]);
			}
			return sb.ToString().TrimStart('0', ',');
		}

		public static ushort YearToTurn(int year)
		{
			if (year < -4000) return 0;
			if (year < 1000) return (ushort)Math.Floor(((double)year + 4000) / 20);
			if (year < 1500) return (ushort)Math.Floor(((double)year + 1500) / 10);
			if (year < 1750) return (ushort)Math.Floor(((double)year) / 5);
			if (year < 1850) return (ushort)Math.Floor(((double)year - 1050) / 2);
			return (ushort)(year - 1450);
		}
		
		public static int TurnToYear(ushort turn)
		{
			if (turn < 200) return -(200 - turn) * 20;
			else if (turn == 200) return 1;
			else if (turn < 250) return (turn - 200) * 20;
			else if (turn < 300) return ((turn - 250) * 10) + 1000;
			else if (turn < 350) return ((turn - 300) * 5) + 1500;
			else if (turn < 400) return ((turn - 350) * 2) + 1750;
			return (turn - 400) + 1850;
		}
		
		public static string YearString(ushort turn, bool zeroAd = false)
		{
			int year = TurnToYear(turn);
			if (zeroAd && year == 1) year = 0;
			if (year < 0)
				return $"{-year} " + TranslationServiceFactory.GetCurrent().Translate("BC");
			return $"{year} " + TranslationServiceFactory.GetCurrent().Translate("AD");
		}

		public static string DifficultyName(int difficuly)
		{
			ITranslationService translation = TranslationServiceFactory.GetCurrent();
			return difficuly switch
			{
				1 => translation.Translate("Warlord"),
				2 => translation.Translate("Prince"),
				3 => translation.Translate("King"),
				4 => translation.Translate("Emperor"),
				5 => translation.Translate("Deity"),
				_ => translation.Translate("Chieftain"),
			};
		}

		internal static int CitizenGroup(Citizen citizen)
		{
			return citizen switch
			{
				Citizen.HappyMale => 0,
				Citizen.HappyFemale => 0,
				Citizen.ContentMale => 1,
				Citizen.ContentFemale => 1,
				Citizen.UnhappyMale => 2,
				Citizen.UnhappyFemale => 2,
				Citizen.Taxman => 3,
				Citizen.Scientist => 3,
				Citizen.Entertainer => 3,
				_ => 3
			};
		}

		
		public static bool InCityRange(int x1, int y1, int x2, int y2) => new Rectangle(x2 - 2, y2 - 2, 5, 5).IntersectsWith(new Rectangle(x1, y1, 1, 1));
		
		public static int DistanceToTile(int x1, int y1, int x2, int y2) => Math.Max(Math.Min(Math.Abs(x2 - x1), Map.WIDTH - Math.Abs(x2 - x1)), Math.Abs(y2 - y1));

        // The above function do not work properly at "dateline"  ( methink is is just a "Math.Abs" that is missing ? )   I use the one below.   JR
        /*  ******************************************************************************************************** */
        public static int Distance( int X1, int Y1, int X2, int Y2 )
        {
            int X = Math.Abs( X1 - X2 );
            int Y = Math.Abs( Y1 - Y2 );

            if( X > Map.WIDTH / 2 )
            {
                X = Map.WIDTH - X;
            }
            if( X > Y ) return X;
            return Y;
        }

        public static byte BinaryReadByte(BinaryReader reader, int position)
		{
			if (reader.BaseStream.Position != position)
				reader.BaseStream.Seek(position, SeekOrigin.Begin);
			return reader.ReadByte();
		}
		
		public static ushort BinaryReadUShort(BinaryReader reader, int position)
		{
			if (reader.BaseStream.Position != position)
				reader.BaseStream.Seek(position, SeekOrigin.Begin);
			return reader.ReadUInt16();
		}
		
		public static byte[] BinaryReadBytes(BinaryReader reader, int position, int count)
		{
			if (reader.BaseStream.Position != position)
				reader.BaseStream.Seek(position, SeekOrigin.Begin);
			return reader.ReadBytes(count);
		}
		
		private static string[] BytesToArray(byte[] bytes, int maxLength)
		{
			List<string> output = new List<string>();
			StringBuilder sb = new StringBuilder();
			foreach (byte b in bytes)
			{
				sb.Append((char)b);
				if (sb.Length != maxLength) continue;
				
				output.Add(sb.ToString().Split((char)0)[0].Trim());
				sb.Clear();
			}
			
			return output.ToArray();
		}
		public static string[] BinaryReadStrings(BinaryReader reader, int position, int length, int itemLength)
		{
			if (reader.BaseStream.Position != position)
				reader.BaseStream.Seek(position, SeekOrigin.Begin);
			return BytesToArray(reader.ReadBytes(length), itemLength);
		}
		
		private static Palette? _palette16;
		public static Palette GetPalette16
		{
			get
			{
				if (_palette16 == null)
				{
					byte[] shades = [0, 104, 183, 255];
					_palette16 = new[]
					{
						Colour.Transparent,
						new Colour(shades[0], shades[0], shades[2]),
						new Colour(shades[0], shades[2], shades[0]),
						new Colour(shades[0], shades[2], shades[2]),
						new Colour(shades[2], shades[0], shades[0]),
						new Colour(shades[0], shades[0], shades[0]),
						new Colour(shades[2], shades[1], shades[0]),
						new Colour(shades[2], shades[2], shades[2]),
						new Colour(shades[1], shades[1], shades[1]),
						new Colour(shades[1], shades[1], shades[3]),
						new Colour(shades[1], shades[3], shades[1]),
						new Colour(shades[1], shades[3], shades[3]),
						new Colour(shades[3], shades[1], shades[1]),
						new Colour(shades[3], shades[1], shades[3]),
						new Colour(shades[3], shades[3], shades[1]),
						new Colour(shades[3], shades[3], shades[3]),
					};
				}
				return _palette16;
			}
		}

		private static Palette? _palette256;
		public static Palette GetPalette256
		{
			get
			{
				if (_palette256 == null)
				{
					_palette256 = new Palette(256);
					for (int i = 0; i < 256; i++)
					{
						if (i >= 16 && i < 32)
						{
							int ii = (i % 16);
							_palette256[i] = new Colour(254 - (ii * 16), 253 - (ii * 16), 252 - (ii * 16));
							continue;
						}
						if (i >= 32 && i < 40)
						{
							// Greens
							int ii = (i % 8);
							_palette256[i] = new Colour(0, 197 - (ii * 11), 80 - (ii * 7));
							continue;
						}
						if (i >= 40 && i < 42)
						{
							// Browns
							int ii = (i % 2);
							_palette256[i] = new Colour(128 + (ii * 16), 64 + (ii * 8), 0);
							continue;
						}
						if (i >= 42 && i < 48)
						{
							// Yellows
							int ii = (i + 2 % 6);
							_palette256[i] = new Colour(254 - (ii * 6), 245 - (ii * 6), 0);
							continue;
						}
						if (i >= 48 && i < 64)
						{
							int r = Convert.ToInt32((float)_palette16[i % 16].R * 0.7F);
							int g = Convert.ToInt32((float)_palette16[i % 16].G * 0.7F);
							int b = Convert.ToInt32((float)_palette16[i % 16].B * 0.7F);
							_palette256[i] = new Colour(r, g, b);
							continue;
						}
						if (i >= 64 && i < 80)
						{
							// Blues
							int ii = (i % 8);
							_palette256[i] = new Colour(0, 67 - (ii * 5), 211 - (ii * 9));
							continue;
						}
						_palette256[i] = GetPalette16[i % 16];
					}
				}
				return _palette256;
			}
		}

		public static bool AllowSaveGame
		{
			get
			{
				// SaveGame supports COS/YAML for all map sizes.
				// The save game compatibility service will determine if the current game is compatible with SaveGame based on the map size and other factors, and provide appropriate messaging to the user if it is not compatible.
				return Map.Instance.Ready;
			}
		}
	}
}