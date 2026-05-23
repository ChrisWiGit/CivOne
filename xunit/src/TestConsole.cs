using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Suppresses console output from CivOne code during unit tests when the SUPPRESS_CONSOLE_LOGS symbol is defined. This allows test output to be cleaner and more focused on test results,
	/// while still allowing xUnit's own test status output to be visible. Enable this by defining the SUPPRESS_CONSOLE_LOGS symbol in the test project file or by passing -p:SuppressConsoleLogs=true when running tests.
	/// </summary>
	internal static class TestConsole
	{
		[ModuleInitializer]
		internal static void Initialize()
		{
#if SUPPRESS_CONSOLE_LOGS
			// Enable this with dotnet test -p:SuppressConsoleLogs=true.
			Console.SetOut(TextWriter.Null);
			Console.SetError(TextWriter.Null);
#endif
		}
	}
}