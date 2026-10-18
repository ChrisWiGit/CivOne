using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CivOne.Civilizations;
using CivOne.Leaders;
using CivOne.Units;

namespace CivOne.Services.Plugins
{
	/// <summary>
	/// Default <see cref="IPluginService"/>: scans the plugins directory, keeps each plugin in its
	/// own collectible load context and rebuilds the derived state whenever plugins change.
	/// </summary>
	internal sealed class PluginService(
		ISettings? settings = null,
		ITranslationService? translationService = null) : IPluginService
	{
		private readonly ISettings? _settings = settings;
		private readonly ITranslationService? _translationService = translationService;
		private readonly PluginAiRegistrationDelegate _aiRegistration = new();

		private Plugin[]? _plugins;
		private IPluginAiProvider[] _aiProviders = [];
		private IPluginMapGeneratorProvider[] _mapGeneratorProviders = [];
		private IPluginImageProvider[] _imageProviders = [];

		// Resolved lazily, never as an eager default: constructing this service must not force the
		// static singletons to initialize, so a unit test can create it without a running engine.
		private ISettings SettingsInstance => _settings ?? Settings.Instance;
		private ITranslationService TranslationService => _translationService ?? TranslationServiceFactory.CreateDefault();

		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		public IReadOnlyList<Plugin> Plugins
		{
			get
			{
				if (_plugins == null)
				{
					LoadPlugins();
					ApplyPlugins();
				}
				return _plugins ?? [];
			}
		}

		public IEnumerable<Assembly> EnabledAssemblies
		{
			get
			{
				if (_plugins == null) yield break;
				foreach (Plugin plugin in _plugins)
				{
					if (!plugin.Enabled) continue;
					if (plugin.Assembly is not Assembly assembly) continue;
					yield return assembly;
				}
			}
		}

		public IReadOnlyList<IPluginAiProvider> AiProviders => _aiProviders;

		public IReadOnlyList<IPluginMapGeneratorProvider> MapGeneratorProviders => _mapGeneratorProviders;

		public IReadOnlyList<IPluginImageProvider> ImageProviders => _imageProviders;

		public void LoadPlugin(string filePath)
		{
			ArgumentNullException.ThrowIfNull(filePath);

			Plugin? plugin = TryLoadPlugin(filePath);
			if (plugin == null) return;

			List<Plugin> plugins = [.. _plugins ?? []];

			string fileName = Path.GetFileName(filePath);
			foreach (Plugin replaced in plugins.Where(x => x.Filename == fileName))
			{
				replaced.Unload();
			}
			plugins.RemoveAll(x => x.Filename == fileName);
			plugins.Add(plugin);

			_plugins = [.. plugins];
			plugin.Enabled = true;

			ApplyPlugins();
		}

		public void ApplyPlugins()
		{
			RefreshCapabilityProviders();

			Common.ResetContentCaches();
			BaseCivilization.LoadModifications();
			BaseLeader.LoadModifications();
			BaseUnit.LoadModifications();

			_aiRegistration.Refresh(_aiProviders, TranslationService);
		}

		public void OnPluginStateChanged(Plugin plugin)
		{
			ArgumentNullException.ThrowIfNull(plugin);

			if (plugin.Enabled && plugin.IsUnloaded)
			{
				TryReload(plugin);
			}

			// Rebuild first, then unload: the rebuild drops the modification entries and cached
			// content instances that would otherwise keep the plugin's load context alive.
			ApplyPlugins();

			if (!plugin.Enabled)
			{
				plugin.Unload();
			}
		}

		private void LoadPlugins()
		{
			if (_plugins != null) return;

			_plugins = [.. LoadPluginFiles()];

			string[] disabledPlugins = [.. Settings.Instance.DisabledPlugins];
			if (_plugins.Any(x => !disabledPlugins.Contains(x.Filename)))
			{
				Settings.Instance.DisabledPlugins = [.. _plugins.Where(x => !x.Enabled).Select(x => x.Filename)];
			}
		}

		/// <summary>
		/// Loads every plugin assembly from the plugins directory.
		/// Plugins are third-party code, so a single unreadable, corrupt or non-managed file must
		/// never prevent the remaining plugins - or the game itself - from starting.
		/// </summary>
		/// <returns>
		/// The plugins that could be loaded, in directory order.
		/// </returns>
		private IEnumerable<Plugin> LoadPluginFiles()
		{
			string pluginsDirectory = SettingsInstance.PluginsDirectory;
			if (!Directory.Exists(pluginsDirectory))
			{
				Log($"Plugins: directory not found, skipping: {pluginsDirectory}");
				yield break;
			}

			string[] filenames;
			try
			{
				filenames = Directory.GetFiles(pluginsDirectory, "*.dll");
			}
			catch (IOException exception)
			{
				Log($"Plugins: could not read directory {pluginsDirectory}: {exception.Message}");
				yield break;
			}
			catch (UnauthorizedAccessException exception)
			{
				Log($"Plugins: could not read directory {pluginsDirectory}: {exception.Message}");
				yield break;
			}

			foreach (string filename in filenames)
			{
				Plugin? plugin = TryLoadPlugin(filename);
				if (plugin != null) yield return plugin;
			}
		}

		private static Plugin? TryLoadPlugin(string filePath)
		{
			try
			{
				return Plugin.Load(filePath);
			}
			catch (Exception exception) when (PluginFailure.IsLoadFailure(exception))
			{
				Log($"Plugins: failed to load {Path.GetFileName(filePath)}: {exception.Message}");
				return null;
			}
		}

		private static void TryReload(Plugin plugin)
		{
			try
			{
				plugin.Reload();
			}
			catch (Exception exception) when (PluginFailure.IsLoadFailure(exception))
			{
				Log($"Plugins: failed to reload {plugin.Filename}: {exception.Message}");
			}
		}

		private void RefreshCapabilityProviders()
		{
			_aiProviders = [.. CreateProviders<IPluginAiProvider>()];
			_mapGeneratorProviders = [.. CreateProviders<IPluginMapGeneratorProvider>()];
			_imageProviders = [.. CreateProviders<IPluginImageProvider>()];
		}

		/// <summary>
		/// Instantiates every type of every enabled plugin that implements a capability interface.
		/// The whole assembly is scanned, not just the plugin entry point, so a plugin may either
		/// bundle its capabilities into the entry point or keep them in separate types.
		/// </summary>
		/// <typeparam name="T">
		/// The capability interface to look for.
		/// </typeparam>
		/// <returns>
		/// One instance per implementing type.
		/// </returns>
		private IEnumerable<T> CreateProviders<T>()
		{
			foreach (Assembly assembly in EnabledAssemblies)
			{
				foreach (Type type in Reflect.GetLoadableTypes(assembly).Where(x => typeof(T).IsAssignableFrom(x) && Reflect.IsInstantiable(x)))
				{
					T? provider = TryCreateProvider<T>(type);
					if (provider != null) yield return provider;
				}
			}
		}

		private static T? TryCreateProvider<T>(Type type)
		{
			try
			{
				return Reflect.SafeCreateInstance<T>(type);
			}
			catch (Exception exception) when (PluginFailure.IsLoadFailure(exception))
			{
				Log($"Plugins: failed to create {typeof(T).Name} from {type.FullName}: {exception.Message}");
				return default;
			}
		}
	}
}
