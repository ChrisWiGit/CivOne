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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Services.Random;
using CivOne.IO;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.Mcp;
using CivOne.Mcp.Contracts;
using CivOne.Screens;
using CivOne.Screens.StartupWizard;
using CivOne.Screens.Reports;
using CivOne.Graphics.Sprites;
using CivOne.Services;
using CivOne.Sound.Playback;
using CivOne.Tasks;
using CivOne.Tiles;

namespace CivOne
{
	#pragma warning disable CA1822 // Mark members as static
	public sealed class RuntimeHandler : IDisposable
	{
		private static RuntimeHandler? _instance;
		internal static RuntimeHandler? Instance => _instance;
		
		private static IRuntime? _runtime;
		internal static IRuntime Runtime { 
			get 
			{
				// The previous implementation of Runtime assumed that Runtime.Register is always called before any access to RuntimeHandler.Runtime.
				// To enforce this assumption, we throw an exception when Runtime is accessed before registration.
				if (_runtime == null)
				{
					throw new InvalidOperationException("RuntimeHandler is not initialized. Ensure RuntimeHandler.Register or RuntimeHandler.RegisterForTest is called before accessing the Runtime.");
				}
				return _runtime;
			}
			private set => _runtime = value;
		}
		internal static uint CurrentGameTick => _instance?._gameTick ?? 0;
		public static bool IsFullWindowCanvasRequested => _instance?.TopScreen?.UseFullWindowCanvas ?? false;
		public static FpsCorner CurrentFpsCorner => Settings.Instance.FpsCorner;
		
		private Settings Settings => Settings.Instance;
		private IScreen? TopScreen => Common.TopScreen;
		private MouseCursor _currentCursor = MouseCursor.None;
		private CursorType _cursorType = CursorType.Native;
		private readonly IQuickSaveLoadHotkeyService _quickSaveLoadHotkeyService;

		internal int CanvasWidth => IsFullWindowCanvasRequested
			? Math.Max(Settings.MinWidth, Runtime.CanvasWidth)
			: Math.Max(Settings.MinWidth, Math.Min(Settings.MaxScreenWidth, Runtime.CanvasWidth));
		internal int CanvasHeight => IsFullWindowCanvasRequested
			? Math.Max(Settings.MinHeight, Runtime.CanvasHeight)
			: Math.Max(Settings.MinHeight, Math.Min(Settings.MaxScreenHeight, Runtime.CanvasHeight));
		internal static int WindowWidth => Runtime.WindowWidth;
		internal static int WindowHeight => Runtime.WindowHeight;

		private Stopwatch _tickWatch = new();

		private uint _tickWatchOffset;

		/// <summary>
		/// Maximum number of game ticks processed in a single frame.
		/// </summary>
		/// <remarks>
		/// Ticks advance at 60 per second while screens are updated every fourth tick, so this allows
		/// three screen updates per frame. A normal frame consumes one tick or none at all, so the limit
		/// only takes effect once the game has fallen behind real time.
		/// </remarks>
		private const uint MaxTicksPerFrame = 12;

		private uint TickWatch
		{
			get
			{
				if (!_tickWatch.IsRunning)
				{
					_tickWatch.Start();
				}

				uint elapsedTicks = Convert.ToUInt32(((double)_tickWatch.ElapsedMilliseconds / 1000) * 60);
				return _tickWatchOffset + elapsedTicks;
			}
		}

		/// <summary>
		/// Drops the accumulated tick backlog and continues counting from the current game tick.
		/// </summary>
		/// <remarks>
		/// The tick counter itself is never reset, because screens derive animation state from it.
		/// Restarting the stopwatch with the current tick as offset makes the outstanding ticks
		/// disappear without moving the counter backwards.
		/// </remarks>
		private void ResyncTickWatch()
		{
			_tickWatchOffset = _gameTick;
			_tickWatch.Restart();
		}
		private uint _gameTick;
		private string? _notificationText;
		private uint _notificationUntilTick;
		private Picture? _notificationLayer;
		private string? _notificationLayerText;
		private Size _notificationLayerSize;
		private readonly IMcpService _mcpService;
		private bool _disposed;

