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
using System.IO;
using System.Linq;
using CivOne.IO;
using CivOne.Graphics;

namespace CivOne.Graphics.ImageFormats
{
	internal class PicFile : IImageFormat
	{
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		private static Dictionary<string, PicFile> _cache = new Dictionary<string, PicFile>();
		private readonly byte[] _bytes;
		private readonly byte[,] _colourTable = null;
		private readonly Palette _palette16 = Common.GetPalette16;
		private readonly Palette _palette256 = new Palette(256);
		private Bytemap _picture16;
		private Bytemap _picture256;

		public bool HasPalette16 { get; internal set; }
		public bool HasPalette256 { get; internal set; }
		public bool HasPicture16 { get; internal set; }
		public bool HasPicture256 { get; internal set; }

		public Palette GetPalette16 => _palette16.Copy();
		public Palette GetPalette256 => _palette256.Copy();

		public Bytemap GetPicture16 => _picture16;
		public Bytemap GetPicture256 => _picture256;

		/// <summary>
		/// Read the E0 colour replacement table from the PIC file.
		/// </summary>
		/// <param name="index">Current file reading index.</param>
		/// <returns>Returns a byte array containing the colour replacement table.</returns>
		private byte[,] ReadColourTable(ref int index)
		{
			// the 4-bit colour conversion table has 2 entries for each colour
			// that are painted in a chessboard-pattern, so one 8-bit colour can
			// be replaced by two different 4-bit colours
			byte[,] colourTable = new byte[256, 2];
			uint length = BitConverter.ToUInt16(_bytes, index); index += 2;
			byte firstIndex = _bytes[index++];
			byte lastIndex = _bytes[index++];
			
			// create all colour entries
			for (int i = 0; i < 256; i++)
			{
				// if the colour entries fall outside the first/last index range, they
				// will use colour 0 (transparent)
				// this never happens for any of the original Civilization resources
				if (i < firstIndex || i > lastIndex)
				{
					for (int j = 0; j < 2; j++)
					{
						colourTable[i, j] = 0;
					}
					continue;
				}
				
				// split the byte into two nibbles, each containing a colour number
				colourTable[i, 0] = (byte)((_bytes[index] & 0xF0) >> 4);
				colourTable[i, 1] = (byte)(_bytes[index] & 0x0F);
				index++;
			}
			
			// This is a fix for transparency in 16 colour mode
			colourTable[0, 0] = 0;
			colourTable[0, 1] = 0;
			
			return colourTable;
		}

		/// <summary>
		/// Read the M0 colour palette.
		/// </summary>
		/// <param name="index">Current file reading index.</param>
		private void ReadColourPalette(ref int index)
		{
			uint length = BitConverter.ToUInt16(_bytes, index); index += 2;
			byte firstIndex = _bytes[index++];
			byte lastIndex = _bytes[index++];
			for (int i = 0; i < 256; i++)
			{
				// if the colour entry fall outside the first/last index range, use
				// a transparent colour entry
				// this never happens for any of the original Civilization resources
				if (i < firstIndex || i > lastIndex)
				{
					_palette256[i] = Colour.Transparent;
					continue;
				}
				byte red = _bytes[index++], green = _bytes[index++], blue = _bytes[index++];
				_palette256[i] = new Colour(red * 4, green * 4, blue * 4);
			}
			
			// always set colour 0 to transparent
			_palette256[0] = Colour.Transparent;
		}

		/// <summary>
		/// Extract/Decode the LZW/RLE encoded bytes.
		/// </summary>
		/// <param name="index">Current file reading index.</param>
		/// <param name="length">Number of bytes to decode.</param>
		/// <returns></returns>
		private byte[] DecodePicture(ref int index, uint length)
		{
			byte bits = _bytes[index++];
			byte[] img = new byte[length - 5];
			Array.Copy(_bytes, index, img, 0, (int)(length - 5));
			index += (int)(length - 5);
			return RLE.Decode(LZW.Decode(img));
		}
		
