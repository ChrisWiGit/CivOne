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

namespace CivOne.IO
{
	/// <summary>
	/// Defines LZW compression and decompression operations.
	/// </summary>
	public interface ILzwCodec
	{
		/// <summary>
		/// Decodes LZW-compressed bytes.
		/// </summary>
		/// <param name="input">Compressed input bytes.</param>
		/// <param name="clearEnd">
		/// Indicates whether clear and end codes are used.
		/// </param>
		/// <param name="flushDictionary">
		/// Indicates whether the dictionary is reset when it reaches max size.
		/// </param>
		/// <param name="minBits">Minimum symbol bit size.</param>
		/// <param name="maxBits">Maximum code bit size.</param>
		/// <returns>Decoded bytes.</returns>
		byte[] Decode(byte[] input, bool clearEnd = false, bool flushDictionary = true, int minBits = 8, int maxBits = 11);

		/// <summary>
		/// Encodes bytes using LZW compression.
		/// </summary>
		/// <param name="input">Raw input bytes.</param>
		/// <param name="clearEnd">
		/// Indicates whether clear and end codes are written.
		/// </param>
		/// <param name="flushDictionary">
		/// Indicates whether the dictionary is reset when it reaches max size.
		/// </param>
		/// <param name="maxBits">Maximum code bit size.</param>
		/// <param name="minBits">Minimum symbol bit size.</param>
		/// <returns>Encoded bytes.</returns>
		byte[] Encode(byte[] input, bool clearEnd = false, bool flushDictionary = true, int maxBits = 11, int minBits = 8);
	}

	/// <summary>
	/// DI-friendly LZW implementation with corrected code-width and dictionary handling.
	/// </summary>
	public sealed class LZWCorrected : ILzwCodec
	{
		private sealed class BitWriter
		{
			private readonly List<byte> _bytes = [];
			private int _bitIndex;
			private byte _current;
			private bool _closed;

			public void Write(int value, int bitCount)
			{
				for (int bit = 0; bit < bitCount; bit++)
				{
					int outputBit = (value >> bit) & 0x01;
					_current |= (byte)(outputBit << _bitIndex);
					_bitIndex++;
					if (_bitIndex < 8)
					{
						continue;
					}

					_bytes.Add(_current);
					_current = 0;
					_bitIndex = 0;
				}
			}

			public byte[] ToArray()
			{
				if (!_closed && _bitIndex > 0)
				{
					_bytes.Add(_current);
				}
				_closed = true;
				return [.._bytes];
			}
		}

		private sealed class BitReader(byte[] input)
		{
			private readonly byte[] _input = input;
			private int _byteIndex;
			private int _bitIndex;

			public bool TryRead(int bitCount, out int value)
			{
				value = 0;
				for (int bit = 0; bit < bitCount; bit++)
				{
					if (_byteIndex >= _input.Length)
					{
						return false;
					}

					int inputBit = (_input[_byteIndex] >> _bitIndex) & 0x01;
					value |= inputBit << bit;

					_bitIndex++;
					if (_bitIndex < 8)
					{
						continue;
					}

					_bitIndex = 0;
					_byteIndex++;
				}
				return true;
			}
		}

		private static int CodeLength(int value)
		{
			if (value <= 0)
			{
				return 1;
			}

			int bits = 0;
			while (value > 0)
			{
				bits++;
				value >>= 1;
			}
			return bits;
		}

		private static byte[] Append(byte[] first, byte value)
		{
			byte[] output = new byte[first.Length + 1];
			if (first.Length > 0)
			{
				Array.Copy(first, output, first.Length);
			}
			output[first.Length] = value;
			return output;
		}

		private static string Key(byte[] values)
		{
			return Convert.ToHexString(values);
		}

		private static Dictionary<int, byte[]> CreateDecodeDictionary(bool clearEnd, int minBits)
		{
			Dictionary<int, byte[]> dictionary = Enumerable
				.Range(0, 1 << minBits)
				.ToDictionary(index => index, index => new byte[] { (byte)index });

			if (clearEnd)
			{
				dictionary.Add(dictionary.Count, []);
				dictionary.Add(dictionary.Count, []);
			}

			return dictionary;
		}

		private static Dictionary<string, int> CreateEncodeDictionary(bool clearEnd, int minBits)
		{
			Dictionary<string, int> dictionary = [];
			for (int value = 0; value < (1 << minBits); value++)
			{
				dictionary.Add(Key([(byte)value]), value);
			}

			if (clearEnd)
			{
				dictionary.Add("CLR", dictionary.Count);
				dictionary.Add("END", dictionary.Count);
			}

			return dictionary;
		}