		private bool Update()
		{
			if (!GameTask.Update() && (!GameTask.Fast && (_gameTick % 4) > 0)) return false;

			IScreen[] currentScreens = Common.Screens;
			for (int i = currentScreens.Length - 1; i >= 0; i--)
			{
				if (Common.HasAttribute<ModalAttribute>(currentScreens[i]))
				{
					return currentScreens[i].Update(_gameTick / 4);
				}
			}
			
			bool update = false;
			for (int i = currentScreens.Length - 1; i >= 0; i--)
			{
				IScreen screen = currentScreens[i];
				// A previous screen update may destroy screens during this loop.
				// Skip stale instances to avoid updating disposed bytemaps.
				if (!Common.Screens.Contains(screen)) continue;
				if (screen.Update(_gameTick / 4)) update = true;
				if (!Common.Screens.Contains(screen)) continue;

				if (Common.HasAttribute<BreakAttribute>(screen))
				{
					return update;
				}
			}
			return update;
		}

		private IEnumerable<Type> StartupScreens
		{
			get
			{
				bool dataMissing = Runtime.Settings.DataCheck && !FileSystem.DataFilesExist();
				bool showWizard = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && (dataMissing || Runtime.Settings.Setup);

				if (showWizard)
				{
					yield return typeof(WizardScreen);
				}
				else if (dataMissing)
				{
					yield return typeof(MissingFiles);
				}

				if (Runtime.Settings.Demo) yield return typeof(Demo);
				if (Runtime.Settings.Setup && !showWizard) yield return typeof(Setup);
				yield return typeof(Credits);
			}
		}

		private void OnInitialize(object? _, EventArgs args)
		{
			Runtime.SetWindowTitle(Settings.WindowTitle);
			_mcpService.Start();

			// Render the selected sound pack while the startup screens are still going, so the
			// first tune does not have to wait for it.
			SoundPlaybackStrategyProvider.WarmUp();

			GameTask.Enqueue(Show.Screens(StartupScreens));
		}

		private void OnUpdate(object? _, UpdateEventArgs args)
		{
			_mcpService.Process();

			// Sound pack tunes are rendered off the game thread. This starts one whose render has
			// finished since the last frame.
			SoundPlaybackStrategyProvider.Process();

			// The tick budget bounds how much work a single frame may do. Without it, one slow screen
			// update lets real time run ahead, which queues up further updates in the same frame, which
			// costs more time again. The loop then keeps running instead of returning to the event loop,
			// so the window stops drawing and stops reacting to input until the backlog is worked off.
			// The same runaway happens when the game tick stops advancing under a debugger while real
			// time continues. Dropping the backlog turns both cases into a skipped frame.
			uint ticksThisFrame = 0;

			while (_gameTick < TickWatch)
			{
				if (ticksThisFrame >= MaxTicksPerFrame)
				{
					ResyncTickWatch();
					break;
				}

				_gameTick++;
				ticksThisFrame++;

				if (!Update()) continue;
				args.HasUpdate = true;

			}
		}

		private void OnDraw(object? _, EventArgs args)
			=> OnDrawCore();

		private void OnDrawCore()
		{
			IScreen? topScreen = TopScreen;
			if (topScreen == null)
			{
				Runtime.Layers = null;
				return;
			}

			// During screen/menu transitions, a disposed screen palette can briefly be observed.
			// Guard palette copy so rendering falls back safely instead of crashing.
			if (topScreen.Palette is { IsDisposed: false })
			{
				try
				{
					Runtime.Palette = topScreen.Palette.Copy();
				}
				catch (ObjectDisposedException)
				{
					Runtime.Palette = Common.DefaultPalette;
				}
			}
			else
			{
				Runtime.Palette = Common.DefaultPalette;
			}
			
			if (Common.HasAttribute<ModalAttribute>(topScreen))
			{
				Runtime.Layers = [topScreen.Bitmap];
			}
			else
			{
				Runtime.Layers = [.. Common.Screens.Select(x => x.Bitmap)];
			}

			AddNotificationLayer();

			if (_currentCursor != Common.MouseCursor || _cursorType != Settings.Instance.CursorType)
			{
				_currentCursor = Common.MouseCursor;
				_cursorType = Settings.Instance.CursorType;
				Runtime.SetCurrentCursor(_currentCursor);

				if (_cursorType != CursorType.Native && _currentCursor != MouseCursor.None && Cursor.Current?.Bitmap != null)
				{
					Runtime.SetCursor(Cursor.Current.ToBitmap());
				}
				else
				{
					// Explicitly clear software cursor texture when using native cursor,
					// when cursor is hidden, or when no bitmap is available.
					Runtime.SetCursor(null);
					Cursor.ClearCache();
				}
			}

		}

