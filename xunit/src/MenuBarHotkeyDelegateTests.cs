// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Screens.GamePlayPanels;
using Xunit;

namespace CivOne.UnitTests
{
	public class MenuBarHotkeyDelegateTests
	{
		private readonly MenuBarHotkeyDelegate _testee = new();

		[Fact]
		public void ParseUsesMarkedCharacterAsHotkeyAndRemovesMarker()
		{
			// Arrange
			const string translationKey = "GAME";
			const string translatedText = "~SPIEL";

			// Act
			MenuBarTitle actual = _testee.Parse(translationKey, translatedText);

			// Assert
			Assert.Equal("SPIEL", actual.VisibleText);
			Assert.Equal(0, actual.HighlightedCharacterIndex);
			Assert.Equal('S', actual.Hotkey);
		}

		[Fact]
		public void ParseUsesFirstValidMarkerAndRemovesAllMarkersFromVisibleText()
		{
			// Arrange
			const string translationKey = "ADVISORS";
			const string translatedText = "BE~RA~TER";

			// Act
			MenuBarTitle actual = _testee.Parse(translationKey, translatedText);

			// Assert
			Assert.Equal("BERATER", actual.VisibleText);
			Assert.Equal(2, actual.HighlightedCharacterIndex);
			Assert.Equal('R', actual.Hotkey);
		}

		[Fact]
		public void ParseFallsBackToFirstVisibleCharacterWhenMarkerIsTrailing()
		{
			// Arrange
			const string translationKey = "WORLD";
			const string translatedText = "WELT~";

			// Act
			MenuBarTitle actual = _testee.Parse(translationKey, translatedText);

			// Assert
			Assert.Equal("WELT", actual.VisibleText);
			Assert.Equal(0, actual.HighlightedCharacterIndex);
			Assert.Equal('W', actual.Hotkey);
		}
	}
}