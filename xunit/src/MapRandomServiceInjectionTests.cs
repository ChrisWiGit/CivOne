// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Services.Random;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Proves that <see cref="Map"/> consumes randomness via the injected
	/// <see cref="IRandomService"/> rather than the global <c>Common.Random</c>.
	/// Locks down the RNG seam introduced in Phase B so future refactors of the
	/// random pipeline cannot silently re-couple <see cref="Map"/> to a global RNG.
	///
	/// Full pipeline determinism (running <c>GenerateThread</c> end-to-end and
	/// snapshotting tile histograms) is intentionally not covered here — it
	/// requires either making <c>GenerateThread</c> internally callable or
	/// reflection over private members. Tracked as Phase B follow-up.
	/// </summary>
	public class MapRandomServiceInjectionTests : src.TestsBase
	{
		private FixedRandomService? testee;
		[Fact]
		public void ConstructorUsesInjectedRandomServiceForTerrainMasterWord()
		{
			testee = new(7);

			Map actual = new(testee);

			Assert.Equal(7, actual.TerrainMasterWord);
			Assert.Equal(1, testee.NextCallCount);
		}

		[Fact]
		public void ConstructorWithoutInjectionFallsBackToFactoryOverride()
		{
			testee = new(13);
			RandomServiceFactory.Override(testee);

			// Reset clears the singleton so the next access constructs a fresh Map
			// through the parameterless ctor, which in turn calls the factory.
			Map.Reset();
			Map actual = Map.Instance;

			Assert.Equal(13, actual.TerrainMasterWord);
		}

		[Fact]
		public void TwoInstancesWithEquivalentTesteesHaveSameTerrainMasterWord()
		{
			Map first = new(new FixedRandomService(5));
			Map second = new(new FixedRandomService(5));

			Assert.Equal(first.TerrainMasterWord, second.TerrainMasterWord);
		}

		/// <summary>
		/// Test double that returns a fixed value from every <see cref="NextInt(int)"/> call
		/// and records the number of invocations. Sufficient to prove that <see cref="Map"/>
		/// actually consults its <see cref="IRandomService"/> dependency.
		/// </summary>
		private sealed class FixedRandomService(int value) : IRandomService
		{
			private readonly int _value = value;

			public int NextCallCount { get; private set; }

			public int NextInt(int max)
			{
				NextCallCount++;
				return _value;
			}

			public int NextInt(int min, int max)
			{
				NextCallCount++;
				return _value;
			}

			public bool Hit(int percent) => false;

			public byte NextByte(byte min, byte maxExclusive) => (byte)NextInt(min, maxExclusive);

			public byte NextByte(byte maxExclusive) => (byte)NextInt(maxExclusive);
		}
	}
}
