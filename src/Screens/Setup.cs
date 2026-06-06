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
using System.Globalization;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Services;
using CivOne.Services.Translation;
using CivOne.Tasks;
using CivOne.UserInterface;

using static CivOne.Enums.AspectRatio;
using static CivOne.Enums.CursorType;
using static CivOne.Enums.DestroyAnimation;
using static CivOne.Enums.GraphicsMode;

namespace CivOne.Screens
{
	#pragma warning disable CA1822 // Mark members as static
	[Break, ScreenResizeable]
	internal class Setup : BaseScreen
	{
		private const int MenuFont = 6;

		private (string Label, int Width, int Height)[] ExpandSizeOptions() =>
		[
			(Translate("Auto (stretch)"), -1, -1),

			// SD
			("640x360", 640, 360),
			("854x480 (FWVGA)", 854, 480),
			("960x540", 960, 540),

			// HD
			("1280x720 (HD)", 1280, 720),
			("1366x768", 1366, 768),
			("1600x900", 1600, 900),

			// Full HD / WUXGA
			("1920x1080 (FHD)", 1920, 1080),
			("1920x1200 (WUXGA)", 1920, 1200),

			// QHD / WQHD
			("2560x1440 (QHD)", 2560, 1440),
			("2560x1600 (WQXGA)", 2560, 1600),

			// Ultrawide
			("2560x1080 (UW-FHD)", 2560, 1080),
			("3440x1440 (UW-QHD)", 3440, 1440),

			// 4K / UHD
			(Translate("3840x2160 (4K UHD)"), 3840, 2160),
			(TranslateFormatted("{0} (Warning: may be slow)", "4096x2160 (DCI 4K)"), 4096, 2160),

			// Higher resolutions
			(TranslateFormatted("{0} (Warning: may be slow)", "5120x2880 (5K)"), 5120, 2880),
			(TranslateFormatted("{0} (Warning: may be slow)", "7680x4320 (8K UHD)"), 7680, 4320)
		];

		private (string Label, int Width, int Height)[] WindowSizeOptions() =>
		[
			(Translate("Auto (scale-based)"), -1, -1),

			// Classic / small
			(Translate("800x600 (SVGA)"), 800, 600),
			(Translate("1024x768 (XGA)"), 1024, 768),

			// HD range
			(Translate("1280x720 (HD)"), 1280, 720),
			(Translate("1366x768"), 1366, 768),
			(Translate("1440x900 (WXGA+)"), 1440, 900),
			(Translate("1600x900"), 1600, 900),

			// Full HD
			(Translate("1920x1080 (FHD)"), 1920, 1080),
			(Translate("1920x1200 (WUXGA)"), 1920, 1200),

			// QHD range
			(Translate("2560x1440 (QHD)"), 2560, 1440),
			(Translate("2560x1600 (WQXGA)"), 2560, 1600),

			// Ultrawide
			(Translate("2560x1080 (UW-FHD)"), 2560, 1080),
			(Translate("3440x1440 (UW-QHD)"), 3440, 1440),

			// 4K+
			(TranslateFormatted("{0} (Warning: may be slow)", "3840x2160 (4K UHD)"), 3840, 2160),
			(TranslateFormatted("{0} (Warning: may be slow)", "4096x2160 (DCI 4K)"), 4096, 2160),

			// High-end
			(TranslateFormatted("{0} (Warning: may be slow)", "5120x2880 (5K)"), 5120, 2880),
			(TranslateFormatted("{0} (Warning: may be slow)", "7680x4320 (8K UHD)"), 7680, 4320)
		];

		private bool _update = true;

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;
			_update = false;

			if (!HasMenu)
			{
				MainMenu();
			}

			return false;
		}

		private void BrowseForSoundFiles(object sender, MenuItemEventArgs<int> args)
		{
			string? path = Runtime.BrowseFolder(Translate("Location of Civilization for Windows sound files"));
			if (path == null)
			{
				// User pressed cancel
				return;
			}

			FileSystem.CopySoundFiles(path);
		}

		private void BrowseForPlugins(object sender, MenuItemEventArgs<int> args)
		{
			string? path = Runtime.BrowseFolder(Translate("Location of CivOne plugin(s)"));
			if (path == null)
			{
				// User pressed cancel
				return;
			}

			CloseMenus();
			MainMenu(2);
			FileSystem.CopyPlugins(path);
		}

		private void OpenProfileFolder(object sender, MenuItemEventArgs<int> args)
		{
			string storageDirectory = Runtime.StorageDirectory;
			if (string.IsNullOrWhiteSpace(storageDirectory))
			{
				Log("Profile folder unavailable.");
				return;
			}

			if (!Runtime.TryOpenUrl(storageDirectory, out string? errorMessage))
			{
				Log("Could not open profile folder '{0}': {1}", storageDirectory, errorMessage ?? "unknown error");
			}
		}

		private void CreateMenu(string title, int activeItem, MenuItemEventAction<int>? always, params MenuItem<int>[] items) =>
			AddMenu(new Menu("Setup", Palette)
			{
				Title = $"{title.ToUpper(CultureInfo.InvariantCulture)}:",
				TitleColour = 15,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 8,
				FontId = MenuFont,
				IndentTitle = 2
			}
			.Items([.. items.Where(item => item != null)])
			.Always(always)
			.Center(this)
			.SetActiveItem(activeItem)
		);
		private void CreateMenu(string title, MenuItemEventAction<int> always, params MenuItem<int>[] items) => CreateMenu(title, -1, always, items);
		private void CreateMenu(string title, int activeItem, params MenuItem<int>[] items) => CreateMenu(title, activeItem, null, items);
		private void CreateMenu(string title, params MenuItem<int>[] items) => CreateMenu(title, -1, null, items);

		private MenuItemEventAction<int> GotoMenu(Action<int> action, int selectedItem = 0) => (s, a) =>
		{
			CloseMenus();
			action(selectedItem);
		};

		private MenuItemEventAction<int> GotoMenu(Action action) => (s, a) =>
		{
			CloseMenus();
			action();
		};

		private MenuItemEventAction<int> GotoScreen<T>(Action doneAction) where T : IScreen, new() => (s, a) =>
		{
			CloseMenus();
			T screen = new T();
			screen.Closed += (sender, args) => doneAction();
			Common.AddScreen(screen);
		};

		private MenuItemEventAction<int> CloseScreen(Action? action = null) => (s, a) =>
		{
			Destroy();
			if (action != null) action();
		};

		private void ChangeWindowTitle()
		{
			RuntimeHandler.Runtime.SetWindowTitle(Settings.WindowTitle);
			SettingsMenu(0);
		}

