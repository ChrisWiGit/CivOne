using System;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Provides checked numeric conversions with signed/unsigned awareness.
	/// On overflow/underflow, implementations should log and return a clamped fallback value.
	/// </summary>
	public interface ICheckedValueSanitizer
	{
		/// <summary>
		/// Attempts a checked cast to <c>byte</c> (unsigned), logs and clamps on overflow/underflow.
		/// </summary>
		byte CheckedByte(long value, string mapperName, string fieldName, byte min = byte.MinValue, byte max = byte.MaxValue);

		/// <summary>
		/// Attempts a checked cast to <c>ushort</c> (unsigned), logs and clamps on overflow/underflow.
		/// </summary>
		ushort CheckedUInt16(long value, string mapperName, string fieldName, ushort min = ushort.MinValue, ushort max = ushort.MaxValue);

		/// <summary>
		/// Attempts a checked cast to <c>short</c> (signed), logs and clamps on overflow/underflow.
		/// </summary>
		short CheckedInt16(long value, string mapperName, string fieldName, short min = short.MinValue, short max = short.MaxValue);

		/// <summary>
		/// Attempts a checked cast to <c>int</c> (signed), logs and clamps on overflow/underflow.
		/// </summary>
		int CheckedInt32(long value, string mapperName, string fieldName, int min = int.MinValue, int max = int.MaxValue);
	}
}
