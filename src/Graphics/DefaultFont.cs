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
	internal class DefaultFont : IFont
	{
		private const bool B0 = false, B1 = true;
		private readonly Dictionary<char, bool[,]> _characters;

		public int FontHeight => 8;
		public byte FirstChar => 32;
		public byte LastChar => 127;

		public Bytemap GetLetter(char character, byte colour)
		{
			if (!_characters.TryGetValue(character, out bool[,]? pixels)) return new Bytemap(7, 7);

			Bytemap output = new Bytemap(pixels.GetLength(0), pixels.GetLength(1));
			for (int yy = 0; yy < pixels.GetLength(1); yy++)
			{
				for (int xx = 0; xx < pixels.GetLength(0); xx++)
				{
					output[xx, yy] = (byte)(pixels[xx, yy] ? colour : 0);
				}
			}
			return output;
		}

		public DefaultFont()
		{
			_characters = new Dictionary<char, bool[,]>
			{
				{ (char)32, new bool[,] { { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 } } },
				{ (char)33, new bool[,] { { B0, B1, B1, B1, B0, B1, B0 } } },
				{ (char)34, new bool[,] { { B1, B1, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B1, B1, B0, B0, B0, B0, B0 } } },
				{ (char)35, new bool[,] { { B0, B0, B1, B0, B1, B0, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B0, B1, B0, B1, B0, B0 } } },
				{ (char)36, new bool[,] { { B0, B0, B0, B1, B1, B0, B0 }, { B0, B0, B1, B1, B0, B1, B0 }, { B0, B0, B1, B0, B1, B1, B0 }, { B0, B0, B0, B1, B1, B0, B0 } } },
				{ (char)37, new bool[,] { { B0, B1, B0, B0, B0, B1, B0 }, { B0, B0, B0, B0, B1, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B1, B0, B0, B0, B1, B0 } } },
				{ (char)38, new bool[,] { { B0, B0, B0, B0, B1, B0, B0 }, { B0, B1, B0, B1, B0, B1, B0 }, { B1, B0, B1, B0, B0, B1, B0 }, { B0, B1, B0, B1, B1, B0, B0 }, { B0, B0, B0, B0, B1, B1, B0 } } },
				{ (char)39, new bool[,] { { B1, B1, B0, B0, B0, B0, B0 } } },
				{ (char)40, new bool[,] { { B0, B1, B1, B1, B1, B1, B0 }, { B1, B0, B0, B0, B0, B0, B1 } } },
				{ (char)41, new bool[,] { { B1, B0, B0, B0, B0, B0, B1 }, { B0, B1, B1, B1, B1, B1, B0 } } },
				{ (char)42, new bool[,] { { B0, B0, B1, B0, B1, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 } } },
				{ (char)43, new bool[,] { { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 } } },
				{ (char)44, new bool[,] { { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B0, B0, B0, B1, B0 } } },
				{ (char)45, new bool[,] { { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 } } },
				{ (char)46, new bool[,] { { B0, B0, B0, B0, B0, B1, B0 } } },
				{ (char)47, new bool[,] { { B0, B0, B0, B0, B0, B1, B1 }, { B0, B0, B1, B1, B1, B0, B0 }, { B1, B1, B0, B0, B0, B0, B0 } } },
				{ (char)48, new bool[,] { { B0, B1, B1, B1, B1, B1, B0 }, { B1, B0, B0, B0, B0, B0, B1 }, { B1, B0, B0, B0, B0, B0, B1 }, { B0, B1, B1, B1, B1, B1, B0 } } },
				{ (char)49, new bool[,] { { B0, B1, B0, B0, B0, B0, B0 }, { B1, B1, B1, B1, B1, B1, B1 } } },
				{ (char)50, new bool[,] { { B0, B1, B0, B0, B0, B1, B1 }, { B1, B0, B0, B0, B1, B0, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B0, B1, B1, B0, B0, B0, B1 } } },
				{ (char)51, new bool[,] { { B0, B1, B0, B0, B0, B1, B0 }, { B1, B0, B0, B0, B0, B0, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B0, B1, B1, B0, B1, B1, B0 } } },
				{ (char)52, new bool[,] { { B0, B0, B0, B1, B1, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B1, B0, B0, B1, B0, B0 }, { B1, B1, B1, B1, B1, B1, B1 }, { B0, B0, B0, B0, B1, B0, B0 } } },
				{ (char)53, new bool[,] { { B1, B1, B1, B0, B0, B1, B0 }, { B1, B0, B1, B0, B0, B0, B1 }, { B1, B0, B1, B0, B0, B0, B1 }, { B1, B0, B0, B1, B1, B1, B0 } } },
				{ (char)54, new bool[,] { { B0, B1, B1, B1, B1, B1, B0 }, { B1, B0, B0, B1, B0, B0, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B0, B1, B0, B0, B1, B1, B0 } } },
				{ (char)55, new bool[,] { { B1, B0, B0, B0, B0, B0, B0 }, { B1, B0, B0, B0, B0, B1, B1 }, { B1, B0, B1, B1, B1, B0, B0 }, { B1, B1, B0, B0, B0, B0, B0 } } },
				{ (char)56, new bool[,] { { B0, B1, B1, B0, B1, B1, B0 }, { B1, B0, B0, B1, B0, B0, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B0, B1, B1, B0, B1, B1, B0 } } },
				{ (char)57, new bool[,] { { B0, B1, B1, B0, B0, B1, B0 }, { B1, B0, B0, B1, B0, B0, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B0, B1, B1, B1, B1, B1, B0 } } },
				{ (char)58, new bool[,] { { B0, B0, B0, B1, B0, B1, B0 } } },
				{ (char)59, new bool[,] { { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B0, B1, B0, B1, B0 } } },
				{ (char)60, new bool[,] { { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B1, B0, B0, B0, B1, B0 } } },
				{ (char)61, new bool[,] { { B0, B0, B1, B0, B1, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 } } },
				{ (char)62, new bool[,] { { B0, B1, B0, B0, B0, B1, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 } } },
				{ (char)63, new bool[,] { { B0, B1, B0, B0, B0, B0, B0 }, { B1, B0, B0, B0, B0, B0, B0 }, { B1, B0, B0, B1, B0, B1, B0 }, { B0, B1, B1, B0, B0, B0, B0 } } },
				{ (char)64, new bool[,] { { B0, B0, B1, B1, B1, B0, B0 }, { B0, B1, B0, B0, B0, B1, B0 }, { B1, B0, B0, B1, B0, B0, B1 }, { B1, B0, B1, B0, B1, B0, B1 }, { B1, B0, B1, B1, B1, B0, B1 }, { B1, B1, B0, B0, B1, B0, B1 }, { B0, B0, B1, B1, B0, B0, B0 } } },
				{ (char)65, new bool[,] { { B0, B0, B0, B0, B0, B1, B1 }, { B0, B0, B0, B1, B1, B0, B0 }, { B0, B1, B1, B0, B1, B0, B0 }, { B1, B0, B0, B0, B1, B0, B0 }, { B0, B1, B1, B0, B1, B0, B0 }, { B0, B0, B0, B1, B1, B0, B0 }, { B0, B0, B0, B0, B0, B1, B1 } } },
				{ (char)66, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B0, B1, B1, B0, B1, B1, B0 } } },
				{ (char)67, new bool[,] { { B0, B1, B1, B1, B1, B1, B0 }, { B1, B0, B0, B0, B0, B0, B1 }, { B1, B0, B0, B0, B0, B0, B1 }, { B1, B0, B0, B0, B0, B0, B1 }, { B0, B1, B0, B0, B0, B1, B0 } } },
				{ (char)68, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B1, B0, B0, B0, B0, B0, B1 }, { B1, B0, B0, B0, B0, B0, B1 }, { B0, B1, B0, B0, B0, B1, B0 }, { B0, B0, B1, B1, B1, B0, B0 } } },
				{ (char)69, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B1, B0, B0, B1, B0, B0, B1 } } },
				{ (char)70, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B1, B0, B0, B1, B0, B0, B0 }, { B1, B0, B0, B1, B0, B0, B0 }, { B1, B0, B0, B1, B0, B0, B0 } } },
				{ (char)71, new bool[,] { { B0, B1, B1, B1, B1, B1, B0 }, { B1, B0, B0, B0, B0, B0, B1 }, { B1, B0, B0, B0, B0, B0, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B0, B1, B0, B1, B1, B1, B1 } } },
				{ (char)72, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B1, B1, B1, B1, B1, B1, B1 } } },
				{ (char)73, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 } } },
				{ (char)74, new bool[,] { { B0, B0, B0, B0, B0, B1, B0 }, { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B0, B0, B0, B0, B1 }, { B1, B1, B1, B1, B1, B1, B0 } } },
				{ (char)75, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B1, B0, B0, B0, B1, B0 }, { B1, B0, B0, B0, B0, B0, B1 } } },
				{ (char)76, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B0, B0, B0, B0, B1 } } },
				{ (char)77, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B0, B0, B1, B1, B0, B0 }, { B0, B0, B0, B0, B0, B1, B0 }, { B0, B0, B0, B1, B1, B0, B0 }, { B0, B0, B1, B0, B0, B0, B0 }, { B1, B1, B1, B1, B1, B1, B1 } } },
				{ (char)78, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B0, B1, B1, B0, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B0, B1, B1, B0 }, { B1, B1, B1, B1, B1, B1, B1 } } },
				{ (char)79, new bool[,] { { B0, B1, B1, B1, B1, B1, B0 }, { B1, B0, B0, B0, B0, B0, B1 }, { B1, B0, B0, B0, B0, B0, B1 }, { B1, B0, B0, B0, B0, B0, B1 }, { B0, B1, B1, B1, B1, B1, B0 } } },
				{ (char)80, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B1, B0, B0, B1, B0, B0, B0 }, { B1, B0, B0, B1, B0, B0, B0 }, { B1, B0, B0, B1, B0, B0, B0 }, { B0, B1, B1, B0, B0, B0, B0 } } },
				{ (char)81, new bool[,] { { B0, B1, B1, B1, B1, B1, B0 }, { B1, B0, B0, B0, B0, B0, B1 }, { B1, B0, B0, B0, B1, B0, B1 }, { B1, B0, B0, B0, B0, B1, B1 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B0, B0, B0, B0, B0, B1 } } },
				{ (char)82, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B1, B0, B0, B1, B0, B0, B0 }, { B1, B0, B0, B1, B0, B0, B0 }, { B1, B0, B0, B1, B1, B0, B0 }, { B0, B1, B1, B0, B0, B1, B1 } } },
				{ (char)83, new bool[,] { { B0, B1, B1, B0, B0, B1, B0 }, { B1, B0, B0, B1, B0, B0, B1 }, { B1, B0, B0, B1, B0, B0, B1 }, { B0, B1, B0, B0, B1, B1, B0 } } },
				{ (char)84, new bool[,] { { B1, B0, B0, B0, B0, B0, B0 }, { B1, B0, B0, B0, B0, B0, B0 }, { B1, B1, B1, B1, B1, B1, B1 }, { B1, B0, B0, B0, B0, B0, B0 }, { B1, B0, B0, B0, B0, B0, B0 } } },
				{ (char)85, new bool[,] { { B1, B1, B1, B1, B1, B1, B0 }, { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B0, B0, B0, B0, B1 }, { B1, B1, B1, B1, B1, B1, B0 } } },
				{ (char)86, new bool[,] { { B1, B1, B1, B0, B0, B0, B0 }, { B0, B0, B0, B1, B1, B1, B0 }, { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B0, B1, B1, B1, B0 }, { B1, B1, B1, B0, B0, B0, B0 } } },
				{ (char)87, new bool[,] { { B1, B1, B0, B0, B0, B0, B0 }, { B0, B0, B1, B1, B0, B0, B0 }, { B0, B0, B0, B0, B1, B1, B1 }, { B0, B0, B1, B1, B0, B0, B0 }, { B1, B1, B0, B0, B0, B0, B0 }, { B0, B0, B1, B1, B0, B0, B0 }, { B0, B0, B0, B0, B1, B1, B1 }, { B0, B0, B1, B1, B0, B0, B0 }, { B1, B1, B0, B0, B0, B0, B0 } } },
				{ (char)88, new bool[,] { { B1, B0, B0, B0, B0, B0, B1 }, { B0, B1, B0, B0, B0, B1, B0 }, { B0, B0, B1, B1, B1, B0, B0 }, { B0, B0, B1, B1, B1, B0, B0 }, { B0, B1, B0, B0, B0, B1, B0 }, { B1, B0, B0, B0, B0, B0, B1 } } },
				{ (char)89, new bool[,] { { B1, B1, B0, B0, B0, B0, B0 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B0, B0, B1, B1, B1, B1 }, { B0, B0, B1, B0, B0, B0, B0 }, { B1, B1, B0, B0, B0, B0, B0 } } },
				{ (char)90, new bool[,] { { B1, B0, B0, B0, B0, B1, B1 }, { B1, B0, B0, B1, B1, B0, B1 }, { B1, B0, B1, B0, B0, B0, B1 }, { B1, B1, B0, B0, B0, B0, B1 } } },
				{ (char)91, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B1, B0, B0, B0, B0, B0, B1 } } },
				{ (char)92, new bool[,] { { B0, B1, B0, B0, B0, B0, B0 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B0, B1, B0, B0 }, { B0, B0, B0, B0, B0, B1, B0 } } },
				{ (char)93, new bool[,] { { B1, B0, B0, B0, B0, B0, B1 }, { B1, B1, B1, B1, B1, B1, B1 } } },
				{ (char)94, new bool[,] { { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B0, B1, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B1, B0, B0, B0, B0, B0 } } },
				{ (char)95, new bool[,] { { B0, B0, B1, B1, B1, B0, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B0, B1, B1, B1, B0, B0 } } },
				{ (char)96, new bool[,] { { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 } } },
				{ (char)97, new bool[,] { { B0, B0, B1, B0, B0, B1, B0 }, { B0, B0, B1, B0, B1, B0, B1 }, { B0, B0, B1, B0, B1, B0, B1 }, { B0, B0, B0, B1, B1, B1, B1 } } },
				{ (char)98, new bool[,] { { B1, B1, B1, B1, B1, B1, B1 }, { B0, B0, B1, B0, B0, B0, B1 }, { B0, B0, B1, B0, B0, B0, B1 }, { B0, B0, B0, B1, B1, B1, B0 } } },
				{ (char)99, new bool[,] { { B0, B0, B0, B1, B1, B1, B0 }, { B0, B0, B1, B0, B0, B0, B1 }, { B0, B0, B1, B0, B0, B0, B1 }, { B0, B0, B0, B1, B0, B1, B0 } } },
				{ (char)100, new bool[,] { { B0, B0, B0, B0, B1, B1, B0 }, { B0, B0, B0, B1, B0, B0, B1 }, { B0, B0, B0, B1, B0, B0, B1 }, { B0, B1, B1, B1, B1, B1, B1 } } },
				{ (char)101, new bool[,] { { B0, B0, B0, B1, B1, B1, B0 }, { B0, B0, B1, B0, B1, B0, B1 }, { B0, B0, B1, B0, B1, B0, B1 }, { B0, B0, B0, B1, B1, B0, B1 } } },
				{ (char)102, new bool[,] { { B0, B0, B0, B1, B1, B1, B1 }, { B0, B0, B1, B0, B1, B0, B0 } } },
				{ (char)103, new bool[,] { { B0, B0, B0, B1, B0, B0, B1 }, { B0, B0, B1, B0, B1, B0, B1 }, { B0, B0, B1, B0, B1, B0, B1 }, { B0, B0, B1, B1, B1, B1, B0 } } },
				{ (char)104, new bool[,] { { B0, B1, B1, B1, B1, B1, B1 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B0, B0, B1, B1, B1 } } },
				{ (char)105, new bool[,] { { B0, B1, B0, B1, B1, B1, B1 } } },
				{ (char)106, new bool[,] { { B0, B0, B0, B0, B0, B0, B1 }, { B0, B1, B0, B1, B1, B1, B0 } } },
				{ (char)107, new bool[,] { { B0, B1, B1, B1, B1, B1, B1 }, { B0, B0, B0, B0, B1, B0, B0 }, { B0, B0, B0, B1, B0, B1, B0 }, { B0, B0, B1, B0, B0, B0, B1 } } },
				{ (char)108, new bool[,] { { B0, B1, B1, B1, B1, B1, B1 } } },
				{ (char)109, new bool[,] { { B0, B0, B1, B1, B1, B1, B1 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B0, B1, B1, B1, B1, B1 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B0, B0, B1, B1, B1, B1 } } },
				{ (char)110, new bool[,] { { B0, B0, B1, B1, B1, B1, B1 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B0, B1, B0, B0, B0, B0 }, { B0, B0, B0, B1, B1, B1, B1 } } },
				{ (char)111, new bool[,] { { B0, B0, B0, B1, B1, B1, B0 }, { B0, B0, B1, B0, B0, B0, B1 }, { B0, B0, B1, B0, B0, B0, B1 }, { B0, B0, B0, B1, B1, B1, B0 } } },
				{ (char)112, new bool[,] { { B0, B0, B1, B1, B1, B1, B1 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B0, B0, B1, B0, B0, B0 } } },
				{ (char)113, new bool[,] { { B0, B0, B0, B1, B0, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B0, B1, B0, B1, B0, B0 }, { B0, B0, B1, B1, B1, B1, B1 } } },
				{ (char)114, new bool[,] { { B0, B0, B1, B1, B1, B1, B1 }, { B0, B0, B1, B0, B0, B0, B0 } } },
				{ (char)115, new bool[,] { { B0, B0, B0, B1, B0, B0, B1 }, { B0, B0, B1, B0, B1, B0, B1 }, { B0, B0, B1, B0, B1, B0, B1 }, { B0, B0, B1, B0, B0, B1, B0 } } },
				{ (char)116, new bool[,] { { B0, B1, B1, B1, B1, B1, B0 }, { B0, B0, B0, B1, B0, B0, B1 }, { B0, B0, B0, B0, B0, B0, B1 } } },
				{ (char)117, new bool[,] { { B0, B0, B1, B1, B1, B1, B0 }, { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B1, B1, B1, B1, B1 } } },
				{ (char)118, new bool[,] { { B0, B0, B1, B1, B0, B0, B0 }, { B0, B0, B0, B0, B1, B1, B0 }, { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B0, B0, B1, B1, B0 }, { B0, B0, B1, B1, B0, B0, B0 } } },
				{ (char)119, new bool[,] { { B0, B0, B1, B1, B1, B0, B0 }, { B0, B0, B0, B0, B0, B1, B1 }, { B0, B0, B1, B1, B1, B0, B0 }, { B0, B0, B0, B0, B0, B1, B1 }, { B0, B0, B1, B1, B1, B0, B0 } } },
				{ (char)120, new bool[,] { { B0, B0, B1, B1, B0, B1, B1 }, { B0, B0, B0, B0, B1, B0, B0 }, { B0, B0, B1, B1, B0, B1, B1 } } },
				{ (char)121, new bool[,] { { B0, B0, B0, B0, B0, B0, B1 }, { B0, B0, B1, B1, B0, B0, B1 }, { B0, B0, B0, B0, B1, B1, B0 }, { B0, B0, B0, B0, B1, B0, B0 }, { B0, B0, B1, B1, B0, B0, B0 } } },
				{ (char)122, new bool[,] { { B0, B0, B1, B0, B0, B1, B1 }, { B0, B0, B1, B0, B1, B0, B1 }, { B0, B0, B1, B0, B1, B0, B1 }, { B0, B0, B1, B1, B0, B0, B1 } } },
				{ (char)123, new bool[,] { { B0, B1, B1, B0, B0, B0, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B1, B1, B1, B1, B0 }, { B0, B1, B1, B1, B0, B0, B0 } } },
				{ (char)124, new bool[,] { { B0, B1, B1, B1, B1, B0, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B1, B1, B1, B1, B1, B0 }, { B0, B1, B1, B1, B1, B0, B0 } } },
				{ (char)125, new bool[,] { { B0, B0, B0, B1, B0, B0, B0 }, { B1, B0, B1, B0, B1, B1, B1 }, { B1, B1, B0, B0, B0, B1, B1 }, { B1, B1, B1, B0, B1, B0, B1 }, { B0, B0, B0, B1, B0, B0, B0 } } },
				{ (char)126, new bool[,] { { B0, B1, B1, B1, B0, B0, B0 }, { B1, B0, B0, B0, B1, B1, B0 }, { B1, B0, B1, B1, B1, B1, B0 }, { B1, B0, B0, B0, B1, B1, B0 }, { B0, B1, B1, B1, B0, B0, B0 } } },
				{ (char)127, new bool[,] { { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 }, { B0, B0, B0, B0, B0, B0, B0 } } }
			};
		}
	}
}