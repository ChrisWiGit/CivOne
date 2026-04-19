using System;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Test-only checked sanitizer implementation that preserves legacy unchecked cast semantics (wrap-around, no clamping).
	/// </summary>
	/// <remarks>
	/// This class exists to keep integration-like test scenarios behavior-compatible with historical runtime encoding,
	/// where clamping would hide or alter values under test.
	///
	/// It is intended to be activated via <c>ValueSanitizerFactory.UseCheckedValueSanitizer(...)</c>
	/// from the integration test base classes <c>TestsBase</c> and <c>TestsBase2</c>.
	/// </remarks>
	public sealed class UncheckedCastValueSanitizer : ICheckedValueSanitizer
	{
		public byte CheckedByte(long value, string mapperName, string fieldName, byte min = byte.MinValue, byte max = byte.MaxValue)
			=> unchecked((byte)value);

		public ushort CheckedUInt16(long value, string mapperName, string fieldName, ushort min = ushort.MinValue, ushort max = ushort.MaxValue)
			=> unchecked((ushort)value);

		public short CheckedInt16(long value, string mapperName, string fieldName, short min = short.MinValue, short max = short.MaxValue)
			=> unchecked((short)value);

		public int CheckedInt32(long value, string mapperName, string fieldName, int min = int.MinValue, int max = int.MaxValue)
			=> unchecked((int)value);
	}
}
