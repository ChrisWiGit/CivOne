// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using CivOne.Services.Plugins;

namespace CivOne
{
	internal class Plugin
	{
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);
		private static Settings Settings => Settings.Instance;
		private static int _seed;

		private readonly string _filePath;
		private readonly string _fileName;

		private Assembly? _assembly;
		private PluginLoadContext? _loadContext;

		public bool Deleted => !File.Exists(_filePath);

		/// <summary>
		/// True once <see cref="Unload"/> has run.
		/// An unloaded plugin keeps its metadata for the settings menu, but no longer contributes
		/// any types.
		/// </summary>
		public bool IsUnloaded => _loadContext == null;

		public bool Enabled
		{
			get => !Deleted && !Settings.DisabledPlugins.Any(x => x == _fileName);
			set
			{
				if (Deleted) return;
				if (value)
					Settings.DisabledPlugins = [.. Settings.DisabledPlugins.Where(x => x != _fileName)];
				else
					Settings.DisabledPlugins = [.. Settings.DisabledPlugins.Concat([_fileName]).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()];

				PluginServiceFactory.Create().OnPluginStateChanged(this);
			}
		}

		public int Id { get; }

		/// <summary>
		/// The loaded plugin assembly, or <see langword="null"/> once the plugin has been unloaded.
		/// </summary>
		public Assembly? Assembly => _assembly;

		public string Name { get; }
		public string Filename => Path.GetFileName(_filePath);
		public string Author { get; }
		public string Version { get; }

		/// <summary>
		/// The full path of the plugin file on disk.
		/// </summary>
		public string FilePath => _filePath;

		/// <summary>
		/// The load context backing this plugin, exposed so a test can hold a weak reference to it
		/// and verify that it is collected after unloading.
		/// </summary>
		internal PluginLoadContext? LoadContextForTests => _loadContext;

		/// <summary>
		/// Checks whether a file is a loadable plugin without keeping it in the process.
		/// Used before copying a candidate file into the plugins directory, which is a different
		/// file from the one that is loaded afterwards.
		/// </summary>
		/// <param name="filePath">
		/// The full path of the candidate file.
		/// </param>
		/// <returns>
		/// True when the file contains exactly one valid plugin entry point.
		/// </returns>
		public static bool Validate(string filePath)
		{
			ArgumentNullException.ThrowIfNull(filePath);

			PluginLoadContext context = new(filePath);
			try
			{
				Assembly assembly = context.LoadFromBytes(File.ReadAllBytes(filePath));
				return FindEntryPoints(assembly).Length == 1;
			}
			finally
			{
				// A validation must not pin the assembly for the rest of the process; the file is
				// about to be copied and then loaded again from its destination path.
				context.Unload();
			}
		}

		/// <summary>
		/// Loads a plugin assembly into its own collectible load context.
		/// </summary>
		/// <param name="filePath">
		/// The full path of the plugin assembly.
		/// </param>
		/// <returns>
		/// The loaded plugin, or <see langword="null"/> when the file has no valid entry point.
		/// </returns>
		public static Plugin? Load(string filePath)
		{
			ArgumentNullException.ThrowIfNull(filePath);

			PluginLoadContext context = new(filePath);
			Assembly assembly = context.LoadFromBytes(File.ReadAllBytes(filePath));

			Type[] entryPoints = FindEntryPoints(assembly);
			if (entryPoints.Length != 1)
			{
				Log($" - Invalid plugin format: {filePath}");
				context.Unload();
				return null;
			}

			IPlugin plugin = Reflect.SafeCreateInstance<IPlugin>(entryPoints[0]);

			return new Plugin(filePath, plugin, assembly, context);
		}

		/// <summary>
		/// Releases the plugin assembly and requests unloading of its load context.
		/// Collection is best effort: the runtime frees the context only once no instance created
		/// from it is referenced any more. A running game holding plugin units or civilizations
		/// therefore keeps the assembly alive until that game ends.
		/// </summary>
		public void Unload()
		{
			if (_loadContext == null) return;

			_assembly = null;

			PluginLoadContext context = _loadContext;
			_loadContext = null;
			context.Unload();
		}

		/// <summary>
		/// Reloads the plugin assembly after it has been unloaded.
		/// </summary>
		/// <returns>
		/// True when the assembly is available again.
		/// </returns>
		public bool Reload()
		{
			if (_loadContext != null) return true;
			if (Deleted) return false;

			PluginLoadContext context = new(_filePath);
			Assembly assembly = context.LoadFromBytes(File.ReadAllBytes(_filePath));

			Type[] entryPoints = FindEntryPoints(assembly);
			if (entryPoints.Length != 1)
			{
				context.Unload();
				return false;
			}

			// The entry point is instantiated to confirm it is constructible; the metadata was
			// already captured at first load and does not change.
			Reflect.SafeCreateInstance<IPlugin>(entryPoints[0]);
			_assembly = assembly;
			_loadContext = context;
			return true;
		}

		public void Delete()
		{
			File.Delete(_filePath);
			PluginServiceFactory.Create().OnPluginStateChanged(this);
		}

		[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "We want to return the value kind in lowercase for consistency.")]
		public override string ToString()
		{
			StringBuilder output = new(Name);
			if (Deleted)
			{
				output.Append(" (deleted)"); // do not translate
			}
			else if (!Enabled)
			{
				output.Append(CultureInfo.InvariantCulture, $" ({false.EnabledDisabled().ToLowerInvariant()})");
			}
			return output.ToString();
		}

		private static Type[] FindEntryPoints(Assembly assembly) =>
			[.. assembly.GetTypes().Where(x => x.Namespace == "CivOne" && x.Name == "Plugin" && x.GetInterfaces().Contains(typeof(IPlugin)))];

		private Plugin(string filePath, IPlugin plugin, Assembly assembly, PluginLoadContext loadContext)
		{
			Id = Interlocked.Increment(ref _seed) - 1;
			_assembly = assembly;
			_loadContext = loadContext;
			_filePath = filePath;
			_fileName = Path.GetFileName(filePath);

			// Snapshot the metadata: it is shown in the settings menu even after the plugin has been
			// unloaded, when the IPlugin instance is gone.
			Name = plugin.Name;
			Author = plugin.Author;
			Version = plugin.Version;
		}
	}
}
