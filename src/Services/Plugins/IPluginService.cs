using System.Collections.Generic;
using System.Reflection;

namespace CivOne.Services.Plugins
{
	/// <summary>
	/// Loads plugin assemblies, tracks their enabled state and exposes the capabilities they offer.
	/// </summary>
	internal interface IPluginService
	{
		/// <summary>
		/// All plugins found in the plugins directory, loading them on first access.
		/// </summary>
		IReadOnlyList<Plugin> Plugins { get; }

		/// <summary>
		/// The assemblies of all currently enabled plugins.
		/// Reading this never triggers plugin discovery: before the first explicit load it is empty.
		/// That keeps ordinary game content lookups from pulling in settings and the plugins
		/// directory as a side effect.
		/// </summary>
		IEnumerable<Assembly> EnabledAssemblies { get; }

		/// <summary>
		/// The AI providers offered by the enabled plugins.
		/// Their descriptors are registered in the AI selection menu.
		/// </summary>
		IReadOnlyList<IPluginAiProvider> AiProviders { get; }

		/// <summary>
		/// The map generator providers offered by the enabled plugins.
		/// </summary>
		/// <remarks>
		/// NOT YET CONSUMED. These providers are discovered and instantiated, but nothing calls them:
		/// world generation is hard-wired in <c>Map.Generate</c>. See <c>TODO.md</c>.
		/// </remarks>
		IReadOnlyList<IPluginMapGeneratorProvider> MapGeneratorProviders { get; }

		/// <summary>
		/// The image pack providers offered by the enabled plugins.
		/// </summary>
		/// <remarks>
		/// NOT YET CONSUMED. These providers are discovered and instantiated, but nothing calls them:
		/// sprite lookup in <c>Resources</c> has no override step. See <c>TODO.md</c>.
		/// </remarks>
		IReadOnlyList<IPluginImageProvider> ImageProviders { get; }

		/// <summary>
		/// Loads a single plugin file and enables it, replacing an already loaded plugin of the same
		/// file name.
		/// </summary>
		/// <param name="filePath">
		/// The full path of the plugin assembly inside the plugins directory.
		/// </param>
		void LoadPlugin(string filePath);

		/// <summary>
		/// Rebuilds everything derived from the enabled plugins: content caches, modifications and
		/// the registered plugin AIs.
		/// </summary>
		void ApplyPlugins();

		/// <summary>
		/// Reacts to a plugin being enabled, disabled or deleted.
		/// Rebuilds the derived state and unloads the assembly when the plugin is no longer active.
		/// </summary>
		/// <param name="plugin">
		/// The plugin whose state changed.
		/// </param>
		void OnPluginStateChanged(Plugin plugin);
	}
}
