using CivOne.Screens.Reports;
using Xunit;

namespace CivOne.UnitTests.Screens
{
	public class CivilizationScoreLayoutDelegateTests
	{
		private readonly CivilizationScoreLayoutDelegate _layout = new();

		[Fact]
		public void FirstCitizenIconStartsOnTheLineBelowTheHeader()
		{
			CivilizationScoreLayout layout = _layout.Calculate(3, 0, false, false);

			Assert.Equal(26, layout.CitizensHeaderY);
			Assert.Equal([(16, 34), (24, 34), (32, 34)], layout.CitizenIcons);
		}

		[Fact]
		public void CitizenIconsWrapAfterX292WithAlternatingIndent()
		{
			// 35 icons fill x 16..288 (35 positions); the 36th wraps to the next row.
			CivilizationScoreLayout layout = _layout.Calculate(36, 0, false, false);

			Assert.Equal((288, 34), layout.CitizenIcons[34]);
			// y 42: (42 >> 1) & 4 = 4 -> x 20
			Assert.Equal((20, 42), layout.CitizenIcons[35]);
		}

		[Fact]
		public void WondersUseTwoColumns()
		{
			CivilizationScoreLayout layout = _layout.Calculate(1, 3, false, false);

			Assert.Equal(50, layout.AchievementsHeaderY);
			Assert.Equal((16, 58, 36, 60), layout.Wonders[0]);
			Assert.Equal((168, 58, 188, 60), layout.Wonders[1]);
			Assert.Equal((16, 68, 36, 70), layout.Wonders[2]);
		}

		[Fact]
		public void TextLinesFollowTheWonders()
		{
			CivilizationScoreLayout layout = _layout.Calculate(1, 1, true, true);

			Assert.Equal(70, layout.SpaceShipY);
			Assert.Equal(80, layout.OtherTermsY);
			Assert.Equal(90, layout.TotalY);
		}

		[Fact]
		public void MissingLinesAreSkipped()
		{
			CivilizationScoreLayout layout = _layout.Calculate(1, 1, false, false);

			Assert.Null(layout.SpaceShipY);
			Assert.Null(layout.OtherTermsY);
			Assert.Equal(70, layout.TotalY);
		}

		[Fact]
		public void CrowdedScreenShrinksLineHeightUntilTotalFits()
		{
			CivilizationScoreLayout layout = _layout.Calculate(400, 21, true, true);

			Assert.True(layout.TotalY < 186);
		}

		[Fact]
		public void HugeEmpireShrinksLineHeightToZeroSoTheTotalStillFits()
		{
			CivilizationScoreLayout layout = _layout.Calculate(1000, 21, true, true);

			Assert.True(layout.TotalY < 186);
			Assert.All(layout.CitizenIcons, icon => Assert.Equal(26, icon.Y));
		}
	}
}
