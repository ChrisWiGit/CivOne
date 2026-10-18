using System;

namespace CivOne.Services.Plugins
{
	/// <summary>
	/// Provides the shared <see cref="IPluginService"/> instance.
	/// The service is a singleton because plugin assemblies are process-wide state: loading the same
	/// plugin twice would create two incompatible copies of its types.
	/// </summary>
	internal static class PluginServiceFactory
	{
		private static IPluginService? _cached;

		/// <summary>
		/// Gets the shared plugin service, creating it on first use.
		/// </summary>
		/// <returns>
		/// The plugin service.
		/// </returns>
		public static IPluginService Create() => _cached ??= new PluginService();

		/// <summary>
		/// Replaces the shared instance, for tests that need a plugins directory of their own.
		/// </summary>
		/// <param name="pluginService">
		/// The service to use from now on.
		/// </param>
		/// <returns>
		/// The service that was installed.
		/// </returns>
		public static IPluginService Override(IPluginService pluginService)
		{
			ArgumentNullException.ThrowIfNull(pluginService);
			_cached = pluginService;
			return _cached;
		}

		/// <summary>
		/// Drops the shared instance so the next call to <see cref="Create"/> builds a new one.
		/// </summary>
		public static void Reset() => _cached = null;
	}
}
