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
