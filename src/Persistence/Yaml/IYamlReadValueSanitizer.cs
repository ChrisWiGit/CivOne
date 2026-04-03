using System;

namespace CivOne.Persistence.Model
{
	public interface IYamlReadValueSanitizer
	{
		byte ClampToByte(long value, string mapperName, string fieldName, byte min = byte.MinValue, byte max = byte.MaxValue);
		short ClampToInt16(long value, string mapperName, string fieldName, short min = short.MinValue, short max = short.MaxValue);
		int ClampToInt32(long value, string mapperName, string fieldName, int min = int.MinValue, int max = int.MaxValue);
	}

	public class YamlReadValueSanitizer(ILogger logger) : IYamlReadValueSanitizer
	{
		private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

		public byte ClampToByte(long value, string mapperName, string fieldName, byte min = byte.MinValue, byte max = byte.MaxValue)
		{
			var clamped = Math.Clamp(value, min, max);
			LogClampIfNeeded(value, clamped, min, max, mapperName, fieldName);
			return (byte)clamped;
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