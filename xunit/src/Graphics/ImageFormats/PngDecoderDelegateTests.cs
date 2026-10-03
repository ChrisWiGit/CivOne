using System;
using System.IO;
using System.IO.Compression;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.Mcp.Automation;
using Xunit;

namespace CivOne.UnitTests.Graphics.ImageFormats
{
	/// <summary>
	/// Verifies that <see cref="PngDecoderDelegate"/> reads PNG files correctly.
	/// <br/>
	/// <see cref="PngWriter"/> writes exactly the indexed PNG files the decoder has to read, so most
	/// of these tests are round trips that need no binary fixtures.
	/// </summary>
	public class PngDecoderDelegateTests
	{
		private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

		private static Colour[] CreatePalette()
		{
			Colour[] palette = new Colour[256];
			palette[0] = new Colour(0, 0, 0);
			palette[1] = new Colour(10, 20, 30);
			palette[2] = new Colour(40, 50, 60);
			palette[3] = new Colour(70, 80, 90);
			return palette;
		}

		private static byte[] WriteIndexedPng(byte[,] pixels, Colour[] palette)
		{
			using Bytemap bytemap = new(pixels);
			return PngWriter.Write(bytemap, palette);
		}

		[Fact]
		public void CanDecodeAcceptsPngSignature()
		{
			PngDecoderDelegate testee = new();

			Assert.True(testee.CanDecode(Signature));
			Assert.False(testee.CanDecode([71, 73, 70, 56, 57, 97, 0, 0]));
			Assert.False(testee.CanDecode([137, 80]));
		}

		[Fact]
		public void DecodeReadsIndexedImageWrittenByPngWriter()
		{
			// Indices are addressed as [x, y], so this is a 3x2 image.
			byte[,] pixels = new byte[3, 2];
			pixels[0, 0] = 1; pixels[1, 0] = 2; pixels[2, 0] = 3;
			pixels[0, 1] = 3; pixels[1, 1] = 2; pixels[2, 1] = 1;
			Colour[] palette = CreatePalette();

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(WriteIndexedPng(pixels, palette));

			Assert.True(result.IsIndexed);
			Assert.Equal(3, result.Width);
			Assert.Equal(2, result.Height);
			Assert.Equal([1, 2, 3, 3, 2, 1], result.Indices.ToArray());
			Assert.Equal(palette[2], result.SourcePalette[2]);
		}

		[Fact]
		public void DecodeBuildsPixelsFromPalette()
		{
			byte[,] pixels = new byte[2, 1];
			pixels[0, 0] = 1; pixels[1, 0] = 3;
			Colour[] palette = CreatePalette();

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(WriteIndexedPng(pixels, palette));

			Colour[] expected = [palette[1], palette[3]];
			Assert.Equal(expected, result.Pixels.ToArray());
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(3)]
		[InlineData(4)]
		public void DecodeReversesEveryScanlineFilter(byte filter)
		{
			// A gradient makes every filter produce different stored bytes, so a filter that was
			// reversed incorrectly cannot accidentally match the expected result.
			const int width = 8;
			const int height = 4;
			byte[] expected = new byte[width * height];
			for (int i = 0; i < expected.Length; i++)
			{
				expected[i] = (byte)(i * 7);
			}

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(BuildIndexedPng(width, height, 8, expected, filter));

			Assert.Equal(expected, result.Indices.ToArray());
		}

		[Theory]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(4)]
		public void DecodeUnpacksSubByteBitDepths(byte bitDepth)
		{
			const int width = 6;
			const int height = 2;
			byte maximum = (byte)((1 << bitDepth) - 1);
			byte[] expected = new byte[width * height];
			for (int i = 0; i < expected.Length; i++)
			{
				expected[i] = (byte)(i % (maximum + 1));
			}

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(BuildIndexedPng(width, height, bitDepth, expected, 0));

			Assert.Equal(expected, result.Indices.ToArray());
		}