		private void AddNotificationLayer()
		{
			if (string.IsNullOrEmpty(_notificationText) || _gameTick >= _notificationUntilTick || Runtime.Layers == null)
			{
				ClearNotificationLayer();
				return;
			}

			Size layerSize = new(CanvasWidth, CanvasHeight);
			if (_notificationLayer == null || _notificationLayerSize != layerSize || _notificationLayerText != _notificationText)
			{
				ClearNotificationLayer();
				_notificationLayer = CreateNotificationLayer(_notificationText, layerSize);
				_notificationLayerText = _notificationText;
				_notificationLayerSize = layerSize;
			}

			Runtime.Layers = [.. Runtime.Layers, _notificationLayer.Bitmap];
		}

		private Picture CreateNotificationLayer(string text, Size layerSize)
		{
			Picture overlay = new(layerSize.Width, layerSize.Height);
			Size textSize = Resources.Instance.GetTextSize(1, text);
			int width = Math.Min(layerSize.Width, textSize.Width + 12);
			int left = Math.Max(0, (layerSize.Width - width) / 2);
			overlay.FillRectangle(left, 0, width, Math.Min(layerSize.Height, textSize.Height + 6), 4);
			overlay.DrawText(text, 1, 15, layerSize.Width / 2, 3, TextAlign.Center);
			return overlay;
		}

		private void ClearNotificationLayer()
		{
			_notificationLayer?.Dispose();
			_notificationLayer = null;
			_notificationLayerText = null;
			_notificationLayerSize = Size.Empty;
		}

		private void OnKeyboardUp(object? _, KeyboardEventArgs args)
		{
			Common.CapsLockActive = args.CapsLock;
			Common.ShiftKeyHeld = args.Shift;
		}

		private void OnKeyboardDown(object? _, KeyboardEventArgs args)
		{
			Common.CapsLockActive = args.CapsLock;
			Common.ShiftKeyHeld = args.Shift;
			if (TryHandleSoundToggleHotkey(args))
			{
				return;
			}

			if (_quickSaveLoadHotkeyService.TryHandle(args))
			{
				return;
			}

			if (args[KeyModifier.Control, Key.F5])
			{
				string? filename = Common.CaptureFilename;
				if (filename == null) return;
				if (Runtime.Layers == null) return;
				
				IScreen? topScreen = TopScreen;
				if (topScreen == null) return;
				using Palette screenshotPalette = topScreen.Palette.Copy();

				using (Picture bitmap = new(CanvasWidth, CanvasHeight, screenshotPalette))
				{
					bitmap.Palette[0] = Colour.Black;
					if (Common.HasAttribute<ModalAttribute>(topScreen))
					{
						bitmap.AddLayer(topScreen);
					}
					else
					{
						Runtime.Layers.ToList().ForEach(x => bitmap.AddLayer(x));
					}

					using (GifFile file = new(bitmap))
					using (FileStream fs = new(filename, FileMode.Create, FileAccess.Write))
					{
						byte[] output = file.GetBytes();
						fs.Write(output, 0, output.Length);
						Runtime.Log($"Screenshot saved: {filename}");
					}
				}
				return;
			}

			if (args[KeyModifier.Control, Key.F6] && Game.Started)
			{
				string? filename = Common.CaptureFilename;
				if (filename == null) return;
				
				using IBitmap tilesPicture = Map.Instance[0, 0, Map.WIDTH, Map.HEIGHT].ToBitmap();
				using GifFile file = new(tilesPicture);
				using FileStream fs = new(filename, FileMode.Create, FileAccess.Write);
				byte[] output = file.GetBytes();
				fs.Write(output, 0, output.Length);
				Runtime.Log($"Screenshot saved: {filename}");

				return;
			}

			TopScreen?.KeyDown(args);
		}

