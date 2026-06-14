namespace CivOne.Persistence.Model
{
	using System.Collections.Generic;
	using System.Globalization;
	using Xunit;

	/// <summary>
	/// Verifies range sanitization and logging behavior for <see cref="ValueSanitizer"/>.
	/// </summary>
	public class ValueSanitizerTest
	{
		/// <summary>
		/// Ensures that values above <see cref="byte.MaxValue"/> are clamped and produce an overflow log entry.
		/// </summary>
		[Fact]
		public void ClampToByteLogsOverflowWhenValueExceedsMaximum()
		{
			var logger = new CapturingLogger();
			var testee = new ValueSanitizer(logger);

			var actual = testee.ClampToByte(999, "TestMapper", "TestField");

			Assert.Equal(byte.MaxValue, actual);
			Assert.Single(logger.Messages);
			Assert.Contains("overflow", logger.Messages[0]);
			Assert.Contains("TestMapper.TestField", logger.Messages[0]);
		}

		/// <summary>
		/// Ensures that negative values are clamped to <see cref="ushort.MinValue"/> and produce an underflow log entry.
		/// </summary>
		[Fact]
		public void ClampToUInt16LogsUnderflowWhenValueFallsBelowMinimum()
		{
			var logger = new CapturingLogger();
			var testee = new ValueSanitizer(logger);

			var actual = testee.ClampToUInt16(-1, "TestMapper", "UnsignedField");

			Assert.Equal(ushort.MinValue, actual);
			Assert.Single(logger.Messages);
			Assert.Contains("underflow", logger.Messages[0]);
			Assert.Contains("TestMapper.UnsignedField", logger.Messages[0]);
		}

		/// <summary>
		/// Ensures that in-range values are returned unchanged and do not produce log entries.
		/// </summary>
		[Fact]
		public void ClampToInt32DoesNotLogWhenValueIsInRange()
		{
			var logger = new CapturingLogger();
			var testee = new ValueSanitizer(logger);

			var actual = testee.ClampToInt32(42, "TestMapper", "TestField");

			Assert.Equal(42, actual);
			Assert.Empty(logger.Messages);
		}

		/// <summary>
		/// Ensures checked conversion for unsigned target logs underflow and clamps negative values.
		/// </summary>
		[Fact]
		public void CheckedToUInt16OrClampLogsUnderflowWhenValueIsNegative()
		{
			var logger = new CapturingLogger();
			var testee = new ValueSanitizer(logger);

			var actual = testee.CheckedUInt16(-123, "TestMapper", "UnsignedCheckedField");

			Assert.Equal(ushort.MinValue, actual);
			Assert.Single(logger.Messages);
			Assert.Contains("underflow", logger.Messages[0]);
			Assert.Contains("unsigned", logger.Messages[0]);
		}

		/// <summary>
		/// Ensures checked conversion for signed target logs overflow and clamps large values.
		/// </summary>
		[Fact]
		public void CheckedToInt16OrClampLogsOverflowWhenValueExceedsMaximum()
		{
			var logger = new CapturingLogger();
			var testee = new ValueSanitizer(logger);

			var actual = testee.CheckedInt16(999_999, "TestMapper", "SignedCheckedField");

			Assert.Equal(short.MaxValue, actual);
			Assert.Single(logger.Messages);
			Assert.Contains("overflow", logger.Messages[0]);
			Assert.Contains("signed", logger.Messages[0]);
		}

		/// <summary>
		/// Test logger used to capture formatted messages emitted by the sanitizer.
		/// </summary>
		private sealed class CapturingLogger : ILogger
		{
			/// <summary>
			/// Gets all captured log messages.
			/// </summary>
			public List<string> Messages { get; } = [];

			/// <summary>
			/// Captures a formatted log message.
			/// </summary>
			/// <param name="text">The message template.</param>
			/// <param name="parameters">Template parameters.</param>
			public void Log(string text, params object[] parameters)
			{
				Messages.Add(string.Format(CultureInfo.InvariantCulture, text, parameters));
			}
		}
	}
}
