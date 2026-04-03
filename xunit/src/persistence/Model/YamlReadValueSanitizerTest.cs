namespace CivOne.Persistence.Model
{
	using System.Collections.Generic;
	using Xunit;

	/// <summary>
	/// Verifies range sanitization and logging behavior for <see cref="YamlReadValueSanitizer"/>.
	/// </summary>
	public class YamlReadValueSanitizerTest
	{
		/// <summary>
		/// Ensures that values above <see cref="byte.MaxValue"/> are clamped and produce an overflow log entry.
		/// </summary>
		[Fact]
		public void ClampToByte_LogsOverflow_WhenValueExceedsMaximum()
		{
			var logger = new CapturingLogger();
			var testee = new YamlReadValueSanitizer(logger);

			var actual = testee.ClampToByte(999, "TestMapper", "TestField");

			Assert.Equal(byte.MaxValue, actual);
			Assert.Single(logger.Messages);
			Assert.Contains("overflow", logger.Messages[0]);
			Assert.Contains("TestMapper.TestField", logger.Messages[0]);
		}

		/// <summary>
		/// Ensures that in-range values are returned unchanged and do not produce log entries.
		/// </summary>
		[Fact]
		public void ClampToInt32_DoesNotLog_WhenValueIsInRange()
		{
			var logger = new CapturingLogger();
			var testee = new YamlReadValueSanitizer(logger);

			var actual = testee.ClampToInt32(42, "TestMapper", "TestField");

			Assert.Equal(42, actual);
			Assert.Empty(logger.Messages);
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
				Messages.Add(string.Format(text, parameters));
			}
		}
	}
}
