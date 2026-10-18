using System;
using System.Collections.Generic;
using System.Globalization;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Test double for <see cref="ILogger"/> that records the formatted messages instead of writing them anywhere.
	/// </summary>
	public sealed class MockedLogger : ILogger
	{
		/// <summary>
		/// The messages logged so far, in call order and already formatted.
		/// </summary>
		public List<string> Messages { get; } = [];

		public void Log(string text, params object[] parameters)
		{
			ArgumentNullException.ThrowIfNull(text);
			ArgumentNullException.ThrowIfNull(parameters);
			
			Messages.Add(parameters.Length == 0
				? text
				: string.Format(CultureInfo.InvariantCulture, text, parameters));
		}
	}
}