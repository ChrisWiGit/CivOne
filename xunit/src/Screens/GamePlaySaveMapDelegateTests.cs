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
using CivOne.Services.Maps;
using CivOne.Tasks;
using CivOne.Units;
using Xunit;

namespace CivOne.Screens
{
	/// <summary>
	/// Tests for <see cref="GamePlay.GamePlaySaveMapDelegate.OnSaveMapMenuAction"/>, the terrain-editor
	/// "Save Map" orchestration split out of <c>GamePlayTerrainEditorDelegate</c>. Newly testable because
	/// every dependency (settings, file dialog, directory creation, map writer, task queue, error message)
	/// is now injected as an interface instead of being reached through static singletons.
	/// </summary>
	/// <remarks>
	/// The exception/error-message branch (<c>IMessageService.Error</c>) is intentionally not exercised
	/// here: <c>Message.Error</c> can only be produced by the real static <c>Message</c> factory (its
	/// constructor is private), which constructs a real <c>PopupMessage</c> screen that renders text via
	/// <c>Resources</c> - font/resource loading well outside a plain unit test's scope. The fake below
	/// throws if <c>IMessageService</c> is invoked, which itself verifies the happy/cancel paths never
	/// reach the catch block.
	/// </remarks>
	public class GamePlaySaveMapDelegateTests
	{
		private readonly FakeSettings _settings = new();
		private readonly FakeRuntime _runtime = new();
		private readonly FakeMapSaveService _mapSaveService = new();
		private readonly FakeGameTaskCommandQueue _gameTaskCommandQueue = new();
		private readonly FakeDirectoryService _directoryService = new();

		private GamePlay.GamePlaySaveMapDelegate CreateTestee()
			=> new(
				new TranslationIdentityService(),
				_settings,
				_runtime,
				_mapSaveService,
				_gameTaskCommandQueue,
				new ThrowingMessageService(),
				_directoryService);

		[Fact]
		public void OnSaveMapMenuActionCreatesMapsDirectoryBeforeShowingFileChooser()
		{
			_runtime.FileChooserResult = () => null;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			Assert.Contains(_settings.MapsDirectory, _directoryService.CreateDirectoryCalls);
		}

		[Fact]
		public void OnSaveMapMenuActionDoesNothingWhenFileChooserIsCancelled()
		{
			_runtime.FileChooserResult = () => null;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			Assert.Empty(_mapSaveService.SaveCivOneMapCalls);
			Assert.Equal(0, _gameTaskCommandQueue.EnqueueCallCount);
		}

		[Fact]
		public void OnSaveMapMenuActionNormalizesExtensionToComap()
		{
			string selected = Path.Combine(_settings.MapsDirectory, "mymap.txt");
			_runtime.FileChooserResult = () => selected;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			string savedPath = Assert.Single(_mapSaveService.SaveCivOneMapCalls);
			Assert.Equal(".comap", Path.GetExtension(savedPath));
			Assert.Equal("mymap", Path.GetFileNameWithoutExtension(savedPath));
		}

		[Fact]
		public void OnSaveMapMenuActionSavesSelectedFileAndDoesNotEnqueueErrorOnSuccess()
		{
			string selected = Path.Combine(_settings.MapsDirectory, "earth.comap");
			_runtime.FileChooserResult = () => selected;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			Assert.Single(_mapSaveService.SaveCivOneMapCalls);
			Assert.Equal(0, _gameTaskCommandQueue.EnqueueCallCount);
		}

		[Fact]
		public void OnSaveMapMenuActionSavesLegacyMapWhenExtensionIsMapAndCompatible()
		{
			string selected = Path.Combine(_settings.MapsDirectory, "earth.map");
			_runtime.FileChooserResult = () => selected;
			_mapSaveService.LegacyCompatibility = LegacyMapSaveCompatibilityResult.Compatible;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			string savedPath = Assert.Single(_mapSaveService.SaveLegacyMapCalls);
			Assert.Equal(".map", Path.GetExtension(savedPath));
			Assert.Empty(_mapSaveService.SaveCivOneMapCalls);
			Assert.Equal(0, _gameTaskCommandQueue.EnqueueCallCount);
		}

		[Fact]
		public void OnSaveMapMenuActionTreatsUppercaseMapExtensionAsLegacy()
		{
			string selected = Path.Combine(_settings.MapsDirectory, "earth.MAP");
			_runtime.FileChooserResult = () => selected;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			Assert.Single(_mapSaveService.SaveLegacyMapCalls);
			Assert.Empty(_mapSaveService.SaveCivOneMapCalls);
		}

