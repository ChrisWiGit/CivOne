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
using System.Text;
using CivOne.IO;

namespace CivOne.Graphics
{
	/// <summary>
	/// Extends <see cref="Fontset"/> to support non-ASCII characters when the underlying
	/// <c>FONTS.CV</c> file contains only English glyphs.
	/// <br/>
	/// The original <c>FONTS.CV</c> encodes a small set of international characters in the
	/// control-character range (0–31). When a translation requires letters outside that set
	/// (e.g. German umlauts, French accented letters), this class synthesises the missing glyphs
	/// by decomposing the Unicode character into its base letter and combining diacritic mark
	/// and then drawing the accent pixel-by-pixel on top of the base glyph.
	/// <br/>
	/// This font set is selected automatically in <em>auto</em> language mode and can also be
	/// activated explicitly via <b>Shift+F1 → Game Options → Language</b>.
	/// </summary>
	internal class InternationalSimulatedFontSet : Fontset
	{
		private static readonly Dictionary<char, char> _specialCharacterMapping = new Dictionary<char, char>
		{
			['\u00E7'] = (char)0,
			['\u00FC'] = (char)1,
			['\u00E9'] = (char)2,
			['\u00E2'] = (char)3,
			['\u00E4'] = (char)4,
			['\u00E0'] = (char)5,
			['\u00EA'] = (char)8,
			['\u00EB'] = (char)9,
			['\u00E8'] = (char)10,
			['\u00EF'] = (char)11,
			['\u00EE'] = (char)12,
			['\u00EC'] = (char)13,
			['\u00C4'] = (char)14,
			['\u00F4'] = (char)19,
			['\u00F6'] = (char)20,
			['\u00F2'] = (char)21,
			['\u00FB'] = (char)22,
			['\u00D6'] = (char)25,
			['\u00DC'] = (char)26,
			['\u00DF'] = (char)30,
			['\u00C7'] = (char)31
		};

		public InternationalSimulatedFontSet(byte[] bytes, ushort offset)
			: base(bytes, offset)
		{
		}

		public override Bytemap GetLetter(char character, byte colour)
		{
			if (TryMapCharacter(character, out char mappedCharacter) && HasCharacter(mappedCharacter))
			{
				return base.GetLetter(mappedCharacter, colour);
			}

			if (HasCharacter(character))
			{
				return base.GetLetter(character, colour);
			}

			if (TryComposeFromUnicode(character, colour, out Bytemap? compositeGlyph))
			{
				return compositeGlyph!; //always non-null if TryComposeFromUnicode returns true
			}

			if (TryGetBaseCharacter(character, out char baseCharacter) && HasCharacter(baseCharacter))
			{
				return base.GetLetter(baseCharacter, colour);
			}

			return base.GetLetter(character, colour);
		}

		private static bool TryMapCharacter(char character, out char mappedCharacter)
		{
			if (_specialCharacterMapping.TryGetValue(character, out mappedCharacter))
			{
				return true;
			}

			mappedCharacter = '\0';
			return false;
		}

		private static bool TryGetBaseCharacter(char character, out char baseCharacter)
		{
			baseCharacter = '\0';
			string decomposed = character.ToString().Normalize(NormalizationForm.FormD);
			if (decomposed.Length == 0)
			{
				return false;
			}

			char first = decomposed[0];
			if ((first >= 'A' && first <= 'Z') || (first >= 'a' && first <= 'z'))
			{
				baseCharacter = first;
				return true;
			}

			return false;
		}

		private bool TryComposeFromUnicode(char character, byte colour, out Bytemap? compositeGlyph)
		{
			compositeGlyph = null;
			string decomposed = character.ToString().Normalize(NormalizationForm.FormD);
			if (decomposed.Length <= 1)
			{
				return false;
			}

			char baseCharacter = decomposed[0];
			if (!HasCharacter(baseCharacter))
			{
				return false;
			}

			Bytemap glyph = Bytemap.Copy(base.GetLetter(baseCharacter, colour));
			bool preparedTopAccentHeadroom = false;
			for (int i = 1; i < decomposed.Length; i++)
			{
				char mark = decomposed[i];
				if (!preparedTopAccentHeadroom && IsTopAccentMark(mark))
				{
					EnsureTopAccentHeadroom(glyph);
					preparedTopAccentHeadroom = true;
				}

				ApplyCombiningMark(glyph, mark, colour);
			}

			compositeGlyph = glyph;
			return true;
		}

		private static bool IsTopAccentMark(char mark)
		{
			return mark == '\u0308'
				|| mark == '\u0301'
				|| mark == '\u0300'
				|| mark == '\u0302'
				|| mark == '\u0303'
				|| mark == '\u030A';
		}

		private static void EnsureTopAccentHeadroom(Bytemap glyph)
		{
			for (int x = 0; x < glyph.Width; x++)
			{
				if (glyph[x, 0] == 0)
				{
					continue;
				}

				for (int y = glyph.Height - 1; y > 0; y--)
				{
					for (int xx = 0; xx < glyph.Width; xx++)
					{
						glyph[xx, y] = glyph[xx, y - 1];
					}
				}

				for (int xx = 0; xx < glyph.Width; xx++)
				{
					glyph[xx, 0] = 0;
				}

				return;
			}
		}

