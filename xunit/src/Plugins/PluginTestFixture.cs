using System;
using System.IO;
using System.Linq;
using CivOne.Services.Plugins;

namespace CivOne.UnitTests.Plugins
{
	/// <summary>
	/// Sets up an isolated plugins directory for one test and installs a plugin service that uses it.
	/// The real plugins directory is never touched.
	/// </summary>
	internal sealed class PluginTestFixture : IDisposable
	{
		private readonly MockRuntime _runtime;
		private readonly string _directory;
		private bool _disposed;

		/// <summary>
		/// Creates the fixture with an empty temporary plugins directory.
		/// </summary>
		public PluginTestFixture()
		{
			_runtime = new MockRuntime(new RuntimeSettings());
			_directory = Path.Combine(Path.GetTempPath(), "civone-plugin-tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_directory);

			Settings.Instance.DisabledPlugins = [];
			Service = (PluginService)PluginServiceFactory.Override(new PluginService(new StubSettings(_directory)));
		}

		/// <summary>
		/// The plugin service under test.
		/// </summary>
		public PluginService Service { get; private set; }

		/// <summary>
		/// The temporary plugins directory.
		/// </summary>
		public string PluginsDirectory => _directory;

		/// <summary>
		/// Copies the built test plugin into the plugins directory.
		/// </summary>
		/// <returns>
		/// The full path of the copied plugin.
		/// </returns>
		public string InstallTestPlugin()
		{
			string source = Path.Combine(AppContext.BaseDirectory, "testplugin", "CivOne.TestPlugin.dll");
			if (!File.Exists(source))
			{
				throw new FileNotFoundException(
					$"The test plugin was not built. Expected it at '{source}'. Build xunit/TestPlugin/CivOne.TestPlugin.csproj.",
					source);
			}

			string destination = Path.Combine(_directory, "CivOne.TestPlugin.dll");
			File.Copy(source, destination, overwrite: true);
			return destination;
		}

		/// <summary>
		/// Writes a file that has a .dll extension but is not a managed assembly.
		/// </summary>
		/// <param name="fileName">
		/// The file name to create inside the plugins directory.
		/// </param>
		public void InstallBrokenPlugin(string fileName)
		{
			File.WriteAllText(Path.Combine(_directory, fileName), "this is not an assembly");
		}

		/// <summary>
		/// Deletes the plugins directory, so a load has to cope with a missing directory.
		/// </summary>
		public void RemoveDirectory()
		{
			System.IO.Directory.Delete(_directory, recursive: true);
		}

		/// <summary>
		/// Replaces the service with a fresh one that reads the same directory again.
		/// </summary>
		public void RecreateService()
		{
			Service = (PluginService)PluginServiceFactory.Override(new PluginService(new StubSettings(_directory)));
		}

		/// <summary>
		/// Reads a public static field from a type inside the loaded test plugin.
		/// The test assembly cannot reference the plugin directly - that would load it into the
		/// default context and defeat the isolation being tested.
		/// </summary>
		/// <param name="typeName">
		/// The full name of the type inside the plugin assembly.
		/// </param>
		/// <param name="fieldName">
		/// The name of the public static field.
		/// </param>
		/// <returns>
		/// The current field value.
		/// </returns>
		public object? ReadPluginStatic(string typeName, string fieldName)
		{
			Plugin plugin = Service.Plugins.First(x => x.Filename == "CivOne.TestPlugin.dll");
			Type type = plugin.Assembly?.GetType(typeName)
				?? throw new InvalidOperationException($"Type '{typeName}' not found in the test plugin.");
			return type.GetField(fieldName)?.GetValue(null);
		}

		/// <summary>
		/// Restores the global plugin service and removes the temporary directory.
		/// </summary>
		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			PluginServiceFactory.Reset();
			Settings.Instance.DisabledPlugins = [];

			try
			{
				if (System.IO.Directory.Exists(_directory))
				{
					System.IO.Directory.Delete(_directory, recursive: true);
				}
			}
			catch (IOException)
			{
				// A plugin assembly that has not been collected yet can keep the file locked on
				// Windows. Leaving a temp directory behind must not fail the test.
			}

			_runtime.Dispose();
			RuntimeHandler.Wipe();
		}

		/// <summary>
		/// Minimal <see cref="ISettings"/> that only answers the plugins directory.
		/// Any other member would indicate the service reached beyond what it needs.
		/// </summary>
		private sealed class StubSettings(string pluginsDirectory) : ISettings
		{
			public string PluginsDirectory { get; } = pluginsDirectory;

			public string StorageDirectory => throw new NotImplementedException();

			public string CaptureDirectory => throw new NotImplementedException();

			public string DataDirectory => throw new NotImplementedException();

			public string SavesDirectory => throw new NotImplementedException();

			public string CosSavesDirectory => throw new NotImplementedException();

			public string MapsDirectory => throw new NotImplementedException();

			public string PicturesDirectory => throw new NotImplementedException();

			public string SoundsDirectory => throw new NotImplementedException();

			public bool RevealWorld => throw new NotImplementedException();
		}
	}
}