		private void MainMenu(int activeItem = 0)
		{
			List<MenuItem<int>> items =
			[
				MenuItem.Create(Translate("Settings")).OnSelect(GotoMenu(SettingsMenu))
					.WithDescription(Translate("Configure graphics, sound, and other options.")),
				MenuItem.Create(Translate("Patches")).OnSelect(GotoMenu(PatchesMenu))
					.WithDescription(Translate("Enable or disable various game behavior patches.")),
				MenuItem.Create(Translate("Plugins")).OnSelect(GotoMenu(PluginsMenu))
					.WithDescription(TranslateArray("Browse for and install optional third-party plugins.\nThis feature is not really implemented.")).SetEnabled(!Game.Started),
				MenuItem.Create(Translate("Game Options")).OnSelect(GotoMenu(GameOptionsMenu))
					.WithDescription(Translate("Configure game rules and difficulty settings.")),
				MenuItem.Create(Translate("Open CivOne Profile folder...")).OnSelect(OpenProfileFolder)
					.WithDescription(Translate("Open the folder where CivOne stores profiles, save games, and settings.")),
				MenuItem.Create(GetReturnTargetString()).OnSelect(CloseScreen())
			];

			if (IsAllowedToQuit())
			{
				items.Add(MenuItem.Create(Translate("Quit")).OnSelect(CloseScreen(Runtime.Quit)));
			}

			CreateMenu(Translate("CivOne Setup"), activeItem, [.. items]);
		}

		private string GetReturnTargetString()
		{
			if (Game.Started)
			{
				return Translate("Return to game");
			}
			else if (Common.HasScreenType<StartupWizard.WizardScreen>())
			{
				return Translate("Return to startup wizard");
			}
			else
			{
				return Translate("Launch Game");
			}
		}

		private static bool IsAllowedToQuit()
		{
			return Game.Started || !Common.HasScreenType<StartupWizard.WizardScreen>();
		}

		private void SettingsMenu(int activeItem = 0) => CreateMenu("Settings", activeItem,
			MenuItem.Create(TranslateFormatted("Window Title: {0}", Settings.WindowTitle)).OnSelect(GotoScreen<WindowTitle>(ChangeWindowTitle)),
			MenuItem.Create(TranslateFormatted("Graphics Mode: {0}", Settings.GraphicsMode.ToText())).OnSelect(GotoMenu(GraphicsModeMenu)),
			MenuItem.Create(TranslateFormatted("Simulate intl font: {0}", Settings.SimulateInternationalFont.ToText()))
				.WithDescription(
					Translate("Override international font simulation behavior."),
					Translate("Auto detects, Yes forces simulation, No disables it."))
				.OnSelect(GotoMenu(SimulateInternationalFontMenu)),
			MenuItem.Create(TranslateFormatted("Aspect Ratio: {0}", Settings.AspectRatio.ToText()))
					.WithDescription(Translate("Use different aspect ratios."))
					.OnSelect(GotoMenu(AspectRatioMenu)),
			MenuItem.Create(TranslateFormatted("Expand Size: {0}", ExpandSizeText()))
					.WithDescription(Translate("Resolution when using Expand ratio."))
					.OnSelect(GotoMenu(ExpandCanvasSizeMenu)),
			MenuItem.Create(TranslateFormatted("Full Screen: {0}", Settings.FullScreen.YesNo())).OnSelect(GotoMenu(FullScreenMenu)),
			MenuItem.Create(TranslateFormatted("VSync: {0}", Settings.VSync.YesNo()))
				.WithDescription(
					Translate("Synchronize rendering to the display refresh rate."),
					Translate("Restart the game after changing this setting."))
				.OnSelect(GotoMenu(VSyncMenu)),
			MenuItem.Create(TranslateFormatted("Window Size: {0}", WindowSizeText())).OnSelect(GotoMenu(WindowSizeMenu)),
			MenuItem.Create(TranslateFormatted("Window Scale: {0}x", Settings.Scale)).OnSelect(GotoMenu(WindowScaleMenu)),
			MenuItem.Create(Translate("In-game sound")).OnSelect(GotoMenu(SoundMenu)),
			MenuItem.Create(Translate("Back")).OnSelect(GotoMenu(MainMenu, 0))
		);

		private void GraphicsModeMenu() => CreateMenu("Graphics Mode", GotoMenu(SettingsMenu, 1),
			MenuItem.Create(TranslateFormatted("{0} (default)", Graphics256.ToText())).OnSelect((s, a) => Settings.GraphicsMode = Graphics256).SetActive(() => Settings.GraphicsMode == Graphics256),
			MenuItem.Create(Graphics16.ToText()).OnSelect((s, a) => Settings.GraphicsMode = Graphics16).SetActive(() => Settings.GraphicsMode == Graphics16),
			MenuItem.Create(Translate("Back"))
		);

		private void SimulateInternationalFontMenu() => CreateMenu("Simulate international font", GotoMenu(SettingsMenu, 2),
			MenuItem.Create(Translate("AUTO (default)"))
				.WithDescription(Translate("Automatically detect and simulate when needed."))
				.OnSelect((s, a) => Settings.SimulateInternationalFont = SimulateInternationalFont.Auto).SetActive(() => Settings.SimulateInternationalFont == SimulateInternationalFont.Auto),
			MenuItem.Create(Translate("YES"))
				.WithDescription(Translate("Always use simulated international font rendering."))
				.OnSelect((s, a) => Settings.SimulateInternationalFont = SimulateInternationalFont.Yes).SetActive(() => Settings.SimulateInternationalFont == SimulateInternationalFont.Yes),
			MenuItem.Create(Translate("NO"))
				.WithDescription(Translate("Never simulate; use original font set as-is."))
				.OnSelect((s, a) => Settings.SimulateInternationalFont = SimulateInternationalFont.No).SetActive(() => Settings.SimulateInternationalFont == SimulateInternationalFont.No),
			MenuItem.Create(Translate("Back"))
		);