		private static bool CanGrowDictionary(int dictionaryCount, int maxBits)
		{
			return dictionaryCount < (1 << maxBits);
		}

		/// <inheritdoc />
		public byte[] Decode(byte[] input, bool clearEnd = false, bool flushDictionary = true, int minBits = 8, int maxBits = 11)
		{
			ArgumentNullException.ThrowIfNull(input);
			if (minBits < 2 || minBits > 8)
			{
				throw new ArgumentOutOfRangeException(nameof(minBits));
			}
			if (maxBits < minBits + 1 || maxBits > 16)
			{
				throw new ArgumentOutOfRangeException(nameof(maxBits));
			}

			Dictionary<int, byte[]> dictionary = CreateDecodeDictionary(clearEnd, minBits);
			BitReader reader = new(input);
			List<byte> output = [];

			int clearCode = 1 << minBits;
			int endCode = clearCode + 1;
			byte[] entry = [];

			while (reader.TryRead(Math.Min(CodeLength(dictionary.Count - 1), maxBits), out int value))
			{
				if (clearEnd && value == clearCode)
				{
					dictionary = CreateDecodeDictionary(clearEnd, minBits);
					entry = [];
					continue;
				}

				if (clearEnd && value == endCode)
				{
					break;
				}

				byte[] outValue;
				if (dictionary.TryGetValue(value, out byte[]? existing))
				{
					outValue = existing;
				}
				else if (value == dictionary.Count && entry.Length > 0)
				{
					outValue = Append(entry, entry[0]);
				}
				else
				{
					if (clearEnd)
					{
						throw new InvalidDataException("Invalid LZW code sequence.");
					}

					// Legacy PIC streams can contain non-canonical tails or dictionary drift.
					// For non-clear/end mode, recover instead of aborting startup.
					if (flushDictionary)
					{
						dictionary = CreateDecodeDictionary(clearEnd, minBits);
						entry = [];
						continue;
					}

					break;
				}

				output.AddRange(outValue);

				if (entry.Length > 0)
				{
					if (CanGrowDictionary(dictionary.Count, maxBits))
					{
						dictionary.Add(dictionary.Count, Append(entry, outValue[0]));
					}
					else if (flushDictionary && !clearEnd)
					{
						dictionary = CreateDecodeDictionary(clearEnd, minBits);
						entry = [];
						continue;
					}
				}

				entry = outValue;
			}

			return [..output];
		}

		/// <inheritdoc />
		public byte[] Encode(byte[] input, bool clearEnd = false, bool flushDictionary = true, int maxBits = 11, int minBits = 8)
		{
			ArgumentNullException.ThrowIfNull(input);
			if (minBits < 2 || minBits > 8)
			{
				throw new ArgumentOutOfRangeException(nameof(minBits));
			}
			if (maxBits < minBits + 1 || maxBits > 16)
			{
				throw new ArgumentOutOfRangeException(nameof(maxBits));
			}

			Dictionary<string, int> dictionary = CreateEncodeDictionary(clearEnd, minBits);
			BitWriter writer = new();

			if (clearEnd)
			{
				writer.Write(dictionary["CLR"], Math.Min(CodeLength(dictionary.Count - 1), maxBits));
			}

			byte[] entry = [];
			for (int i = 0; i < input.Length; i++)
			{
				byte[] newEntry = Append(entry, input[i]);
				if (dictionary.ContainsKey(Key(newEntry)))
				{
					entry = newEntry;
					continue;
				}

				writer.Write(dictionary[Key(entry)], Math.Min(CodeLength(dictionary.Count - 1), maxBits));
				if (CanGrowDictionary(dictionary.Count, maxBits))
				{
					dictionary.Add(Key(newEntry), dictionary.Count);
				}
				else if (flushDictionary)
				{
					if (clearEnd)
					{
						writer.Write(dictionary["CLR"], Math.Min(CodeLength(dictionary.Count - 1), maxBits));
					}
					dictionary = CreateEncodeDictionary(clearEnd, minBits);
				}

				entry = [input[i]];
			}

			if (entry.Length > 0)
			{
				writer.Write(dictionary[Key(entry)], Math.Min(CodeLength(dictionary.Count - 1), maxBits));
			}

			if (clearEnd)
			{
				writer.Write(dictionary["END"], Math.Min(CodeLength(dictionary.Count - 1), maxBits));
			}

			return writer.ToArray();
		}
	}
}
