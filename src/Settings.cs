// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using System.IO;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Persistence.Factories;

namespace CivOne
{
	public class Settings : ISettings
	{
		private static IRuntime Runtime => RuntimeHandler.Runtime;
		//private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		// ── Window / OS canvas ────────────────────────────────────────────────
		// Hard limits for the physical OS window size and the raw SDL canvas.
		public const int MinWidth  = 320;
		public const int MinHeight = 200;
		public const int MaxWindowWidth  = 7680;  // maximum OS window width  (8K)
		public const int MaxWindowHeight = 4320;  // maximum OS window height (8K)

		// ── Expand mode stored values ──────────────────────────────────────
		// Upper bound for the ExpandWidth / ExpandHeight values that the user
		// can configure and that are persisted in the profile.  These may be
		// as large as the largest supported display.
		public const int MaxExpandWidth  = 7680;
		public const int MaxExpandHeight = 4320;

		// ── Effective gameplay surface ─────────────────────────────────────
		// Cap on the logical canvas size that game-logic code (tile counts,
		// map queries, etc.) ever sees via RuntimeHandler.CanvasWidth/Height.
		// Keeping this small prevents _tilesX/_tilesY from growing beyond what
		// Map.QueryMapPart can safely handle.
		public const int MaxScreenWidth  = 512;
		public const int MaxScreenHeight = 384;

		// ── Auto-expand fallback ───────────────────────────────────────────
		// When AspectRatio=Auto and no explicit expand size is stored, the
		// canvas is capped to these values instead of the full window size.
		public const int AutoExpandMaxWidth  = 1280;
		public const int AutoExpandMaxHeight = 720;

		// Set default settings
		private string _windowTitle = "CivOne";
		private GraphicsMode _graphicsMode = GraphicsMode.Graphics256;
		private bool _fullScreen = false;
		private int _windowWidth = -1, _windowHeight = -1;
		private Point _windowPosition = new Point(-1, -1);
		private bool _windowMaximized = false;
		private bool _rightSideBar = false;
		private int _scale = 2;
		private AspectRatio _aspectRatio = AspectRatio.Auto;
		private int _expandWidth, _expandHeight;
		private bool _revealWorld = false;
		private bool _debugMenu = false;
		private bool _deityEnabled = false;
		private bool _arrowHelper = false;
		private bool _customMapSize = false;
		private bool _pathFinding = false;
		private bool _computerPlayerPathFinding = true;
		private bool _riverFastMovement = false;
		private bool _canalCity = false;
		private bool _removeObsoleteBuildings = true;
		private bool _preferSveSaveFormat = true;
		private string _languagePostfix = string.Empty;
		private SimulateInternationalFont _simulateInternationalFont = SimulateInternationalFont.Auto;
		private bool _useUncheckedCastSanitizer = false;
		private GlobalWarmingFeatureFlag _globalWarmingFeatureFlags = GlobalWarmingFeatureFlag.None;
        private bool _autoSettlers;
		private CursorType _cursorType = CursorType.Default;
		private DestroyAnimation _destroyAnimation = DestroyAnimation.Sprites;
		private GameOption _instantAdvice, _autoSave, _endOfTurn, _animations, _sound, _enemyMoves, _civilopediaText, _palace;
        private int _taxRate = 5;

		internal string StorageDirectory => Runtime.StorageDirectory;
		internal string CaptureDirectory => Path.Combine(StorageDirectory, "capture");
		internal string DataDirectory => Path.Combine(StorageDirectory, "data");
		internal string PluginsDirectory => Path.Combine(StorageDirectory, "plugins");
		internal string SavesDirectory => Path.Combine(StorageDirectory, "saves");
		internal string CosSavesDirectory => Path.Combine(StorageDirectory, "saves", "cos");
		internal string SoundsDirectory => Path.Combine(StorageDirectory, "sounds");

		string ISettings.SavesDirectory => SavesDirectory;
		string ISettings.CosSavesDirectory => CosSavesDirectory;

		// Settings

		internal string WindowTitle
		{
			get => _windowTitle;
			set
			{
				_windowTitle = value;
				SetSetting("WindowTitle", _windowTitle);
				Common.ReloadSettings = true;
			}
		}
		