		[Fact]
		public void DecodeReadsTransparencyChunk()
		{
			byte[] indices = [0, 1, 2, 3];
			byte[] transparency = [0, 128];

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(BuildIndexedPng(4, 1, 8, indices, 0, transparency));

			Assert.Equal(0, result.SourcePalette[0].A);
			Assert.Equal(128, result.SourcePalette[1].A);
			// Entries the chunk does not cover stay opaque.
			Assert.Equal(255, result.SourcePalette[2].A);
		}

		[Fact]
		public void DecodeRejectsDataWithoutPngSignature()
		{
			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode([71, 73, 70, 56, 57, 97, 0, 0]));
		}

		[Fact]
		public void DecodeRejectsBrokenChecksum()
		{
			byte[,] pixels = new byte[2, 1];
			byte[] file = WriteIndexedPng(pixels, CreatePalette());
			// Byte 29 is the first of the four IHDR checksum bytes (8 signature + 4 length + 4 type
			// + 13 content).
			file[29] ^= 0xFF;

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsTruncatedFile()
		{
			byte[,] pixels = new byte[2, 1];
			byte[] file = WriteIndexedPng(pixels, CreatePalette());

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file.AsSpan(0, file.Length - 10)));
		}

		[Fact]
		public void DecodeRejectsFileWithoutEndChunk()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, writeEnd: false);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsNonEmptyEndChunk()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, writeEnd: false);
			using MemoryStream stream = new();
			stream.Write(file);
			WriteChunk(stream, "IEND", [0]);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(stream.ToArray()));
		}

		[Fact]
		public void DecodeRejectsTrailingBytesAfterEndChunk()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0);
			byte[] padded = [.. file, 0, 0, 0, 0];

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(padded));
		}

		[Fact]
		public void DecodeRejectsImageDataLongerThanTheImage()
		{
			// The header announces a 2x1 image, the compressed data holds four scanlines.
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, rawPadding: 3 * 3);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsPixelOutsideThePalette()
		{
			// Index 5 cannot be resolved against a palette of three colours.
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 5], 0, paletteEntries: 3);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsPaletteLargerThanTheBitDepthAllows()
		{
			// Four bits address 16 colours, so a palette of 32 cannot belong to this image.
			byte[] file = BuildIndexedPng(2, 1, 4, [0, 1], 0, paletteEntries: 32);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsPaletteWithIncompleteColour()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, paletteTrim: 1);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsChunkLengthThatOverflows()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0);
			// A length of int.MaxValue would wrap a naive "offset + 12 + length" bounds check.
			WriteInt32(file, Signature.Length, int.MaxValue);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsUnsupportedCriticalChunk()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, extraChunk: "CrIT");

			PngDecoderDelegate testee = new();

			Assert.Throws<NotSupportedException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeSkipsAncillaryChunk()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, extraChunk: "gAMA");

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(file);

			Assert.Equal<byte[]>([0, 1], result.Indices.ToArray());
		}

		[Fact]
		public void DecodeRejectsFileWithoutLeadingHeaderChunk()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, writeHeader: false);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsHugeImageWithTinyImageData()
		{
			// The header announces 900 million pixels while the data holds 64 bytes. Decoding must
			// fail on the data, not try to allocate the announced size first.
			byte[] header = new byte[13];
			WriteInt32(header, 0, 30000);
			WriteInt32(header, 4, 30000);
			header[8] = 8;
			header[9] = 3;

			using MemoryStream compressed = new();
			using (ZLibStream zlib = new(compressed, CompressionLevel.Fastest, leaveOpen: true))
			{
				zlib.Write(new byte[64], 0, 64);
			}

			using MemoryStream file = new();
			file.Write(Signature);
			WriteChunk(file, "IHDR", header);
			WriteChunk(file, "PLTE", new byte[256 * 3]);
			WriteChunk(file, "IDAT", compressed.ToArray());
			WriteChunk(file, "IEND", []);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file.ToArray()));
		}

		[Fact]
		public void DecodeReadsFileWrittenWithShortPalette()
		{
			// PngWriter pads the palette, so even an index above the given colours stays readable.
			byte[,] pixels = new byte[2, 1];
			pixels[0, 0] = 1; pixels[1, 0] = 9;
			Colour[] palette = [new Colour(0, 0, 0), new Colour(10, 20, 30)];

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(WriteIndexedPng(pixels, palette));

			Assert.Equal<byte[]>([1, 9], result.Indices.ToArray());
			Assert.Equal(palette[1], result.SourcePalette[1]);
		}

		[Fact]
		public void DecodeReadsRgbaImage()
		{
			// Two pixels, the second one half transparent, so a swapped channel or alpha position
			// cannot pass unnoticed.
			byte[] samples = [10, 20, 30, 255, 40, 50, 60, 128];

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(BuildTrueColourPng(2, 1, 6, samples));

			Assert.False(result.IsIndexed);
			Assert.Equal(new Colour(255, 10, 20, 30), result.Pixels[0]);
			Assert.Equal(new Colour(128, 40, 50, 60), result.Pixels[1]);
			// Colour.Equals compares the colour channels only, so alpha needs its own assertion.
			Assert.Equal(255, result.Pixels[0].A);
			Assert.Equal(128, result.Pixels[1].A);
		}

		[Fact]
		public void DecodeReadsRgbImage()
		{
			byte[] samples = [10, 20, 30, 40, 50, 60];

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(BuildTrueColourPng(2, 1, 2, samples));

			Assert.Equal(new Colour(255, 10, 20, 30), result.Pixels[0]);
			Assert.Equal(new Colour(255, 40, 50, 60), result.Pixels[1]);
			Assert.Equal(255, result.Pixels[1].A);
		}

		[Fact]
		public void DecodeReadsGreyscaleImageWithAlpha()
		{
			// Colour type 4 stores the grey sample first and its alpha second.
			byte[] samples = [70, 255, 90, 64];

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(BuildTrueColourPng(2, 1, 4, samples));

			Assert.Equal(new Colour(255, 70, 70, 70), result.Pixels[0]);
			Assert.Equal(new Colour(64, 90, 90, 90), result.Pixels[1]);
			Assert.Equal(255, result.Pixels[0].A);
			Assert.Equal(64, result.Pixels[1].A);
		}

		[Fact]
		public void DecodeReadsGreyscaleImage()
		{
			byte[] samples = [70, 90];

			PngDecoderDelegate testee = new();
			DecodedImage result = testee.Decode(BuildTrueColourPng(2, 1, 0, samples));

			Assert.Equal(new Colour(255, 70, 70, 70), result.Pixels[0]);
			Assert.Equal(new Colour(255, 90, 90, 90), result.Pixels[1]);
			Assert.Equal(255, result.Pixels[1].A);
		}

		[Fact]
		public void DecodeRejectsTransparencyChunkOutsideIndexedFile()
		{
			byte[] file = BuildTrueColourPng(2, 1, 2, [10, 20, 30, 40, 50, 60], transparency: [0, 10]);

			PngDecoderDelegate testee = new();

			Assert.Throws<NotSupportedException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsTransparencyChunkBeforePalette()
		{
			byte[] file = BuildIndexedPng(4, 1, 8, [0, 1, 2, 3], 0, [0, 128], transparencyBeforePalette: true);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsEmptyTransparencyChunk()
		{
			byte[] file = BuildIndexedPng(4, 1, 8, [0, 1, 2, 3], 0, []);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsTransparencyChunkLongerThanThePalette()
		{
			// Four alpha values cannot belong to a palette of three colours.
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, [0, 1, 2, 3], paletteEntries: 3);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsDuplicatePaletteChunk()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, duplicatePalette: true);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsPaletteAfterImageData()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, paletteAfterData: true);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsNonConsecutiveImageData()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, splitImageData: true);

			PngDecoderDelegate testee = new();

			Assert.Throws<InvalidDataException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsInterlacedFile()
		{
			byte[] file = BuildIndexedPng(2, 1, 8, [0, 1], 0, null, interlace: 1);

			PngDecoderDelegate testee = new();

			Assert.Throws<NotSupportedException>(() => testee.Decode(file));
		}

		[Fact]
		public void DecodeRejectsSixteenBitChannels()
		{
			byte[] file = BuildIndexedPng(2, 1, 16, [0, 1], 0);

			PngDecoderDelegate testee = new();

			Assert.Throws<NotSupportedException>(() => testee.Decode(file));
		}

		/// <summary>
		/// Builds an indexed PNG file with a chosen bit depth, scanline filter and interlace flag,
		/// which <see cref="PngWriter"/> cannot produce because it always writes 8 bit, filter 0.
		/// </summary>
		private static byte[] BuildIndexedPng(int width, int height, byte bitDepth, byte[] indices, byte filter, byte[]? transparency = null, byte interlace = 0, bool writeEnd = true, bool writeHeader = true, string? extraChunk = null, int rawPadding = 0, int paletteEntries = 0, int paletteTrim = 0, bool duplicatePalette = false, bool paletteAfterData = false, bool splitImageData = false, bool transparencyBeforePalette = false)
		{
			byte[] header = new byte[13];
			WriteInt32(header, 0, width);
			WriteInt32(header, 4, height);
			header[8] = bitDepth;
			header[9] = 3;
			header[12] = interlace;

			// A palette may hold at most 2^bitDepth colours, so the default follows the bit depth.
			if (paletteEntries == 0)
			{
				paletteEntries = bitDepth >= 8 ? 256 : 1 << bitDepth;
			}

			byte[] palette = new byte[(paletteEntries * 3) - paletteTrim];
			for (int i = 0; i < paletteEntries; i++)
			{
				palette[i * 3] = (byte)i;
			}

			int stride = ((width * bitDepth) + 7) / 8;
			byte[] raw = new byte[(height * (stride + 1)) + rawPadding];
			for (int y = 0; y < height; y++)
			{
				int row = y * (stride + 1);
				raw[row] = filter;
				for (int x = 0; x < width; x++)
				{
					byte value = indices[(y * width) + x];
					if (bitDepth == 8)
					{
						raw[row + 1 + x] = value;
						continue;
					}
					if (bitDepth == 16)
					{
						continue;
					}
					int perByte = 8 / bitDepth;
					int shift = 8 - (bitDepth * ((x % perByte) + 1));
					raw[row + 1 + (x / perByte)] |= (byte)(value << shift);
				}
			}

			ApplyFilter(raw, height, stride, filter);

			using MemoryStream compressed = new();
			using (ZLibStream zlib = new(compressed, CompressionLevel.Fastest, leaveOpen: true))
			{
				zlib.Write(raw, 0, raw.Length);
			}

			using MemoryStream file = new();
			file.Write(Signature);
			if (writeHeader)
			{
				WriteChunk(file, "IHDR", header);
			}
			if (extraChunk != null)
			{
				WriteChunk(file, extraChunk, [1, 2, 3, 4]);
			}
			if (transparency != null && transparencyBeforePalette)
			{
				WriteChunk(file, "tRNS", transparency);
			}
			if (!paletteAfterData)
			{
				WriteChunk(file, "PLTE", palette);
			}
			if (duplicatePalette)
			{
				WriteChunk(file, "PLTE", palette);
			}
			if (transparency != null && !transparencyBeforePalette)
			{
				WriteChunk(file, "tRNS", transparency);
			}
			byte[] imageData = compressed.ToArray();
			if (splitImageData)
			{
				// An ancillary chunk between two IDAT chunks breaks the single compressed stream.
				WriteChunk(file, "IDAT", imageData[..1]);
				WriteChunk(file, "gAMA", [0, 0, 0, 1]);
				WriteChunk(file, "IDAT", imageData[1..]);
			}
			else
			{
				WriteChunk(file, "IDAT", imageData);
			}
			if (paletteAfterData)
			{
				WriteChunk(file, "PLTE", palette);
			}
			if (writeEnd)
			{
				WriteChunk(file, "IEND", []);
			}
			return file.ToArray();
		}

		/// <summary>
		/// Builds a true-colour or greyscale PNG file with filter 0 from raw samples, which
		/// <see cref="PngWriter"/> cannot produce because it only writes indexed files.
		/// </summary>
		private static byte[] BuildTrueColourPng(int width, int height, byte colourType, byte[] samples, byte[]? transparency = null)
		{
			byte[] header = new byte[13];
			WriteInt32(header, 0, width);
			WriteInt32(header, 4, height);
			header[8] = 8;
			header[9] = colourType;

			int channels = colourType switch { 0 => 1, 2 => 3, 4 => 2, _ => 4 };
			int stride = width * channels;
			byte[] raw = new byte[height * (stride + 1)];
			for (int y = 0; y < height; y++)
			{
				Array.Copy(samples, y * stride, raw, (y * (stride + 1)) + 1, stride);
			}

			using MemoryStream compressed = new();
			using (ZLibStream zlib = new(compressed, CompressionLevel.Fastest, leaveOpen: true))
			{
				zlib.Write(raw, 0, raw.Length);
			}

			using MemoryStream file = new();
			file.Write(Signature);
			WriteChunk(file, "IHDR", header);
			if (transparency != null)
			{
				WriteChunk(file, "tRNS", transparency);
			}
			WriteChunk(file, "IDAT", compressed.ToArray());
			WriteChunk(file, "IEND", []);
			return file.ToArray();
		}

		/// <summary>
		/// Encodes already written scanlines with the given filter, in place.
		/// </summary>
		private static void ApplyFilter(byte[] raw, int height, int stride, byte filter)
		{
			if (filter == 0)
			{
				return;
			}

			// Encoding runs bottom to top so that every row still sees the unfiltered row above it.
			for (int y = height - 1; y >= 0; y--)
			{
				int row = (y * (stride + 1)) + 1;
				int previous = row - (stride + 1);
				for (int i = stride - 1; i >= 0; i--)
				{
					byte left = i >= 1 ? raw[row + i - 1] : (byte)0;
					byte above = y > 0 ? raw[previous + i] : (byte)0;
					byte aboveLeft = y > 0 && i >= 1 ? raw[previous + i - 1] : (byte)0;

					raw[row + i] = filter switch
					{
						1 => (byte)(raw[row + i] - left),
						2 => (byte)(raw[row + i] - above),
						3 => (byte)(raw[row + i] - ((left + above) >> 1)),
						_ => (byte)(raw[row + i] - Paeth(left, above, aboveLeft))
					};
				}
			}
		}

		private static byte Paeth(byte left, byte above, byte aboveLeft)
		{
			int estimate = left + above - aboveLeft;
			int distanceLeft = Math.Abs(estimate - left);
			int distanceAbove = Math.Abs(estimate - above);
			int distanceAboveLeft = Math.Abs(estimate - aboveLeft);

			if (distanceLeft <= distanceAbove && distanceLeft <= distanceAboveLeft)
			{
				return left;
			}
			return distanceAbove <= distanceAboveLeft ? above : aboveLeft;
		}

		private static void WriteChunk(Stream stream, string type, byte[] data)
		{
			byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
			byte[] length = new byte[4];
			WriteInt32(length, 0, data.Length);
			stream.Write(length);
			stream.Write(typeBytes);
			stream.Write(data);

			Crc32Delegate crc = new();
			byte[] checksum = new byte[4];
			WriteInt32(checksum, 0, (int)crc.Finish(crc.Update(crc.Update(0xFFFFFFFF, typeBytes), data)));
			stream.Write(checksum);
		}

		private static void WriteInt32(byte[] buffer, int offset, int value)
		{
			buffer[offset] = (byte)(value >> 24);
			buffer[offset + 1] = (byte)(value >> 16);
			buffer[offset + 2] = (byte)(value >> 8);
			buffer[offset + 3] = (byte)value;
		}
	}
}
