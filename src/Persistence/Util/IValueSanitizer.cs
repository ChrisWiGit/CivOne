using System;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Sanitizes numeric values read from YAML during deserialization,
	/// clamping them to the valid range of the target type and logging when clamping occurs.
	/// </summary>
	public interface IValueSanitizer
	{
		/// <summary>
		/// Clamps <paramref name="value"/> to [<paramref name="min"/>, <paramref name="max"/>]
		/// and returns the result as <c>byte</c> (System.Byte, 0–255).
		/// Logs a warning if clamping was applied.
		/// </summary>
		byte ClampToByte(long value, string mapperName, string fieldName, byte min = byte.MinValue, byte max = byte.MaxValue);

		/// <summary>
		/// Clamps <paramref name="value"/> to [<paramref name="min"/>, <paramref name="max"/>]
		/// and returns the result as <c>ushort</c> (System.UInt16, 0–65535).
		/// Logs a warning if clamping was applied.
		/// </summary>
		ushort ClampToUInt16(long value, string mapperName, string fieldName, ushort min = ushort.MinValue, ushort max = ushort.MaxValue);

		/// <summary>
		/// Clamps <paramref name="value"/> to [<paramref name="min"/>, <paramref name="max"/>]
		/// and returns the result as <c>short</c> (System.Int16, −32768–32767).
		/// Logs a warning if clamping was applied.
		/// </summary>
		short ClampToInt16(long value, string mapperName, string fieldName, short min = short.MinValue, short max = short.MaxValue);

		/// <summary>
		/// Clamps <paramref name="value"/> to [<paramref name="min"/>, <paramref name="max"/>]
		/// and returns the result as <c>int</c> (System.Int32, −2147483648–2147483647).
		/// Logs a warning if clamping was applied.
		/// </summary>
		int ClampToInt32(long value, string mapperName, string fieldName, int min = int.MinValue, int max = int.MaxValue);
	}

	public class ValueSanitizer(ILogger logger) : IValueSanitizer
	{
		private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

		public byte ClampToByte(long value, string mapperName, string fieldName, byte min = byte.MinValue, byte max = byte.MaxValue)
		{
			var clamped = Math.Clamp(value, min, max);
			LogClampIfNeeded(value, clamped, min, max, mapperName, fieldName);
			return (byte)clamped;
		}

		public ushort ClampToUInt16(long value, string mapperName, string fieldName, ushort min = ushort.MinValue, ushort max = ushort.MaxValue)
		{
			var clamped = Math.Clamp(value, min, max);
			LogClampIfNeeded(value, clamped, min, max, mapperName, fieldName);
			return (ushort)clamped;
		}

		public short ClampToInt16(long value, string mapperName, string fieldName, short min = short.MinValue, short max = short.MaxValue)
		{
			var clamped = Math.Clamp(value, min, max);
			LogClampIfNeeded(value, clamped, min, max, mapperName, fieldName);
			return (short)clamped;
		}

		public int ClampToInt32(long value, string mapperName, string fieldName, int min = int.MinValue, int max = int.MaxValue)
		{
			var clamped = Math.Clamp(value, min, max);
			LogClampIfNeeded(value, clamped, min, max, mapperName, fieldName);
			return (int)clamped;
		}

		private void LogClampIfNeeded(long value, long clamped, long min, long max, string mapperName, string fieldName)
		{
			if (value == clamped)
			{
				return;
			}

			var reason = value < min ? "underflow" : "overflow";
			_logger.Log(
				"YAML {0} in {1}.{2}: value {3} is outside [{4}..{5}], clamped to {6}.",
				reason,
				mapperName,
				fieldName,
				value,
				min,
				max,
				clamped);
		}
	}
}