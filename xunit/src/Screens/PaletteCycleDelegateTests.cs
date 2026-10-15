using CivOne.Graphics;
using CivOne.Screens.SpaceShipAssets;
using Xunit;

namespace CivOne.UnitTests.Screens
{
	public class PaletteCycleDelegateTests
	{
		[Fact]
		public void FirstRangeRotatesUpwardsEveryFifteenOriginalTicks()
		{
			using Palette palette = new();
			for (int i = 0; i < 256; i++)
			{
				palette[i] = new Colour(i, i, i);
			}

			PaletteCycleDelegate testee = new(palette, PaletteCycleDelegate.SpaceStationCycles);

			// 15 ticks at 70 Hz are about 3.2 screen ticks at 15 Hz.
			Assert.False(testee.Update(3));
			Assert.True(testee.Update(4));

			Assert.Equal(new Colour(31, 31, 31), palette[16]);
			Assert.Equal(new Colour(16, 16, 16), palette[17]);
			Assert.Equal(new Colour(32, 32, 32), palette[32]);
		}

		[Fact]
		public void SpaceShipLightsSwapEveryHundredFiftyOriginalTicks()
		{
			using Palette palette = new();
			for (int i = 0; i < 256; i++)
			{
				palette[i] = new Colour(i, i, i);
			}

			PaletteCycleDelegate testee = new(palette, PaletteCycleDelegate.SpaceShipCycles);

			// 150 ticks at 70 Hz are about 32.1 screen ticks at 15 Hz.
			testee.Update(32);
			Assert.Equal(new Colour(128, 128, 128), palette[128]);

			testee.Update(33);
			Assert.Equal(new Colour(129, 129, 129), palette[128]);
			Assert.Equal(new Colour(128, 128, 128), palette[129]);
		}

		[Theory]
		[InlineData(127, false)]
		[InlineData(129, true)]
		[InlineData(150, true)]
		[InlineData(191, false)]
		[InlineData(239, true)]
		[InlineData(240, false)]
		public void SpaceShipCyclesCoverTheOriginalRanges(int index, bool expected)
		{
			using Palette palette = new();

			Assert.Equal(expected, new PaletteCycleDelegate(palette, PaletteCycleDelegate.SpaceShipCycles).IsAnimated(index));
		}

	
		[Theory]
		[InlineData(15, false)]
		[InlineData(16, true)]
		[InlineData(63, true)]
		[InlineData(64, false)]
		public void IsAnimatedCoversTheRotatingRanges(int index, bool expected)
		{
			using Palette palette = new();

			Assert.Equal(expected, new PaletteCycleDelegate(palette, PaletteCycleDelegate.SpaceStationCycles).IsAnimated(index));
		}
	}
}
