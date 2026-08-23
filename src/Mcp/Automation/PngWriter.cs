using System;
using System.IO;
using System.IO.Compression;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne.Mcp.Automation
{
	/// <summary>
	/// Writes an 8-bit indexed (palette) PNG file from a Bytemap + Colour array.
	/// </summary>
	internal static class PngWriter
	{
		private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

		public static byte[] Write(Bytemap bitmap, Colour[] palette)
		{
			using MemoryStream ms = new MemoryStream();
			ms.Write(Signature);
			WriteChunk(ms, "IHDR", BuildIHDR(bitmap.Width, bitmap.Height));
			WriteChunk(ms, "PLTE", BuildPLTE(palette));
			WriteChunk(ms, "IDAT", BuildIDAT(bitmap));
			WriteChunk(ms, "IEND", []);
			return ms.ToArray();
		}

		private static byte[] BuildIHDR(int width, int height)
		{
			byte[] data = new byte[13];
			WriteInt32BE(data, 0, width);
			WriteInt32BE(data, 4, height);
			data[8] = 8; // bit depth
			data[9] = 3; // color type: indexed
			// compression=0, filter=0, interlace=0 already zeroed
			return data;
		}

		private static byte[] BuildPLTE(Colour[] palette)
		{
			int count = Math.Min(palette?.Length ?? 0, 256);
			byte[] data = new byte[count * 3];
			for (int i = 0; i < count; i++)
			{
				data[i * 3 + 0] = palette![i].R;
				data[i * 3 + 1] = palette![i].G;
				data[i * 3 + 2] = palette![i].B;
			}
			return data;
		}

		private static byte[] BuildIDAT(Bytemap bitmap)
		{
			byte[] raw = bitmap.ToByteArray();
			int width = bitmap.Width;
			int height = bitmap.Height;

			// Filter type 0 (None) per row: each row prefixed by a 0x00 byte
			byte[] filtered = new byte[height * (1 + width)];
			for (int y = 0; y < height; y++)
			{
				// filter byte = 0
				Array.Copy(raw, y * width, filtered, y * (1 + width) + 1, width);
			}

			using MemoryStream compressed = new MemoryStream();
			using (ZLibStream zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
				zlib.Write(filtered, 0, filtered.Length);

			return compressed.ToArray();
		}

		private static void WriteChunk(Stream stream, string type, byte[] data)
		{
			byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
			byte[] lenBytes = new byte[4];
			WriteInt32BE(lenBytes, 0, data.Length);
			stream.Write(lenBytes);
			stream.Write(typeBytes);
			stream.Write(data);

			uint crc = Crc32(typeBytes, 0xFFFFFFFF);
			crc = Crc32(data, crc);
			crc ^= 0xFFFFFFFF;
			byte[] crcBytes = new byte[4];
			WriteInt32BE(crcBytes, 0, (int)crc);
			stream.Write(crcBytes);
		}

		private static void WriteInt32BE(byte[] buffer, int offset, int value)
		{
			buffer[offset + 0] = (byte)(value >> 24);
			buffer[offset + 1] = (byte)(value >> 16);
			buffer[offset + 2] = (byte)(value >> 8);
			buffer[offset + 3] = (byte)value;
		}

		private static readonly uint[] CrcTable = BuildCrcTable();

		private static uint[] BuildCrcTable()
		{
			uint[] table = new uint[256];
			for (uint i = 0; i < 256; i++)
			{
				uint c = i;
				for (int k = 0; k < 8; k++)
					c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
				table[i] = c;
			}
			return table;
		}

		private static uint Crc32(byte[] data, uint crc)
		{
			foreach (byte b in data)
				crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
			return crc;
		}
	}
}
