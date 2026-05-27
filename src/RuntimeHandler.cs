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
using CivOne.Tasks;
using CivOne.Tiles;

namespace CivOne
{
	public class RuntimeHandler
	{
		private static RuntimeHandler _instance;
		internal static RuntimeHandler Instance => _instance;
		internal static IRuntime Runtime { get; private set; }
		internal static uint CurrentGameTick => _instance?._gameTick ?? 0;
		public static bool IsFullWindowCanvasRequested => _instance?.TopScreen?.UseFullWindowCanvas ?? false;
		
		private Settings Settings => Settings.Instance;
		private IScreen TopScreen => Common.TopScreen;
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

		private Stopwatch _tickWatch = new Stopwatch();

		private uint _tickWatchOffset = 0;
		private uint TickWatch
		{
			get
			{
				if (!_tickWatch.IsRunning)
				{
					_tickWatch.Start();
				}
				return _tickWatchOffset + Convert.ToUInt32(((double)_tickWatch.ElapsedMilliseconds / 1000) * 60);
			}
		}
		private uint _gameTick = 0;
		private readonly IMcpService _mcpService;

		private bool Update()
		{
			if (!GameTask.Update() && (!GameTask.Fast && (_gameTick % 4) > 0)) return false;
			if (Common.Screens.Any(x => Common.HasAttribute<Modal>(x)))
				return Common.Screens.Last(x => Common.HasAttribute<Modal>(x)).Update(_gameTick / 4);
			
			bool update = false;
			IScreen[] screens = Common.Screens.Reverse().ToArray();
			foreach (IScreen screen in screens)
			{
				// A previous screen update may destroy screens during this loop.
				// Skip stale instances to avoid updating disposed bytemaps.
				if (!Common.Screens.Contains(screen)) continue;
				if (screen.Update(_gameTick / 4)) update = true;
				if (!Common.Screens.Contains(screen)) continue;
				if (Common.HasAttribute<Break>(screen)) return update;
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

		private void OnInitialize(object sender, EventArgs args)
		{
			Runtime.WindowTitle = Settings.WindowTitle;
			_mcpService.Start();
			GameTask.Enqueue(Show.Screens(StartupScreens));
		}

		private void OnUpdate(object sender, UpdateEventArgs args)
		{
			_mcpService.Process();

			while (_gameTick < TickWatch)
			{
				_gameTick++;

#if DEBUG
				if (TickWatch - _gameTick > 1_000)
				{
					// NOTE:
					// When debugging, the game tick stops advancing while real time continues.
					// This causes the difference between TickWatch and _gameTick to grow continuously,
					// making this while-loop take an increasingly long time to finish.
					//
					// To avoid resetting _gameTick, we restart the TickWatch and apply the current
					// _gameTick as an offset, effectively continuing from the current tick.
					//
					// In the previous implementation, after continuing from the debugger and
					// returning to normal gameplay, all user input was stalled until the loop
					// had completed.
					_tickWatchOffset = _gameTick;
					_tickWatch.Restart();
					break;
				}
#endif
				if (!Update()) continue;
				args.HasUpdate = true;

			}
		}

		private void OnDraw(object sender, EventArgs args)
		{
			IScreen topScreen = TopScreen;
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
					Runtime.Palette?.Dispose();
					Runtime.Palette = topScreen.Palette.Copy();
				}
				catch (ObjectDisposedException)
				{
					Runtime.Palette?.Dispose();
					Runtime.Palette = Common.DefaultPalette;
				}
			}
			else
			{
				Runtime.Palette?.Dispose();
				Runtime.Palette = Common.DefaultPalette;
			}
			
			if (Common.HasAttribute<Modal>(topScreen))
			{
				Runtime.Layers = new[] { topScreen.Bitmap };
			}
			else
			{
				Runtime.Layers = Common.Screens.Select(x => x.Bitmap).ToArray();
			}

			if (_currentCursor != Common.MouseCursor || _cursorType != Settings.Instance.CursorType)
			{
				_currentCursor = Common.MouseCursor;
				_cursorType = Settings.Instance.CursorType;
				Runtime.CurrentCursor = _currentCursor;

				if (_cursorType != CursorType.Native && _currentCursor != MouseCursor.None && Cursor.Current?.Bitmap != null)
				{
					Runtime.Cursor = Cursor.Current.ToBitmap();
				}
				else
				{
					// Explicitly clear software cursor texture when using native cursor,
					// when cursor is hidden, or when no bitmap is available.
					Runtime.Cursor = null;
					Cursor.ClearCache();
				}
			}
		}

