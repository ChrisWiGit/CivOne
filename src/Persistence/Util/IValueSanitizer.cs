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

	public class ValueSanitizer(ILogger logger) : IValueSanitizer, ICheckedValueSanitizer
	{
		private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

		public byte CheckedByte(long value, string mapperName, string fieldName, byte min = byte.MinValue, byte max = byte.MaxValue)
		{
			try
			{
				byte castValue = checked((byte)value);
				byte clamped = Math.Clamp(castValue, min, max);
				LogClampIfNeeded(castValue, clamped, min, max, mapperName, fieldName);
				return clamped;
			}
			catch (OverflowException ex)
			{
				var clamped = Math.Clamp(value, (long)min, (long)max);
				LogCheckedCastFailure(value, clamped, min, max, mapperName, fieldName, "byte", signed: false, ex);
				return (byte)clamped;
			}
		}

		public ushort CheckedUInt16(long value, string mapperName, string fieldName, ushort min = ushort.MinValue, ushort max = ushort.MaxValue)
		{
			try
			{
				ushort castValue = checked((ushort)value);
				ushort clamped = Math.Clamp(castValue, min, max);
				LogClampIfNeeded(castValue, clamped, min, max, mapperName, fieldName);
				return clamped;
			}
			catch (OverflowException ex)
			{
				var clamped = Math.Clamp(value, (long)min, (long)max);
				LogCheckedCastFailure(value, clamped, min, max, mapperName, fieldName, "ushort", signed: false, ex);
				return (ushort)clamped;
			}
		}

		public short CheckedInt16(long value, string mapperName, string fieldName, short min = short.MinValue, short max = short.MaxValue)
		{
			try
			{
				short castValue = checked((short)value);
				short clamped = Math.Clamp(castValue, min, max);
				LogClampIfNeeded(castValue, clamped, min, max, mapperName, fieldName);
				return clamped;
			}
			catch (OverflowException ex)
			{
				var clamped = Math.Clamp(value, (long)min, (long)max);
				LogCheckedCastFailure(value, clamped, min, max, mapperName, fieldName, "short", signed: true, ex);
				return (short)clamped;
			}
		}

		public int CheckedInt32(long value, string mapperName, string fieldName, int min = int.MinValue, int max = int.MaxValue)
		{
			try
			{
				int castValue = checked((int)value);
				int clamped = Math.Clamp(castValue, min, max);
				LogClampIfNeeded(castValue, clamped, min, max, mapperName, fieldName);
				return clamped;
			}
			catch (OverflowException ex)
			{
				var clamped = Math.Clamp(value, (long)min, (long)max);
				LogCheckedCastFailure(value, clamped, min, max, mapperName, fieldName, "int", signed: true, ex);
				return (int)clamped;
			}
		}

		public byte ClampToByte(long value, string mapperName, string fieldName, byte min = byte.MinValue, byte max = byte.MaxValue)
			=> CheckedByte(value, mapperName, fieldName, min, max);

		public ushort ClampToUInt16(long value, string mapperName, string fieldName, ushort min = ushort.MinValue, ushort max = ushort.MaxValue)
			=> CheckedUInt16(value, mapperName, fieldName, min, max);

		public short ClampToInt16(long value, string mapperName, string fieldName, short min = short.MinValue, short max = short.MaxValue)
			=> CheckedInt16(value, mapperName, fieldName, min, max);

		public int ClampToInt32(long value, string mapperName, string fieldName, int min = int.MinValue, int max = int.MaxValue)
			=> CheckedInt32(value, mapperName, fieldName, min, max);

		private void LogCheckedCastFailure(
			long value,
			long clamped,
			long min,
			long max,
			string mapperName,
			string fieldName,
			string targetType,
			bool signed,
			Exception ex)
		{
			var reason = value < min ? "underflow" : "overflow";
			var numberKind = signed ? "signed" : "unsigned";
			_logger.Log(
				"YAML {0} during checked cast in {1}.{2}: value {3} is outside [{4}..{5}] for {6} ({7}), clamped to {8}. ({9})",
				reason,
				mapperName,
				fieldName,
				value,
				min,
				max,
				targetType,
				numberKind,
				clamped,
				ex.Message);
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