// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using Xunit;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Tests for <see cref="LegacyMapSaveCompatibilityService.Evaluate"/>, the rule set that decides
	/// whether the current map may be written as a legacy Civilization I <c>*.map</c> file.
	/// </summary>
	public class LegacyMapSaveCompatibilityServiceTests
	{
		private readonly LegacyMapSaveCompatibilityService _testee = new();

		private static LegacyMapSaveCompatibilitySnapshot CreateSnapshot(
			int mapWidth = 80,
			int mapHeight = 50,
			uint mapSeed = 0,
			int startPositionCount = 0,
			bool hasPollution = false,
			bool hasFortress = false)
			=> new(mapWidth, mapHeight, mapSeed, startPositionCount, hasPollution, hasFortress);

		[Fact]
		public void EvaluateWithDefaultMapReturnsCompatible()
		{
			LegacyMapSaveCompatibilityResult actual = _testee.Evaluate(CreateSnapshot());

			Assert.True(actual.CanSaveAsLegacyMap);
			Assert.Equal(string.Empty, actual.Reason);
		}

		[Theory]
		[InlineData(64, 50)]
		[InlineData(80, 40)]
		[InlineData(100, 100)]
		public void EvaluateWithNonStandardSizeReturnsIncompatible(int width, int height)
		{
			LegacyMapSaveCompatibilityResult actual = _testee.Evaluate(CreateSnapshot(mapWidth: width, mapHeight: height));

			Assert.False(actual.CanSaveAsLegacyMap);
			Assert.Contains("size", actual.Reason, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void EvaluateWithSeedAboveSixteenBitReturnsIncompatible()
		{
			LegacyMapSaveCompatibilityResult actual = _testee.Evaluate(CreateSnapshot(mapSeed: 65536));

			Assert.False(actual.CanSaveAsLegacyMap);
			Assert.Contains("seed", actual.Reason, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void EvaluateWithMaxSixteenBitSeedReturnsCompatible()
		{
			LegacyMapSaveCompatibilityResult actual = _testee.Evaluate(CreateSnapshot(mapSeed: ushort.MaxValue));

			Assert.True(actual.CanSaveAsLegacyMap);
		}

		[Fact]
		public void EvaluateWithStartPositionsReturnsIncompatible()
		{
			LegacyMapSaveCompatibilityResult actual = _testee.Evaluate(CreateSnapshot(startPositionCount: 1));

			Assert.False(actual.CanSaveAsLegacyMap);
			Assert.Contains("start position", actual.Reason, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void EvaluateWithPollutionReturnsIncompatible()
		{
			LegacyMapSaveCompatibilityResult actual = _testee.Evaluate(CreateSnapshot(hasPollution: true));

			Assert.False(actual.CanSaveAsLegacyMap);
			Assert.Contains("pollution", actual.Reason, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void EvaluateWithFortressReturnsIncompatible()
		{
			LegacyMapSaveCompatibilityResult actual = _testee.Evaluate(CreateSnapshot(hasFortress: true));

			Assert.False(actual.CanSaveAsLegacyMap);
			Assert.Contains("fortress", actual.Reason, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void EvaluateWithNullSnapshotThrows()
		{
			Assert.Throws<ArgumentNullException>(() => _testee.Evaluate(null!));
		}
	}
}