		private void OnKeyboardUp(object sender, KeyboardEventArgs args)
		{
		}

		private void OnKeyboardDown(object sender, KeyboardEventArgs args)
		{
			if (_quickSaveLoadHotkeyService.TryHandle(args))
			{
				return;
			}

			if (args[KeyModifier.Control, Key.F5])
			{
				string filename = Common.CaptureFilename;
				if (Runtime.Layers == null) return;
				using (IBitmap bitmap = new Picture(CanvasWidth, CanvasHeight, Common.TopScreen.Palette.Copy()))
				{
					bitmap.Palette[0] = Colour.Black;
					if (Common.HasAttribute<Modal>(TopScreen))
					{
						bitmap.AddLayer(TopScreen);
					}
					else
					{
						Runtime.Layers.ToList().ForEach(x => bitmap.AddLayer(x));
					}

					using (GifFile file = new GifFile(bitmap))
					using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
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
				string filename = Common.CaptureFilename;
				using (IBitmap tilesPicture = Map.Instance[0, 0, Map.WIDTH, Map.HEIGHT].ToBitmap())
				using (GifFile file = new GifFile(tilesPicture))
				using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
				{
					byte[] output = file.GetBytes();
					fs.Write(output, 0, output.Length);
					Runtime.Log($"Screenshot saved: {filename}");
				}
				return;
			}

			TopScreen?.KeyDown(args);
		}

		private void OnMouseUp(object sender, ScreenEventArgs args) => TopScreen?.MouseUp(args);

		private void OnMouseDown(object sender, ScreenEventArgs args) => TopScreen?.MouseDown(args);

		private void OnMouseMove(object sender, ScreenEventArgs args)
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

			CivilizationScore civScore = new CivilizationScore();
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
				throw new Exception("Only one runtime can be registered.");
			}

			EnsureTranslationFilesAvailable(runtime);
			ConfigureTranslation(runtime);
			_instance = new RuntimeHandler(runtime, CreateQuickSaveLoadHotkeyService(runtime));
		}
		public static void RegisterForTest(IRuntime runtime)
		{
			if (_instance != null)
			{
				throw new Exception("Only one runtime can be registered.");
			}

			ConfigureTranslation(runtime);
			_instance = new RuntimeHandler(runtime, CreateQuickSaveLoadHotkeyService(runtime), false);
		}

		private static IQuickSaveLoadHotkeyService CreateQuickSaveLoadHotkeyService(IRuntime runtime)
		{
			ITranslationService translationService = TranslationServiceFactory.GetCurrent();
			IYamlSaveGameServiceFactory yamlSaveGameServiceFactory = new YamlSaveGameServiceFactory();
			return new QuickSaveLoadHotkeyService(runtime, translationService, yamlSaveGameServiceFactory);
		}

		private static void EnsureTranslationFilesAvailable(IRuntime runtime)
		{
			string sourceDirectory = FindTranslationSourceDirectory();
			if (string.IsNullOrEmpty(sourceDirectory))
			{
				runtime.Log("Translation source directory not found. Skipping translation file copy.");
				return;
			}

			string targetDirectory = Path.Combine(runtime.StorageDirectory, "translations");
			int copiedCount = TranslationServiceFactory.SyncTranslationFiles(sourceDirectory, runtime.StorageDirectory, message => runtime.Log(message));
			runtime.Log("Copied {0} translation file(s) to {1}", copiedCount, targetDirectory);
		}

		private static string FindTranslationSourceDirectory()
		{
			string[] roots =
			[
				AppContext.BaseDirectory,
				Environment.CurrentDirectory
			];

			foreach (string root in roots.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				DirectoryInfo directory = new(root);
				while (directory != null)
				{
					string candidate = Path.Combine(directory.FullName, "translation");
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
			string languagePostfix = runtime.Settings.LanguagePostfix;

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

			if (TranslationServiceFactory.TryUseLanguage(runtime.StorageDirectory, languagePostfix, out string error, message => runtime.Log(message)))
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

		private void Dispose()
		{
			_mcpService.Stop();
			_mcpService.Dispose();
		}
	}
}