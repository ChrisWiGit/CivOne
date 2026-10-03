using System;
using System.IO;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.Mcp.Automation;
using Xunit;

namespace CivOne.UnitTests.Graphics.ImageFormats
{
	/// <summary>
	/// Verifies that <see cref="ImageDecoderService"/> reads images from every supported source and
	/// that <see cref="DecodedImageToBitmapDelegate"/> converts them into engine bitmaps.
	/// </summary>
	public sealed class ImageDecoderServiceTests : IDisposable
	{
		private readonly string _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"civone-png-{Guid.NewGuid():N}");

		private static byte[] CreatePngFile()
		{
			Colour[] palette = new Colour[256];
			palette[1] = new Colour(10, 20, 30);
			palette[2] = new Colour(40, 50, 60);

			byte[,] pixels = new byte[2, 2];
			pixels[0, 0] = 1; pixels[1, 0] = 2;
			pixels[0, 1] = 2; pixels[1, 1] = 1;

			using Bytemap bytemap = new(pixels);
			return PngWriter.Write(bytemap, palette);
		}

		[Fact]
		public void DecodeReadsFromMemory()
		{
			ImageDecoderService testee = new();

			DecodedImage result = testee.Decode(CreatePngFile());

			Assert.Equal(2, result.Width);
			Assert.Equal(2, result.Height);
		}

		[Fact]
		public void DecodeReadsFromStream()
		{
			ImageDecoderService testee = new();
			using MemoryStream stream = new(CreatePngFile());

			DecodedImage result = testee.Decode(stream);

			Assert.Equal([1, 2, 2, 1], result.Indices.ToArray());
		}

		[Fact]
		public void DecodeFileReadsFromDisk()
		{
			Directory.CreateDirectory(_temporaryDirectory);
			string path = Path.Combine(_temporaryDirectory, "image.png");
			File.WriteAllBytes(path, CreatePngFile());
			ImageDecoderService testee = new();

			DecodedImage result = testee.DecodeFile(path);

			Assert.Equal([1, 2, 2, 1], result.Indices.ToArray());
		}

		[Fact]
		public void DecodeRejectsUnsupportedFormat()
		{
			ImageDecoderService testee = new();

			Assert.Throws<NotSupportedException>(() => testee.Decode([71, 73, 70, 56, 57, 97, 0, 0]));
		}

		[Fact]
		public void DecodeBitmapProducesBitmapWithSourcePalette()
		{
			ImageDecoderService testee = new();
			using MemoryStream stream = new(CreatePngFile());

			using IBitmap result = testee.DecodeBitmap(stream);

			Assert.Equal(2, result.Bitmap.Width);
			Assert.Equal(2, result.Bitmap.Height);
			Assert.Equal(1, result.Bitmap[0, 0]);
			Assert.Equal(2, result.Bitmap[1, 0]);
			Assert.Equal(new Colour(40, 50, 60), result.Palette[2]);
		}

		[Fact]
		public void DecodeBitmapMapsOntoTargetPalette()
		{
			ImageDecoderService testee = new();
			using MemoryStream stream = new(CreatePngFile());
			Colour[] colours = new Colour[4];
			colours[0] = Colour.Transparent;
			colours[1] = new Colour(9, 21, 29);
			colours[2] = new Colour(200, 200, 200);
			colours[3] = new Colour(41, 49, 61);
			using Palette target = Palette.ToPalette(colours);

			using IBitmap result = testee.DecodeBitmap(stream, target);

			// The source colours (10,20,30) and (40,50,60) are closest to entries 1 and 3.
			Assert.Equal(1, result.Bitmap[0, 0]);
			Assert.Equal(3, result.Bitmap[1, 0]);
			Assert.Equal(new Colour(9, 21, 29), result.Palette[1]);
		}

		[Fact]
		public void ToBitmapMapsTrueColourImageOntoTargetPalette()
		{
			DecodedImageToBitmapDelegate testee = new();
			DecodedImage image = DecodedImage.TrueColour(2, 1, [new Colour(250, 0, 0), new Colour(0, 0, 250)]);
			Colour[] colours = [new Colour(255, 0, 0), new Colour(0, 0, 255)];
			using Palette target = Palette.ToPalette(colours);

			using IBitmap result = testee.ToBitmap(image, target);

			Assert.Equal(0, result.Bitmap[0, 0]);
			Assert.Equal(1, result.Bitmap[1, 0]);
		}

		[Fact]
		public void ToBitmapRejectsImageWithoutPalette()
		{
			DecodedImageToBitmapDelegate testee = new();
			DecodedImage image = DecodedImage.TrueColour(1, 1, [new Colour(1, 2, 3)]);

			Assert.Throws<NotSupportedException>(() => testee.ToBitmap(image));
		}

		[Fact]
		public void ToBitmapPadsShortPaletteToFullLength()
		{
			DecodedImageToBitmapDelegate testee = new();
			DecodedImage image = DecodedImage.Indexed(2, 1, [new Colour(1, 2, 3), new Colour(4, 5, 6)], [0, 1]);

			using IBitmap result = testee.ToBitmap(image);

			Assert.Equal(new Colour(4, 5, 6), result.Palette[1]);
			Assert.Equal(Colour.Black, result.Palette[255]);
		}

		public void Dispose()
		{
			if (Directory.Exists(_temporaryDirectory))
			{
				Directory.Delete(_temporaryDirectory, true);
			}
			GC.SuppressFinalize(this);
		}
	}
}
