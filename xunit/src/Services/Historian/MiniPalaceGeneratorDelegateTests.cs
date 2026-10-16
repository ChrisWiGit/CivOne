using System.Linq;
using CivOne.Enums;
using CivOne.Screens.PalaceAssets;
using CivOne.Services.Historian;
using Xunit;

namespace CivOne.UnitTests.Services.Historian
{
	public class MiniPalaceGeneratorDelegateTests
	{
		// Reference values from a simulation of the DOS routine with 16-bit semantics.
		private static readonly int[] NoForcedWrites = [0, 0, 0, 2, 2, 2, 1, 1, -1];
		private static readonly int[] WithForcedWrites = [3, 3, 4, 3, 3, 3, 4, 3, 4];
		private static readonly int[] ScreenshotVisibleSlots = [3, 4];

		private readonly MiniPalaceGeneratorDelegate _generator = new();

		[Theory]
		[InlineData(0, 0)]
		[InlineData(5, 0)]
		[InlineData(6, 1)]
		[InlineData(15, 1)]
		[InlineData(16, 2)]
		[InlineData(50, 3)]
		[InlineData(150, 7)]
		[InlineData(-20, 0)]
		public void PalaceLevelFollowsOriginalLoop(int citySum, int expected)
		{
			Assert.Equal(expected, new HistorianPalaceService().PalaceLevel(citySum));
		}

		[Fact]
		public void GeneratorMatchesDosReferenceWithoutForcedWrites()
		{
			Assert.Equal(NoForcedWrites, _generator.GenerateOriginalLevels(1, 10, 1234));
		}

		[Fact]
		public void GeneratorMatchesDosReferenceWithForcedWrites()
		{
			Assert.Equal(WithForcedWrites, _generator.GenerateOriginalLevels(2, 33, 1234));
		}

		[Fact]
		public void GeneratorIsDeterministic()
		{
			Assert.Equal(_generator.GenerateOriginalLevels(5, 20, 4711), _generator.GenerateOriginalLevels(5, 20, 4711));
		}

		[Fact]
		public void LayoutKeepsAllNineOriginalLevelsAndPlayerStyle()
		{
			MiniPalaceLayout palace = _generator.Generate(1, 10, 1234);

			int[] levels = [.. Enumerable.Range(0, MiniPalaceLayout.SlotCount).Select(palace.Level)];
			Assert.Equal(NoForcedWrites, levels);
			Assert.Equal(PalaceStyle.Classical, palace.Style(0));
			Assert.Equal(PalaceStyle.Classical, palace.Style(8));
		}

		[Fact]
		public void SmallestPalaceShowsNothing()
		{
			MiniPalaceLayout palace = _generator.Generate(1, 0, 1234);

			Assert.False(palace.HasVisibleSlot);
		}

		[Fact]
		public void ForcedLevelsAreCappedAtFour()
		{
			MiniPalaceLayout palace = _generator.Generate(2, 33, 1234);

			for (int i = 0; i < MiniPalaceLayout.SlotCount; i++)
			{
				Assert.InRange(palace.Level(i), -1, 4);
			}
		}

		// Original game screenshot: Romans (slot 1) with a city sum of 48 and random seed 18314 show only the
		// classical slots 3 and 4 at level 1. See docs/plan-palace.impl.md, Historian.
		[Fact]
		public void MatchesOriginalGameScreenshot()
		{
			int buildCount = new HistorianPalaceService().PalaceLevel(48);

			MiniPalaceLayout palace = _generator.Generate(1, buildCount, 18314);

			Assert.Equal(3, buildCount);
			int[] visible = [.. Enumerable.Range(0, MiniPalaceLayout.SlotCount).Where(palace.IsVisible)];
			Assert.Equal(ScreenshotVisibleSlots, visible);
			Assert.Equal(1, palace.Level(3));
			Assert.Equal(1, palace.Level(4));
			Assert.Equal(PalaceStyle.Classical, palace.Style(3));
			Assert.Equal(0, palace.Level(2)); // base section: not drawn, but not missing
			Assert.Equal(PreviewPalacePart.WallLeft, palace.PartAt(3));
			Assert.Equal(PreviewPalacePart.Center, palace.PartAt(4));
		}
	}
}
