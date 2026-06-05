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
using System.Globalization;
using System.IO;
using System.Linq;
using CivOne.IO;

namespace CivOne.Graphics.ImageFormats
{
	internal class PicFile : IImageFormat, IDisposable
	{
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		private static Dictionary<string, PicFile> _cache = new Dictionary<string, PicFile>();
		private readonly byte[] _bytes;
		private readonly byte[,]? _colourTable;
		private readonly Palette _palette16 = Common.GetPalette16;
		private readonly Palette _palette256 = new Palette(256);
		private Bytemap? _picture16;
		private Bytemap? _picture256;
		private bool _ownsPalette256 = true;
		private bool _ownsPicture16;
		private bool _ownsPicture256;
		private bool _disposed;

		public bool HasPalette16 { get; internal set; }
		public bool HasPalette256 { get; internal set; }
		public bool HasPicture16 { get; internal set; }
		public bool HasPicture256 { get; internal set; }

		public Palette GetPalette16 => _palette16.Copy();
		public Palette GetPalette256 => _palette256.Copy();

		public Bytemap? GetPicture16 => _picture16;
		public Bytemap? GetPicture256 => _picture256;

		private void SetPicture16(Bytemap? value, bool owns)
		{
			if (_ownsPicture16)
			{
				_picture16?.Dispose();
			}
			_picture16 = value;
			_ownsPicture16 = owns;
		}

		private void SetPicture256(Bytemap? value, bool owns)
		{
			if (_ownsPicture256)
			{
				_picture256?.Dispose();
			}
			_picture256 = value;
			_ownsPicture256 = owns;
		}

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
			// Skip 2-byte length header; we always read the fixed 256-entry table.
			index += 2;
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
			// Skip 2-byte length header; we always read the fixed 256-entry palette.
			index += 2;
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
			// Skip the 1-byte LZW initial-bits header; the decoder uses its default 9..12 bit range.
			index++;
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

			SetPicture256(new Bytemap(width, height), owns: true);
			Bytemap picture256 = _picture256 ?? throw new InvalidOperationException("8-bit picture buffer was not initialized.");
			
			byte[] image = DecodePicture(ref index, length);
			int c = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (image.Length <= c)
					{
						picture256[x, y] = 0;
						continue;
					}
					picture256[x, y] = image[c++];
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

			SetPicture16(new Bytemap(width, height), owns: true);
			Bytemap picture16 = _picture16 ?? throw new InvalidOperationException("4-bit picture buffer was not initialized.");

			byte[] image = DecodePicture(ref index, length);
			int c = 0;
			for (int y = 0; y < height; y++)
			{
				// Each source byte packs two 4-bit pixels (low nibble first, high nibble second).
				for (int x = 0; x < width; x += 2)
				{
					picture16[x, y] = (byte)(image[c] & 0x0F);
					picture16[x + 1, y] = (byte)((image[c++] & 0xF0) >> 4);
				}
			}
		}
		
		/// <summary>
		/// Generate a 4-bit image from the 8-bit image and colourtable.
		/// </summary>
		/// <param name="colourTable">Colour table that was generated</param>
		private void ConvertPictureX0(byte[,]? colourTable)
		{
			if (colourTable == null) return;
			if (_picture256 == null) return;
			Bytemap picture256 = _picture256;
			
			int width = picture256.Width;
			int height = picture256.Height;

			SetPicture16(new Bytemap(width, height), owns: true);
			Bytemap picture16 = _picture16 ?? throw new InvalidOperationException("4-bit picture buffer was not initialized.");
			
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					byte col256 = picture256[x, y];
					picture16[x, y] = colourTable[col256, (y + x) % 2];
				}
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
			ArgumentNullException.ThrowIfNull(_picture256);
			Bytemap picture256 = _picture256;

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

					byte[] encoded = RLE.Encode(picture256.ToByteArray());
					encoded = LZW.Encode(encoded);
					
