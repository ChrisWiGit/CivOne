using System;
using System.IO;
using System.Text;

namespace CivOne.Graphics.ImageFormats
{
	/// <summary>
	/// Writes an <see cref="IBitmap"/> as an uncompressed 8-bit indexed Windows bitmap (<c>*.bmp</c>).
	///
	/// The game stores pictures as palette indices, which maps directly onto the 8-bit BMP layout,
	/// so no colour conversion or compression is required.
	/// </summary>
	internal sealed class BmpImageWriterDelegate
	{
		private const int FileHeaderSize = 14;
		private const int InfoHeaderSize = 40;
		private const int PaletteColourCount = 256;
		private const int PaletteSize = PaletteColourCount * 4;
		private const int PixelDataOffset = FileHeaderSize + InfoHeaderSize + PaletteSize;

		/// <summary>
		/// Writes the bitmap to the given file, replacing an existing file.
		/// </summary>
		/// <param name="bitmap">The bitmap to write.</param>
		/// <param name="filePath">The target file path.</param>
		public void Write(IBitmap bitmap, string filePath)
		{
			ArgumentNullException.ThrowIfNull(bitmap);
			ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

			using FileStream stream = File.Create(filePath);
			Write(bitmap, stream);
		}

		/// <summary>
		/// Writes the bitmap into the given stream.
		/// The stream is left open so callers keep control over its lifetime.
		/// </summary>
		/// <param name="bitmap">The bitmap to write.</param>
		/// <param name="stream">The stream that receives the BMP data.</param>
		/// <exception cref="ArgumentException">The bitmap has no pixels.</exception>
		public void Write(IBitmap bitmap, Stream stream)
		{
			ArgumentNullException.ThrowIfNull(bitmap);
			ArgumentNullException.ThrowIfNull(stream);

			int width = bitmap.Bitmap.Width;
			int height = bitmap.Bitmap.Height;
			if (width <= 0 || height <= 0)
			{
				throw new ArgumentException("The bitmap must have a positive width and height.", nameof(bitmap));
			}

			// Every BMP pixel row is padded to a multiple of four bytes.
			int rowSize = (width + 3) / 4 * 4;
			int imageSize = rowSize * height;

			using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
			WriteFileHeader(writer, imageSize);
			WriteInfoHeader(writer, width, height, imageSize);
			WritePalette(writer, bitmap.Palette);
			WritePixelData(writer, bitmap, width, height, rowSize);
		}

		private static void WriteFileHeader(BinaryWriter writer, int imageSize)
		{
			writer.Write((byte)'B');
			writer.Write((byte)'M');
			writer.Write(PixelDataOffset + imageSize);
			writer.Write((short)0);
			writer.Write((short)0);
			writer.Write(PixelDataOffset);
		}

		private static void WriteInfoHeader(BinaryWriter writer, int width, int height, int imageSize)
		{
			writer.Write(InfoHeaderSize);
			writer.Write(width);
			// A positive height selects the classic bottom-up row order.
			writer.Write(height);
			writer.Write((short)1);
			writer.Write((short)8);
			writer.Write(0);
			writer.Write(imageSize);
			writer.Write(0);
			writer.Write(0);
			writer.Write(PaletteColourCount);
			writer.Write(0);
		}

		private static void WritePalette(BinaryWriter writer, Palette palette)
		{
			for (int index = 0; index < PaletteColourCount; index++)
			{
				Colour colour = index < palette.Length ? palette[index] : Colour.Black;
				writer.Write(colour.B);
				writer.Write(colour.G);
				writer.Write(colour.R);
				writer.Write((byte)0);
			}
		}

		private static void WritePixelData(BinaryWriter writer, IBitmap bitmap, int width, int height, int rowSize)
		{
			byte[] pixels = bitmap.Bitmap.ToByteArray();
			// Allocated zeroed, and only the first width bytes are ever overwritten,
			// so the trailing padding bytes stay zero for every row.
			byte[] row = new byte[rowSize];

			for (int y = height - 1; y >= 0; y--)
			{
				Array.Copy(pixels, y * width, row, 0, width);
				writer.Write(row);
			}
		}
	}
}