		private bool TryHandleSoundToggleHotkey(KeyboardEventArgs args)
		{
			if (!args.Control || !args.Alt || args.Key != Key.Character || char.ToUpperInvariant(args.KeyChar) != 'M') return false;

			Settings.Sound = Settings.Sound == GameOption.Off ? GameOption.On : GameOption.Off;
			if (Settings.Sound == GameOption.Off)
			{
				SoundPlaybackStrategyProvider.Current.Abort();
			}

			ShowNotification(Settings.Sound == GameOption.Off ? "Sound off" : "Sound on");
			return true;
		}

		private void ShowNotification(string text)
		{
			_notificationText = text;
			_notificationUntilTick = _gameTick + 180;
		}

		private void OnMouseUp(object? _, ScreenEventArgs args)
		{
			Common.ShiftKeyHeld = (args.Modifier & KeyModifier.Shift) > 0;
			TopScreen?.MouseUp(args);
		}

		private void OnMouseDown(object? _, ScreenEventArgs args)
		{
			Common.ShiftKeyHeld = (args.Modifier & KeyModifier.Shift) > 0;
			TopScreen?.MouseDown(args);
		}

		private void OnMouseWheel(object? _, ScreenEventArgs args) => TopScreen?.MouseWheel(args);

		private void OnMouseMove(object? _, ScreenEventArgs args)
		{
			if (args.Buttons != MouseButton.None)
			{
				TopScreen?.MouseDrag(args);
			}
			TopScreen?.MouseMove(args);
		}

		internal static void EndGame()
		{
			GameTask.ClearAll();

			foreach (IScreen screen in Common.Screens.ToArray())
			{
				Common.DestroyScreen(screen);
			}

			CivilizationScore civScore = new();
			civScore.Closed += (s, a) => ReturnToCredits();
			Common.AddScreen(civScore);
		}

		internal static void ReturnToCredits()
		{
			GameTask.ClearAll();

			foreach (IScreen screen in Common.Screens.ToArray())
			{
				Common.DestroyScreen(screen);
			}

			Game.Wipe();
			Map.Reset();
			Common.AddScreen(new Credits());
		}

		public static void Register(IRuntime runtime)
		{
			if (_instance != null)
			{
				ArgumentNullException.ThrowIfNull(runtime, nameof(runtime));
			}

			EnsureTranslationFilesAvailable(runtime);
			EnsureMapFilesAvailable(runtime);
			ConfigureTranslation(runtime);
			_instance = new RuntimeHandler(runtime, CreateQuickSaveLoadHotkeyService(runtime));
		}
		public static void RegisterForTest(IRuntime runtime)
		{
			if (_instance != null)
			{
				ArgumentNullException.ThrowIfNull(runtime, nameof(runtime));
			}

			ConfigureTranslation(runtime);
			_instance = new RuntimeHandler(runtime, CreateQuickSaveLoadHotkeyService(runtime), false);
		}

		private static QuickSaveLoadHotkeyService CreateQuickSaveLoadHotkeyService(IRuntime runtime)
		{
			ITranslationService translationService = TranslationServiceFactory.GetCurrent();
			IYamlSaveGameServiceFactory yamlSaveGameServiceFactory = new YamlSaveGameServiceFactory();
			return new QuickSaveLoadHotkeyService(runtime, translationService, yamlSaveGameServiceFactory);
		}

		private static void EnsureTranslationFilesAvailable(IRuntime runtime)
		{
			string? sourceDirectory = FindTranslationSourceDirectory();
			if (string.IsNullOrEmpty(sourceDirectory))
			{
				runtime.Log("Translation source directory not found. Skipping translation file copy.");
				return;
			}

			string targetDirectory = Path.Combine(runtime.StorageDirectory, "translations");
			int copiedCount = TranslationServiceFactory.SyncTranslationFiles(sourceDirectory, runtime.StorageDirectory, message => runtime.Log(message));
			runtime.Log("Copied {0} translation file(s) to {1}", copiedCount, targetDirectory);
		}

