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
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests
{
	public class SaveGamePathProviderTests : IDisposable
	{
		private const string LastUsedSaveGameDialogPathKey = "LastUsedSaveGameDialogPath";
		private readonly FakeRuntime _runtime;
		private readonly FakeSettings _settings;

		public SaveGamePathProviderTests()
		{
			_runtime = new FakeRuntime();
			string savesDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"), "saves");
			_settings = new FakeSettings(savesDirectory, Path.Combine(savesDirectory, "cos"));
		}

		[Fact]
		public void EnsureCurrentSaveDirectory_CreatesAndReturnsCosDirectory()
		{
			List<string> createdPaths = [];
			var provider = CreateProvider(
				createDirectory: createdPaths.Add,
				directoryExists: _ => true);

			string actual = provider.EnsureCurrentSaveDirectory();

			Assert.Equal(_settings.CosSavesDirectory, actual);
			Assert.Contains(_settings.CosSavesDirectory, createdPaths);
		}

		[Fact]
		public void GetInitialSaveFilePath_UsesLastUsedPathAndEnsuresDirectory()
		{
			List<string> createdPaths = [];
			string lastUsedPath = Path.Combine(Path.GetDirectoryName(_settings.CosSavesDirectory), "custom");
			_runtime.SetSetting(LastUsedSaveGameDialogPathKey, lastUsedPath);

			var provider = CreateProvider(
				createDirectory: createdPaths.Add,
				directoryExists: path => path == lastUsedPath);

			string actual = provider.EnsureInitialSaveFilePath();

			Assert.Equal(Path.Combine(lastUsedPath, "savegame.cos"), actual);
			Assert.Contains(lastUsedPath, createdPaths);
		}

		[Fact]
		public void GetInitialSaveFilePath_FallsBackToCosDirectoryWhenLastUsedMissing()
		{
			List<string> createdPaths = [];
			var provider = CreateProvider(
				createDirectory: createdPaths.Add,
				directoryExists: _ => false);

			string actual = provider.EnsureInitialSaveFilePath();

			Assert.Equal(Path.Combine(_settings.CosSavesDirectory, "savegame.cos"), actual);
			Assert.Contains(_settings.CosSavesDirectory, createdPaths);
		}

		[Fact]
		public void SetLastUsedSaveGamePath_PersistsDirectoryPartInRuntimeSetting()
		{
			var provider = CreateProvider(
				createDirectory: _ => { },
				directoryExists: _ => true);
			string filePath = Path.Combine(_settings.CosSavesDirectory, "new-save.cos");

			provider.SetLastUsedSaveGamePath(filePath);

			Assert.Equal(_settings.CosSavesDirectory, _runtime.GetSetting(LastUsedSaveGameDialogPathKey));
		}

		[Fact]
		public void EnsureAutoSaveDirectory_UsesLastUsedPathWhenAvailable()
		{
			List<string> createdPaths = [];
			string lastUsedPath = Path.Combine(_settings.SavesDirectory, "custom");
			_runtime.SetSetting(LastUsedSaveGameDialogPathKey, lastUsedPath);

			var provider = CreateProvider(
				createDirectory: createdPaths.Add,
				directoryExists: path => path == lastUsedPath);

			string actual = provider.EnsureAutoSaveDirectory();

			Assert.Equal(lastUsedPath, actual);
			Assert.Contains(lastUsedPath, createdPaths);
		}

		[Fact]
		public void EnsureAutoSaveDirectory_FallsBackToProfileSavesDirectory()
		{
			List<string> createdPaths = [];
			var provider = CreateProvider(
				createDirectory: createdPaths.Add,
				directoryExists: _ => false);

			string actual = provider.EnsureAutoSaveDirectory();

			Assert.Equal(_settings.SavesDirectory, actual);
			Assert.Contains(_settings.SavesDirectory, createdPaths);
		}

		public void Dispose()
		{
		}

		private SaveGamePathProvider CreateProvider(Action<string> createDirectory, Func<string, bool> directoryExists)
			=> new(_runtime, _settings, createDirectory, directoryExists);

		private sealed class FakeSettings(string savesDirectory, string cosSavesDirectory) : ISettings
		{
			public string SavesDirectory { get; } = savesDirectory;
			public string CosSavesDirectory { get; } = cosSavesDirectory;

			public string StorageDirectory => throw new NotImplementedException();

			public string CaptureDirectory => throw new NotImplementedException();

			public string DataDirectory => throw new NotImplementedException();

			public string PluginsDirectory => throw new NotImplementedException();

			public string SoundsDirectory => throw new NotImplementedException();
		}

		private sealed class FakeRuntime : IRuntime
		{
			private readonly Dictionary<string, string> _storedSettings = [];

			public event EventHandler Initialize { add { } remove { } }
			public event EventHandler Draw { add { } remove { } }
			public event UpdateEventHandler Update { add { } remove { } }
			public event KeyboardEventHandler KeyboardUp { add { } remove { } }
			public event KeyboardEventHandler KeyboardDown { add { } remove { } }
			public event ScreenEventHandler MouseUp { add { } remove { } }
			public event ScreenEventHandler MouseDown { add { } remove { } }
			public event ScreenEventHandler MouseMove { add { } remove { } }
			public Platform CurrentPlatform => Platform.Linux;
			public string StorageDirectory => Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			public RuntimeSettings Settings { get; } = new RuntimeSettings();
			public Bytemap[] Layers { get; set; }
			public Palette Palette { get; set; }
			public int CanvasWidth => 320;
			public int CanvasHeight => 200;
			public void SetCurrentCursor(MouseCursor cursor)
			{
			}
			public void SetCursor(IBitmap cursor)
			{
			}
			public void SetWindowTitle(string title)
			{
			}

			public string GetSetting(string key)
				=> _storedSettings.TryGetValue(key, out string value) ? value : null;

			public void SetSetting(string key, string value)
				=> _storedSettings[key] = value;

			public void Log(string text, params object[] parameters)
			{
			}

			public string BrowseFolder(string caption = "") => string.Empty;

			public string FileChooser(bool save, string title, string initialFileName, string filter) => string.Empty;

			public void PlaySound(string file)
			{
			}

			public void StopSound()
			{
			}

			public void Quit()
			{
			}
		}
	}
}
