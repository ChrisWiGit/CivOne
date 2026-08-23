// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Services.Random;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Reproducible <see cref="IRandomService"/> test double backed by a small linear congruential generator.
	/// Unlike <see cref="StubRandomService"/> it really spreads values over the requested range, which is what
	/// tests need when they check properties of the search itself (e.g. distances between placements).
	/// The same seed always produces the same sequence, so failures stay reproducible.
	/// </summary>
	/// <param name="seed">The seed of the generator.</param>
	internal sealed class SeededRandomService(uint seed) : IRandomService
	{
		private uint _state = seed == 0 ? 1u : seed;

		private uint Next()
		{
			// Numerical Recipes LCG constants.
			_state = (_state * 1664525u) + 1013904223u;
			return _state;
		}

		public int NextInt(int maxExclusive) => NextInt(0, maxExclusive);

		public int NextInt(int min, int maxExclusive)
		{
			int range = maxExclusive - min;
			if (range <= 0)
			{
				return min;
			}

			return min + (int)(Next() % (uint)range);
		}

		public bool Hit(int percent) => NextInt(100) < percent;
		public bool Hit(int numerator, int denominator) => NextInt(denominator) < numerator;
		public byte NextByte(byte min, byte maxExclusive) => (byte)NextInt(min, maxExclusive);
		public byte NextByte(byte maxExclusive) => (byte)NextInt(maxExclusive);
	}
}