		[Fact]
		public void OnSaveMapMenuActionFallsBackToComapWhenIncompatibleButUserTypesMapExtensionManually()
		{
			// The *.map filter entry is hidden in this case (see OnSaveMapMenuActionOmitsLegacyMapFilterWhenIncompatible),
			// but a native save dialog still lets the user type any extension by hand - simulate that override.
			string selected = Path.Combine(_settings.MapsDirectory, "earth.map");
			_runtime.FileChooserResult = () => selected;
			_mapSaveService.LegacyCompatibility = new LegacyMapSaveCompatibilityResult(
				false, "The legacy Civ1 map format cannot store custom start positions.");
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			string savedPath = Assert.Single(_mapSaveService.SaveCivOneMapCalls);
			Assert.Equal(".comap", Path.GetExtension(savedPath));
			Assert.Empty(_mapSaveService.SaveLegacyMapCalls);
			Assert.Equal(0, _gameTaskCommandQueue.EnqueueCallCount);
		}

		[Fact]
		public void OnSaveMapMenuActionChecksLegacyCompatibilityOnceBeforeShowingDialog()
		{
			string selected = Path.Combine(_settings.MapsDirectory, "earth.comap");
			_runtime.FileChooserResult = () => selected;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			Assert.Equal(1, _mapSaveService.GetLegacyMapCompatibilityCallCount);
			Assert.Single(_mapSaveService.SaveCivOneMapCalls);
		}

		[Fact]
		public void OnSaveMapMenuActionOmitsLegacyMapFilterWhenIncompatible()
		{
			_mapSaveService.LegacyCompatibility = new LegacyMapSaveCompatibilityResult(false, "reason");
			_runtime.FileChooserResult = () => null;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			Assert.DoesNotContain("*.map", _runtime.LastFilter);
			Assert.Contains("*.comap", _runtime.LastFilter);
		}

		[Fact]
		public void OnSaveMapMenuActionLogsReasonBeforeDialogWhenLegacyMapIsIncompatible()
		{
			_mapSaveService.LegacyCompatibility = new LegacyMapSaveCompatibilityResult(
				false, "The legacy Civ1 map format cannot store custom start positions.");
			_runtime.FileChooserResult = () => null;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			Assert.Contains(_runtime.LogCalls, log => log.Contains(
				"The legacy Civ1 map format cannot store custom start positions.", StringComparison.Ordinal));
		}

		[Fact]
		public void OnSaveMapMenuActionDoesNotLogWhenLegacyMapIsCompatible()
		{
			_mapSaveService.LegacyCompatibility = LegacyMapSaveCompatibilityResult.Compatible;
			_runtime.FileChooserResult = () => null;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			Assert.Empty(_runtime.LogCalls);
		}

		[Fact]
		public void OnSaveMapMenuActionIncludesLegacyMapFilterWhenCompatible()
		{
			_mapSaveService.LegacyCompatibility = LegacyMapSaveCompatibilityResult.Compatible;
			_runtime.FileChooserResult = () => null;
			GamePlay.GamePlaySaveMapDelegate testee = CreateTestee();

			testee.OnSaveMapMenuAction(this, new MenuItemEventArgs<int>(0));

			Assert.Contains("*.map", _runtime.LastFilter);
			Assert.Contains("*.comap", _runtime.LastFilter);
		}

		private sealed class FakeSettings : ISettings
		{
			public string MapsDirectory { get; } = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"), "maps");
			public string PicturesDirectory => throw new NotImplementedException();
			public string SavesDirectory => throw new NotImplementedException();
			public string CosSavesDirectory => throw new NotImplementedException();
			public string StorageDirectory => throw new NotImplementedException();
			public string CaptureDirectory => throw new NotImplementedException();
			public string DataDirectory => throw new NotImplementedException();
			public string PluginsDirectory => throw new NotImplementedException();
			public string SoundsDirectory => throw new NotImplementedException();
			public bool RevealWorld => throw new NotImplementedException();
		}

		private sealed class FakeRuntime : IRuntime
		{
			private readonly Dictionary<string, string> _storedSettings = [];

