using System;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Graphics.ImageFormats
{
	/// <summary>
	/// Computes CRC-32 checksums as used by the PNG format (IEEE 802.3 polynomial <c>0xEDB88320</c>).
	/// <br/>
	/// PNG stores one checksum per chunk, covering the chunk type and the chunk data but not the
	/// length field. Both the reader and the writer need the same calculation.
	/// </summary>
	internal sealed class Crc32Delegate
	{
		private static uint[]? _table;

		/// <summary>
		/// The lookup table, built on first use.
		/// </summary>
		private static uint[] Table => _table ??= BuildTable();

		private static uint[] BuildTable()
		{
			uint[] table = new uint[256];
			for (uint i = 0; i < 256; i++)
			{
				uint value = i;
				for (int bit = 0; bit < 8; bit++)
				{
					value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
				}
				table[i] = value;
			}
			return table;
		}

		/// <summary>
		/// Computes the checksum of a complete block of data.
		/// </summary>
		/// <param name="data">The bytes to checksum.</param>
		/// <returns>The CRC-32 value.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public uint Compute(ReadOnlySpan<byte> data) => Finish(Update(0xFFFFFFFFu, data));

		/// <summary>
		/// Adds more data to a running checksum.
		/// Start with <c>0xFFFFFFFF</c> and pass the result to <see cref="Finish"/> when done.
		/// </summary>
		/// <param name="crc">The running checksum value.</param>
		/// <param name="data">The bytes to add.</param>
		/// <returns>The updated running checksum value.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public uint Update(uint crc, ReadOnlySpan<byte> data)
		{
			uint[] table = Table;
			foreach (byte value in data)
			{
				crc = table[(crc ^ value) & 0xFF] ^ (crc >> 8);
			}
			return crc;
		}

		/// <summary>
		/// Turns a running checksum value into the final checksum.
		/// </summary>
		/// <param name="crc">The running checksum value.</param>
		/// <returns>The CRC-32 value.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public uint Finish(uint crc) => crc ^ 0xFFFFFFFFu;
	}
}