		/// <summary>
		/// Read the 8-bit image into a 2D byte array.
		/// </summary>
		/// <param name="index">Current file reading index.</param>
		private void ReadPictureX0(ref int index)
		{
			uint length = BitConverter.ToUInt16(_bytes, index); index += 2;
			int width = BitConverter.ToUInt16(_bytes, index); index += 2;
			int height = BitConverter.ToUInt16(_bytes, index); index += 2;
			
			_picture256?.Dispose();
			_picture256 = new Bytemap(width, height);
			
			byte[] image = DecodePicture(ref index, length);
			int c = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (image.Length <= c)
					{
						_picture256[x, y] = 0;
						continue;
					}
					_picture256[x, y] = image[c++];
				}
			}
		}
		
		/// <summary>
		/// Read the 4-bit image into a 2D byte array.
		/// </summary>
		/// <param name="index">Current file reading index.</param>
		private void ReadPictureX1(ref int index)
		{
			uint length = BitConverter.ToUInt16(_bytes, index); index += 2;
			int width = BitConverter.ToUInt16(_bytes, index); index += 2;
			int height = BitConverter.ToUInt16(_bytes, index); index += 2;
			
			_picture16?.Dispose();
			_picture16 = new Bytemap(width, height);

			byte[] image = DecodePicture(ref index, length);
			int c = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					_picture16[x++, y] = (byte)(image[c] & 0x0F);
					_picture16[x, y] = (byte)((image[c++] & 0xF0) >> 4);
				}
			}
		}
		
		/// <summary>
		/// Generate a 4-bit image from the 8-bit image and colourtable.
		/// </summary>
		/// <param name="colourTable">Colour table that was generated</param>
		private void ConvertPictureX0(byte[,] colourTable)
		{
			if (colourTable == null) return;
			
			int width = _picture256.Width;
			int height = _picture256.Height;
			
			_picture16?.Dispose();
			_picture16 = new Bytemap(width, height);
			
			for (int y = 0; y < height; y++)
			for (int x = 0; x < width; x++)
			{
				byte col256 = _picture256[x, y];
				_picture16[x, y] = colourTable[col256, (y + x) % 2];
			}
		}
		
		private IEnumerable<byte> GetColourPaletteBytes()
		{
			// Length is always 770, startIndex is always 0, endIndex is always 255
			foreach (byte b in BitConverter.GetBytes((ushort)770)) yield return b;
			yield return (byte)0x00;
			yield return (byte)0xFF;

			for (int i = 0; i < _palette256.Length; i++)
			{
				yield return (byte)(_palette256[i].R / 4);
				yield return (byte)(_palette256[i].G / 4);
				yield return (byte)(_palette256[i].B / 4);
			}
		}

		public byte[] GetBytes()
		{
			using (MemoryStream ms = new MemoryStream())
			using (BinaryWriter br = new BinaryWriter(ms))
			{
				if (HasPalette16)
				{
					br.Write((ushort)0x3045);
					throw new NotImplementedException();
				}
				if (HasPalette256)
				{
					br.Write((ushort)0x304D);
					br.Write(GetColourPaletteBytes().ToArray());
				}
				if (HasPicture256)
				{
					br.Write((ushort)0x3058);

					byte[] encoded = RLE.Encode(_picture256.ToByteArray());
					encoded = LZW.Encode(encoded);
					
					br.Write((ushort)(encoded.Length + 5));
					br.Write((ushort)_picture256.Width);
					br.Write((ushort)_picture256.Height);
					br.Write((byte)11);
					br.Write(encoded);
				}
				if (HasPalette16)
				{
					br.Write((ushort)0x3158);
					throw new NotImplementedException();
				}
				return ms.ToArray();
			}
		}

		private static string GetFilename(string filename)
		{
			if (!filename.ToLower().EndsWith(".map"))
			{
				// fix for case sensitive file systems
				foreach (string fileEntry in Directory.GetFiles(Settings.Instance.DataDirectory))
				{
					if (Path.GetFileName(fileEntry).ToLower() != $"{filename.ToLower()}.pic") continue;
					return fileEntry;
				}
			}
			return filename;
		}

		internal static bool Exists(string filename)
		{
			return File.Exists(GetFilename(filename));
		}

		public PicFile(Picture picture)
		{
			_palette256 = picture.Palette;
			_picture16 = picture.Bitmap;
			_picture256 = picture.Bitmap;

			HasPalette16 = false;
			HasPicture16 = false;
			HasPalette256 = true;
			HasPicture256 = true;
		}

		internal static void ClearCache() => _cache.Clear();
		
		public PicFile(string filename)
		{
			filename = GetFilename(filename);

			// generate an exception if the file is not found
			if (RuntimeHandler.Runtime.Settings.Free || !File.Exists(filename))
			{
				if (!File.Exists(filename))  {
					Log($"File not found: {filename.ToUpper()}.PIC");
				}
				HasPalette16 = true;
				HasPalette256 = true;
				_palette256 = Common.GetPalette256;
				_picture16 = new Bytemap(320, 200);
				_picture256 = new Bytemap(320, 200);
				for (int yy = 0; yy < 200; yy++)
				for (int xx = 0; xx < 320; xx++)
				{
					_picture16[xx, yy] = 1;
					_picture256[xx, yy] = 1;
				}
				return;
			}

			// read all bytes into a byte array
			using (FileStream fs = new FileStream(filename, FileMode.Open))
			{
				_bytes = new byte[fs.Length];
				fs.ReadExactly(_bytes, 0, _bytes.Length);
			}
			
			int index = 0;
			while (index < (_bytes.Length - 1))
			{
				uint magicCode = BitConverter.ToUInt16(_bytes, index); index += 2;
				switch (magicCode)
				{
					case 0x3045:
						_colourTable = ReadColourTable(ref index);
						HasPalette16 = true;
						break;
					case 0x304D:
						ReadColourPalette(ref index);
						HasPalette256 = true;
						break;
					case 0x3058:
						ReadPictureX0(ref index);
						ConvertPictureX0(_colourTable);
						HasPicture256 = true;
						break;
					case 0x3158:
						ReadPictureX1(ref index);
						HasPicture16 = true;
						break;
				}
			}

			Log($"Loaded {filename}");
		}
	}
}