		private static void ApplyCombiningMark(Bytemap glyph, char mark, byte colour)
		{
			switch (mark)
			{
				case '\u0308':
					ApplyDiaeresis(glyph, colour);
					break;
				case '\u0301':
					ApplyAcute(glyph, colour);
					break;
				case '\u0300':
					ApplyGrave(glyph, colour);
					break;
				case '\u0302':
					ApplyCircumflex(glyph, colour);
					break;
				case '\u0303':
					ApplyTilde(glyph, colour);
					break;
				case '\u030A':
					ApplyRing(glyph, colour);
					break;
				case '\u0327':
					ApplyCedilla(glyph, colour);
					break;
			}
		}

		private static void ApplyDiaeresis(Bytemap glyph, byte colour)
		{
			int y = FindAccentAnchorRow(glyph);
			(int leftX, int rightX) = GetUmlautDotPositions(glyph.Width);
			SetPixelSafe(glyph, leftX, y, colour);
			SetPixelSafe(glyph, rightX, y, colour);
		}

		private static void ApplyAcute(Bytemap glyph, byte colour)
		{
			int y = FindAccentAnchorRow(glyph);
			int x = Math.Min(glyph.Width - 1, Math.Max(1, glyph.Width / 2));
			SetPixelSafe(glyph, x, y, colour);
			SetPixelSafe(glyph, x - 1, Math.Min(glyph.Height - 1, y + 1), colour);
		}

		private static void ApplyGrave(Bytemap glyph, byte colour)
		{
			int y = FindAccentAnchorRow(glyph);
			int x = Math.Max(0, (glyph.Width / 2) - 1);
			SetPixelSafe(glyph, x, y, colour);
			SetPixelSafe(glyph, x + 1, Math.Min(glyph.Height - 1, y + 1), colour);
		}

		private static void ApplyCircumflex(Bytemap glyph, byte colour)
		{
			int y = FindAccentAnchorRow(glyph);
			int x = Math.Max(1, glyph.Width / 2);
			SetPixelSafe(glyph, x, y, colour);
			SetPixelSafe(glyph, x - 1, Math.Min(glyph.Height - 1, y + 1), colour);
			SetPixelSafe(glyph, x + 1, Math.Min(glyph.Height - 1, y + 1), colour);
		}

		private static void ApplyTilde(Bytemap glyph, byte colour)
		{
			int y = FindAccentAnchorRow(glyph);
			SetPixelSafe(glyph, Math.Max(0, (glyph.Width / 2) - 2), Math.Min(glyph.Height - 1, y + 1), colour);
			SetPixelSafe(glyph, Math.Max(0, (glyph.Width / 2) - 1), y, colour);
			SetPixelSafe(glyph, Math.Min(glyph.Width - 1, glyph.Width / 2), Math.Min(glyph.Height - 1, y + 1), colour);
			SetPixelSafe(glyph, Math.Min(glyph.Width - 1, (glyph.Width / 2) + 1), y, colour);
		}

		private static void ApplyRing(Bytemap glyph, byte colour)
		{
			int y = FindAccentAnchorRow(glyph);
			int cx = glyph.Width / 2;
			SetPixelSafe(glyph, cx, y, colour);
			SetPixelSafe(glyph, cx - 1, Math.Min(glyph.Height - 1, y + 1), colour);
			SetPixelSafe(glyph, cx + 1, Math.Min(glyph.Height - 1, y + 1), colour);
		}

		private static void ApplyCedilla(Bytemap glyph, byte colour)
		{
			int x = Math.Max(0, glyph.Width / 2);
			int y = Math.Max(0, glyph.Height - 1);
			SetPixelSafe(glyph, x, y, colour);
			SetPixelSafe(glyph, Math.Max(0, x - 1), y, colour);
		}

		private static int FindAccentAnchorRow(Bytemap glyph)
		{
			for (int y = 0; y < glyph.Height; y++)
			{
				for (int x = 0; x < glyph.Width; x++)
				{
					if (glyph[x, y] == 0)
					{
						continue;
					}

					return Math.Max(0, y - 1);
				}
			}

			return 0;
		}

		private static void SetPixelSafe(Bytemap glyph, int x, int y, byte colour)
		{
			if (x < 0 || y < 0 || x >= glyph.Width || y >= glyph.Height)
			{
				return;
			}

			glyph[x, y] = colour;
		}

		private static (int LeftDotX, int RightDotX) GetUmlautDotPositions(int width)
		{
			if (width <= 1)
			{
				return (0, 0);
			}

			int leftDotX = Math.Max(0, (width - 3) / 2);
			int rightDotX = Math.Min(width - 1, leftDotX + 2);
			return (leftDotX, rightDotX);
		}
	}
}
