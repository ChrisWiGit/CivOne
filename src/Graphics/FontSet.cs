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
	internal class Fontset : IFont
	{
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		private readonly byte _fontAsciiFirst;
		private readonly byte _fontAsciiLast;
		private readonly byte _charByteLength;
		private readonly byte _charTopRow;
		private readonly byte _charBottomRow;
		private readonly byte _fontSpaceX;
		private readonly byte _fontSpaceY;
		private Dictionary<char, byte> _charWidths = new Dictionary<char, byte>();
		private Dictionary<char, byte[]> _characters = new Dictionary<char, byte[]>();
		
		public int FontHeight => 1 + _charBottomRow - _charTopRow;
		public byte FirstChar => _fontAsciiFirst;
		public byte LastChar => _fontAsciiLast;
		
		public Bytemap GetLetter(char character, byte colour)
		{
			if (!_charWidths.ContainsKey(character) || !_characters.ContainsKey(character)) return new Bytemap(8, 8);
			int ww = _charWidths[character];

			byte[] pixels = new byte[ww * FontHeight];
			int index = 0, b = 0, bit = 0;;
			for (int y = 0; y < FontHeight; y++)
			{
				if (bit > 0)
				{
					bit = 0;
					b++;
				}
				for (int x = 0; x < _charByteLength * 8; x++)
				{
					if (x < _charWidths[character])
					{
						if ((_characters[character][b] & (0x80 >> bit)) > 0)
						{
							pixels[index++] = colour;
						}
						else
						{
							pixels[index++] = 0;
						}
					}

					if (++bit == 8)
					{
						bit = 0;
						b++;
					}
				}
			}
			return new Bytemap(ww, FontHeight).FromByteArray(pixels);
		}

		public Fontset(byte[] bytes, ushort offset)
		{
			_fontAsciiFirst = bytes[offset - 8];
			_fontAsciiLast = bytes[offset - 7];
			_charByteLength = bytes[offset - 6];
			_charTopRow = bytes[offset - 5];
			_charBottomRow = bytes[offset - 4];
			_fontSpaceX = bytes[offset - 3];
			_fontSpaceY = bytes[offset - 2];

			int i = 0;
			int index = (int)offset;
			int charCount = 1 + (_fontAsciiLast - _fontAsciiFirst);
			for (int c = _fontAsciiFirst; c <= _fontAsciiLast; c++)
			{
				int ww = i++;
				char character = (char)c;
				byte[] b = new byte[(1 + _charBottomRow - _charTopRow) * _charByteLength];
				for (int row = 0; row < (1 + _charBottomRow - _charTopRow); row++)
				for (int col = 0; col < _charByteLength; col++)
				{
					int ind = (row * _charByteLength) + col;
					int bin = index + (row * (_charByteLength * charCount)) + col;

					b[ind] = bytes[bin];
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