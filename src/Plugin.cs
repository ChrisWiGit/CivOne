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

namespace CivOne
{
	internal class Plugin
	{
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);
		private static Settings Settings => Settings.Instance;
		private static int _seed;
		
		private readonly IPlugin _plugin;
		private readonly string _filePath;
		private readonly string _fileName;

		public bool Deleted => !File.Exists(_filePath);

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

				Reflect.ApplyPlugins();
			}
		}

		public int Id { get; }
		public Assembly Assembly { get; }
		public string Name => _plugin.Name;
		public string Filename => Path.GetFileName(_filePath);
		public string Author => _plugin.Author;
		public string Version => _plugin.Version;

		public static bool Validate(string filePath)
		{
			using MemoryStream ms = new(File.ReadAllBytes(filePath));
			Assembly assembly = Assembly.Load(ms.ToArray());
			Type[] types = [.. assembly.GetTypes().Where(x => x.Namespace == "CivOne" && x.Name == "Plugin" && x.GetInterfaces().Contains(typeof(IPlugin)))];
			return types.Length == 1;
		}

		public static Plugin? Load(string filePath)
		{
			using MemoryStream ms = new(File.ReadAllBytes(filePath));
			Assembly assembly = Assembly.Load(ms.ToArray());
			Type[] types = [.. assembly.GetTypes().Where(x => x.Namespace == "CivOne" && x.Name == "Plugin" && x.GetInterfaces().Contains(typeof(IPlugin)))];
			if (types.Length != 1)
			{
				Log($" - Invalid plugin format: {filePath}");
				return null;
			}

			IPlugin plugin = Reflect.SafeCreateInstance<IPlugin>(types[0]);

			return new Plugin(filePath, plugin, assembly);
		}

		public void Delete()
		{
			File.Delete(_filePath);
			Reflect.ApplyPlugins();
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

		private Plugin(string filePath, IPlugin plugin, Assembly assembly)
		{
			_plugin = plugin;
			Id = _seed++;
			Assembly = assembly;
			_filePath = filePath;
			_fileName = Path.GetFileName(filePath);
		}
	}
}