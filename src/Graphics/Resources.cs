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
using System.Text;
using CivOne.Enums;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.IO.Text;
using CivOne.Tiles;

namespace CivOne.Graphics
{
	public class Resources : IResourceFileBitmapProvider, IResourceFontHeightProvider
	{
		private static Settings Settings => Settings.Instance;

		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		private readonly Dictionary<string, Picture> _cache = new Dictionary<string, Picture>();
		private readonly Dictionary<string, Bytemap> _textCache = new Dictionary<string, Bytemap>();
		private readonly IFont _defaultFont = new DefaultFont();
		private readonly List<IFont> _fonts = [];
		private readonly Dictionary<Direction, IBitmap> _fog = new Dictionary<Direction, IBitmap>();
		private readonly PalaceResourcesDelegate _palaceResources;
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
			string filename = Path.Combine(Settings.DataDirectory, "FONTS.CV");
			if (!File.Exists(filename))
			{
				Log("Font file not found, fallback to default font");
				return;
			}

			using (FileStream fs = new FileStream(filename, FileMode.Open))
			{
				file = new byte[fs.Length];
				fs.ReadExactly(file, 0, file.Length);
			}
			
			List<ushort> fontOffsets = [];
			int index = 0;
			uint fontCount = BitConverter.ToUInt16(file, index);
			index += 2;
			
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
			return (asciiChar >= Font(fontId).FirstChar && asciiChar <= Font(fontId).LastChar);
		}
		
		public Size GetTextSize(int font, string text)
		{
			int width = 0, height = 0;
			foreach (char c in text)
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
			text = text.Normalize(NormalizationForm.FormC);

			List<Bytemap> letters = new List<Bytemap>();
			for (int i = 0; i < text.Length; i++)
			{
				char current = text[i];
				letters.Add(GetLetter(i == highlightedCharacterIndex ? colourFirstLetter : colour, font, current));
			}
			
			int width = 0, height = 0;
			foreach (Bytemap letter in letters)
			{
				width += letter.Width + 1;
				if (height < letter.Height) height = letter.Height;
			}
			
			Picture output = new Picture(width, height);
			
			int xx = 0;
			foreach (Bytemap letter in letters)
			{
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
			string key = string.Format("letter{0}|{1}|{2}", colour, font, letter);
			if (!_textCache.ContainsKey(key))
			{
				_textCache.Add(key, Font(font).GetLetter(letter, colour));
			}
			return _textCache[key];
		}

		public bool Exists(string filename)
		{
			if (RuntimeHandler.Runtime.Settings.Free) return false;
			return PicFile.Exists(filename);
		}
		
		internal string[] GetCivilopediaText(string name)
		{
			List<string> textLines = [];
			string text = string.Join(" ", TextFileFactory.Get().GetGameText(name));
			string t = "";
			while (text.Length > 0)
			{
				if (text.IndexOf(' ') == -1)
				{
					if (t.Length > 0 && GetTextSize(6, string.Join(" ", t, text)).Width < 294)
						text = string.Join(" ", t, text);
					else if (t.Length > 0)
						textLines.Add(t);
					t = text;
					text = "";
				}
				else if (GetTextSize(6, t + text.Substring(0, text.IndexOf(' '))).Width < 294)
				{
					if (t.Length > 0) t += " ";
					t += text.Substring(0, text.IndexOf(' '));
					text = text.Substring(text.IndexOf(' ')).Trim();
					continue;
				}
				textLines.Add(t);
				t = "";
			}
			return textLines.ToArray();
		}
		
		private static Picture _worldMapTiles;
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

		public Picture this[string filename]
		{
			get
			{
				string key = filename.ToUpper();
				if (_cache.ContainsKey(key))
				{
					return new Picture(_cache[key].Bitmap, _cache[key].Palette);
				}
				
				Picture output = null;
				PicFile picFile = new PicFile(filename);
				if ((Settings.GraphicsMode == GraphicsMode.Graphics256 && picFile.GetPicture256 != null) || picFile.GetPicture16 == null)
				{
					output = new Picture(picFile.GetPicture256, picFile.GetPalette256);
				}
				else
				{
					output = new Picture(picFile.GetPicture16, picFile.GetPalette16);
				}
				
				if (!_cache.ContainsKey(key)) _cache.Add(key, output);
				return new Picture(_cache[key].Bitmap, _cache[key].Palette);
			}
		}

		// Explicit interface implementation for IResourceService
		IBitmap IResourceFileBitmapProvider.this[string filename]
		{
			get { return this[filename]; }
		}

		public Picture GetPalace(PalaceStyle style, PalacePart part, int level)
			=> _palaceResources.GetPalacePart(style, part, level);
		
		private static Resources _instance;
		public static Resources Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Resources();
				}
				return _instance;
			}
		}

		public static void ClearInstance()
		{
			_instance = null;
			_worldMapTiles = null;
			PicFile.ClearCache();
			TextFileFactory.ClearInstance();
			Sprites.Cursor.ClearCache();
		}
		
		private Resources()
		{
			_palaceResources = new PalaceResourcesDelegate(name => this[name]);
			if (!RuntimeHandler.Runtime.Settings.Free) LoadFonts();
		}
	}
}