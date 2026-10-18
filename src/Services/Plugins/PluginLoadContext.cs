using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace CivOne.Services.Plugins
{
	/// <summary>
	/// A collectible load context for a single plugin assembly.
	/// Each plugin gets its own context so it can be unloaded again when the user disables or
	/// deletes it; the default context never releases an assembly for the lifetime of the process.
	/// </summary>
	internal sealed class PluginLoadContext : AssemblyLoadContext
	{
		private readonly AssemblyDependencyResolver? _resolver;

		/// <summary>
		/// Creates a collectible context for one plugin file.
		/// </summary>
		/// <param name="pluginPath">
		/// The full path of the plugin assembly.
		/// It is used to resolve the plugin's own dependencies, not to load the plugin itself.
		/// </param>
		public PluginLoadContext(string pluginPath)
			: base(name: $"Plugin:{Path.GetFileName(pluginPath)}", isCollectible: true)
		{
			ArgumentNullException.ThrowIfNull(pluginPath);

			// The resolver reads the plugin's .deps.json. Plugins built without one are the normal
			// case, so a missing file is not an error - dependency resolution then falls back to the
			// default context, which is where the game assemblies live anyway.
			_resolver = TryCreateResolver(pluginPath);
		}

		/// <summary>
		/// Loads a plugin assembly from its raw bytes.
		/// Loading from bytes rather than from a path is deliberate: it leaves no file handle open,
		/// which is what allows the plugin file to be overwritten or deleted while the game runs.
		/// </summary>
		/// <param name="assemblyBytes">
		/// The complete contents of the plugin assembly file.
		/// </param>
		/// <returns>
		/// The loaded assembly.
		/// </returns>
		public Assembly LoadFromBytes(byte[] assemblyBytes)
		{
			ArgumentNullException.ThrowIfNull(assemblyBytes);

			using MemoryStream stream = new(assemblyBytes, writable: false);
			return LoadFromStream(stream);
		}

		/// <summary>
		/// Resolves an assembly the plugin depends on.
		/// </summary>
		/// <param name="assemblyName">
		/// The requested assembly.
		/// </param>
		/// <returns>
		/// The assembly loaded into this context, or <see langword="null"/> to let the default
		/// context serve the request.
		/// </returns>
		protected override Assembly? Load(AssemblyName assemblyName)
		{
			// Shared contracts must come from the default context. If this context loaded its own
			// copy of CivOne.API, a plugin's type would no longer be assignable to IPlugin,
			// Modification or IUnit - every check would silently fail and the plugin would appear
			// to do nothing at all.
			if (IsLoadedInDefaultContext(assemblyName))
			{
				return null;
			}

			string? path = _resolver?.ResolveAssemblyToPath(assemblyName);
			return path == null ? null : LoadFromAssemblyPath(path);
		}

		/// <summary>
		/// Resolves an unmanaged library the plugin depends on.
		/// </summary>
		/// <param name="unmanagedDllName">
		/// The requested native library.
		/// </param>
		/// <returns>
		/// A handle to the loaded library, or <see cref="IntPtr.Zero"/> to use the default probing.
		/// </returns>
		protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
		{
			string? path = _resolver?.ResolveUnmanagedDllToPath(unmanagedDllName);
			return path == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
		}

		private static AssemblyDependencyResolver? TryCreateResolver(string pluginPath)
		{
			try
			{
				return new AssemblyDependencyResolver(pluginPath);
			}
			catch (ArgumentException)
			{
				return null;
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		private static bool IsLoadedInDefaultContext(AssemblyName assemblyName) =>
			Default.Assemblies.Any(x => string.Equals(x.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
	}
}