		private static void EnsureMapFilesAvailable(IRuntime runtime)
		{
			string? sourceDirectory = FindStartupContentDirectory("maps");
			if (string.IsNullOrEmpty(sourceDirectory))
			{
				runtime.Log("Map source directory not found. Skipping .comap file copy.");
				return;
			}

			string targetDirectory = Path.Combine(runtime.StorageDirectory, "maps");
			Directory.CreateDirectory(targetDirectory);

			int copiedCount = 0;
			foreach (string sourcePath in Directory.EnumerateFiles(sourceDirectory, "*.comap", SearchOption.TopDirectoryOnly))
			{
				string filename = Path.GetFileName(sourcePath);
				string targetPath = Path.Combine(targetDirectory, filename);
				if (File.Exists(targetPath))
				{
					continue;
				}

				File.Copy(sourcePath, targetPath);
				copiedCount++;
			}

			runtime.Log("Copied {0} .comap file(s) to {1}", copiedCount, targetDirectory);
		}

		private static string? FindTranslationSourceDirectory()
		{
			return FindStartupContentDirectory("translation");
		}

		private static string? FindStartupContentDirectory(string directoryName)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(directoryName);

			string[] roots =
			[
				AppContext.BaseDirectory,
				Environment.CurrentDirectory
			];

			foreach (string root in roots.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				DirectoryInfo? directory = new(root);
				while (directory != null)
				{
					string candidate = Path.Combine(directory.FullName, directoryName);
					if (Directory.Exists(candidate))
					{
						return candidate;
					}

					directory = directory.Parent;
				}
			}

			return null;
		}

		private static void ConfigureTranslation(IRuntime runtime)
		{
			string? languagePostfix = runtime.Settings.LanguagePostfix;

			if (string.IsNullOrEmpty(languagePostfix))
			{
				// During registration RuntimeHandler.Runtime is not assigned yet,
				// so Settings.Instance would recurse into an uninitialized runtime.
				languagePostfix = runtime.GetSetting("LanguagePostfix");
			}

			if (string.IsNullOrEmpty(languagePostfix)
				|| string.Equals(languagePostfix, "identity", StringComparison.OrdinalIgnoreCase))
			{
				TranslationServiceFactory.UseIdentity();
				return;
			}

			if (TranslationServiceFactory.TryUseLanguage(runtime.StorageDirectory, languagePostfix, out string? error, message => runtime.Log(message)))
			{
				runtime.Log("Translation language activated: {0}", languagePostfix);
				return;
			}

			runtime.Log("Could not activate translation language '{0}': {1}", languagePostfix, error);
			TranslationServiceFactory.UseIdentity();
		}

        /// <summary>
        /// Fire-eggs 20190704: for unit testing, reset
        /// </summary>
        internal static void Wipe()
        {
			_instance?.Dispose();
            _instance = null;
        }

		private RuntimeHandler(IRuntime runtime, IQuickSaveLoadHotkeyService quickSaveLoadHotkeyService, bool concurrent = true)
		{
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			_mcpService = McpServiceFactory.Create(runtime);
			_quickSaveLoadHotkeyService = quickSaveLoadHotkeyService ?? throw new ArgumentNullException(nameof(quickSaveLoadHotkeyService));

			// fire-eggs 20170711 init the RNG if user specified
			// Be aware: Game.LoadSave will override this with the seed from the save game
            if (runtime.Settings.InitialSeed != 0)
				RandomServiceFactory.Reset(runtime.Settings.InitialSeed);
			else
				RandomServiceFactory.Reset();


            runtime.Initialize += OnInitialize;
			runtime.Update += OnUpdate;
			runtime.Draw += OnDraw;
			runtime.KeyboardUp += OnKeyboardUp;
			runtime.KeyboardDown += OnKeyboardDown;
			runtime.MouseUp += OnMouseUp;
			runtime.MouseDown += OnMouseDown;
			runtime.MouseMove += OnMouseMove;
			runtime.MouseWheel += OnMouseWheel;

			foreach (Plugin plugin in Reflect.Plugins())
			{
				runtime.Log($"Plugin loaded: {plugin.Name} version {plugin.Version} by {plugin.Author}");
			}

			if (concurrent)
			{
				runtime.Log("Preloading Civilopedia in background task");
				Task.Run(() => Reflect.PreloadCivilopedia());
			}
			else
			{
				runtime.Log("Preloading Civilopedia synchronously");
				Reflect.PreloadCivilopedia();
			}
		}

		public static void Shutdown()
		{
			_instance?.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			if (disposing)
			{
				ClearNotificationLayer();
				_mcpService.StopService();
				_mcpService.Dispose();
			}

			_disposed = true;
		}
	}
}