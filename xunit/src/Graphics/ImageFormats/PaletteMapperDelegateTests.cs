using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using Xunit;

namespace CivOne.UnitTests.Graphics.ImageFormats
{
	/// <summary>
	/// Verifies that <see cref="PaletteMapperDelegate"/> maps colours onto the closest palette entry.
	/// </summary>
	public class PaletteMapperDelegateTests
	{
		private static Palette CreatePalette()
		{
			Colour[] colours = new Colour[4];
			colours[0] = Colour.Transparent;
			colours[1] = new Colour(0, 0, 0);
			colours[2] = new Colour(255, 0, 0);
			colours[3] = new Colour(0, 0, 255);
			return Palette.ToPalette(colours);
		}

		[Fact]
		public void MapColourFindsExactMatch()
		{
			PaletteMapperDelegate testee = new();
			using Palette palette = CreatePalette();

			Assert.Equal(2, testee.MapColour(new Colour(255, 0, 0), palette));
			Assert.Equal(3, testee.MapColour(new Colour(0, 0, 255), palette));
		}

		[Fact]
		public void MapColourFindsNearestMatch()
		{
			PaletteMapperDelegate testee = new();
			using Palette palette = CreatePalette();

			// Closer to red than to black or blue.
			Assert.Equal(2, testee.MapColour(new Colour(200, 30, 20), palette));
			// Closer to black than to any of the saturated entries.
			Assert.Equal(1, testee.MapColour(new Colour(20, 20, 20), palette));
		}

		[Fact]
		public void MapColourUsesTransparentEntryForTransparentColour()
		{
			PaletteMapperDelegate testee = new();
			using Palette palette = CreatePalette();

			Assert.Equal(0, testee.MapColour(new Colour(0, 255, 0, 0), palette));
		}

		[Fact]
		public void MapColourNeverPicksTransparentEntryByDistance()
		{
			PaletteMapperDelegate testee = new();
			Colour[] colours = new Colour[2];
			// The transparent entry carries the exact colour that is searched for, so a mapper that
			// ignored its alpha would return index 0 and make an opaque pixel disappear.
			colours[0] = new Colour(0, 10, 20, 30);
			colours[1] = new Colour(200, 200, 200);
			using Palette palette = Palette.ToPalette(colours);

			Assert.Equal(1, testee.MapColour(new Colour(10, 20, 30), palette));
		}

		[Fact]
		public void MapPixelsMapsEveryPixel()
		{
			PaletteMapperDelegate testee = new();
			using Palette palette = CreatePalette();
			Colour[] pixels = [new Colour(250, 5, 5), new Colour(5, 5, 250), new Colour(0, 0, 0, 0)];

			byte[] result = testee.MapPixels(pixels, palette);

			Assert.Equal([2, 3, 0], result);
		}

		[Fact]
		public void MapPaletteBuildsLookupTable()
		{
			PaletteMapperDelegate testee = new();
			using Palette palette = CreatePalette();
			Colour[] source = [new Colour(0, 0, 250), new Colour(250, 0, 0)];

			byte[] result = testee.MapPalette(source, palette);

			Assert.Equal([3, 2], result);
		}
	}
}
