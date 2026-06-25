// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using CivOne.IO;

namespace CivOne.Graphics.ImageFormats
{
	public class GifFile : IImageFormat, IDisposable
	{
		private readonly Palette? _palette;
		private readonly Bytemap? _pixels;

		private static ILzwCodec LzwCodec => LzwServiceFactory.Codec;

		private static IEnumerable<byte[]> OutputBlock(byte[] buffer)
		{
			for (int offset = 0; offset < buffer.Length; offset += 255)
			{
				int length = buffer.Length - offset;
				if (length > 255) length = 255;
				byte[] output = new byte[length];
				Array.Copy(buffer, offset, output, 0, length);
				yield return output;
			}
		}

		private static IEnumerable<byte[]> InputBlock(byte[] buffer, int startIndex)
		{
			for (int offset = startIndex; offset < buffer.Length; )
			{
				int length = buffer[offset++];
				if (length == 0) yield break;
				byte[] output = new byte[length];
				Array.Copy(buffer, offset, output, 0, length);
				yield return output;
				offset += output.Length;
			}
		}

		public byte[] GetBytes()
		{
			Debug.Assert(_pixels != null, "No pixel data to write.");
			Debug.Assert(_palette != null, "No palette data to write.");

			using MemoryStream output = new();
			using BinaryWriter writer = new(output);
			// GIF header
			writer.Write("GIF89a".ToCharArray());

			// Width x Height
			writer.Write((ushort)_pixels.Width);
			writer.Write((ushort)_pixels.Height);

			// GCT Descriptor
			writer.Write((byte)0xF7);

			// Background colour #0
			writer.Write((byte)0x00);

			//Default pixel aspect ratio
			writer.Write((byte)0x00);

			// Write colour palette
			for (int i = 0; i < 256; i++)
			{
				byte r = 0, g = 0, b = 0;
				if (_palette.Length > i)
				{
					r = _palette[i].R;
					g = _palette[i].G;
					b = _palette[i].B;
				}
				writer.Write(new byte[] { r, g, b });
			}

			// Image Descriptor
			writer.Write((byte)0x2C);
			// NW Corner
			writer.Write((ushort)0);
			writer.Write((ushort)0);
			// Width x Height
			writer.Write((ushort)_pixels.Width);
			writer.Write((ushort)_pixels.Height);
			// No local colour table
			writer.Write((byte)0x00);

			byte[] encoded = LzwCodec.Encode(_pixels.ToByteArray(), true, false, 12);

			// Code length
			writer.Write((byte)0x08);
			foreach (byte[] byteBlock in OutputBlock(encoded))
			{
				writer.Write((byte)byteBlock.Length);
				writer.Write(byteBlock);
			}
			writer.Write((byte)0x00);

			// End of file
			writer.Write((byte)0x3B);

			return output.ToArray();
		}
		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This method may perform non-trivial logic to create a new bitmap instance, so we prefer to keep it as a method for clarity and to avoid giving the impression that it is a simple property access.")]
		public IBitmap? GetNewBitmap()
		{
			if (_pixels == null || _palette == null)
				return null;
			return new Picture(_pixels, _palette);
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch all exceptions that may occur during GIF decoding and log them without crashing the application, as a failure to decode a GIF file should not be considered a critical error.")]
		public GifFile(byte[] buffer)
		{
			// Check for valid GIF-file header
			if (Encoding.UTF8.GetString(buffer, 0, 6) != "GIF87a" && Encoding.UTF8.GetString(buffer, 0, 6) != "GIF89a") return;

			int width = BitConverter.ToUInt16(buffer, 6);
			int height = BitConverter.ToUInt16(buffer, 8);

			// GCT Descriptor
			bool hasGct = ((buffer[10] >> 7) & 1) == 1;
			int colourCount = (int)Math.Pow(2, (buffer[10] & 7) + 1);
			byte aspectRatio = buffer[12];

			if (aspectRatio != 0) return; // Can not handle this file
			if (!hasGct) return; // Can not handle this file
			//if (colourResolution != 8) return; // Can not handle this file

			int index = 13;
			Colour[] palette = new Colour[256];
			for (int i = 0; i < colourCount; i++)
			{
				byte r = buffer[index++], g = buffer[index++], b = buffer[index++];
				palette[i] = new Colour(r, g, b);
			}

			while (index < buffer.Length)
			{
				switch (buffer[index])
				{
					case 0x2C:
						// Image descriptor
						if (buffer[index++] != 0x2C) return; // Unexpected byte
						if (BitConverter.ToUInt16(buffer, index) != 0) return; // Can not handle this file
						if (BitConverter.ToUInt16(buffer, index + 2) != 0) return; // Can not handle this file
						if (BitConverter.ToUInt16(buffer, index + 4) != width) return; // Can not handle this file
						if (BitConverter.ToUInt16(buffer, index + 6) != height) return; // Can not handle this file
						index += 8;
						if (buffer[index++] != 0) return; // Can not handle this file
						break;
					case 0x21:
						// Graphics Control Extension/Comment Extension Block
						index++;
						switch (buffer[index++])
						{
							case 0xF9:
								// Graphics Control Extension
								{
									int size = buffer[index++];
									if (size == 4)
									{
										byte packed = buffer[index++];
										index += 2;
										byte transparentColour = buffer[index++];
										if ((packed & 1) == 1)
										{
											palette[transparentColour] = Colour.Transparent;
										}
										index++;
									}
									else
									{
										index += size + 1;
									}
								}
								break;
							case 0xFE:
								// Comment Extension Block
								{
									int size = buffer[index++];
									index += size + 1;
								}
								break;
							default:
								// Unexpected byte
								return;
						}
						break;
					case 0x3B:
						// Trailer (end of file)
						index = buffer.Length;
						break;
					default:
						int minCode = buffer[index++];
						byte[] lzwData;
						using (MemoryStream lzwStream = new())
						{
							foreach (byte[] data in InputBlock(buffer, index))
							{
								index += data.Length + 2;
								lzwStream.Write(data, 0, data.Length);
							}
							lzwData = lzwStream.ToArray();
						}

						try
						{
							byte[] pixels = LzwCodec.Decode(lzwData, true, false, minCode, 12);
							_pixels = new Bytemap(width, height).FromByteArray(pixels);
							_palette = palette;
						}
						catch
						{
							RuntimeHandler.Runtime.Log("Could not decode GIF file.");
						}
						return;
				}
			}
		}

		/// <summary>
		/// Creates a new GIF file from the specified bitmap. 
		/// <b>The bitmap should not be used after creating the GIF file, as it may be disposed when the GIF file is disposed.</b>
		/// </summary>
		/// <param name="transferBitmap">The bitmap to create the GIF file from. The ownership of the bitmap is transferred to the class.</param>
		public GifFile(IBitmap transferBitmap)
		{
			_palette = transferBitmap.Palette;
			_pixels = transferBitmap.Bitmap;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_palette?.Dispose();
				_pixels?.Dispose();
			}
		}
	}
}