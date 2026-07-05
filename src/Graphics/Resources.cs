// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Text;
using CivOne.Enums;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.IO.Text;
using CivOne.Tiles;

namespace CivOne.Graphics
{
	[SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Resources is the main class for accessing game resources, and it is appropriate to have the same name as the namespace.")]
	public class Resources : IResourceFileBitmapProvider, IResourceFontHeightProvider
	{
		private static Settings Settings => Settings.Instance;

		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		// Concurrent dictionaries: PreloadCivilopedia (RuntimeHandler) can populate caches from a
		// background task while the render loop reads them. Plain Dictionary<,> is not safe here.
		private readonly ConcurrentDictionary<string, Picture> _cache = new();
		private readonly ConcurrentDictionary<(byte Colour, int Font, char Letter), Bytemap> _textCache = new();
		private readonly ConcurrentDictionary<string, bool> _existsCache = new(StringComparer.OrdinalIgnoreCase);
		private readonly IFont _defaultFont = new DefaultFont();
		private readonly List<IFont> _fonts = [];
		private readonly PalaceResourcesWrapper _palaceResources;
		internal void ClearTextCache() => _textCache.Clear();
		
		/// <summary>
		/// Reloads all fonts and clears associated caches.
		/// Call this when font settings have changed in the game.
		/// </summary>
		public void ReloadFonts()
		{
			_fonts.Clear();
			_textCache.Clear();
			LoadFonts();
		}
		
		private void LoadFonts()
		{
			byte[] file;
			string? filename = FileSystem.FindFileIgnoreCase(Settings.DataDirectory, "FONTS.CV");
			if (filename == null)
			{
				Log("Font file not found, fallback to default font");
				return;
			}

			using (FileStream fs = new FileStream(filename, FileMode.Open))
			{
				file = new byte[fs.Length];
				fs.ReadExactly(file, 0, file.Length);
			}

			if (file.Length < 2)
			{
				Log("Font file too short, fallback to default font");
				return;
			}

			List<ushort> fontOffsets = [];
			int index = 0;
			uint fontCount = BitConverter.ToUInt16(file, index);
			index += 2;

			if (index + (fontCount * 2) > file.Length)
			{
				Log("Font file corrupted: insufficient data for {0} font offsets", fontCount);
				return;
			}

			for (int i = 0; i < fontCount; i++)
			{
				fontOffsets.Add(BitConverter.ToUInt16(file, index));
				index += 2;
			}
			
			bool isInternationalFontSet = false;
			ClearTextCache();
			for (int fontId = 0; fontId < fontOffsets.Count; fontId++)
			{
				ushort offset = fontOffsets[fontId];
				IFont font = FontSetFactory.Create(file, offset);
				_fonts.Add(font);
				isInternationalFontSet = isInternationalFontSet || FontSetFactory.IsInternationalFontSet(font);
			}

			if (!isInternationalFontSet)
			{
				Log("The file FONTS.CV does not contain an international font set, " +
				"which means that the game will not be able to display Unicode characters." +
				" Instead these characters will be simulated as fallback to ensure that text is still visible, albeit not in the correct way. ");
			}
		}
		
		public bool ValidCharacter(int fontId, char c)
		{
			byte asciiChar = (byte)c;
			return asciiChar >= Font(fontId).FirstChar && asciiChar <= Font(fontId).LastChar;
		}
		
		public Size GetTextSize(int font, string text)
		{
			int width = 0, height = 0;
			foreach (char c in text ?? string.Empty)
			{
				Size size = GetLetterSize(font, c);
				width += size.Width + 1;
				if (height < size.Height) height = size.Height;
			}
			return new Size(width, height);
		}
		
		public Picture GetText(string text, int font, byte colour)
		{
			return GetText(text, font, colour, colour);
		}
		
		public Picture GetText(string text, int font, byte colourFirstLetter, byte colour)
		{
			return GetText(text, font, colourFirstLetter, colour, 0);
		}

		public Picture GetText(string text, int font, byte colourFirstLetter, byte colour, int highlightedCharacterIndex)
		{
			text ??= "[MISSING STRING]";
			// Fast path: pure ASCII text doesn't need Unicode normalization.
			if (!IsAscii(text))
				text = text.Normalize(NormalizationForm.FormC);

			int length = text.Length;
			Bytemap[] letters = new Bytemap[length];
			int width = 0, height = 0;
			for (int i = 0; i < length; i++)
			{
				char current = text[i];
				Bytemap letter = GetLetter(i == highlightedCharacterIndex ? colourFirstLetter : colour, font, current);
				letters[i] = letter;
				width += letter.Width + 1;
				if (height < letter.Height) height = letter.Height;
			}

			Picture output = new(width, height);

			int xx = 0;
			for (int i = 0; i < length; i++)
			{
				Bytemap letter = letters[i];
				output.AddLayer(letter, xx, 0);
				xx += letter.Width + 1;
			}

			return output;
		}
		
		internal Size GetLetterSize(int font, char letter) => GetLetter(5, font, letter).Size;

		private IFont Font(int font)
		{
			if (font < 0 || (_fonts.Count - 1) < font)
				return _defaultFont;
			return _fonts[font];
		}
		
		public int GetFontHeight(int FontId)
		{
			return Font(FontId).FontHeight;
		}
		
		private Bytemap GetLetter(byte colour, int font, char letter)
		{
			var key = (colour, font, letter);
			if (_textCache.TryGetValue(key, out Bytemap? cached))
				return cached;
			return _textCache.GetOrAdd(key, k => Font(k.Font).GetLetter(k.Letter, k.Colour));
		}

		private static bool IsAscii(string text)
		{
			for (int i = 0; i < text.Length; i++)
				if (text[i] > 0x7F) return false;
			return true;
		}

		public bool Exists(string filename)
		{
			if (RuntimeHandler.Runtime.Settings.Free) return false;
			return _existsCache.GetOrAdd(filename, PicFile.Exists);
		}

		/// <summary>
		/// Clears the existence cache. Call this when the data directory contents may have changed.
		/// </summary>
		internal void ClearExistsCache() => _existsCache.Clear();
		
		private const int CivilopediaFont = 6;
		private const int CivilopediaLineWidth = 294;

		[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching all exceptions is necessary to ensure that failure to load Civilopedia text does not crash the application, and that any exceptions are logged appropriately.")]
		internal string[] GetCivilopediaText(string name)
		{
			try
			{
				string text = LoadCivilopediaText(name);
				if (string.IsNullOrEmpty(text))
					return [];

				return WrapText(text, CivilopediaFont, CivilopediaLineWidth);
			}
			catch (Exception ex)
			{
				Log("Error in GetCivilopediaText({0}): {1}", name, ex.Message);
				return [];
			}
		}

		private static string LoadCivilopediaText(string name)
		{
			var factory = TextFileFactory.Get();
			if (factory == null)
			{
				Log("TextFileFactory returned null for: {0}", name);
				return string.Empty;
			}

			var gameText = factory.GetGameText(name);
			if (gameText == null) return string.Empty;

			return string.Join(" ", gameText);
		}

		private string[] WrapText(string text, int font, int maxWidth)
		{
			List<string> lines = [];
			StringBuilder current = new();

			foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
			{
				AppendWord(word, current, lines, font, maxWidth);
			}

			if (current.Length > 0)
				lines.Add(current.ToString());

			return [.. lines];
		}

		private void AppendWord(string word, StringBuilder current, List<string> lines, int font, int maxWidth)
		{
			string candidate = current.Length == 0 ? word : current + " " + word;
			if (GetTextSize(font, candidate).Width < maxWidth)
			{
				if (current.Length > 0) current.Append(' ');
				current.Append(word);
				return;
			}

			if (current.Length > 0)
				lines.Add(current.ToString());
			current.Clear().Append(word);
		}
		
		private static Picture? _worldMapTiles;
		public static Picture WorldMapTiles
		{
			get
			{
				if (_worldMapTiles == null)
				{
					Picture sp299 = Instance["SP299"];
					_worldMapTiles = new Picture(48, 8, sp299.Palette);
					_worldMapTiles.AddLayer(sp299[160, 111, 48, 8]);
				}
				return _worldMapTiles;
			}
		}

		/// <summary>
		/// Gets a picture by resource filename.
		/// </summary>
		/// <param name="filename">
		/// The resource filename.
		/// </param>
		/// <returns>
		/// A new <see cref="Picture"/> instance containing the requested bitmap data.
		/// </returns>
		/// <remarks>
		/// This indexer does not return the internal cache instance.
		/// Even on cache hits, it returns a newly constructed copy.
		/// Callers own the returned instance and may dispose it safely.
		/// </remarks>
		public Picture this[string filename]
		{
			get
			{
				ArgumentNullException.ThrowIfNull(filename);

				string key = filename.ToUpperInvariant();
				if (_cache.TryGetValue(key, out Picture? cached))
				{
					return new Picture(cached.Bitmap, cached.Palette);
				}

				Picture output;
				using PicFile picFile = new(filename);
				if ((Settings.GraphicsMode == GraphicsMode.Graphics256 && picFile.GetPicture256 != null) || picFile.GetPicture16 == null)
				{
					Debug.Assert(picFile.GetPicture256 != null, $"Expected 256-color version of {filename} to be available.");
					output = new Picture(picFile.GetPicture256, picFile.GetPalette256);
				}
				else
				{
					output = new Picture(picFile.GetPicture16, picFile.GetPalette16);
				}

				Picture stored = _cache.GetOrAdd(key, output);
				return new Picture(stored.Bitmap, stored.Palette);
			}
		}

		// Explicit interface implementation for IResourceService
		IBitmap IResourceFileBitmapProvider.this[string filename]
		{
			get { return this[filename]; }
		}

		public Picture GetPalace(PalaceStyle style, PalacePart part, int level)
			=> _palaceResources.GetPalacePart(style, part, level);
		
		private static Resources? _instance;
		public static Resources Instance
		{
			get
			{
				_instance ??= new Resources();
				return _instance;
			}
		}

		public static void ClearInstance()
		{
			_instance?._palaceResources.ClearCache();
			_instance?._cache.Clear();
			_instance?._textCache.Clear();
			_instance?._fonts.Clear();
			_instance?.ClearExistsCache();
			_instance?.ClearTextCache();
			
			_instance = null;
			_worldMapTiles = null;
			PicFile.ClearCache();
			TextFileFactory.ClearInstance();
			Sprites.Cursor.ClearCache();
		}
		
		private Resources()
		{
			_palaceResources = new PalaceResourcesWrapper(name => this[name]);
			if (!RuntimeHandler.Runtime.Settings.Free) LoadFonts();
		}
	}
}