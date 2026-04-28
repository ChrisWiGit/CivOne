// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.UserInterface;

using static CivOne.Enums.AspectRatio;
using static CivOne.Enums.CursorType;
using static CivOne.Enums.DestroyAnimation;
using static CivOne.Enums.GraphicsMode;

namespace CivOne.Screens
{
	[Break, ScreenResizeable]
	internal class Setup : BaseScreen
	{
		private const int MenuFont = 6;

		/// <summary>
		/// List of preset options for the "Expand Size" setting, which determines the target resolution when Aspect Ratio is set to Expand. 
		/// The first option is always "Auto", which means the game will use the window size as the target resolution and stretch the canvas to fill it.
		/// If you use the other options, be warned that parts of the game may be cut off if the window is smaller than the selected resolution, 
		/// and that performance may be worse on lower-end hardware when using very high resolutions.
		/// If you add new options here, make sure to update Settings.Max* values if necessary.
		/// </summary>
		private static readonly (string Label, int Width, int Height)[] ExpandSizeOptions =
		[
			("Auto (stretch)", -1, -1),

			// SD / kleinere Formate
			("640x360", 640, 360),
			("854x480 (FWVGA)", 854, 480),
			("960x540", 960, 540),

			// HD / Standard
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
			("3840x2160 (4K UHD)", 3840, 2160),
			("4096x2160 (DCI 4K) (Warning: may be slow)", 4096, 2160),

			// Higher resolutions
			("5120x2880 (5K) (Warning: may be slow)", 5120, 2880),
			("7680x4320 (8K UHD) (Warning: may be slow)", 7680, 4320)
		];


		/// <summary>
		/// List of preset options for the "Window Size" setting, which determines the initial window size when the game is launched.
		/// If you add new options here, make sure to update Settings.Max* values if necessary.
		/// </summary>
		private static readonly (string Label, int Width, int Height)[] WindowSizeOptions =
		[
			("Auto (scale-based)", -1, -1),

			// Classic / small
			("800x600 (SVGA)", 800, 600),
			("1024x768 (XGA)", 1024, 768),

			// HD range
			("1280x720 (HD)", 1280, 720),
			("1366x768", 1366, 768),
			("1440x900 (WXGA+)", 1440, 900),
			("1600x900", 1600, 900),

			// Full HD
			("1920x1080 (FHD)", 1920, 1080),
			("1920x1200 (WUXGA)", 1920, 1200),

			// QHD range
			("2560x1440 (QHD)", 2560, 1440),
			("2560x1600 (WQXGA)", 2560, 1600),

			// Ultrawide
			("2560x1080 (UW-FHD)", 2560, 1080),
			("3440x1440 (UW-QHD)", 3440, 1440),

			// 4K+
			("3840x2160 (4K UHD) (Warning: may be slow)", 3840, 2160),
			("4096x2160 (DCI 4K) (Warning: may be slow)", 4096, 2160),

			// High-end
			("5120x2880 (5K) (Warning: may be slow)", 5120, 2880),
			("7680x4320 (8K UHD) (Warning: may be slow)", 7680, 4320)
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
			string path = Runtime.BrowseFolder("Location of Civilization for Windows sound files");
			if (path == null)
			{
				// User pressed cancel
				return;
			}

			FileSystem.CopySoundFiles(path);
		}

		private void BrowseForPlugins(object sender, MenuItemEventArgs<int> args)
		{
			string path = Runtime.BrowseFolder("Location of CivOne plugin(s)");
			if (path == null)
			{
				// User pressed cancel
				return;
			}

			CloseMenus();
			MainMenu(2);
			FileSystem.CopyPlugins(path);
		}

