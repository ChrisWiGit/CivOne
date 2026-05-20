// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using CivOne.IO;

namespace CivOne.Graphics
{
	/// <summary>
	/// Loads and renders a bitmap font from a <c>FONTS.CV</c> data block.
	/// <br/>
	/// The font covers only the ASCII range stored in the file (typically English characters).
	/// For non-ASCII characters (e.g. German umlauts, accented letters), use
	/// <see cref="InternationalSimulatedFontSet"/> which simulates missing glyphs on top of this base.
	/// </summary>
	internal class Fontset : IFont
	{
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		private readonly byte _fontAsciiFirst;
		private readonly byte _fontAsciiLast;
		private readonly byte _charByteLength;
		private readonly byte _charTopRow;
		private readonly byte _charBottomRow;
		private readonly Dictionary<char, byte> _charWidths = [];
		private readonly Dictionary<char, byte[]> _characters = [];

		public int FontHeight => 1 + _charBottomRow - _charTopRow;
		public byte FirstChar => _fontAsciiFirst;
		public byte LastChar => _fontAsciiLast;

		public virtual Bytemap GetLetter(char character, byte colour)
		{
			if (!TryGetCharacterData(character, out int width, out byte[] characterData))
			{
				return new Bytemap(8, 8);
			}

			return new Bytemap(width, FontHeight).FromByteArray(BuildPixels(characterData, width, colour));
		}

		protected bool HasCharacter(char character)
		=> _charWidths.ContainsKey(character) && _characters.ContainsKey(character);

		private bool TryGetCharacterData(char character, out int width, out byte[] characterData)
		{
			width = 0;
			characterData = null;

			if (!_charWidths.TryGetValue(character, out byte charWidth))
			{
				return false;
			}

			if (!_characters.TryGetValue(character, out characterData))
			{
				return false;
			}

			width = charWidth;
			return true;
		}

		private byte[] BuildPixels(byte[] characterData, int width, byte colour)
		{
			byte[] pixels = new byte[width * FontHeight];
			int index = 0;
			int dataIndex = 0;
			int bit = 0;
			for (int y = 0; y < FontHeight; y++)
			{
				if (bit > 0)
				{
					bit = 0;
					dataIndex++;
				}

				for (int x = 0; x < _charByteLength * 8; x++)
				{
					if (x < width)
					{
						pixels[index++] = ((characterData[dataIndex] & (0x80 >> bit)) > 0) ? colour : (byte)0;
					}

					if (++bit == 8)
					{
						bit = 0;
						dataIndex++;
					}
				}
			}

			return pixels;
		}

		public Fontset(byte[] bytes, ushort offset)
		{
			_fontAsciiFirst = bytes[offset - 8];
			_fontAsciiLast = bytes[offset - 7];
			_charByteLength = bytes[offset - 6];
			_charTopRow = bytes[offset - 5];
			_charBottomRow = bytes[offset - 4];

			int i = 0;
			int index = offset;
			int charCount = 1 + (_fontAsciiLast - _fontAsciiFirst);
			for (int c = _fontAsciiFirst; c <= _fontAsciiLast; c++)
			{
				i++;
				char character = (char)c;
				byte[] b = new byte[(1 + _charBottomRow - _charTopRow) * _charByteLength];
				for (int row = 0; row < (1 + _charBottomRow - _charTopRow); row++)
				{
					for (int col = 0; col < _charByteLength; col++)
					{
						int ind = (row * _charByteLength) + col;
						int bin = index + (row * (_charByteLength * charCount)) + col;

						b[ind] = bytes[bin];
					}
				}
				_characters.Add(character, b);

				byte charWidth = bytes[offset - 9 - charCount + i];
				if (charWidth > (_charByteLength * 8))
				{
					/*
					The _ character in FONTS.CV has a length of 9 bytes or 72 bits,
					but the width can be 8 bytes or 64 bits.
					This is a mistake in the original game and will be fixed here.
					*/
					Log($"Warning: Character width larger than bytes per character. (ID: {(int)character}, Width: {charWidth})");
					charWidth = (byte)(_charByteLength * 8);
				}
				_charWidths.Add(character, charWidth);

				index += _charByteLength;
			}
		}
	}
}
