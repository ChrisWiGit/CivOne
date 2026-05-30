using System.Drawing;
using CivOne.Screens;
using Xunit;

namespace CivOne.UnitTests
{
	public class CustomizeWorldMapSizeParserTests
	{
		[Theory]
		[InlineData("40x25", 40, 25)]
		[InlineData(" 120x75 ", 120, 75)]
		[InlineData("20 x 20", 20, 20)]
		[InlineData("1000X1000", 1000, 1000)]
		public void TryParseMapSizeWhenInputIsValidReturnsExpectedSize(string input, int expectedWidth, int expectedHeight)
		{
			bool parsed = CustomizeWorld.TryParseMapSize(input, out Size actual);

			Assert.True(parsed);
			Assert.Equal(expectedWidth, actual.Width);
			Assert.Equal(expectedHeight, actual.Height);
		}

		[Theory]
		[InlineData("")]
		[InlineData("19x20")]
		[InlineData("20x19")]
		[InlineData("1001x1000")]
		[InlineData("1000x1001")]
		[InlineData("40*25")]
		[InlineData("abc")]
		[InlineData("40x")]
		public void TryParseMapSizeWhenInputIsInvalidReturnsFalse(string input)
		{
			bool parsed = CustomizeWorld.TryParseMapSize(input, out Size actual);

			Assert.False(parsed);
			Assert.Equal(Size.Empty, actual);
		}
	}
}
