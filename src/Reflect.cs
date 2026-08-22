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
using System.IO;
using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Concepts;
using CivOne.Governments;
using CivOne.Leaders;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	public interface IReflect
	{
		IEnumerable<IProduction> GetProduction();
	}

	/**
	 * Implementation of IReflect that is used
	 * by dto mappers to get access to the internal types. 
	*/
	public class GameReflect : IReflect
	{
		public IEnumerable<IProduction> GetProduction() => Reflect.GetProduction();
	}
		
	internal static class Reflect
	{
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		private static Plugin[]? _plugins;
		private static void LoadPlugins()
		{
			if (_plugins != null) return;

			_plugins = [.. LoadPluginFiles()];

			string[] disabledPlugins = [.. Settings.Instance.DisabledPlugins];
			if (_plugins.Any(x => !disabledPlugins.Contains(x?.Filename)))
			{
				Settings.Instance.DisabledPlugins = [.. _plugins.Where(x => !x.Enabled).Select(x => x.Filename)];
			}
		}

		/// <summary>
		/// Loads every plugin assembly from the plugins directory.
		/// Plugins are third-party code, so a single unreadable, corrupt or non-managed file must never
		/// prevent the remaining plugins - or the game itself - from starting.
		/// </summary>
		/// <returns>
		/// The plugins that could be loaded, in directory order.
		/// </returns>
		private static IEnumerable<Plugin> LoadPluginFiles()
		{
			string pluginsDirectory = Settings.Instance.PluginsDirectory;
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

		/// <summary>
		/// Decides whether an exception was caused by the plugin assembly rather than by the game.
		/// Plugin code is third-party code, so these failures are reported and skipped instead of
		/// taking down the caller.
		/// </summary>
		/// <param name="exception">
		/// The exception raised while reading, loading or instantiating a plugin.
		/// </param>
		/// <returns>
		/// True when the exception is a known plugin load failure.
		/// </returns>
		private static bool IsPluginLoadFailure(Exception exception) => exception switch
		{
			// Not a managed assembly, or built for an incompatible architecture.
			BadImageFormatException => true,
			// Unreadable, missing or locked file.
			IOException => true,
			UnauthorizedAccessException => true,
			// The assembly loads, but its types or their dependencies do not resolve.
			ReflectionTypeLoadException => true,
			TypeLoadException => true,
			// The plugin entry point cannot be constructed, or its constructor throws.
			MemberAccessException => true,
			TargetInvocationException => true,
			TypeInitializationException => true,
			// Raised by SafeCreateInstance when the entry point does not match IPlugin.
			ArgumentException => true,
			InvalidOperationException => true,
			_ => false
		};

		/// <summary>
		/// Loads a single plugin assembly, swallowing any failure caused by the plugin itself.
		/// </summary>
		/// <param name="filename">
		/// The full path of the plugin assembly.
		/// </param>
		/// <returns>
		/// The loaded plugin, or <c>null</c> when the file is not a valid plugin or could not be loaded.
		/// </returns>
		private static Plugin? TryLoadPlugin(string filename)
		{
			try
			{
				return Plugin.Load(filename);
			}
			catch (Exception exception) when (IsPluginLoadFailure(exception))
			{
				Log($"Plugins: failed to load {Path.GetFileName(filename)}: {exception.Message}");
				return null;
			}
		}

		internal static void LoadPlugin(string filename)
		{
			bool valid;
			try
			{
				valid = Plugin.Validate(filename);
			}
			catch (Exception exception) when (IsPluginLoadFailure(exception))
			{
				Log($"Plugins: failed to validate {Path.GetFileName(filename)}: {exception.Message}");
				return;
			}
			if (!valid) return;

			List<Plugin>? plugins = [.. _plugins ?? []];

			Plugin? plugin = TryLoadPlugin(filename);
			if (plugin == null) return;
			plugin.Enabled = true;

			plugins.RemoveAll(x => x?.Filename == null || x?.Filename == Path.GetFileName(filename));
			plugins.Add(plugin);

			_plugins = [.. plugins];

			ApplyPlugins();
		}

		/// <summary>
		/// The assemblies scanned for game content: the game itself plus every enabled plugin.
		/// Plugins are only included once they have been loaded; this property never triggers the load
		/// itself, so touching game content does not force plugin discovery.
		/// </summary>
		private static IEnumerable<Assembly> GetAssemblies
		{
			get
			{
				yield return typeof(Reflect).GetTypeInfo().Assembly;

				if (_plugins == null) yield break;
				foreach (Assembly assembly in _plugins.Where(x => x.Enabled).Select(x => x.Assembly))
				{
					yield return assembly;
				}
			}
		}

		/// <summary>
		/// Returns the types of an assembly, tolerating plugins whose types cannot all be resolved.
		/// </summary>
		/// <param name="assembly">
		/// The assembly to inspect.
		/// </param>
		/// <returns>
		/// Every type that could be loaded from the assembly.
		/// </returns>
		private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException exception)
			{
				Log($"Plugins: some types of {assembly.GetName().Name} could not be loaded: {exception.Message}");
				return exception.Types.OfType<Type>();
			}
		}

		public static T SafeCreateInstance<T>(Type type)
		{
			if (!typeof(T).IsAssignableFrom(type))
			{
				throw new ArgumentException(
					$"Type '{type.FullName}' is not assignable to '{typeof(T).FullName}'.",
					nameof(type));
			}

			return (T)(Activator.CreateInstance(type)
				?? throw new InvalidOperationException(
					$"Could not create instance of type '{type.FullName}'."));
		}
		
		/// <summary>
		/// True when a type can be created by <see cref="SafeCreateInstance{T}(Type)"/>.
		/// Plugin assemblies may contain concrete types without a public parameterless constructor;
		/// those are skipped instead of aborting the whole enumeration.
		/// </summary>
		/// <param name="type">
		/// The candidate type.
		/// </param>
		/// <returns>
		/// True when the type is a concrete class with a public parameterless constructor.
		/// </returns>
		private static bool IsInstantiable(Type type) =>
			type.GetTypeInfo().IsClass &&
			!type.GetTypeInfo().IsAbstract &&
			!type.GetTypeInfo().ContainsGenericParameters &&
			type.GetConstructor(Type.EmptyTypes) != null;

		private static IEnumerable<T> GetTypes<T>()
		{
			foreach (Assembly asm in GetAssemblies)
			foreach (Type type in GetLoadableTypes(asm).Where(t => typeof(T).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()) && IsInstantiable(t)))
			{
				yield return SafeCreateInstance<T>(type);
			}

			foreach (Assembly asm in GetAssemblies)
			foreach (Type type in GetLoadableTypes(asm).Where(t => (t is T) && IsInstantiable(t)))
			{
				yield return SafeCreateInstance<T>(type);
			}
		}
		
		internal static IEnumerable<IAdvance> GetAdvances() => GetTypes<IAdvance>().OrderBy(x => x.Id);

		internal static IEnumerable<ICivilization> GetCivilizations() => GetTypes<ICivilization>().OrderBy(x => (int)x.Id);
		
		internal static IEnumerable<IGovernment> GetGovernments() => GetTypes<IGovernment>().OrderBy(x => x.Id);
		
		internal static IEnumerable<IUnit> GetUnits() => GetTypes<IUnit>().OrderBy(x => (int)x.Type);
		
		internal static IEnumerable<IBuilding> GetBuildings() => GetTypes<IBuilding>().OrderBy(x => x.Id);
		
		internal static IEnumerable<IWonder> GetWonders() => GetTypes<IWonder>().OrderBy(x => x.Id);

		internal static IEnumerable<IProduction> GetProduction()
		{
			foreach (IProduction production in GetUnits())
				yield return production;
			foreach (IProduction production in GetBuildings())
				yield return production;
			foreach (IProduction production in GetWonders())
				yield return production;
		}
		
		internal static IEnumerable<IConcept> GetConcepts() => GetTypes<IConcept>();
		
		internal static IEnumerable<ICivilopedia> GetCivilopediaAll()
		{
			List<string> articles = [];
			foreach (ICivilopedia article in GetTypes<ICivilopedia>().OrderBy(a => (a is IConcept) ? 1 : 0))
			{
				if (articles.Contains(article.Name)) continue;
				articles.Add(article.Name);
				yield return article;
			}
		}
		
		internal static IEnumerable<ICivilopedia> GetCivilopediaAdvances() => GetTypes<IAdvance>();
		
		internal static IEnumerable<ICivilopedia> GetCivilopediaCityImprovements()
		{
			foreach (ICivilopedia civilopedia in GetTypes<IBuilding>())
				yield return civilopedia;
			foreach (ICivilopedia civilopedia in GetTypes<IWonder>())
				yield return civilopedia;
		}
		
		internal static IEnumerable<ICivilopedia> GetCivilopediaUnits() => GetTypes<IUnit>();
		
		internal static IEnumerable<ICivilopedia> GetCivilopediaTerrainTypes() => GetTypes<ITile>();

		internal static void ApplyPlugins()
		{
			Common.ResetContentCaches();
			BaseCivilization.LoadModifications();
			BaseLeader.LoadModifications();
			BaseUnit.LoadModifications();
		}

		internal static IEnumerable<Plugin> Plugins()
		{
			if (_plugins == null)
			{
				LoadPlugins();
				ApplyPlugins();
			}
			return _plugins ?? [];
		}

		private static IEnumerable<Type> PluginModifications
		{
			get
			{
				if (_plugins == null) yield break;
				foreach (Assembly assembly in _plugins.Where(x => x.Enabled).Select(x => x.Assembly))
				// Modification is an abstract base class, not an interface, so the candidates have to be
				// matched by assignability - GetInterfaces() never contains it.
				foreach (Type type in GetLoadableTypes(assembly).Where(x => typeof(Modification).IsAssignableFrom(x) && IsInstantiable(x)))
				{
					yield return type;
				}
			}
		}

		private static object[] ParseParameters(params object[] parameters)
		{
			List<object> output = [];
			foreach (object parameter in parameters)
			{
				switch (parameter)
				{
					case string stringParameter:
						output.Add(stringParameter);
						break;
					case int intParameter:
						output.Add(intParameter);
						break;
					default:
						output.Add(parameter);
						break;
				}
			}
			return [.. output];
		}

		internal static IEnumerable<T> GetModifications<T>()
		{
			foreach (Type type in PluginModifications.Where(x => x.IsSubclassOf(typeof(T))))
			{
				yield return SafeCreateInstance<T>(type);
			}
		}
		
		internal static void PreloadCivilopedia()
		{
			Log("Civilopedia: Preloading articles...");
			foreach (ICivilopedia _ in GetCivilopediaAll());
			Log("Civilopedia: Preloading done!");
		}
	}
}