		private void AspectRatioMenu() => CreateMenu("Aspect Ratio", GotoMenu(SettingsMenu, 3),
			MenuItem.Create(TranslateFormatted("{0} (default)", Auto.ToText())).OnSelect((s, a) => Settings.AspectRatio = Auto)
				.WithDescription(TranslateArray("Scale without correct aspect ratio\nThis may cause distortion."))
				.SetActive(() => Settings.AspectRatio == Auto),
			MenuItem.Create(Fixed.ToText()).OnSelect((s, a) => Settings.AspectRatio = Fixed)
				.WithDescription(TranslateArray("Scale with correct aspect ratio\nThis may cause black borders."))
				.SetActive(() => Settings.AspectRatio == Fixed),
			MenuItem.Create(Scaled.ToText()).OnSelect((s, a) => Settings.AspectRatio = Scaled)
				.WithDescription(TranslateArray("Scale to resolution\nThis may cause blurry image."))
				.SetActive(() => Settings.AspectRatio == Scaled),
			MenuItem.Create(ScaledFixed.ToText()).OnSelect((s, a) => Settings.AspectRatio = ScaledFixed)
				.WithDescription(TranslateArray("Scale with correct aspect ratio\nThis may cause blurry\n and black borders."))
				.SetActive(() => Settings.AspectRatio == ScaledFixed),
			MenuItem.Create(AspectRatio.Expand.ToText()).OnSelect((s, a) => Settings.AspectRatio = AspectRatio.Expand)
				.WithDescription(TranslateArray("Expand image to fit the screen.\nSome screens may show borders."))
				.SetActive(() => Settings.AspectRatio == AspectRatio.Expand),
			MenuItem.Create(Translate("Back"))
		);

		private string SizeText(int width, int height, string autoText)
			=> width <= 0 || height <= 0 ? autoText : $"{width}x{height}";

		private string ExpandSizeText() => SizeText(Settings.ExpandWidth, Settings.ExpandHeight, Translate("Auto"));

		private void SetExpandSize(int width, int height)
		{
			Settings.ExpandWidth = width;
			Settings.ExpandHeight = height;
		}

		private bool IsActiveSize(int currentWidth, int currentHeight, int optionWidth, int optionHeight)
		{
			if (optionWidth <= 0 || optionHeight <= 0)
				return currentWidth <= 0 || currentHeight <= 0;
			return currentWidth == optionWidth && currentHeight == optionHeight;
		}

		private MenuItem<int>[] BuildSizeMenuItems(
			(string Label, int Width, int Height)[] options,
			Func<int, int, bool> isActive,
			Action<int, int> onSelect)
			=>
			[
				.. options.Select(option =>
					MenuItem.Create(option.Label)
						.OnSelect((s, a) => onSelect(option.Width, option.Height))
						.SetActive(() => isActive(option.Width, option.Height))),
				MenuItem.Create(Translate("Back"))
			];

		private void ExpandCanvasSizeMenu() => CreateMenu(
			Translate("Expand Size (only if Aspect Ratio is set to Expand)"),
			GotoMenu(SettingsMenu, 4),
			BuildSizeMenuItems(
				ExpandSizeOptions(),
				(width, height) => IsActiveSize(Settings.ExpandWidth, Settings.ExpandHeight, width, height),
				SetExpandSize));

