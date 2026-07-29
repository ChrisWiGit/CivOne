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
using CivOne.Services.Random;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Deterministic <see cref="IRandomService"/> test double. By default every range returns its minimum,
	/// which keeps starting-position searches deterministic (they always try the "first" candidate tile first).
	/// A scripted sequence can be supplied instead, so a test can steer the search through specific tiles.
	/// </summary>
	internal sealed class StubRandomService : IRandomService
	{
		private readonly int[] _sequence;
		private int _sequenceIndex;

		/// <summary>
		/// Creates a stub that always returns the minimum of the requested range.
		/// </summary>
		public StubRandomService() : this([])
		{
		}

		/// <summary>
		/// Creates a stub that replays the given values in order, repeating from the start when exhausted.
		/// Each value is mapped into the requested range, so it never returns an out-of-range result.
		/// </summary>
		/// <param name="sequence">The values to replay.</param>
		public StubRandomService(params int[] sequence)
		{
			_sequence = sequence ?? [];
		}

		/// <summary>
		/// Overrides how a range is resolved. Takes precedence over the scripted sequence.
		/// </summary>
		public Func<int, int, int>? NextIntRangeHandler { get; set; }

		/// <summary>
		/// The value returned by both <see cref="Hit(int)"/> overloads.
		/// </summary>
		public bool HitResult { get; set; }

		/// <summary>
		/// The ranges the service was asked for, in call order. Useful to assert which areas were searched.
		/// </summary>
		public List<(int Min, int MaxExclusive)> RequestedRanges { get; } = [];

		public int NextInt(int maxExclusive) => NextInt(0, maxExclusive);

		public int NextInt(int min, int maxExclusive)
		{
			RequestedRanges.Add((min, maxExclusive));

			if (NextIntRangeHandler != null)
			{
				return NextIntRangeHandler(min, maxExclusive);
			}

			int range = maxExclusive - min;
			if (range <= 0 || _sequence.Length == 0)
			{
				return min;
			}

			int value = _sequence[_sequenceIndex % _sequence.Length];
			_sequenceIndex++;
			return min + (Math.Abs(value) % range);
		}

		public bool Hit(int percent) => HitResult;
		public bool Hit(int numerator, int denominator) => HitResult;
		public byte NextByte(byte min, byte maxExclusive) => (byte)NextInt(min, maxExclusive);
		public byte NextByte(byte maxExclusive) => (byte)NextInt(maxExclusive);
	}
}
