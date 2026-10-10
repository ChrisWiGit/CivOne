using CivOne.Graphics;
using Xunit;

namespace CivOne.UnitTests.Graphics
{
	/// <summary>
	/// Verifies that <see cref="PaletteExpansionDelegate"/> expands short palettes to the requested length.
	/// </summary>
	public sealed class PaletteExpansionDelegateTests
	{
		[Fact]
		public void ExpandReturnsSamePaletteWhenLongEnough()
		{
			using Palette palette = new(256);
			PaletteExpansionDelegate testee = new();

			Palette result = testee.Expand(palette);

			Assert.Same(palette, result);
		}

		[Fact]
		public void ExpandRepeatsShortPalette()
		{
			using Palette palette = Palette.ToPalette([new Colour(1, 2, 3), new Colour(4, 5, 6)]);
			PaletteExpansionDelegate testee = new();

			using Palette result = testee.Expand(palette, 5);

			Assert.Equal(5, result.Length);
			Assert.Equal(new Colour(1, 2, 3), result[0]);
			Assert.Equal(new Colour(4, 5, 6), result[1]);
			Assert.Equal(new Colour(1, 2, 3), result[4]);
		}

		[Fact]
		public void ExpandFillsEmptyPaletteWithBlack()
		{
			using Palette palette = new(0);
			PaletteExpansionDelegate testee = new();

			using Palette result = testee.Expand(palette, 4);

			Assert.Equal(4, result.Length);
			Assert.Equal(Colour.Black, result[3]);
		}
	}
}
