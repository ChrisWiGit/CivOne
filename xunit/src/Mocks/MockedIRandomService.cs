using CivOne.Services.Random;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Random service that always returns the same value, so tests stay deterministic.
	/// </summary>
	/// <param name="fixedValue">The value returned by every draw.</param>
	/// <param name="hit">The result of every chance check.</param>
	internal sealed class MockedIRandomService(int fixedValue = 0, bool hit = false) : IRandomService
	{
		/// <summary>
		/// Number of draws taken so far.
		/// Lets tests verify that a code path keeps the random sequence of a game stable.
		/// </summary>
		public int DrawCount { get; private set; }

		/// <summary>
		/// Returns the fixed value, capped at the exclusive maximum.
		/// </summary>
		/// <param name="maxExclusive">Exclusive upper bound.</param>
		/// <returns>The fixed value, or one below the bound when the fixed value is too large.</returns>
		public int NextInt(int maxExclusive)
		{
			DrawCount++;
			return maxExclusive <= 0 ? 0 : System.Math.Min(fixedValue, maxExclusive - 1);
		}

		/// <summary>
		/// Returns the fixed value, pulled into the given range.
		/// </summary>
		/// <param name="min">Inclusive lower bound.</param>
		/// <param name="maxExclusive">Exclusive upper bound.</param>
		/// <returns>The fixed value inside the range.</returns>
		public int NextInt(int min, int maxExclusive)
		{
			DrawCount++;
			return maxExclusive <= min ? min : System.Math.Clamp(fixedValue, min, maxExclusive - 1);
		}

		/// <summary>
		/// Returns the configured chance result.
		/// </summary>
		/// <param name="percent">Ignored.</param>
		/// <returns>The configured result.</returns>
		public bool Hit(int percent) => hit;

		/// <summary>
		/// Returns the configured chance result.
		/// </summary>
		/// <param name="numerator">Ignored.</param>
		/// <param name="denominator">Ignored.</param>
		/// <returns>The configured result.</returns>
		public bool Hit(int numerator, int denominator) => hit;
	}
}