					br.Write((ushort)(encoded.Length + 5));
					br.Write((ushort)picture256.Width);
					br.Write((ushort)picture256.Height);
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

		// Cache: lowercase-with-extension -> actual full path. Built lazily, invalidated
		// when the data directory changes. Avoids an O(N) Directory.GetFiles scan per lookup.
		private static Dictionary<string, string>? _filenameCache;
		private static string? _filenameCacheDirectory;
		private static readonly object _filenameCacheLock = new();

		private static Dictionary<string, string> GetFilenameCache(string dataDirectory)
		{
			lock (_filenameCacheLock)
			{
				if (_filenameCache != null && string.Equals(_filenameCacheDirectory, dataDirectory, StringComparison.OrdinalIgnoreCase))
					return _filenameCache;

				Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
				if (Directory.Exists(dataDirectory))
				{
					foreach (string fileEntry in Directory.GetFiles(dataDirectory))
						map[Path.GetFileName(fileEntry)] = fileEntry;
				}
				_filenameCache = map;
				_filenameCacheDirectory = dataDirectory;
				return map;
			}
		}

		internal static void ClearFilenameCache()
		{
			lock (_filenameCacheLock)
			{
				_filenameCache = null;
				_filenameCacheDirectory = null;
			}
		}

		private static string? GetFilename(string filename)
		{
			if (filename.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
				return filename;

			Dictionary<string, string> cache = GetFilenameCache(Settings.Instance.DataDirectory);
			if (cache.TryGetValue($"{filename}.pic", out string? fullPath))
				return fullPath;
			return filename;
		}

		internal static bool Exists(string filename)
		{
			return File.Exists(GetFilename(filename));
		}
		internal static void ClearCache() => _cache.Clear();

		public PicFile(Picture picture)
		{
			_palette256 = picture.Palette;
			_ownsPalette256 = false;
			SetPicture16(picture.Bitmap, owns: false);
			SetPicture256(picture.Bitmap, owns: false);

			HasPalette16 = false;
			HasPicture16 = false;
			HasPalette256 = true;
			HasPicture256 = true;

			// never used, but initialize to avoid warnings about uninitialized readonly field
			_bytes = [];
		}

		
		public PicFile(string inputFileName)
		{
			string? fileName = GetFilename(inputFileName);

			// generate an exception if the file is not found
			if (RuntimeHandler.Runtime.Settings.Free || !File.Exists(fileName))
			{
				if (!File.Exists(fileName))  {
					Log($"File not found: {fileName?.ToUpper(CultureInfo.InvariantCulture)}.PIC");
				}
				HasPalette16 = true;
				HasPalette256 = true;
				_palette256 = Common.GetPalette256;
				_ownsPalette256 = false;
				SetPicture16(new Bytemap(320, 200), owns: true);
				SetPicture256(new Bytemap(320, 200), owns: true);
				Bytemap picture16 = _picture16 ?? throw new InvalidOperationException("4-bit fallback picture buffer was not initialized.");
				Bytemap picture256 = _picture256 ?? throw new InvalidOperationException("8-bit fallback picture buffer was not initialized.");
				for (int yy = 0; yy < 200; yy++)
				{
					for (int xx = 0; xx < 320; xx++)
					{
						picture16[xx, yy] = 1;
						picture256[xx, yy] = 1;
					}
				}
				// never used, but initialize to avoid warnings about uninitialized readonly field
				_bytes = [];
				return;
			}

			// read all bytes into a byte array
			using (FileStream fs = new(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
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

			Log($"Loaded {fileName?.ToUpper(CultureInfo.InvariantCulture)}.PIC");
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			if (_ownsPicture16)
			{
				_picture16?.Dispose();
			}
			if (_ownsPicture256)
			{
				_picture256?.Dispose();
			}
			if (_ownsPalette256)
			{
				_palette256?.Dispose();
			}

			_picture16 = null;
			_picture256 = null;
			_ownsPicture16 = false;
			_ownsPicture256 = false;
			_ownsPalette256 = false;
			_disposed = true;
		}
	}
}