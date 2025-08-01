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
using CivOne.Enums;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.Tiles;

namespace CivOne.Graphics
{
	public class Resources
	{
		private static Settings Settings => Settings.Instance;

		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		private readonly Dictionary<string, Picture> _cache = new Dictionary<string, Picture>();
		private readonly Dictionary<string, Bytemap> _textCache = new Dictionary<string, Bytemap>();
		private readonly IFont _defaultFont = new DefaultFont();
		private readonly List<Fontset> _fonts = new List<Fontset>();
		private readonly Dictionary<Direction, IBitmap> _fog = new Dictionary<Direction, IBitmap>();
		
		internal void ClearTextCache() => _textCache.Clear();
		
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
			
			foreach (ushort offset in fontOffsets)
			{
				_fonts.Add(new Fontset(file, offset));
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
			if (text == null) text = "[MISSING STRING]";

			List<Bytemap> letters = new List<Bytemap>();
			bool isFirstLetter = true;
			foreach (char c in text)
			{
				letters.Add(GetLetter(isFirstLetter ? colourFirstLetter : colour, font, c));
				isFirstLetter = false;
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
		
		public int GetFontHeight(int font)
		{
			return Font(font).FontHeight;
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
			List<string> textLines = new List<string>();
			string text = string.Join(" ", TextFile.Instance.GetGameText(name));
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

		private static Dictionary<int, Picture> _palacePart = new Dictionary<int, Picture>();
		public Picture GetPalace(PalaceStyle style, PalacePart part, int level)
		{
			if (level == 0)
			{
				style = PalaceStyle.None;
			}

			int combine = (level * 100) + ((int)style * 10) + (int)part;
			if (!_palacePart.ContainsKey(combine))
			{
				Picture picture = null;

				int offsetX = 0, offsetY = 0;
				if (style == PalaceStyle.Classical) offsetX = 160;
				if (style == PalaceStyle.Islamic) offsetY = 100;

				switch (part)
				{
					case PalacePart.LeftTower:
						picture = new Picture(35, 101);
						if (style == PalaceStyle.Classical)
						{
							picture.AddLayer(Instance[$"CASTLE{level}"][160, 1 + offsetY, 35, 99], 0, 2);
							break;
						}
						picture.AddLayer(Instance[$"CASTLE{level}"][104 + offsetX, 1 + offsetY, 27, 99], 8, 2);
						break;
					case PalacePart.RightTower:
						picture = new Picture(35, 101);
						if (style == PalaceStyle.Classical)
						{
							picture.AddLayer(Instance[$"CASTLE{level}"][196, 1 + offsetY, 35, 99], 0, 2);
							break;
						}
						picture.AddLayer(Instance[$"CASTLE{level}"][132 + offsetX, 1 + offsetY, 27, 99], 0, 2);
						break;
					case PalacePart.Wall:
					case PalacePart.WallShadow:
					{
						picture = new Picture(48, 101);
						if (level == 0)
						{
							picture.AddLayer(Instance["CASTLE0"][53 + offsetX, 1 + offsetY, 24, 99]);
							break;
						}
						for (int i = 0; i < 2; i++)
						{
							bool shadow = (part == PalacePart.WallShadow && i == 0);
							picture.AddLayer(Instance[$"CASTLE{level}"][(shadow ? 53 : 78) + offsetX, 1 + offsetY, 24, 99], (24 * i));
						}
						break;
					}
					case PalacePart.LeftTowerWall:
					{
						picture = new Picture(57, 101);
						if (level == 0)
						{
							picture.AddLayer(Instance["CASTLE0"][78 + offsetX, 1 + offsetY, 24, 99], 33);
							break;
						}
						picture.AddLayer(Instance[$"CASTLE{level}"][53 + offsetX, 1 + offsetY, 24, 99], 33);
						if (style == PalaceStyle.Classical)
						{
							picture.AddLayer(Instance[$"CASTLE{level}"][160, 1 + offsetY, 35, 99], 0, 2);
							break;
						}
						picture.AddLayer(Instance[$"CASTLE{level}"][104 + offsetX, 1 + offsetY, 27, 99], 8, 2);
						break;
					}
					case PalacePart.RightTowerWall:
					case PalacePart.RightTowerWallShadow:
					{
						picture = new Picture(57, 101);
						if (level == 0)
						{
							picture.AddLayer(Instance["CASTLE0"][53 + offsetX, 1 + offsetY, 24, 99]);
							break;
						}
						
						bool shadow = (part == PalacePart.RightTowerWallShadow);
						picture.AddLayer(Instance[$"CASTLE{level}"][(shadow ? 53 : 78) + offsetX, 1 + offsetY, 24, 99], 0);
						if (style == PalaceStyle.Classical)
						{
							picture.AddLayer(Instance[$"CASTLE{level}"][196, 1 + offsetY, 35, 99], 21, 2);
							break;
						}
						picture.AddLayer(Instance[$"CASTLE{level}"][132 + offsetX, 1 + offsetY, 27, 99], 21, 2);
						break;
					}
					case PalacePart.Center:
					{
						picture = Instance[$"CASTLE{level}"][0 + offsetX, 1 + offsetY, 52, 99];
						break;
					}
				}

				_palacePart[combine] = picture;
			}
			return _palacePart[combine];
		}
		
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
			_palacePart = null;
			PicFile.ClearCache();
			TextFile.ClearInstance();
			Sprites.Cursor.ClearCache();
		}
		
		private Resources()
		{
			if (!RuntimeHandler.Runtime.Settings.Free) LoadFonts();
		}
	}
}