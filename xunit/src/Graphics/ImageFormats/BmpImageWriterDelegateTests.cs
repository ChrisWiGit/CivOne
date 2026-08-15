using System;
using System.IO;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using Xunit;

namespace CivOne.UnitTests.Graphics.ImageFormats
{
	/// <summary>
	/// Verifies that <see cref="BmpImageWriterDelegate"/> produces a valid 8-bit indexed bitmap.
	/// </summary>
	public class BmpImageWriterDelegateTests
	{
		private const int FileHeaderSize = 14;
		private const int InfoHeaderSize = 40;
		private const int PaletteSize = 256 * 4;
		private const int PixelDataOffset = FileHeaderSize + InfoHeaderSize + PaletteSize;

		private static Picture CreateTestPicture()
		{
			Colour[] colours = new Colour[256];
			colours[0] = new Colour(0, 0, 0);
			colours[1] = new Colour(10, 20, 30);
			colours[2] = new Colour(40, 50, 60);
			colours[3] = new Colour(70, 80, 90);

			// Indices are addressed as [x, y], so this describes a 3x2 image
			// with the top row 1,2,3 and the bottom row 3,2,1.
			byte[,] pixels = new byte[3, 2];
			pixels[0, 0] = 1; pixels[1, 0] = 2; pixels[2, 0] = 3;
			pixels[0, 1] = 3; pixels[1, 1] = 2; pixels[2, 1] = 1;

			return new Picture(pixels, colours);
		}

		private static byte[] WriteToArray(IBitmap bitmap)
		{
			BmpImageWriterDelegate testee = new();
			using MemoryStream stream = new();
			testee.Write(bitmap, stream);
			return stream.ToArray();
		}

		[Fact]
		public void WriteProducesExpectedFileHeader()
		{
			using Picture picture = CreateTestPicture();

			byte[] result = WriteToArray(picture);

			// A three pixel wide row is padded to four bytes, so two rows produce eight bytes.
			const int expectedImageSize = 4 * 2;
			Assert.Equal((byte)'B', result[0]);
			Assert.Equal((byte)'M', result[1]);
			Assert.Equal(PixelDataOffset + expectedImageSize, BitConverter.ToInt32(result, 2));
			Assert.Equal(PixelDataOffset, BitConverter.ToInt32(result, 10));
			Assert.Equal(PixelDataOffset + expectedImageSize, result.Length);
		}

		[Fact]
		public void WriteProducesExpectedInfoHeader()
		{
			using Picture picture = CreateTestPicture();

			byte[] result = WriteToArray(picture);

			Assert.Equal(InfoHeaderSize, BitConverter.ToInt32(result, 14));
			Assert.Equal(3, BitConverter.ToInt32(result, 18));
			Assert.Equal(2, BitConverter.ToInt32(result, 22));
			Assert.Equal(1, BitConverter.ToInt16(result, 26));
			Assert.Equal(8, BitConverter.ToInt16(result, 28));
			Assert.Equal(0, BitConverter.ToInt32(result, 30));
			Assert.Equal(256, BitConverter.ToInt32(result, 46));
		}

		[Fact]
		public void WriteWritesPaletteAsBlueGreenRed()
		{
			using Picture picture = CreateTestPicture();

			byte[] result = WriteToArray(picture);

			int firstEntry = FileHeaderSize + InfoHeaderSize + 4;
			Assert.Equal(30, result[firstEntry]);
			Assert.Equal(20, result[firstEntry + 1]);
			Assert.Equal(10, result[firstEntry + 2]);
			Assert.Equal(0, result[firstEntry + 3]);
		}

		[Fact]
		public void WriteWritesRowsBottomUpWithPadding()
		{
			using Picture picture = CreateTestPicture();

			byte[] result = WriteToArray(picture);

			// The bottom image row is stored first.
			Assert.Equal(new byte[] { 3, 2, 1, 0 }, result[PixelDataOffset..(PixelDataOffset + 4)]);
			Assert.Equal(new byte[] { 1, 2, 3, 0 }, result[(PixelDataOffset + 4)..(PixelDataOffset + 8)]);
		}

		[Fact]
		public void WriteToFileCreatesReadableFile()
		{
			using Picture picture = CreateTestPicture();
			string filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bmp");

			try
			{
				new BmpImageWriterDelegate().Write(picture, filePath);

				byte[] fileContent = File.ReadAllBytes(filePath);
				Assert.Equal(WriteToArray(picture), fileContent);
			}
			finally
			{
				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}
			}
		}

		[Fact]
		public void WriteNullBitmapThrows()
		{
			BmpImageWriterDelegate testee = new();
			using MemoryStream stream = new();

			Assert.Throws<ArgumentNullException>(() => testee.Write(null!, stream));
		}
	}
}