			public event EventHandler Initialize { add { } remove { } }
			public event EventHandler Draw { add { } remove { } }
			public event EventHandler<UpdateEventArgs> Update { add { } remove { } }
			public event EventHandler<KeyboardEventArgs> KeyboardUp { add { } remove { } }
			public event EventHandler<KeyboardEventArgs> KeyboardDown { add { } remove { } }
			public event EventHandler<ScreenEventArgs> MouseUp { add { } remove { } }
			public event EventHandler<ScreenEventArgs> MouseDown { add { } remove { } }
			public event EventHandler<ScreenEventArgs> MouseMove { add { } remove { } }
			public event EventHandler<ScreenEventArgs> MouseWheel { add { } remove { } }
			public Platform CurrentPlatform => Platform.Windows;
			public string StorageDirectory => Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			public RuntimeSettings Settings { get; } = new RuntimeSettings();
			public Bytemap[]? Layers { get; set; }
			public Palette? Palette { get; set; }
			public int CanvasWidth => 320;
			public int CanvasHeight => 200;
			public int WindowWidth => 320;
			public int WindowHeight => 200;
			public void SetCurrentCursor(MouseCursor? _) { }
			public void SetCursor(IBitmap? _) { }
			public void SetWindowTitle(string title) { }

			public bool TryOpenUrl(string url, out string? errorMessage) { errorMessage = null; return false; }
			public bool TryCopyToClipboard(string text, out string? errorMessage) { errorMessage = null; return false; }

			public string? GetSetting(string key)
				=> _storedSettings.TryGetValue(key, out string? value) ? value : null;

			public void SetSetting(string key, string value)
				=> _storedSettings[key] = value;

			public List<string> LogCalls { get; } = [];
			public void Log(string text, params object[] parameters)
				=> LogCalls.Add(parameters.Length > 0 ? string.Format(System.Globalization.CultureInfo.InvariantCulture, text, parameters) : text);

			public string? BrowseFolder(string caption = "") => string.Empty;

			/// <summary>Set before calling the testee; invoked by <see cref="FileChooser"/> to simulate the dialog result.</summary>
			public Func<string?>? FileChooserResult { get; set; }

			/// <summary>Records the filter string passed to the most recent <see cref="FileChooser"/> call.</summary>
			public string? LastFilter { get; private set; }

			public string? FileChooser(bool save, string title, string initialFileName, string filter)
			{
				LastFilter = filter;
				return FileChooserResult != null ? FileChooserResult() : throw new NotImplementedException();
			}

			public void PlaySound(string file) { }

			public void StopSound() { }

			public void Quit() { }
		}

		private sealed class FakeMapSaveService : IMapSaveService
		{
			public List<string> SaveCivOneMapCalls { get; } = [];
			public List<string> SaveLegacyMapCalls { get; } = [];

			/// <summary>Controls the result returned by <see cref="GetLegacyMapCompatibility"/>.</summary>
			public LegacyMapSaveCompatibilityResult LegacyCompatibility { get; set; } = LegacyMapSaveCompatibilityResult.Compatible;

			public int GetLegacyMapCompatibilityCallCount { get; private set; }

			public void SaveCivOneMap(string filePath) => SaveCivOneMapCalls.Add(filePath);

			public void SaveLegacyMap(string filePath) => SaveLegacyMapCalls.Add(filePath);

			public LegacyMapSaveCompatibilityResult GetLegacyMapCompatibility()
			{
				GetLegacyMapCompatibilityCallCount++;
				return LegacyCompatibility;
			}
		}

		private sealed class FakeGameTaskCommandQueue : IGameTaskCommandQueue
		{
			public int EnqueueCallCount { get; private set; }
			public int InsertCallCount { get; private set; }

			public void Enqueue(GameTask task) => EnqueueCallCount++;
			public void Insert(GameTask task) => InsertCallCount++;
		}

		private sealed class FakeDirectoryService : IDirectoryService
		{
			public List<string> CreateDirectoryCalls { get; } = [];

			public void CreateDirectory(string path) => CreateDirectoryCalls.Add(path);
		}

		private sealed class ThrowingMessageService : IMessageService
		{
			private static Message NotExpected() => throw new InvalidOperationException("IMessageService should not be invoked outside the exception path.");

			public Message Advisor(Advisor advisor, bool leftAlign, params string[] message) => NotExpected();
			public Message Spy(params string[] message) => NotExpected();
			public Message DisbandUnit(City city, IUnit unit) => NotExpected();
			public Message NewGoverment(City? city, params string[] message) => NotExpected();
			public Message Newspaper(City? city, params string[] message) => NotExpected();
			public Message General(params string[] message) => NotExpected();
			public Message Help(string title, params string[] message) => NotExpected();
			public Message Error(string title, params string[] message) => NotExpected();
		}
	}
}