		private void FullScreenMenu() => CreateMenu(Translate("Full Screen"), GotoMenu(SettingsMenu, 5),
			MenuItem.Create(TranslateFormatted("{0} (default)", false.YesNo())).OnSelect((s, a) => Settings.FullScreen = false).SetActive(() => !Settings.FullScreen),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.FullScreen = true).SetActive(() => Settings.FullScreen),
			MenuItem.Create(Translate("Back"))
		);

		private void VSyncMenu() => CreateMenu(Translate("VSync"), GotoMenu(SettingsMenu, 6),
			MenuItem.Create(TranslateFormatted("{0} (default)", true.YesNo()))
				.WithDescription(
					Translate("Synchronize rendering to the display refresh rate."),
					Translate("Requires a restart to take effect."))
				.OnSelect((s, a) => Settings.VSync = true).SetActive(() => Settings.VSync),
			MenuItem.Create(false.YesNo())
				.WithDescription(
					Translate("Render without display synchronization."),
					Translate("Requires a restart to take effect."))
				.OnSelect((s, a) => Settings.VSync = false).SetActive(() => !Settings.VSync),
			MenuItem.Create(Translate("Back"))
		);

		private string WindowSizeText() => SizeText(Settings.WindowWidth, Settings.WindowHeight, Translate("Auto"));

		private void SetWindowSizePreset(int width, int height)
		{
			Settings.WindowWidth = width;
			Settings.WindowHeight = height;
		}

		private void WindowSizeMenu() => CreateMenu(
			Translate("Window Size"),
			GotoMenu(SettingsMenu, 7),
			BuildSizeMenuItems(
				WindowSizeOptions(),
				(width, height) => IsActiveSize(Settings.WindowWidth, Settings.WindowHeight, width, height),
				SetWindowSizePreset));

		private void WindowScaleMenu() => CreateMenu(Translate("Window Scale"), GotoMenu(SettingsMenu, 8),
			MenuItem.Create(Translate("1x")).OnSelect((s, a) => Settings.Scale = 1).SetActive(() => Settings.Scale == 1),
			MenuItem.Create(Translate("2x (default)")).OnSelect((s, a) => Settings.Scale = 2).SetActive(() => Settings.Scale == 2),
			MenuItem.Create(Translate("3x")).OnSelect((s, a) => Settings.Scale = 3).SetActive(() => Settings.Scale == 3),
			MenuItem.Create(Translate("4x")).OnSelect((s, a) => Settings.Scale = 4).SetActive(() => Settings.Scale == 4),
			MenuItem.Create(Translate("5x")).OnSelect((s, a) => Settings.Scale = 5).SetActive(() => Settings.Scale == 5),
			MenuItem.Create(Translate("6x")).OnSelect((s, a) => Settings.Scale = 6).SetActive(() => Settings.Scale == 6),
			MenuItem.Create(Translate("7x")).OnSelect((s, a) => Settings.Scale = 7).SetActive(() => Settings.Scale == 7),
			MenuItem.Create(Translate("8x")).OnSelect((s, a) => Settings.Scale = 8).SetActive(() => Settings.Scale == 8),
			MenuItem.Create(Translate("Back"))
		);

		private void SoundMenu() => CreateMenu(Translate("In-game sound"), GotoMenu(SettingsMenu, 9),
			MenuItem.Create(Translate("Browse for files...")).OnSelect(BrowseForSoundFiles).SetEnabled(!FileSystem.SoundFilesExist()).SetEnabled(!Game.Started),
			MenuItem.Create(Translate("Back"))
		);

		private int ActiveBehaviorPatchCount()
			=> new[]
			{
				Settings.PathFinding,
				Settings.AutoSettlers,
				Settings.RiverFastMovement,
				Settings.CanalCity,
				Settings.GlobalWarmingFeatureFlags != Settings.GlobalWarmingFeatureFlag.None
			}.Count(static enabled => enabled);

		private void PatchesMenu(int activeItem = 0) => CreateMenu(Translate("Patches"), activeItem,
			MenuItem.Create(TranslateFormatted("Reveal world: {0}", Settings.RevealWorld.YesNo()))
				.WithDescription(
					Translate("Show map tiles, cities, and units for all players."),
					Translate("Useful for testing and debugging runs."))
				.OnSelect(GotoMenu(RevealWorldMenu)),
			MenuItem.Create(TranslateFormatted("Side bar location: {0}{1}", Settings.RightSideBar ? Translate("right") : Translate("left"), Game.Started ? Translate(" (restart required)") : string.Empty))
				.WithDescription(
					Translate("Choose whether sidebar appears left or right."),
					Translate("Changing in-game requires a restart."))
				.OnSelect(GotoMenu(SideBarMenu)),
			MenuItem.Create(TranslateFormatted("Debug menu: {0}", Settings.DebugMenu.YesNo()))
				.WithDescription(
					Translate("Enable extra debug commands and diagnostics."),
					Translate("Can only be changed before a game starts."))
				.OnSelect(GotoMenu(DebugMenuMenu)).SetEnabled(!Game.Started),
			MenuItem.Create(TranslateFormatted("Cursor type: {0}", Settings.CursorType.ToText()))
				.WithDescription(
					Translate("Select cursor source: original, builtin, or native."))
				.OnSelect(GotoMenu(CursorTypeMenu)),
			MenuItem.Create(TranslateFormatted("Destroy animation: {0}", Settings.DestroyAnimation.ToText()))
				.WithDescription(
					Translate("Choose visual effect used when units are destroyed."))
				.OnSelect(GotoMenu(DestroyAnimationMenu)),
			MenuItem.Create(TranslateFormatted("Enable Deity difficulty: {0}", Settings.DeityEnabled.YesNo()))
				.WithDescription(
					Translate("Allow Deity to appear in difficulty selection."))
				.OnSelect(GotoMenu(DeityEnabledMenu)),
			MenuItem.Create(TranslateFormatted("Enable (no keypad) arrow helper: {0}", Settings.ArrowHelper.YesNo()))
				.WithDescription(
					Translate("Show movement helper for keyboards without keypad."))
				.OnSelect(GotoMenu(ArrowHelperMenu)),
			MenuItem.Create(TranslateFormatted("Game behavior menu: {0} active", ActiveBehaviorPatchCount()))
				.WithDescription(
					Translate("Configure optional rule tweaks and AI behavior."))
				.OnSelect(GotoMenu(BehaviorMenu)),
			MenuItem.Create(TranslateFormatted("AutoSave format: {0}", Settings.PreferSveSaveFormat ? Translate("SVE (fallback COS)") : Translate("COS")))
				.WithDescription(
					Translate("Select preferred format for automatic saves."),
					Translate("SVE uses COS as fallback when needed."))
				.OnSelect(GotoMenu(SaveFormatMenu)),
			MenuItem.Create(TranslateFormatted("Save cast behavior: {0}", Settings.UseUncheckedCastSanitizer ? Translate("Unchecked") : Translate("Checked")))
				.WithDescription(
					Translate("Choose checked or unchecked cast handling."),
					Translate("Unchecked keeps legacy behavior."))
				.OnSelect(GotoMenu(SaveCastBehaviorMenu)),
			MenuItem.Create(TranslateFormatted("FPS display: {0}", Settings.FpsCorner.ToText()))
				.WithDescription(
					Translate("Show frames per second in a screen corner."))
				.OnSelect(GotoMenu(FpsCornerMenu)),
			MenuItem.Create(Translate("Back")).OnSelect(GotoMenu(MainMenu, 1))
		);

		private void RevealWorldMenu() => CreateMenu(Translate("Reveal world"), GotoMenu(PatchesMenu, 0),
			MenuItem.Create(TranslateFormatted("{0} (default)", false.YesNo()))
				.WithDescription(Translate("Keep normal fog of war behavior."))
				.OnSelect((s, a) => Settings.RevealWorld = false).SetActive(() => !Settings.RevealWorld),
			MenuItem.Create(true.YesNo())
				.WithDescription(Translate("Reveal full world and all units."))
				.OnSelect((s, a) => Settings.RevealWorld = true).SetActive(() => Settings.RevealWorld),
			MenuItem.Create(Translate("Back"))
		);

		private void SideBarMenu() => CreateMenu(Translate("Side bar location"), GotoMenu(PatchesMenu, 1),
			MenuItem.Create(Translate("Left (default)"))
				.WithDescription(Translate("Place sidebar on the left side."))
				.OnSelect((s, a) => Settings.RightSideBar = false).SetActive(() => !Settings.RightSideBar),
			MenuItem.Create(Translate("Right"))
				.WithDescription(Translate("Place sidebar on the right side."))
				.OnSelect((s, a) => Settings.RightSideBar = true).SetActive(() => Settings.RightSideBar),
			MenuItem.Create(Translate("Back"))
		);

		private void DebugMenuMenu() => CreateMenu(Translate("Show debug menu"), GotoMenu(PatchesMenu, 2),
			MenuItem.Create(TranslateFormatted("{0} (default)", false.YesNo()))
				.WithDescription(Translate("Hide debug menu entries."))
				.OnSelect((s, a) => Settings.DebugMenu = false || Game.Started).SetActive(() => !Settings.DebugMenu || Game.Started),
			MenuItem.Create(true.YesNo())
				.WithDescription(Translate("Show debug menu entries."))
				.OnSelect((s, a) => Settings.DebugMenu = true).SetActive(() => Settings.DebugMenu),
			MenuItem.Create(Translate("Back"))
		);

		private void CursorTypeMenu() => CreateMenu(Translate("Mouse cursor type"), GotoMenu(PatchesMenu, 3),
			MenuItem.Create(Default.ToText())
				.WithDescription(Translate("Use original cursor assets from game files."))
				.OnSelect((s, a) => Settings.CursorType = Default).SetActive(() => Settings.CursorType == Default && FileSystem.DataFilesExist(FileSystem.MouseCursorFiles)).SetEnabled(FileSystem.DataFilesExist(FileSystem.MouseCursorFiles)),
			MenuItem.Create(Builtin.ToText())
				.WithDescription(Translate("Use built-in CivOne cursor graphics."))
				.OnSelect((s, a) => Settings.CursorType = Builtin).SetActive(() => Settings.CursorType == Builtin || (Settings.CursorType == Default && !FileSystem.DataFilesExist(FileSystem.MouseCursorFiles))),
			MenuItem.Create(Native.ToText())
				.WithDescription(Translate("Use operating system native mouse cursor."))
				.OnSelect((s, a) => Settings.CursorType = Native).SetActive(() => Settings.CursorType == Native),
			MenuItem.Create(Translate("Back"))
		);

		private void DestroyAnimationMenu() => CreateMenu(Translate("Destroy animation"), GotoMenu(PatchesMenu, 4),
			MenuItem.Create(Sprites.ToText())
				.WithDescription(Translate("Use sprite animation for destruction effects."))
				.OnSelect((s, a) => Settings.DestroyAnimation = Sprites).SetActive(() => Settings.DestroyAnimation == Sprites),
			MenuItem.Create(Noise.ToText())
				.WithDescription(Translate("Use classic noise effect for destruction."))
				.OnSelect((s, a) => Settings.DestroyAnimation = Noise).SetActive(() => Settings.DestroyAnimation == Noise),
			MenuItem.Create(Translate("Back"))
		);

		private void DeityEnabledMenu() => CreateMenu(Translate("Enable Deity difficulty"), GotoMenu(PatchesMenu, 5),
			MenuItem.Create(TranslateFormatted("{0} (default)", false.YesNo()))
				.WithDescription(Translate("Hide Deity from difficulty selection."))
				.OnSelect((s, a) => Settings.DeityEnabled = false).SetActive(() => !Settings.DeityEnabled),
			MenuItem.Create(true.YesNo())
				.WithDescription(Translate("Show Deity in difficulty selection."))
				.OnSelect((s, a) => Settings.DeityEnabled = true).SetActive(() => Settings.DeityEnabled),
			MenuItem.Create(Translate("Back"))
		);

		private void ArrowHelperMenu() => CreateMenu(Translate("Enable (no keypad) arrow helper"), GotoMenu(PatchesMenu, 6),
			MenuItem.Create(TranslateFormatted("{0} (default)", false.YesNo()))
				.WithDescription(Translate("Disable movement helper hints."))
				.OnSelect((s, a) => Settings.ArrowHelper = false).SetActive(() => !Settings.ArrowHelper),
			MenuItem.Create(true.YesNo())
				.WithDescription(Translate("Enable movement helper hints."))
				.OnSelect((s, a) => Settings.ArrowHelper = true).SetActive(() => Settings.ArrowHelper),
			MenuItem.Create(Translate("Back"))
		);


		private void PathFindingMenu() => CreateMenu(Translate("Use smart PathFinding for goto"), GotoMenu(BehaviorMenu, 0),
			MenuItem.Create(TranslateFormatted("{0} (default)", false.YesNo()))
				.WithDescription(Translate("Use classic goto route behavior."))
				.OnSelect((s, a) => Settings.PathFinding = false).SetActive(() => !Settings.PathFinding),
			MenuItem.Create(true.YesNo())
				.WithDescription(Translate("Use smarter goto route selection."))
				.OnSelect((s, a) => Settings.PathFinding = true).SetActive(() => Settings.PathFinding),
			MenuItem.Create(Translate("Back"))
		);

		private void ComputerPlayerPathFindingMenu() => CreateMenu(Translate("Use smart pathfinding for computer players"), GotoMenu(BehaviorMenu, 1),
			MenuItem.Create(TranslateFormatted("{0}", false.YesNo()))
				.WithDescription(Translate("Use classic pathfinding for computer players."))
				.OnSelect((s, a) => Settings.ComputerPlayerPathFinding = false).SetActive(() => !Settings.ComputerPlayerPathFinding),
			MenuItem.Create(TranslateFormatted("{0} (default)", true.YesNo()))
				.WithDescription(Translate("Use smarter pathfinding for computer players."))
				.OnSelect((s, a) => Settings.ComputerPlayerPathFinding = true).SetActive(() => Settings.ComputerPlayerPathFinding),
			MenuItem.Create(Translate("Back"))
		);

		private void AutoSettlersMenu() => CreateMenu(Translate("Use auto settlers cheat"), GotoMenu(BehaviorMenu, 2),
			MenuItem.Create(TranslateFormatted("{0} (default)", false.YesNo()))
				.WithDescription(Translate("Settlers remain under normal player control."))
				.OnSelect((s, a) => Settings.AutoSettlers = false).SetActive(() => !Settings.AutoSettlers),
			MenuItem.Create(true.YesNo())
				.WithDescription(Translate("Enable cheat behavior for auto settlers."))
				.OnSelect((s, a) => Settings.AutoSettlers = true).SetActive(() => Settings.AutoSettlers),
			MenuItem.Create(Translate("Back"))
		);

		private void FastRiverMovementMenu() => CreateMenu(Translate("Movements on river like roads"), GotoMenu(BehaviorMenu, 3),
			MenuItem.Create(TranslateFormatted("{0} (default)", false.YesNo()))
				.WithDescription(Translate("Keep normal movement costs on rivers."))
				.OnSelect((s, a) => Settings.RiverFastMovement = false).SetActive(() => !Settings.RiverFastMovement),
			MenuItem.Create(true.YesNo())
				.WithDescription(Translate("Treat river movement similar to roads."))
				.OnSelect((s, a) => Settings.RiverFastMovement = true).SetActive(() => Settings.RiverFastMovement),
			MenuItem.Create(Translate("Back"))
		);

		private void CanalCity() => CreateMenu(Translate("No movement penalty for sea units in city"), GotoMenu(BehaviorMenu, 4),
			MenuItem.Create(TranslateFormatted("{0} (default)", false.YesNo()))
				.WithDescription(Translate("Apply normal movement penalty in city tiles."))
				.OnSelect((s, a) => Settings.CanalCity = false).SetActive(() => !Settings.CanalCity),
			MenuItem.Create(true.YesNo())
				.WithDescription(Translate("Ignore sea-unit penalty when crossing city tiles."))
				.OnSelect((s, a) => Settings.CanalCity = true).SetActive(() => Settings.CanalCity),
			MenuItem.Create(Translate("Back"))
		);

		private void RemoveObsoleteBuildingsMenu() => CreateMenu(Translate("Remove obsolete buildings"), GotoMenu(BehaviorMenu, 5),
			MenuItem.Create(false.YesNo())
				.WithDescription(
					Translate("Leave obsolete buildings in cities when"),
					Translate("new technologies are discovered that obsoletes them."))
				.OnSelect((s, a) => Settings.RemoveObsoleteBuildings = false)
				.SetActive(() => !Settings.RemoveObsoleteBuildings),
			MenuItem.Create(TranslateFormatted("{0} (default)", true.YesNo()))
				.WithDescription(
					Translate("Remove obsolete buildings in cities when new"),
					Translate("technologies are discovered. Barracks are removed"),
					Translate("when Gunpowder or Combustion is discovered."))
				.OnSelect((s, a) => Settings.RemoveObsoleteBuildings = true)
				.SetActive(() => Settings.RemoveObsoleteBuildings),
			MenuItem.Create(Translate("Back")),
			Description.Create(Translate("Return to the game behavior menu."))
		);

		private void SaveFormatMenu() => CreateMenu(Translate("AutoSave format"), GotoMenu(PatchesMenu, 8),
			MenuItem.Create(Translate("SVE with COS fallback (default)"))
				.WithDescription(
					Translate("Prefer SVE save files."),
					Translate("Fallback to COS when SVE cannot be used."))
				.OnSelect((s, a) => Settings.PreferSveSaveFormat = true).SetActive(() => Settings.PreferSveSaveFormat),
			MenuItem.Create(Translate("CivOne Save (COS)"))
				.WithDescription(Translate("Always write CivOne COS save files."))
				.OnSelect((s, a) => Settings.PreferSveSaveFormat = false).SetActive(() => !Settings.PreferSveSaveFormat),
			MenuItem.Create(Translate("Back"))
		);

		private void SaveCastBehaviorMenu() => CreateMenu(Translate("Save cast behavior"), GotoMenu(PatchesMenu, 9),
			MenuItem.Create(Translate("Checked (default)"))
				.WithDescription(Translate("Use checked casts for safer save handling."))
				.OnSelect((s, a) => Settings.UseUncheckedCastSanitizer = false).SetActive(() => !Settings.UseUncheckedCastSanitizer),
			MenuItem.Create(Translate("Unchecked (legacy)"))
				.WithDescription(Translate("Use unchecked casts for legacy compatibility."))
				.OnSelect((s, a) => Settings.UseUncheckedCastSanitizer = true).SetActive(() => Settings.UseUncheckedCastSanitizer),
			MenuItem.Create(Translate("Back"))
		);

		private void FpsCornerMenu() => CreateMenu(Translate("FPS display"), GotoMenu(PatchesMenu, 10),
			MenuItem.Create(FpsCorner.Off.ToText())
				.WithDescription(Translate("Disable FPS display."))
				.OnSelect((s, a) => Settings.FpsCorner = FpsCorner.Off).SetActive(() => Settings.FpsCorner == FpsCorner.Off),
			MenuItem.Create(FpsCorner.TopLeft.ToText())
				.WithDescription(Translate("Show FPS in the top-left corner."))
				.OnSelect((s, a) => Settings.FpsCorner = FpsCorner.TopLeft).SetActive(() => Settings.FpsCorner == FpsCorner.TopLeft),
			MenuItem.Create(FpsCorner.TopRight.ToText())
				.WithDescription(Translate("Show FPS in the top-right corner."))
				.OnSelect((s, a) => Settings.FpsCorner = FpsCorner.TopRight).SetActive(() => Settings.FpsCorner == FpsCorner.TopRight),
			MenuItem.Create(FpsCorner.BottomLeft.ToText())
				.WithDescription(Translate("Show FPS in the bottom-left corner."))
				.OnSelect((s, a) => Settings.FpsCorner = FpsCorner.BottomLeft).SetActive(() => Settings.FpsCorner == FpsCorner.BottomLeft),
			MenuItem.Create(FpsCorner.BottomRight.ToText())
				.WithDescription(Translate("Show FPS in the bottom-right corner."))
				.OnSelect((s, a) => Settings.FpsCorner = FpsCorner.BottomRight).SetActive(() => Settings.FpsCorner == FpsCorner.BottomRight),
			MenuItem.Create(Translate("Back"))
		);

		private void SeaLevelRise() => CreateMenu(Translate("Tiles replace with ocean"), GotoMenu(ExtendedGlobalWarmingMenu, 0),
			MenuItem.Create(TranslateFormatted("{0} (default)", false.YesNo()))
				.WithDescription(Translate("Keep global warming behavior without sea rise."))
				.OnSelect((s, a) => Settings.SetGlobalWarmingFlag(Settings.GlobalWarmingFeatureFlag.SeaLevelRise, false)
				).SetActive(() => !Settings.IsGlobalWarmingFlagSet(Settings.GlobalWarmingFeatureFlag.SeaLevelRise)),
			MenuItem.Create(true.YesNo())
				.WithDescription(Translate("Allow coastal tiles to turn into ocean."))
				.OnSelect((s, a) => Settings.SetGlobalWarmingFlag(Settings.GlobalWarmingFeatureFlag.SeaLevelRise, true)
				).SetActive(() => Settings.IsGlobalWarmingFlagSet(Settings.GlobalWarmingFeatureFlag.SeaLevelRise)),
			MenuItem.Create(Translate("Back"))
		);

		private void ExtendedGlobalWarmingMenu(int activeItem = 0) => CreateMenu(Translate("Extended global warming (needs savegame load)"), activeItem,
			MenuItem.Create(TranslateFormatted("Sea level rise: {0}", Settings.IsGlobalWarmingFlagSet(Settings.GlobalWarmingFeatureFlag.SeaLevelRise).YesNo()))
				.OnSelect(GotoMenu(SeaLevelRise))
				.WithDescription(Translate("Allow coastal tiles to turn into ocean.")),
			MenuItem.Create(Translate("Back")).OnSelect(GotoMenu(BehaviorMenu, 6))
		);

		private void BehaviorMenu(int activeItem = 0) => CreateMenu(Translate("Game behavior menu"), activeItem,
			MenuItem.Create(TranslateFormatted("Use smart PathFinding for goto: {0}", Settings.PathFinding.YesNo()))
				.WithDescription(
					Translate("Improve goto route selection for player units."))
				.OnSelect(GotoMenu(PathFindingMenu)),
			MenuItem.Create(TranslateFormatted("Use smart pathfinding for computer players: {0}", Settings.ComputerPlayerPathFinding.YesNo()))
				.WithDescription(
					Translate("Improve route selection for AI controlled units."))
				.OnSelect(GotoMenu(ComputerPlayerPathFindingMenu)),
			MenuItem.Create(TranslateFormatted("Use auto-settlers-cheat: {0}", Settings.AutoSettlers.YesNo()))
				.WithDescription(
					Translate("Allow cheat behavior for automatic settlers."))
				.OnSelect(GotoMenu(AutoSettlersMenu)),
			MenuItem.Create(TranslateFormatted("Use fast river movement: {0}", Settings.RiverFastMovement.YesNo()))
				.WithDescription(
					Translate("Rivers act closer to roads for movement speed."))
				.OnSelect(GotoMenu(FastRiverMovementMenu)),
			MenuItem.Create(TranslateFormatted("No movement penalty for sea units in city: {0}", Settings.CanalCity.YesNo()))
				.WithDescription(
					Translate("Sea units ignore city movement penalty."))
				.OnSelect(GotoMenu(CanalCity)),
			MenuItem.Create(TranslateFormatted("Remove obsolete buildings: {0}", Settings.RemoveObsoleteBuildings.YesNo()))
				.WithDescription(
					Translate("Remove buildings when technology obsoletes them."))
				.OnSelect(GotoMenu(RemoveObsoleteBuildingsMenu)),
			MenuItem.Create(TranslateFormatted("Extended global warming: {0}", (Settings.GlobalWarmingFeatureFlags != Settings.GlobalWarmingFeatureFlag.None).YesNo()))
				.WithDescription(
					Translate("Enable extra global warming gameplay effects."))
				.OnSelect(GotoMenu(ExtendedGlobalWarmingMenu)),
			MenuItem.Create(Translate("Back")).OnSelect(GotoMenu(PatchesMenu, 7))
		);

		private void PluginsMenu(int activeItem = 0) => CreateMenu(Translate("Plugins"), activeItem,
			Array.Empty<MenuItem<int>>()
				.Concat(
					Reflect.Plugins().Any() ?
						Reflect.Plugins().Select(x => MenuItem.Create(x.ToString()).SetEnabled(!x.Deleted).OnSelect(GotoMenu(PluginMenu(x.Id, x)))) :
						[MenuItem.Create(Translate("No plugins installed")).Disable()]
				)
				.Concat(
				[
					MenuItem.CreateSeparator(),
					MenuItem.Create(Translate("Add plugins")).OnSelect(BrowseForPlugins),
					MenuItem.Create(Translate("Back")).OnSelect(GotoMenu(MainMenu, 2))
				]).ToArray()
		);

		private Action PluginMenu(int item, Plugin plugin) => () => CreateMenu(plugin.Name, 0,
			MenuItem.Create(TranslateFormatted("Version: {0}", plugin.Version)).Disable(),
			MenuItem.Create(TranslateFormatted("Author: {0}", plugin.Author)).Disable(),
			MenuItem.Create(TranslateFormatted("Status: {0}", plugin.Enabled.EnabledDisabled())).OnSelect(GotoMenu(PluginStatusMenu(item, plugin))),
			MenuItem.Create(Translate("Delete plugin")).OnSelect(GotoMenu(PluginDeleteMenu(item, plugin))),
			MenuItem.Create(Translate("Back")).OnSelect(GotoMenu(PluginsMenu, item))
		);

		private Action PluginStatusMenu(int item, Plugin plugin) => () => CreateMenu(TranslateFormatted("{0} Status", plugin.Name), (plugin.Enabled ? 1 : 0), GotoMenu(PluginMenu(item, plugin)),
			MenuItem.Create(false.EnabledDisabled()).OnSelect((s, a) => plugin.Enabled = false),
			MenuItem.Create(true.EnabledDisabled()).OnSelect((s, a) => plugin.Enabled = true),
			MenuItem.Create(Translate("Back"))
		);

		private Action PluginDeleteMenu(int item, Plugin plugin) => () => CreateMenu(TranslateFormatted("Delete {0} from disk?", plugin.Name), 0,
			MenuItem.Create(false.YesNo()).OnSelect(GotoMenu(PluginsMenu, item)),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => plugin.Delete()).OnSelect(GotoMenu(PluginsMenu, item))
		);

		private void GameOptionsMenu(int activeItem = 0) => CreateMenu(Translate("Game Options"), activeItem,
			MenuItem.Create(TranslateFormatted("Instant Advice: {0}", Settings.InstantAdvice.ToText())).OnSelect(GotoMenu(GameOptionMenu(0, Translate("Instant Advice"), () => Settings.InstantAdvice, (GameOption option) => Settings.InstantAdvice = option))),
			MenuItem.Create(TranslateFormatted("AutoSave: {0}", Settings.AutoSave.ToText())).OnSelect(GotoMenu(GameOptionMenu(1, Translate("AutoSave"), () => Settings.AutoSave, (GameOption option) => Settings.AutoSave = option))),
			MenuItem.Create(TranslateFormatted("End of Turn: {0}", Settings.EndOfTurn.ToText())).OnSelect(GotoMenu(GameOptionMenu(2, Translate("End of Turn"), () => Settings.EndOfTurn, (GameOption option) => Settings.EndOfTurn = option))),
			MenuItem.Create(TranslateFormatted("Animations: {0}", Settings.Animations.ToText())).OnSelect(GotoMenu(GameOptionMenu(3, Translate("Animations"), () => Settings.Animations, (GameOption option) => Settings.Animations = option))),
			MenuItem.Create(TranslateFormatted("Sound: {0}", Settings.Sound.ToText())).OnSelect(GotoMenu(GameOptionMenu(4, Translate("Sound"), () => Settings.Sound, (GameOption option) => Settings.Sound = option))),
			MenuItem.Create(TranslateFormatted("Enemy Moves: {0}", Settings.EnemyMoves.ToText())).OnSelect(GotoMenu(GameOptionMenu(5, Translate("Enemy Moves"), () => Settings.EnemyMoves, (GameOption option) => Settings.EnemyMoves = option))),
			MenuItem.Create(TranslateFormatted("Civilopedia Text: {0}", Settings.CivilopediaText.ToText())).OnSelect(GotoMenu(GameOptionMenu(6, Translate("Civilopedia Text"), () => Settings.CivilopediaText, (GameOption option) => Settings.CivilopediaText = option))),
			MenuItem.Create(TranslateFormatted("Palace: {0}", Settings.Palace.ToText())).OnSelect(GotoMenu(GameOptionMenu(7, Translate("Palace"), () => Settings.Palace, (GameOption option) => Settings.Palace = option))),
			MenuItem.Create(TranslateFormatted("Tax Rate: {0}%", Settings.TaxRate * 10)).OnSelect(GotoMenu(TaxRateMenu)),
			MenuItem.Create(TranslateFormatted("Language: {0}", CurrentLanguageText())).OnSelect(GotoMenu(LanguageMenu)),
			MenuItem.Create(Translate("Back")).OnSelect(GotoMenu(MainMenu, 3))
		);

		private Action GameOptionMenu(int item, string title, Func<GameOption> getOption, Action<GameOption> setOption) => () => CreateMenu(title, GotoMenu(GameOptionsMenu, item),
			MenuItem.Create(GameOption.Default.ToText()).OnSelect((s, a) => setOption(GameOption.Default)).SetActive(() => getOption() == GameOption.Default),
			MenuItem.Create(GameOption.On.ToText()).OnSelect((s, a) => setOption(GameOption.On)).SetActive(() => getOption() == GameOption.On),
			MenuItem.Create(GameOption.Off.ToText()).OnSelect((s, a) => setOption(GameOption.Off)).SetActive(() => getOption() == GameOption.Off),
			MenuItem.Create(Translate("Back"))
		);

		private void TaxRateMenu() => CreateMenu(Translate("Window Scale"), GotoMenu(GameOptionsMenu, 8),
			MenuItem.Create(Translate(" 0% Tax, 100% Science")).OnSelect((s, a) => Settings.TaxRate = 0).SetActive(() => Settings.TaxRate == 0),
			MenuItem.Create(Translate("10% Tax,  90% Science")).OnSelect((s, a) => Settings.TaxRate = 1).SetActive(() => Settings.TaxRate == 1),
			MenuItem.Create(Translate("20% Tax,  80% Science")).OnSelect((s, a) => Settings.TaxRate = 2).SetActive(() => Settings.TaxRate == 2),
			MenuItem.Create(Translate("30% Tax,  70% Science")).OnSelect((s, a) => Settings.TaxRate = 3).SetActive(() => Settings.TaxRate == 3),
			MenuItem.Create(Translate("40% Tax,  60% Science")).OnSelect((s, a) => Settings.TaxRate = 4).SetActive(() => Settings.TaxRate == 4),
			MenuItem.Create(Translate("50% Tax,  50% Science")).OnSelect((s, a) => Settings.TaxRate = 5).SetActive(() => Settings.TaxRate == 5),
			MenuItem.Create(Translate("60% Tax,  40% Science")).OnSelect((s, a) => Settings.TaxRate = 6).SetActive(() => Settings.TaxRate == 6),
			MenuItem.Create(Translate("70% Tax,  30% Science")).OnSelect((s, a) => Settings.TaxRate = 7).SetActive(() => Settings.TaxRate == 7),
			MenuItem.Create(Translate("80% Tax,  20% Science")).OnSelect((s, a) => Settings.TaxRate = 8).SetActive(() => Settings.TaxRate == 8),
			MenuItem.Create(Translate("90% Tax,  10% Science")).OnSelect((s, a) => Settings.TaxRate = 9).SetActive(() => Settings.TaxRate == 9),
			MenuItem.Create(Translate("100% Tax,  0% Science")).OnSelect((s, a) => Settings.TaxRate = 10).SetActive(() => Settings.TaxRate == 10),
			MenuItem.Create(Translate("Back"))
		);

		private string CurrentLanguageText()
		{
			IReadOnlyList<TranslationLanguageInfo> availableLanguages = TranslationServiceFactory.GetAvailableLanguages(Runtime.StorageDirectory, message => Log(message));
			string? activePostfix = TranslationServiceFactory.ActiveLanguagePostfix;
			return string.IsNullOrEmpty(activePostfix)
				? Translate("Original (English)")
				: TranslationServiceFactory.GetLanguageDisplayName(activePostfix, availableLanguages, Translate);
		}

		private void LanguageMenu()
		{
			IReadOnlyList<TranslationLanguageInfo> availableLanguages = TranslationServiceFactory.GetAvailableLanguages(Runtime.StorageDirectory, message => Log(message));
			List<MenuItem<int>> menuItems =
			[
				MenuItem.Create(Translate("Original (default)"))
				.WithDescription(Translate("Use original text from game files without translation."))
				.OnSelect((s, a) => SelectLanguage(string.Empty)).SetActive(() => string.IsNullOrEmpty(TranslationServiceFactory.ActiveLanguagePostfix))
			];

			if (availableLanguages.Count == 0)
			{
				menuItems.Add(MenuItem.Create(Translate("No valid language files found.")).Disable());
				menuItems.Add(MenuItem.Create(Translate("Add civ_xx.txt to .../translations.")).Disable());
			}
			else
			{
				menuItems.AddRange(availableLanguages.Select(language => MenuItem.Create(TranslationServiceFactory.GetLanguageDisplayName(language, Translate))
					.OnSelect((s, a) => SelectLanguage(language.Postfix))
					.SetActive(() => string.Equals(TranslationServiceFactory.ActiveLanguagePostfix, language.Postfix, StringComparison.OrdinalIgnoreCase))));
			}

			menuItems.Add(MenuItem.Create(Translate("Back")));
			CreateMenu(Translate("Language"), GotoMenu(GameOptionsMenu, 9), [.. menuItems]);
		}

		private void SelectLanguage(string postfix)
		{
			if (string.IsNullOrEmpty(postfix))
			{
				Settings.LanguagePostfix = string.Empty;
				TranslationServiceFactory.UseIdentity();
				NotifyLanguageSelection(Translate("Original (English)"));
				GameOptionsMenu(9);
				return;
			}

			if (!TranslationServiceFactory.TryUseLanguage(Runtime.StorageDirectory, postfix, out string? message, message => Log(message)))
			{
				Log("Could not activate language '{0}': {1}", postfix, message ?? "unknown error");
				if (Game.Started)
				{
					GameTask.Enqueue(Message.Error(Translate("Language"), TranslateFormatted("Could not load language '{0}'.", postfix), message ?? "unknown error"));
				}
				GameOptionsMenu(9);
				return;
			}

			Settings.LanguagePostfix = postfix;
			IReadOnlyList<TranslationLanguageInfo> availableLanguages = TranslationServiceFactory.GetAvailableLanguages(Runtime.StorageDirectory, message => Log(message));
			NotifyLanguageSelection(TranslationServiceFactory.GetLanguageDisplayName(postfix, availableLanguages, Translate));
			GameOptionsMenu(9);
		}

		private void NotifyLanguageSelection(string languageName)
		{
			if (!Game.Started)
			{
				return;
			}

			GameTask.Enqueue(Message.General(TranslateFormatted("Language switched to {0}.", languageName)));
		}

		private void Resize(object? _, ResizeEventArgs args)
		{
			this.Clear(3);

			foreach (Menu menu in GlobalMenus["Setup"])
			{
				menu.Center(this).ForceUpdate();
			}

			_update = true;
		}

		public Setup() : base(MouseCursor.Pointer)
		{
			OnResize += Resize;

			Palette = Common.GetPalette256;
			this.Clear(3);
		}
	}
}