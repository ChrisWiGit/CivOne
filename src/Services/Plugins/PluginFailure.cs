using System;
using System.IO;
using System.Reflection;

namespace CivOne.Services.Plugins
{
	/// <summary>
	/// Classifies exceptions raised while reading, loading or instantiating plugin code.
	/// </summary>
	internal static class PluginFailure
	{
		/// <summary>
		/// Decides whether an exception was caused by the plugin assembly rather than by the game.
		/// Plugin code is third-party code, so these failures are logged and skipped instead of
		/// taking down the caller. Everything else propagates, so real bugs in the game stay visible.
		/// </summary>
		/// <param name="exception">
		/// The exception raised while reading, loading or instantiating a plugin.
		/// </param>
		/// <returns>
		/// True when the exception is a known plugin load failure.
		/// </returns>
		public static bool IsLoadFailure(Exception exception) => exception switch
		{
			// Not a managed assembly, or built for an incompatible architecture.
			BadImageFormatException => true,
			// Unreadable, missing or locked file.
			IOException => true,
			UnauthorizedAccessException => true,
			// The assembly loads, but its types or their dependencies do not resolve.
			ReflectionTypeLoadException => true,
			TypeLoadException => true,
			// The type cannot be constructed, or its constructor throws.
			MemberAccessException => true,
			TargetInvocationException => true,
			TypeInitializationException => true,
			// Raised by SafeCreateInstance when a type does not match the expected contract.
			ArgumentException => true,
			InvalidOperationException => true,
			_ => false
		};
	}
}