		internal GraphicsMode GraphicsMode
		{
			get => _graphicsMode;
			set
			{
				_graphicsMode = value;
				string saveValue = _graphicsMode == GraphicsMode.Graphics256 ? "1" : "2";
				SetSetting("GraphicsMode", saveValue);
				Common.ReloadSettings = true;
				
				Resources.ClearInstance();
			}
		}
		
		public bool FullScreen
		{
			get => _fullScreen;
			set
			{
				_fullScreen = value;
				SetSetting("FullScreen", _fullScreen ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

		public int WindowWidth
		{
			get => _windowWidth;
			set
			{
				_windowWidth = value;
				SetSetting("WindowWidth", _windowWidth.ToString());
			}
		}

		public int WindowHeight
		{
			get => _windowHeight;
			set
			{
				_windowHeight = value;
				SetSetting("WindowHeight", _windowHeight.ToString());
			}
		}

		public Point WindowPosition
		{
			get => _windowPosition;
			set
			{
				_windowPosition = value;
				SetSetting("WindowPosX", _windowPosition.X.ToString());
				SetSetting("WindowPosY", _windowPosition.Y.ToString());
			}
		}

		public bool WindowMaximized
		{
			get => _windowMaximized;
			set
			{
				_windowMaximized = value;
				SetSetting("WindowMaximized", _windowMaximized ? "1" : "0");
			}
		}
		
		public int Scale
		{
			get => _scale;
			set
			{
				if (value < 1 || value > 8) return;
				_scale = value;
				SetSetting("Scale", _scale.ToString());
				Common.ReloadSettings = true;
			}
		}

		public AspectRatio AspectRatio
		{
			get => _aspectRatio;
			set
			{
				_aspectRatio = value;
				string saveValue = ((int)_aspectRatio).ToString();
				SetSetting("AspectRatio", saveValue);
				Common.ReloadSettings = true;
			}
		}

		public int ExpandWidth
		{
			get => _expandWidth;
			set
			{
				_expandWidth = value;
				string saveValue = ((int)_expandWidth).ToString();
				SetSetting("ExpandWidth", saveValue);
				Common.ReloadSettings = true;
			}
		}

		public int ExpandHeight
		{
			get => _expandHeight;
			set
			{
				_expandHeight = value;
				string saveValue = ((int)_expandHeight).ToString();
				SetSetting("ExpandHeight", saveValue);
				Common.ReloadSettings = true;
			}
		}

		// Patches
		
		internal bool RevealWorld
		{
			get => _revealWorld;
			set
			{
				_revealWorld = value;
				SetSetting("RevealWorld", _revealWorld ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}
		
		internal bool RightSideBar
		{
			get => _rightSideBar;
			set
			{
				_rightSideBar = value;
				SetSetting("SideBar", _rightSideBar ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}
		
		internal bool DebugMenu
		{
			get => _debugMenu;
			set
			{
				_debugMenu = value;
				SetSetting("DebugMenu", _debugMenu ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}
		
		internal bool DeityEnabled
		{
			get => _deityEnabled;
			set
			{
				_deityEnabled = value;
				SetSetting("DeityEnabled", _deityEnabled ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

		internal bool ArrowHelper
		{
			get => _arrowHelper;
			set
			{
				_arrowHelper = value;
				SetSetting("ArrowHelper", _arrowHelper ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

		internal bool CustomMapSize
		{
			get => _customMapSize;
			set
			{
				_customMapSize = value;
				SetSetting("CustomMapSize", _customMapSize ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

		internal bool PathFinding
		{
			get => _pathFinding;
			set
			{
				_pathFinding = value;
				SetSetting("PathFindingAlgorithm", _pathFinding ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

		internal bool ComputerPlayerPathFinding
		{
			get => _computerPlayerPathFinding;
			set
			{
				_computerPlayerPathFinding = value;
				SetSetting("ComputerPlayerPathFindingAlgorithm", _computerPlayerPathFinding ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

		internal bool RiverFastMovement
		{
			get => _riverFastMovement;
			set
			{
				_riverFastMovement = value;
				SetSetting("RiverFastMovement", _riverFastMovement ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

		internal bool CanalCity
		{
			get => _canalCity;
			set
			{
				_canalCity = value;
				SetSetting("CanalCity", _canalCity ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

		internal bool RemoveObsoleteBuildings
		{
			get => _removeObsoleteBuildings;
			set
			{
				_removeObsoleteBuildings = value;
				SetSetting("RemoveObsoleteBuildings", _removeObsoleteBuildings ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

		internal bool PreferSveSaveFormat
		{
			get => _preferSveSaveFormat;
			set
			{
				_preferSveSaveFormat = value;
				SetSetting("PreferSveSaveFormat", _preferSveSaveFormat ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

		internal bool UseUncheckedCastSanitizer
		{
			get => _useUncheckedCastSanitizer;
			set
			{
				_useUncheckedCastSanitizer = value;
				SetSetting("UseUncheckedCastSanitizer", _useUncheckedCastSanitizer ? "1" : "0");
				ValueSanitizerFactory.SetRuntimeUseUncheckedCastSanitizer(_useUncheckedCastSanitizer);
				Common.ReloadSettings = true;
			}
		}

		[Flags]
		public enum GlobalWarmingFeatureFlag
		{
			None = 0,
			SeaLevelRise = 1
		}
		

		internal GlobalWarmingFeatureFlag GlobalWarmingFeatureFlags
		{
			get => _globalWarmingFeatureFlags;
			set
			{
				_globalWarmingFeatureFlags = value;
				SetSetting("GlobalWarmingFeatureFlags", ((int)value).ToString());
				Common.ReloadSettings = true;
			}
		}

		public bool IsGlobalWarmingFlagSet(GlobalWarmingFeatureFlag flag)
		{
			return _globalWarmingFeatureFlags.HasFlag(flag);
		}

		public void SetGlobalWarmingFlag(GlobalWarmingFeatureFlag flag, bool enabled)
		{
			if (enabled)
				_globalWarmingFeatureFlags |= flag;
			else
				_globalWarmingFeatureFlags &= ~flag;

			SetSetting("GlobalWarmingFeatureFlags", ((int)_globalWarmingFeatureFlags).ToString());
			Common.ReloadSettings = true;
		}

        internal bool AutoSettlers
		{
			get => _autoSettlers;
			set
			{
				_autoSettlers = value;
				SetSetting("AutoSettlers", _autoSettlers ? "1" : "0");
				Common.ReloadSettings = true;
			}
		}

        public CursorType CursorType
		{
			get
			{
				if (Runtime.Settings.Free && _cursorType == CursorType.Default)
					return CursorType.Builtin;
				return _cursorType;
			}
			internal set
			{
				_cursorType = value;
				string saveValue = ((int)_cursorType).ToString();
				SetSetting("CursorType", saveValue);
				Cursor.ClearCache();
				Common.ReloadSettings = true;
			}
		}

		internal DestroyAnimation DestroyAnimation
		{
			get => _destroyAnimation;
			set
			{
				_destroyAnimation = value;
				string saveValue = ((int)_destroyAnimation).ToString();
				SetSetting("DestroyAnimation", saveValue);
				Common.ReloadSettings = true;
			}
		}

		// Game options
		public GameOption InstantAdvice
		{
			get => _instantAdvice;
			set
			{
				_instantAdvice = value;
				string saveValue = ((int)_instantAdvice).ToString();
				SetSetting("GameInstantAdvice", saveValue);
				Common.ReloadSettings = true;
			}
		}

		public GameOption AutoSave
		{
			get => _autoSave;
			set
			{
				_autoSave = value;
				string saveValue = ((int)_autoSave).ToString();
				SetSetting("GameAutoSave", saveValue);
				Common.ReloadSettings = true;
			}
		}

		public GameOption EndOfTurn
		{
			get => _endOfTurn;
			set
			{
				_endOfTurn = value;
				string saveValue = ((int)_endOfTurn).ToString();
				SetSetting("GameEndOfTurn", saveValue);
				Common.ReloadSettings = true;
			}
		}

		public GameOption Animations
		{
			get => _animations;
			set
			{
				_animations = value;
				string saveValue = ((int)_animations).ToString();
				SetSetting("GameAnimations", saveValue);
				Common.ReloadSettings = true;
			}
		}

		public GameOption Sound
		{
			get => _sound;
			set
			{
				_sound = value;
				string saveValue = ((int)_sound).ToString();
				SetSetting("GameSound", saveValue);
				Common.ReloadSettings = true;
			}
		}

		public GameOption EnemyMoves
		{
			get => _enemyMoves;
			set
			{
				_enemyMoves = value;
				string saveValue = ((int)_enemyMoves).ToString();
				SetSetting("GameEnemyMoves", saveValue);
				Common.ReloadSettings = true;
			}
		}

		public GameOption CivilopediaText
		{
			get => _civilopediaText;
			set
			{
				_civilopediaText = value;
				string saveValue = ((int)_civilopediaText).ToString();
				SetSetting("GameCivilopediaText", saveValue);
				Common.ReloadSettings = true;
			}
		}

		public GameOption Palace
		{
			get => _palace;
			set
			{
				_palace = value;
				string saveValue = ((int)_palace).ToString();
				SetSetting("GamePalace", saveValue);
				Common.ReloadSettings = true;
			}
		}

        public int TaxRate
        {
            get => _taxRate;
            set
            {
                _taxRate = Math.Max(0,Math.Min(10,value));
                SetSetting("TaxRate", _taxRate.ToString());
                Common.ReloadSettings = true;
            }
        }

		public string[] DisabledPlugins
		{
			get => GetSetting("DisabledPlugins")?.Split(';') ?? new string[0];
			set => SetSetting("DisabledPlugins", string.Join(";", value));
		}

		public string LanguagePostfix
		{
			get => _languagePostfix;
			set
			{
				_languagePostfix = value ?? string.Empty;
				SetSetting("LanguagePostfix", _languagePostfix);
				Common.ReloadSettings = true;
			}
		}

		internal SimulateInternationalFont SimulateInternationalFont
		{
			get => _simulateInternationalFont;
			set
			{
				_simulateInternationalFont = value;
				SetSetting("SimulateInternationalFont", ((int)_simulateInternationalFont).ToString());
				Common.ReloadSettings = true;
				Resources.Instance.ReloadFonts();
			}
		}

		internal void RevealWorldCheat() => _revealWorld = !_revealWorld;
		
		//internal int ScaleX => _scale;
		//internal int ScaleY => _scale;
		
		private string GetSetting(string settingName) => Runtime.GetSetting(settingName);

		private bool GetSetting<T>(string settingName, ref T output) where T: struct, IConvertible
		{
			if (!Int32.TryParse(GetSetting(settingName), out int value)) return false;
			if (!Enum.IsDefined(typeof(T), value)) return false;
			output = (T)Enum.Parse(typeof(T), value.ToString());
			return true;
		}

		private void GetSetting(string settingName, ref string output) => output = GetSetting(settingName) ?? output;

		private void GetSetting(string settingName, ref bool output) => output = (GetSetting(settingName) == "1");
		
		private bool GetSetting(string settingName, ref int output, int minValue = int.MinValue, int maxValue = int.MaxValue)
		{
			if (!int.TryParse(GetSetting(settingName), out var value)) 
                return false;
			if (value < minValue || value > maxValue) 
                return false;
			output = value;
			return true;
		}
		
		private void SetSetting(string settingName, string value) => Runtime.SetSetting(settingName, value);
		
		private void CreateDirectories()
		{
			foreach (string dir in new[] { StorageDirectory, CaptureDirectory, DataDirectory, PluginsDirectory, SavesDirectory, CosSavesDirectory, SoundsDirectory })
                if (!Directory.Exists(dir))
			    {
				    Directory.CreateDirectory(dir);
			    }
			
			for (char c = 'a'; c <= 'z'; c++)
			{
				string dir = Path.Combine(SavesDirectory, c.ToString());
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
			}
		}
		
		private static Settings _instance;
		public static Settings Instance => _instance ?? (_instance = new Settings());

		private Settings()
		{
			CreateDirectories();

			// Read settings
			GetSetting("WindowTitle", ref _windowTitle);
			GetSetting<GraphicsMode>("GraphicsMode", ref _graphicsMode);
			GetSetting("FullScreen", ref _fullScreen);
			GetSetting("SideBar", ref _rightSideBar);
			GetSetting("Scale", ref _scale, 1, 8);
			if (!GetSetting("WindowWidth", ref _windowWidth, MinWidth, MaxWindowWidth) || !GetSetting("WindowHeight", ref _windowHeight, MinHeight, MaxWindowHeight))
			{
				_windowWidth = -1;
				_windowHeight = -1;
			}
			int windowPosX = -1;
			int windowPosY = -1;
			if (!GetSetting("WindowPosX", ref windowPosX, -MaxWindowWidth, MaxWindowWidth) || !GetSetting("WindowPosY", ref windowPosY, -MaxWindowHeight, MaxWindowHeight))
			{
				// Default to a position near the top-left corner, but not exactly (to avoid issues with taskbars and such).
				_windowPosition = new Point(100, 100);
			}
			else
			{
				_windowPosition = new Point(windowPosX, windowPosY);
			}
			GetSetting("WindowMaximized", ref _windowMaximized);
			GetSetting<AspectRatio>("AspectRatio", ref _aspectRatio);
			GetSetting("Sound", ref _sound);
			if (!GetSetting("ExpandWidth", ref _expandWidth, MinWidth, MaxExpandWidth) || !GetSetting("ExpandHeight", ref _expandHeight, MinHeight, MaxExpandHeight))
			{
				_expandWidth = -1;
				_expandHeight = -1;
			}
			GetSetting("RevealWorld", ref _revealWorld);
			GetSetting("DebugMenu", ref _debugMenu);
			GetSetting("DeityEnabled", ref _deityEnabled);
			GetSetting("ArrowHelper", ref _arrowHelper);
			GetSetting("CustomMapSize", ref _customMapSize);
			GetSetting("PathFindingAlgorithm", ref _pathFinding);
			GetSetting("ComputerPlayerPathFindingAlgorithm", ref _computerPlayerPathFinding);
			GetSetting("AutoSettlers", ref _autoSettlers);
			GetSetting("RiverFastMovement", ref _riverFastMovement);
			GetSetting("CanalCity", ref _canalCity);
			GetSetting("RemoveObsoleteBuildings", ref _removeObsoleteBuildings);
			GetSetting("PreferSveSaveFormat", ref _preferSveSaveFormat);
			GetSetting("LanguagePostfix", ref _languagePostfix);
			GetSetting<SimulateInternationalFont>("SimulateInternationalFont", ref _simulateInternationalFont);
			GetSetting("UseUncheckedCastSanitizer", ref _useUncheckedCastSanitizer);
			GetSetting<CursorType>("CursorType", ref _cursorType);
			GetSetting<DestroyAnimation>("DestroyAnimation", ref _destroyAnimation);
			GetSetting<GameOption>("GameInstantAdvice", ref _instantAdvice);
			GetSetting<GameOption>("GameAutoSave", ref _autoSave);
			GetSetting<GameOption>("GameEndOfTurn", ref _endOfTurn);
			GetSetting<GameOption>("GameAnimations", ref _animations);
			GetSetting<GameOption>("GameSound", ref _sound);
			GetSetting<GameOption>("GameEnemyMoves", ref _enemyMoves);
			GetSetting<GameOption>("GameCivilopediaText", ref _civilopediaText);
			GetSetting<GameOption>("GamePalace", ref _palace);
			GetSetting("TaxRate", ref _taxRate, 0, 10);

			string gwFlags = "";
			GetSetting("GlobalWarmingFeatureFlags", ref gwFlags);

			if (!Enum.TryParse(gwFlags, out _globalWarmingFeatureFlags))
			{
				_globalWarmingFeatureFlags = GlobalWarmingFeatureFlag.None;
			}

			ValueSanitizerFactory.SetRuntimeUseUncheckedCastSanitizer(_useUncheckedCastSanitizer);
		}
	}
}