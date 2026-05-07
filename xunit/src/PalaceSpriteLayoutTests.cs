using CivOne.Enums;
using CivOne.Graphics;
using Xunit;

namespace CivOne.UnitTests
{
	public class PalaceSpriteLayoutTests
	{
		[Fact]
		public void GetPartSourceRect_ForIslamicCenter_UsesSecondQuadrantYRange()
		{
			var testee = new PalaceSpriteLayout();
			PalacePictureLayout layout = testee.GetLayout(level: 1);

			PalacePartSourceRect actual = testee.GetPartSourceRect(PalacePart.Center, PalaceStyle.Islamic, layout);

			Assert.Equal(101, actual.Y);
			Assert.Equal(52, actual.Width);
			Assert.Equal(99, actual.Height);
		}

		[Fact]
		public void GetPartSourceRect_ForIslamicTowers_UsesSecondQuadrantYRange()
		{
			var testee = new PalaceSpriteLayout();
			PalacePictureLayout layout = testee.GetLayout(level: 1);

			PalacePartSourceRect leftTower = testee.GetPartSourceRect(PalacePart.LeftTower, PalaceStyle.Islamic, layout);
			PalacePartSourceRect rightTower = testee.GetPartSourceRect(PalacePart.RightTower, PalaceStyle.Islamic, layout);

			Assert.Equal(101, leftTower.Y);
			Assert.Equal(101, rightTower.Y);
		}
	}
}
