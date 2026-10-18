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
using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Concepts;
using CivOne.Governments;
using CivOne.Services.Plugins;
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

		private static IPluginService PluginService => PluginServiceFactory.Create();

		/// <summary>
		/// The assemblies scanned for game content: the game itself plus every enabled plugin.
		/// Plugins only appear once they have been loaded; this property never triggers the load
		/// itself, so touching game content does not force plugin discovery.
		/// </summary>
		private static IEnumerable<Assembly> GetAssemblies
		{
			get
			{
				yield return typeof(Reflect).GetTypeInfo().Assembly;

				foreach (Assembly assembly in PluginService.EnabledAssemblies)
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
		internal static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
		{
			ArgumentNullException.ThrowIfNull(assembly);

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
			ArgumentNullException.ThrowIfNull(type);

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
		internal static bool IsInstantiable(Type type) =>
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

		internal static void LoadPlugin(string filename) => PluginService.LoadPlugin(filename);

		internal static void ApplyPlugins() => PluginService.ApplyPlugins();

		internal static IEnumerable<Plugin> Plugins() => PluginService.Plugins;

		/// <summary>
		/// The concrete modification types contributed by the enabled plugins.
		/// </summary>
		private static IEnumerable<Type> PluginModifications
		{
			get
			{
				foreach (Assembly assembly in PluginService.EnabledAssemblies)
				// Modification is an abstract base class, not an interface, so the candidates have to
				// be matched by assignability - GetInterfaces() never contains it.
				foreach (Type type in GetLoadableTypes(assembly).Where(x => typeof(Modification).IsAssignableFrom(x) && IsInstantiable(x)))
				{
					yield return type;
				}
			}
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
