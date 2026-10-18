using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace CivOne.Graphics.ImageFormats
{
	/// <summary>
	/// Reads PNG files into a <see cref="DecodedImage"/>.
	/// <br/>
	/// Only the parts of the format the project actually needs are implemented. An indexed PNG
	/// (colour type 3) is the format that matches the engine's palette-based bitmaps exactly and
	/// decodes without any loss; greyscale and true-colour files decode to plain colours and still
	/// need a palette decision before they can become an <see cref="IBitmap"/>.
	/// <br/>
	/// Unsupported but valid files are rejected with <see cref="NotSupportedException"/>, broken files
	/// with <see cref="InvalidDataException"/>, so a wrong result is never returned silently.
	/// </summary>
	internal sealed class PngDecoderDelegate
	{
		private const int HeaderChunkLength = 13;

		private static ReadOnlySpan<byte> Signature => [137, 80, 78, 71, 13, 10, 26, 10];

		private readonly Crc32Delegate _crc = new();

		/// <summary>
		/// Checks whether the data starts with the PNG signature.
		/// </summary>
		/// <param name="data">The file contents, or at least its first eight bytes.</param>
		/// <returns><see langword="true"/> when this decoder can read the data.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public bool CanDecode(ReadOnlySpan<byte> data) => data.Length >= Signature.Length && data[..Signature.Length].SequenceEqual(Signature);

		/// <summary>
		/// Decodes a complete PNG file.
		/// </summary>
		/// <param name="data">The file contents.</param>
		/// <returns>The decoded image.</returns>
		/// <exception cref="InvalidDataException">The data is not a valid PNG file.</exception>
		/// <exception cref="NotSupportedException">The file uses a PNG feature this decoder does not implement.</exception>
		public DecodedImage Decode(ReadOnlySpan<byte> data)
		{
			if (!CanDecode(data))
			{
				throw new InvalidDataException("The data does not start with a PNG signature.");
			}

			PngHeader header = default;
			bool headerRead = false;
			bool endRead = false;
			bool paletteRead = false;
			bool alphaRead = false;
			bool dataStarted = false;
			bool dataEnded = false;
			byte[]? palette = null;
			byte[]? alpha = null;
			using MemoryStream imageData = new();

			int offset = Signature.Length;
			while (offset < data.Length)
			{
				if (offset + 12 > data.Length)
				{
					throw new InvalidDataException("The file ends in the middle of a chunk.");
				}

				int length = ReadInt32(data, offset);
				// Compared against the remaining bytes, because offset + 12 + length would overflow
				// for a declared length close to int.MaxValue.
				if (length < 0 || length > data.Length - offset - 12)
				{
					throw new InvalidDataException("A chunk declares a length that does not fit into the file.");
				}

				ReadOnlySpan<byte> type = data.Slice(offset + 4, 4);
				ReadOnlySpan<byte> content = data.Slice(offset + 8, length);
				VerifyCrc(data.Slice(offset + 4, 4 + length), (uint)ReadInt32(data, offset + 8 + length));

				if (type.SequenceEqual("IHDR"u8))
				{
					if (headerRead)
					{
						throw new InvalidDataException("The file contains more than one IHDR chunk.");
					}
					header = ReadHeader(content);
					headerRead = true;
				}
				else if (!headerRead)
				{
					// Every other chunk is read against the header, so IHDR has to come first.
					throw new InvalidDataException("The file does not start with an IHDR chunk.");
				}
				else if (type.SequenceEqual("PLTE"u8))
				{
					// A file carries at most one palette, and it has to be known before the image data
					// that refers to it.
					if (paletteRead || dataStarted)
					{
						throw new InvalidDataException("The file contains a duplicate or late PLTE chunk.");
					}
					VerifyPalette(content, header);
					palette = content.ToArray();
					paletteRead = true;
				}
				else if (type.SequenceEqual("tRNS"u8))
				{
					// For colour types 0 and 2 the chunk names a single transparent sample value
					// instead of per-entry alpha, which this decoder does not evaluate.
					if (header.ColourType != 3)
					{
						throw new NotSupportedException("Transparency is only supported for indexed PNG files.");
					}
					// For an indexed image the chunk holds one alpha value per palette entry, so it
					// belongs between PLTE and the image data.
					if (alphaRead || dataStarted || palette == null)
					{
						throw new InvalidDataException("The file contains a duplicate tRNS chunk, or a tRNS chunk outside of PLTE and IDAT.");
					}
					if (content.Length == 0 || content.Length > palette.Length / 3)
					{
						throw new InvalidDataException("The tRNS chunk contains more alpha values than the palette has entries.");
					}
					alpha = content.ToArray();
					alphaRead = true;
				}
				else if (type.SequenceEqual("IDAT"u8))
				{
					// The IDAT chunks hold one single compressed stream, so they have to follow each
					// other without another chunk in between.
					if (dataEnded)
					{
						throw new InvalidDataException("The IDAT chunks of the file are not consecutive.");
					}
					imageData.Write(content);
					dataStarted = true;
				}
				else if (type.SequenceEqual("IEND"u8))
				{
					// IEND is empty by definition and closes the file, so nothing may follow it.
					if (length != 0 || offset + 12 != data.Length)
					{
						throw new InvalidDataException("The IEND chunk is not an empty chunk at the end of the file.");
					}
					endRead = true;
					break;
				}
				else if ((type[0] & 0x20) == 0)
				{
					// A lower case first letter marks an ancillary chunk, which may be skipped. Any
					// other critical chunk changes how the image has to be read, so it cannot be.
					throw new NotSupportedException("The file contains an unsupported critical chunk.");
				}

				dataEnded |= dataStarted && !type.SequenceEqual("IDAT"u8);
				offset += 12 + length;
			}

			if (!headerRead)
			{
				throw new InvalidDataException("The file contains no IHDR chunk.");
			}
			if (!endRead)
			{
				throw new InvalidDataException("The file contains no IEND chunk, it is truncated.");
			}

			int stride = ScanlineStride(header);
			byte[] scanlines = Unfilter(Inflate(imageData.ToArray(), (stride + 1) * header.Height), header);
			return header.ColourType == 3
				? BuildIndexedImage(header, scanlines, palette, alpha)
				: BuildTrueColourImage(header, scanlines);
		}

		/// <summary>
		/// Checks that a PLTE chunk is allowed and well formed for the image it belongs to.
		/// </summary>
		/// <param name="content">The chunk contents.</param>
		/// <param name="header">The image header.</param>
		/// <exception cref="InvalidDataException">The chunk does not belong to this image, or is broken.</exception>
		private static void VerifyPalette(ReadOnlySpan<byte> content, PngHeader header)
		{
			// A greyscale image has no colours to look up, so it must not carry a palette at all.
			if (header.ColourType is 0 or 4)
			{
				throw new InvalidDataException("A greyscale PNG file must not contain a PLTE chunk.");
			}

			// An index is read with the bit depth of the image, so a 1, 2 or 4 bit indexed file cannot
			// address more than 2, 4 or 16 colours. For a true-colour file the chunk is only a
			// suggested palette and the format limits it to 256 entries.
			int maximumEntries = header.ColourType == 3 ? 1 << header.BitDepth : 256;
			if (content.Length == 0 || content.Length % 3 != 0 || content.Length > maximumEntries * 3)
			{
				throw new InvalidDataException("The PLTE chunk contains no colour triples, incomplete ones, or more than the image can address.");
			}
		}

		private void VerifyCrc(ReadOnlySpan<byte> typeAndContent, uint expected)
		{
			if (_crc.Compute(typeAndContent) != expected)
			{
				throw new InvalidDataException("A chunk checksum does not match, the file is corrupt.");
			}
		}

		private static PngHeader ReadHeader(ReadOnlySpan<byte> content)
		{
			if (content.Length != HeaderChunkLength)
			{
				throw new InvalidDataException("The IHDR chunk has an unexpected size.");
			}

			PngHeader header = new()
			{
				Width = ReadInt32(content, 0),
				Height = ReadInt32(content, 4),
				BitDepth = content[8],
				ColourType = content[9]
			};

			if (header.Width <= 0 || header.Height <= 0)
			{
				throw new InvalidDataException("The image has no pixels.");
			}
			if (content[10] != 0 || content[11] != 0)
			{
				throw new InvalidDataException("The file uses an unknown compression or filter method.");
			}
			if (content[12] != 0)
			{
				throw new NotSupportedException("Interlaced PNG files are not supported.");
			}
			if (header.BitDepth == 16)
			{
				throw new NotSupportedException("PNG files with 16 bits per channel are not supported.");
			}

			header.Channels = header.ColourType switch
			{
				0 => 1,
				2 => 3,
				3 => 1,
				4 => 2,
				6 => 4,
				_ => throw new InvalidDataException("The file uses an unknown colour type.")
			};

			bool validDepth = header.ColourType == 3
				? header.BitDepth is 1 or 2 or 4 or 8
				: header.BitDepth == 8;
			if (!validDepth)
			{
				throw new NotSupportedException($"A bit depth of {header.BitDepth} is not supported for colour type {header.ColourType}.");
			}

			return header;
		}

		/// <summary>
		/// Decompresses the concatenated IDAT contents.
		/// <br/>
		/// The output is limited to the size the header announces, so a file that claims to be small
		/// but expands to gigabytes cannot exhaust memory.
		/// </summary>
		/// <param name="compressed">The raw zlib stream from the IDAT chunks.</param>
		/// <param name="expectedLength">The number of bytes the image size requires.</param>
		/// <returns>The decompressed scanlines, including their filter bytes.</returns>
		private static byte[] Inflate(byte[] compressed, int expectedLength)
		{
			if (compressed.Length == 0)
			{
				throw new InvalidDataException("The file contains no image data.");
			}

			using MemoryStream input = new(compressed);
			using ZLibStream zlib = new(input, CompressionMode.Decompress);

			// The buffer grows with the data that actually arrives instead of being allocated from the
			// announced image size, so a small file that declares huge dimensions cannot force a
			// gigabyte allocation before the first byte is read.
			using MemoryStream output = new();
			byte[] buffer = new byte[16 * 1024];
			long total = 0;
			int read;
			while ((read = zlib.Read(buffer, 0, buffer.Length)) > 0)
			{
				total += read;
				if (total > expectedLength)
				{
					throw new InvalidDataException("The image data is longer than the image size allows.");
				}
				output.Write(buffer, 0, read);
			}
			if (total < expectedLength)
			{
				throw new InvalidDataException("The image data is shorter than the image size requires.");
			}

			return output.ToArray();
		}

		/// <summary>
		/// Returns the number of bytes one filtered scanline occupies, without its filter byte.
		/// </summary>
		/// <param name="header">The image header.</param>
		/// <returns>The scanline length in bytes.</returns>
		/// <exception cref="InvalidDataException">The announced image size does not fit into memory.</exception>
		private static int ScanlineStride(PngHeader header)
		{
			long stride = (((long)header.Width * header.Channels * header.BitDepth) + 7) / 8;
			if ((stride + 1) * header.Height > int.MaxValue || (long)header.Width * header.Height > int.MaxValue)
			{
				throw new InvalidDataException("The image is too large to decode.");
			}
			return (int)stride;
		}

		/// <summary>
		/// Reverses the per-scanline filters and drops the filter byte in front of every row.
		/// </summary>
		private static byte[] Unfilter(byte[] raw, PngHeader header)
		{
			int stride = ScanlineStride(header);
			int step = Math.Max(1, header.Channels * header.BitDepth / 8);
			if (raw.Length < header.Height * (stride + 1))
			{
				throw new InvalidDataException("The image data is shorter than the image size requires.");
			}

			byte[] output = new byte[header.Height * stride];
			int position = 0;
			for (int y = 0; y < header.Height; y++)
			{
				byte filter = raw[position++];
				int row = y * stride;
				int previous = row - stride;

				for (int i = 0; i < stride; i++)
				{
					byte value = raw[position + i];
					byte left = i >= step ? output[row + i - step] : (byte)0;
					byte above = y > 0 ? output[previous + i] : (byte)0;
					byte aboveLeft = y > 0 && i >= step ? output[previous + i - step] : (byte)0;

					output[row + i] = filter switch
					{
						0 => value,
						1 => (byte)(value + left),
						2 => (byte)(value + above),
						3 => (byte)(value + ((left + above) >> 1)),
						4 => (byte)(value + Paeth(left, above, aboveLeft)),
						_ => throw new InvalidDataException($"The image data uses an unknown scanline filter ({filter}).")
					};
				}

				position += stride;
			}
			return output;
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

		private static DecodedImage BuildIndexedImage(PngHeader header, byte[] scanlines, byte[]? palette, byte[]? alpha)
		{
			if (palette == null)
			{
				throw new InvalidDataException("An indexed PNG file must contain a PLTE chunk.");
			}
			int entries = palette.Length / 3;
			Colour[] colours = new Colour[entries];
			for (int i = 0; i < entries; i++)
			{
				// A tRNS chunk may cover only the first entries of the palette; the rest stay opaque.
				byte opacity = alpha != null && i < alpha.Length ? alpha[i] : (byte)255;
				colours[i] = new Colour(opacity, palette[i * 3], palette[(i * 3) + 1], palette[(i * 3) + 2]);
			}

			// The same overflow safe calculation the scanlines were unfiltered with; colour type 3 has
			// one channel, so the packed row length matches.
			int stride = ScanlineStride(header);
			byte[] indices = new byte[header.Width * header.Height];
			int perByte = 8 / header.BitDepth;
			int mask = (1 << header.BitDepth) - 1;

			for (int y = 0; y < header.Height; y++)
			{
				int row = y * stride;
				int target = y * header.Width;
				for (int x = 0; x < header.Width; x++)
				{
					byte index;
					if (header.BitDepth == 8)
					{
						index = scanlines[row + x];
					}
					else
					{
						// Sub-byte depths pack several pixels into one byte, most significant bits first.
						int shift = 8 - (header.BitDepth * ((x % perByte) + 1));
						index = (byte)((scanlines[row + (x / perByte)] >> shift) & mask);
					}

					if (index >= entries)
					{
						throw new InvalidDataException("A pixel refers to a palette entry that the PLTE chunk does not contain.");
					}
					indices[target + x] = index;
				}
			}

			return DecodedImage.Indexed(header.Width, header.Height, colours, indices);
		}

		private static DecodedImage BuildTrueColourImage(PngHeader header, byte[] scanlines)
		{
			Colour[] pixels = new Colour[header.Width * header.Height];
			int stride = header.Width * header.Channels;

			for (int y = 0; y < header.Height; y++)
			{
				int row = y * stride;
				int target = y * header.Width;
				for (int x = 0; x < header.Width; x++)
				{
					int source = row + (x * header.Channels);
					pixels[target + x] = header.ColourType switch
					{
						0 => new Colour(scanlines[source], scanlines[source], scanlines[source]),
						2 => new Colour(scanlines[source], scanlines[source + 1], scanlines[source + 2]),
						4 => new Colour(scanlines[source + 1], scanlines[source], scanlines[source], scanlines[source]),
						_ => new Colour(scanlines[source + 3], scanlines[source], scanlines[source + 1], scanlines[source + 2])
					};
				}
			}

			return DecodedImage.TrueColour(header.Width, header.Height, pixels);
		}

		private static int ReadInt32(ReadOnlySpan<byte> data, int offset) =>
			(data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];

		private struct PngHeader
		{
			public int Width;
			public int Height;
			public byte BitDepth;
			public byte ColourType;
			public int Channels;
		}
	}
}
