using Xunit;

namespace CivOne.UnitTests
{
	public class PalaceDataTests
	{
		[Fact]
		public void IsSlotUnlockedInitialStateUnlocksCenterAndAdjacentWings()
		{
			var palaceData = new PalaceData();

			Assert.True(palaceData.IsSlotUnlocked(2));
			Assert.True(palaceData.IsSlotUnlocked(3));
			Assert.True(palaceData.IsSlotUnlocked(4));
			Assert.False(palaceData.IsSlotUnlocked(1));
			Assert.False(palaceData.IsSlotUnlocked(5));
		}

		[Fact]
		public void IsSlotUnlockedWhenAdjacentWingBuiltUnlocksNextOuterSlot()
		{
			var palaceData = new PalaceData();

			palaceData.SetPalace(2, 1, 1);
			palaceData.SetPalace(4, 1, 1);

			Assert.True(palaceData.IsSlotUnlocked(1));
			Assert.True(palaceData.IsSlotUnlocked(5));
			Assert.False(palaceData.IsSlotUnlocked(0));
			Assert.False(palaceData.IsSlotUnlocked(6));
		}
	}
}