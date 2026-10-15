using System.Linq;
using CivOne.Services.EndGame;
using Xunit;

namespace CivOne.UnitTests.Services.EndGame
{
	public class SpaceVictoryTextDelegateTests
	{
		[Fact]
		public void PlaceholdersAreFilledAndTheTextIsSplitAfterFifteenLines()
		{
			string[] lines =
			[
				"^", "In the year $RPLC1^", "a", "b", "c", "A spaceship populated by^", "$BUCKS0,000 $THEM colonists^",
				"d", "Arriving in $RPLC2^", "e", "f", "g", "h", "i", "j", "$US, your people^", "k",
			];

			SpaceVictoryText text = new SpaceVictoryTextDelegate().Build(lines, "King Hammurabi", "Roman", 32_000, 1802, 1815);

			Assert.Equal(SpaceVictoryTextDelegate.FlightLineCount, text.FlightLines.Count);
			Assert.Equal("", text.FlightLines[0]);
			Assert.Equal("In the year 1802", text.FlightLines[1]);
			Assert.Equal("30,000 Roman colonists", text.FlightLines[6]);
			Assert.Equal("Arriving in 1815", text.FlightLines[8]);
			string[] expectedLanding = ["King Hammurabi, your people", "k"];
			Assert.Equal(expectedLanding, text.LandingLines.ToArray());
		}
	}
}