		private void CreateMenu(string title, int activeItem, MenuItemEventHandler<int> always, params MenuItem<int>[] items) =>
			AddMenu(new Menu("Setup", Palette)
			{
				Title = $"{title.ToUpper()}:",
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
		private void CreateMenu(string title, MenuItemEventHandler<int> always, params MenuItem<int>[] items) => CreateMenu(title, -1, always, items);
		private void CreateMenu(string title, int activeItem, params MenuItem<int>[] items) => CreateMenu(title, activeItem, null, items);
		private void CreateMenu(string title, params MenuItem<int>[] items) => CreateMenu(title, -1, null, items);

		private MenuItemEventHandler<int> GotoMenu(Action<int> action, int selectedItem = 0) => (s, a) =>
		{
			CloseMenus();
			action(selectedItem);
		};

		private MenuItemEventHandler<int> GotoMenu(Action action) => (s, a) =>
		{
			CloseMenus();
			action();
		};

		private MenuItemEventHandler<int> GotoScreen<T>(Action doneAction) where T : IScreen, new() => (s, a) =>
		{
			CloseMenus();
			T screen = new T();
			screen.Closed += (sender, args) => doneAction();
			Common.AddScreen(screen);
		};

		private MenuItemEventHandler<int> CloseScreen(Action action = null) => (s, a) =>
		{
			Destroy();
			if (action != null) action();
		};

		private void ChangeWindowTitle()
		{
			RuntimeHandler.Runtime.WindowTitle = Settings.WindowTitle;
			SettingsMenu(0);
		}

		private void MainMenu(int activeItem = 0) => CreateMenu("CivOne Setup", activeItem,
			MenuItem.Create("Settings").OnSelect(GotoMenu(SettingsMenu)),
			MenuItem.Create("Patches").OnSelect(GotoMenu(PatchesMenu)),
			MenuItem.Create("Plugins").OnSelect(GotoMenu(PluginsMenu)),
			MenuItem.Create("Game Options").OnSelect(GotoMenu(GameOptionsMenu)),
			MenuItem.Create(Game.Started ? "Return to game" : "Launch Game").OnSelect(CloseScreen()),
			Game.Started ? null : MenuItem.Create("Quit").OnSelect(CloseScreen(Runtime.Quit))
		);

		private void SettingsMenu(int activeItem = 0) => CreateMenu("Settings", activeItem,
			MenuItem.Create($"Window Title: {Settings.WindowTitle}").OnSelect(GotoScreen<WindowTitle>(ChangeWindowTitle)),
			MenuItem.Create($"Graphics Mode: {Settings.GraphicsMode.ToText()}").OnSelect(GotoMenu(GraphicsModeMenu)),
			MenuItem.Create($"Aspect Ratio: {Settings.AspectRatio.ToText()}").OnSelect(GotoMenu(AspectRatioMenu)),
			MenuItem.Create($"Expand Size: {ExpandSizeText()}").OnSelect(GotoMenu(ExpandCanvasSizeMenu)),
			MenuItem.Create($"Full Screen: {Settings.FullScreen.YesNo()}").OnSelect(GotoMenu(FullScreenMenu)),
			MenuItem.Create($"Window Size: {WindowSizeText()}").OnSelect(GotoMenu(WindowSizeMenu)),
			MenuItem.Create($"Window Scale: {Settings.Scale}x").OnSelect(GotoMenu(WindowScaleMenu)),
			MenuItem.Create("In-game sound").OnSelect(GotoMenu(SoundMenu)),
			MenuItem.Create($"Back").OnSelect(GotoMenu(MainMenu, 0))
		);

		private void GraphicsModeMenu() => CreateMenu("Graphics Mode", GotoMenu(SettingsMenu, 1),
			MenuItem.Create($"{Graphics256.ToText()} (default)").OnSelect((s, a) => Settings.GraphicsMode = Graphics256).SetActive(() => Settings.GraphicsMode == Graphics256),
			MenuItem.Create(Graphics16.ToText()).OnSelect((s, a) => Settings.GraphicsMode = Graphics16).SetActive(() => Settings.GraphicsMode == Graphics16),
			MenuItem.Create("Back")
		);

		private void AspectRatioMenu() => CreateMenu("Aspect Ratio", GotoMenu(SettingsMenu, 2),
			MenuItem.Create($"{Auto.ToText()} (default)").OnSelect((s, a) => Settings.AspectRatio = Auto).SetActive(() => Settings.AspectRatio == Auto),
			MenuItem.Create(Fixed.ToText()).OnSelect((s, a) => Settings.AspectRatio = Fixed).SetActive(() => Settings.AspectRatio == Fixed),
			MenuItem.Create(Scaled.ToText()).OnSelect((s, a) => Settings.AspectRatio = Scaled).SetActive(() => Settings.AspectRatio == Scaled),
			MenuItem.Create(ScaledFixed.ToText()).OnSelect((s, a) => Settings.AspectRatio = ScaledFixed).SetActive(() => Settings.AspectRatio == ScaledFixed),
			MenuItem.Create(AspectRatio.Expand.ToText()).OnSelect((s, a) => Settings.AspectRatio = AspectRatio.Expand).SetActive(() => Settings.AspectRatio == AspectRatio.Expand),
			MenuItem.Create("Back")
		);

		private string SizeText(int width, int height, string autoText)
			=> width <= 0 || height <= 0 ? autoText : $"{width}x{height}";

		private string ExpandSizeText() => SizeText(Settings.ExpandWidth, Settings.ExpandHeight, "Auto");

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
				MenuItem.Create("Back")
			];

		private void ExpandCanvasSizeMenu() => CreateMenu(
			"Expand Size (only if Aspect Ratio is set to Expand)",
			GotoMenu(SettingsMenu, 3),
			BuildSizeMenuItems(
				ExpandSizeOptions,
				(width, height) => IsActiveSize(Settings.ExpandWidth, Settings.ExpandHeight, width, height),
				SetExpandSize));

		private void FullScreenMenu() => CreateMenu("Full Screen", GotoMenu(SettingsMenu, 4),
			MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) => Settings.FullScreen = false).SetActive(() => !Settings.FullScreen),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.FullScreen = true).SetActive(() => Settings.FullScreen),
			MenuItem.Create("Back")
		);

		private string WindowSizeText() => SizeText(Settings.WindowWidth, Settings.WindowHeight, "Auto");

		private void SetWindowSizePreset(int width, int height)
		{
			Settings.WindowWidth = width;
			Settings.WindowHeight = height;
		}

		private void WindowSizeMenu() => CreateMenu(
			"Window Size",
			GotoMenu(SettingsMenu, 5),
			BuildSizeMenuItems(
				WindowSizeOptions,
				(width, height) => IsActiveSize(Settings.WindowWidth, Settings.WindowHeight, width, height),
				SetWindowSizePreset));

		private void WindowScaleMenu() => CreateMenu("Window Scale", GotoMenu(SettingsMenu, 6),
			MenuItem.Create("1x").OnSelect((s, a) => Settings.Scale = 1).SetActive(() => Settings.Scale == 1),
			MenuItem.Create("2x (default)").OnSelect((s, a) => Settings.Scale = 2).SetActive(() => Settings.Scale == 2),
			MenuItem.Create("3x").OnSelect((s, a) => Settings.Scale = 3).SetActive(() => Settings.Scale == 3),
			MenuItem.Create("4x").OnSelect((s, a) => Settings.Scale = 4).SetActive(() => Settings.Scale == 4),
			MenuItem.Create("5x").OnSelect((s, a) => Settings.Scale = 5).SetActive(() => Settings.Scale == 5),
			MenuItem.Create("6x").OnSelect((s, a) => Settings.Scale = 6).SetActive(() => Settings.Scale == 6),
			MenuItem.Create("7x").OnSelect((s, a) => Settings.Scale = 7).SetActive(() => Settings.Scale == 7),
			MenuItem.Create("8x").OnSelect((s, a) => Settings.Scale = 8).SetActive(() => Settings.Scale == 8),
			MenuItem.Create("Back")
		);

		private void SoundMenu() => CreateMenu("In-game sound", GotoMenu(SettingsMenu, 7),
			MenuItem.Create("Browse for files...").OnSelect(BrowseForSoundFiles).SetEnabled(!FileSystem.SoundFilesExist()).SetEnabled(!Game.Started),
			MenuItem.Create("Back")
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

		private void PatchesMenu(int activeItem = 0) => CreateMenu("Patches", activeItem,
			MenuItem.Create($"Reveal world: {Settings.RevealWorld.YesNo()}").OnSelect(GotoMenu(RevealWorldMenu)),
			MenuItem.Create($"Side bar location: {(Settings.RightSideBar ? "right" : "left")}{(Game.Started ? " (restart required)" : "")}").OnSelect(GotoMenu(SideBarMenu)),
			MenuItem.Create($"Debug menu: {Settings.DebugMenu.YesNo()}").OnSelect(GotoMenu(DebugMenuMenu)).SetEnabled(!Game.Started),
			MenuItem.Create($"Cursor type: {Settings.CursorType.ToText()}").OnSelect(GotoMenu(CursorTypeMenu)),
			MenuItem.Create($"Destroy animation: {Settings.DestroyAnimation.ToText()}").OnSelect(GotoMenu(DestroyAnimationMenu)),
			MenuItem.Create($"Enable Deity difficulty: {Settings.DeityEnabled.YesNo()}").OnSelect(GotoMenu(DeityEnabledMenu)),
			MenuItem.Create($"Enable (no keypad) arrow helper: {Settings.ArrowHelper.YesNo()}").OnSelect(GotoMenu(ArrowHelperMenu)),
			MenuItem.Create($"Custom map sizes (experimental): {Settings.CustomMapSize.YesNo()}").OnSelect(GotoMenu(CustomMapSizeMenu)),
			MenuItem.Create($"Game behavior menu: {ActiveBehaviorPatchCount()} active").OnSelect(GotoMenu(BehaviorMenu)),
			MenuItem.Create($"AutoSave format: {(Settings.PreferSveSaveFormat ? "SVE (fallback COS)" : "COS")}").OnSelect(GotoMenu(SaveFormatMenu)),
			MenuItem.Create($"Save cast behavior: {(Settings.UseUncheckedCastSanitizer ? "Unchecked" : "Checked")}").OnSelect(GotoMenu(SaveCastBehaviorMenu)),
			MenuItem.Create("Back").OnSelect(GotoMenu(MainMenu, 1))
		);

		private void RevealWorldMenu() => CreateMenu("Reveal world", GotoMenu(PatchesMenu, 0),
			MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) => Settings.RevealWorld = false).SetActive(() => !Settings.RevealWorld),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.RevealWorld = true).SetActive(() => Settings.RevealWorld),
			MenuItem.Create("Back")
		);

		private void SideBarMenu() => CreateMenu("Side bar location", GotoMenu(PatchesMenu, 1),
			MenuItem.Create("Left (default)").OnSelect((s, a) => Settings.RightSideBar = false).SetActive(() => !Settings.RightSideBar),
			MenuItem.Create("Right").OnSelect((s, a) => Settings.RightSideBar = true).SetActive(() => Settings.RightSideBar),
			MenuItem.Create("Back")
		);

		private void DebugMenuMenu() => CreateMenu("Show debug menu", GotoMenu(PatchesMenu, 2),
			MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) => Settings.DebugMenu = false || Game.Started).SetActive(() => !Settings.DebugMenu || Game.Started),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.DebugMenu = true).SetActive(() => Settings.DebugMenu),
			MenuItem.Create("Back")
		);

		private void CursorTypeMenu() => CreateMenu("Mouse cursor type", GotoMenu(PatchesMenu, 3),
			MenuItem.Create(Default.ToText()).OnSelect((s, a) => Settings.CursorType = Default).SetActive(() => Settings.CursorType == Default && FileSystem.DataFilesExist(FileSystem.MouseCursorFiles)).SetEnabled(FileSystem.DataFilesExist(FileSystem.MouseCursorFiles)),
			MenuItem.Create(Builtin.ToText()).OnSelect((s, a) => Settings.CursorType = Builtin).SetActive(() => Settings.CursorType == Builtin || (Settings.CursorType == Default && !FileSystem.DataFilesExist(FileSystem.MouseCursorFiles))),
			MenuItem.Create(Native.ToText()).OnSelect((s, a) => Settings.CursorType = Native).SetActive(() => Settings.CursorType == Native),
			MenuItem.Create("Back")
		);

		private void DestroyAnimationMenu() => CreateMenu("Destroy animation", GotoMenu(PatchesMenu, 4),
			MenuItem.Create(Sprites.ToText()).OnSelect((s, a) => Settings.DestroyAnimation = Sprites).SetActive(() => Settings.DestroyAnimation == Sprites),
			MenuItem.Create(Noise.ToText()).OnSelect((s, a) => Settings.DestroyAnimation = Noise).SetActive(() => Settings.DestroyAnimation == Noise),
			MenuItem.Create("Back")
		);

		private void DeityEnabledMenu() => CreateMenu("Enable Deity difficulty", GotoMenu(PatchesMenu, 5),
			MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) => Settings.DeityEnabled = false).SetActive(() => !Settings.DeityEnabled),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.DeityEnabled = true).SetActive(() => Settings.DeityEnabled),
			MenuItem.Create("Back")
		);

		private void ArrowHelperMenu() => CreateMenu("Enable (no keypad) arrow helper", GotoMenu(PatchesMenu, 6),
			MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) => Settings.ArrowHelper = false).SetActive(() => !Settings.ArrowHelper),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.ArrowHelper = true).SetActive(() => Settings.ArrowHelper),
			MenuItem.Create("Back")
		);

		private void CustomMapSizeMenu() => CreateMenu("Custom map sizes (experimental)", GotoMenu(PatchesMenu, 7),
			MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) => Settings.CustomMapSize = false).SetActive(() => !Settings.CustomMapSize),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.CustomMapSize = true).SetActive(() => Settings.CustomMapSize),
			MenuItem.Create("Back")
		);


		private void PathFindingMenu() => CreateMenu("Use smart PathFinding for \"goto\"", GotoMenu(BehaviorMenu, 0),
		MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) => Settings.PathFinding = false).SetActive(() => !Settings.PathFinding),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.PathFinding = true).SetActive(() => Settings.PathFinding),
			MenuItem.Create("Back")
		);

		private void AutoSettlersMenu() => CreateMenu("Use auto settlers cheat", GotoMenu(BehaviorMenu, 1),
			MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) => Settings.AutoSettlers = false).SetActive(() => !Settings.AutoSettlers),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.AutoSettlers = true).SetActive(() => Settings.AutoSettlers),
			MenuItem.Create("Back")
		);

		private void FastRiverMovementMenu() => CreateMenu("Movements on river like roads", GotoMenu(BehaviorMenu, 2),
			MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) => Settings.RiverFastMovement = false).SetActive(() => !Settings.RiverFastMovement),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.RiverFastMovement = true).SetActive(() => Settings.RiverFastMovement),
			MenuItem.Create("Back")
		);

		private void CanalCity() => CreateMenu("No movement penalty for sea units in city", GotoMenu(BehaviorMenu, 3),
			MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) => Settings.CanalCity = false).SetActive(() => !Settings.CanalCity),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.CanalCity = true).SetActive(() => Settings.CanalCity),
			MenuItem.Create("Back")
		);

		private void SaveFormatMenu() => CreateMenu("AutoSave format", GotoMenu(PatchesMenu, 9),
			MenuItem.Create("SVE with COS fallback (default)").OnSelect((s, a) => Settings.PreferSveSaveFormat = true).SetActive(() => Settings.PreferSveSaveFormat),
			MenuItem.Create("CivOne Save (COS)").OnSelect((s, a) => Settings.PreferSveSaveFormat = false).SetActive(() => !Settings.PreferSveSaveFormat),
			MenuItem.Create("Back")
		);

		private void SaveCastBehaviorMenu() => CreateMenu("Save cast behavior", GotoMenu(PatchesMenu, 10),
			MenuItem.Create("Checked (default)").OnSelect((s, a) => Settings.UseUncheckedCastSanitizer = false).SetActive(() => !Settings.UseUncheckedCastSanitizer),
			MenuItem.Create("Unchecked (legacy)").OnSelect((s, a) => Settings.UseUncheckedCastSanitizer = true).SetActive(() => Settings.UseUncheckedCastSanitizer),
			MenuItem.Create("Back")
		);

		private void SeaLevelRise() => CreateMenu("Tiles replace with ocean", GotoMenu(ExtendedGlobalWarmingMenu, 0),
			MenuItem.Create($"{false.YesNo()} (default)").OnSelect((s, a) =>  Settings.SetGlobalWarmingFlag(Settings.GlobalWarmingFeatureFlag.SeaLevelRise, false)
				).SetActive(() => !Settings.IsGlobalWarmingFlagSet(Settings.GlobalWarmingFeatureFlag.SeaLevelRise)),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => Settings.SetGlobalWarmingFlag(Settings.GlobalWarmingFeatureFlag.SeaLevelRise, true)
				).SetActive(() => Settings.IsGlobalWarmingFlagSet(Settings.GlobalWarmingFeatureFlag.SeaLevelRise)),
			MenuItem.Create("Back")
		);

		private void ExtendedGlobalWarmingMenu(int activeItem = 0) => CreateMenu("Extended global warming (needs savegame load)", activeItem,
			MenuItem.Create($"Sea level rise: {Settings.IsGlobalWarmingFlagSet(Settings.GlobalWarmingFeatureFlag.SeaLevelRise).YesNo()}")
				.OnSelect(GotoMenu(SeaLevelRise)),
			MenuItem.Create("Back").OnSelect(GotoMenu(BehaviorMenu, 4))
		);

		private void BehaviorMenu(int activeItem = 0) => CreateMenu("Game behavior menu", activeItem,
			[
					MenuItem.Create($"Use smart PathFinding for \"goto\": {Settings.PathFinding.YesNo()}").OnSelect(GotoMenu(PathFindingMenu)),
					MenuItem.Create($"Use auto-settlers-cheat: {Settings.AutoSettlers.YesNo()}").OnSelect(GotoMenu(AutoSettlersMenu)),
					MenuItem.Create($"Use fast river movement: {Settings.RiverFastMovement.YesNo()}").OnSelect(GotoMenu(FastRiverMovementMenu)),
					MenuItem.Create($"No movement penalty for sea units in city: {Settings.CanalCity.YesNo()}").OnSelect(GotoMenu(CanalCity)),
					MenuItem.Create($"Extended global warming: {(Settings.GlobalWarmingFeatureFlags != Settings.GlobalWarmingFeatureFlag.None).YesNo()}").OnSelect(GotoMenu(ExtendedGlobalWarmingMenu)),
					MenuItem.Create("Back").OnSelect(GotoMenu(PatchesMenu, 8))
			]
		);

		private void PluginsMenu(int activeItem = 0) => CreateMenu("Plugins", activeItem,
			new MenuItem<int>[0]
				.Concat(
					Reflect.Plugins().Any() ?
						Reflect.Plugins().Select(x => MenuItem.Create(x.ToString()).SetEnabled(!x.Deleted).OnSelect(GotoMenu(PluginMenu(x.Id, x)))) :
						new[] { MenuItem.Create("No plugins installed").Disable() }
				)
				.Concat(new[]
				{
					MenuItem.Create(null).Disable(),
					MenuItem.Create("Add plugins").OnSelect(BrowseForPlugins),
					MenuItem.Create("Back").OnSelect(GotoMenu(MainMenu, 2))
				}).ToArray()
		);

		private Action PluginMenu(int item, Plugin plugin) => () => CreateMenu(plugin.Name, 0,
			MenuItem.Create($"Version: {plugin.Version}").Disable(),
			MenuItem.Create($"Author: {plugin.Author}").Disable(),
			MenuItem.Create($"Status: {plugin.Enabled.EnabledDisabled()}").OnSelect(GotoMenu(PluginStatusMenu(item, plugin))),
			MenuItem.Create($"Delete plugin").OnSelect(GotoMenu(PluginDeleteMenu(item, plugin))),
			MenuItem.Create("Back").OnSelect(GotoMenu(PluginsMenu, item))
		);

		private Action PluginStatusMenu(int item, Plugin plugin) => () => CreateMenu($"{plugin.Name} Status", (plugin.Enabled ? 1 : 0), GotoMenu(PluginMenu(item, plugin)),
			MenuItem.Create(false.EnabledDisabled()).OnSelect((s, a) => plugin.Enabled = false),
			MenuItem.Create(true.EnabledDisabled()).OnSelect((s, a) => plugin.Enabled = true),
			MenuItem.Create("Back")
		);

		private Action PluginDeleteMenu(int item, Plugin plugin) => () => CreateMenu($"Delete {plugin.Name} from disk?", 0,
			MenuItem.Create(false.YesNo()).OnSelect(GotoMenu(PluginsMenu, item)),
			MenuItem.Create(true.YesNo()).OnSelect((s, a) => plugin.Delete()).OnSelect(GotoMenu(PluginsMenu, item))
		);

		private void GameOptionsMenu(int activeItem = 0) => CreateMenu("Game Options", activeItem,
			MenuItem.Create($"Instant Advice: {Settings.InstantAdvice.ToText()}").OnSelect(GotoMenu(GameOptionMenu(0, "Instant Advice", () => Settings.InstantAdvice, (GameOption option) => Settings.InstantAdvice = option))),
			MenuItem.Create($"AutoSave: {Settings.AutoSave.ToText()}").OnSelect(GotoMenu(GameOptionMenu(1, "AutoSave", () => Settings.AutoSave, (GameOption option) => Settings.AutoSave = option))),
			MenuItem.Create($"End of Turn: {Settings.EndOfTurn.ToText()}").OnSelect(GotoMenu(GameOptionMenu(2, "End of Turn", () => Settings.EndOfTurn, (GameOption option) => Settings.EndOfTurn = option))),
			MenuItem.Create($"Animations: {Settings.Animations.ToText()}").OnSelect(GotoMenu(GameOptionMenu(3, "Animations", () => Settings.Animations, (GameOption option) => Settings.Animations = option))),
			MenuItem.Create($"Sound: {Settings.Sound.ToText()}").OnSelect(GotoMenu(GameOptionMenu(4, "Sound", () => Settings.Sound, (GameOption option) => Settings.Sound = option))),
			MenuItem.Create($"Enemy Moves: {Settings.EnemyMoves.ToText()}").OnSelect(GotoMenu(GameOptionMenu(5, "Enemy Moves", () => Settings.EnemyMoves, (GameOption option) => Settings.EnemyMoves = option))),
			MenuItem.Create($"Civilopedia Text: {Settings.CivilopediaText.ToText()}").OnSelect(GotoMenu(GameOptionMenu(6, "Civilopedia Text", () => Settings.CivilopediaText, (GameOption option) => Settings.CivilopediaText = option))),
			MenuItem.Create($"Palace: {Settings.Palace.ToText()}").OnSelect(GotoMenu(GameOptionMenu(7, "Palace", () => Settings.Palace, (GameOption option) => Settings.Palace = option))),
			MenuItem.Create($"Tax Rate: {Settings.TaxRate * 10}%").OnSelect(GotoMenu(TaxRateMenu)),
			MenuItem.Create("Back").OnSelect(GotoMenu(MainMenu, 3))
		);

		private Action GameOptionMenu(int item, string title, Func<GameOption> getOption, Action<GameOption> setOption) => () => CreateMenu(title, GotoMenu(GameOptionsMenu, item),
			MenuItem.Create(GameOption.Default.ToText()).OnSelect((s, a) => setOption(GameOption.Default)).SetActive(() => getOption() == GameOption.Default),
			MenuItem.Create(GameOption.On.ToText()).OnSelect((s, a) => setOption(GameOption.On)).SetActive(() => getOption() == GameOption.On),
			MenuItem.Create(GameOption.Off.ToText()).OnSelect((s, a) => setOption(GameOption.Off)).SetActive(() => getOption() == GameOption.Off),
			MenuItem.Create("Back")
		);

		private void TaxRateMenu() => CreateMenu("Window Scale", GotoMenu(GameOptionsMenu, 8),
			MenuItem.Create(" 0% Tax, 100% Science").OnSelect((s, a) => Settings.TaxRate = 0).SetActive(() => Settings.TaxRate == 0),
			MenuItem.Create("10% Tax,  90% Science").OnSelect((s, a) => Settings.TaxRate = 1).SetActive(() => Settings.TaxRate == 1),
			MenuItem.Create("20% Tax,  80% Science").OnSelect((s, a) => Settings.TaxRate = 2).SetActive(() => Settings.TaxRate == 2),
			MenuItem.Create("30% Tax,  70% Science").OnSelect((s, a) => Settings.TaxRate = 3).SetActive(() => Settings.TaxRate == 3),
			MenuItem.Create("40% Tax,  60% Science").OnSelect((s, a) => Settings.TaxRate = 4).SetActive(() => Settings.TaxRate == 4),
			MenuItem.Create("50% Tax,  50% Science").OnSelect((s, a) => Settings.TaxRate = 5).SetActive(() => Settings.TaxRate == 5),
			MenuItem.Create("60% Tax,  40% Science").OnSelect((s, a) => Settings.TaxRate = 6).SetActive(() => Settings.TaxRate == 6),
			MenuItem.Create("70% Tax,  30% Science").OnSelect((s, a) => Settings.TaxRate = 7).SetActive(() => Settings.TaxRate == 7),
			MenuItem.Create("80% Tax,  20% Science").OnSelect((s, a) => Settings.TaxRate = 8).SetActive(() => Settings.TaxRate == 8),
			MenuItem.Create("90% Tax,  10% Science").OnSelect((s, a) => Settings.TaxRate = 9).SetActive(() => Settings.TaxRate == 9),
			MenuItem.Create("100% Tax,  0% Science").OnSelect((s, a) => Settings.TaxRate = 10).SetActive(() => Settings.TaxRate == 10),
			MenuItem.Create("Back")
		);

		private void Resize(object sender, ResizeEventArgs args)
		{
			this.Clear(3);

			foreach (Menu menu in Menus["Setup"])
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