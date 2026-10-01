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