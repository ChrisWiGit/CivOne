// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Services.Random;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Deterministic <see cref="IRandomService"/> test double. By default every range returns its minimum,
	/// which keeps starting-position searches deterministic (they always try the "first" candidate tile first).
	/// </summary>
	internal sealed class StubRandomService : IRandomService
	{
		public Func<int, int, int> NextIntRangeHandler { get; set; } = (min, _) => min;
		public bool HitResult { get; set; }

		public int NextInt(int maxExclusive) => NextIntRangeHandler(0, maxExclusive);
		public int NextInt(int min, int maxExclusive) => NextIntRangeHandler(min, maxExclusive);
		public bool Hit(int percent) => HitResult;
		public bool Hit(int numerator, int denominator) => HitResult;
		public byte NextByte(byte min, byte maxExclusive) => (byte)NextInt(min, maxExclusive);
		public byte NextByte(byte maxExclusive) => (byte)NextInt(maxExclusive);
	}
}
