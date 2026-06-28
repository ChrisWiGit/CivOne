// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;

namespace CivOne.Screens.StartupWizard
{
	/// <summary>
	/// Holds all mutable state for the startup wizard.
	/// </summary>
	/// <remarks>
	/// State is shared between <see cref="IWizardPageBuilder"/> and <see cref="IWizardActionHandler"/>.
	/// Page navigation is controlled via <see cref="MoveNext"/> and <see cref="MoveBack"/>.
	/// </remarks>
	internal sealed class WizardState(string? selectedLanguagePostfix)
	{
		public const int GamePatchesPageIndex = 99;
		private const int LastPageIndex = 6;
		private int _gamePatchesReturnPageIndex = -1;

		/// <summary>
		/// Gets the zero-based index of the currently active wizard page.
		/// </summary>
		public int PageIndex { get; private set; }

		/// <summary>
		/// Gets or sets the status message shown at the bottom of the wizard screen.
		/// </summary>
		/// <remarks>
		/// Set by action handlers to provide user feedback (e.g. "Data files copied successfully.").
		/// Cleared on page navigation.
		/// </remarks>
		public string StatusMessage { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the language postfix selected by the user (e.g. <c>"german"</c>).
		/// </summary>
		/// <remarks>
		/// Empty string means the default/identity translation is active.
		/// </remarks>
		public string SelectedLanguagePostfix { get; set; } = selectedLanguagePostfix ?? string.Empty;

		/// <summary>
		/// Gets or sets the data folder path selected by the user via the folder browser.
		/// </summary>
		/// <remarks>
		/// <see langword="null"/> or empty when no folder has been selected yet.
		/// </remarks>
		public string? DataFolder { get; set; }

		/// <summary>
		/// Gets or sets whether data-file copy is currently running in the background.
		/// </summary>
		public bool IsDataFilesCopyInProgress { get; set; }

		/// <summary>
		/// Gets or sets whether sound is enabled.
		/// </summary>
		/// <remarks>
		/// Initialised from <see cref="Settings.Instance"/> and persisted back when toggled.
		/// </remarks>
		public bool SoundEnabled { get; set; } = Settings.Instance.Sound != GameOption.Off;

		/// <summary>
		/// Gets or sets whether at least one usable sound file is available.
		/// </summary>
		/// <remarks>
		/// <see langword="null"/> means not evaluated yet.
		/// Value is refreshed when opening the sound page and after browsing for sound files.
		/// </remarks>
		public bool? SoundFilesAvailable { get; set; }

		/// <summary>
		/// Gets or sets the currently missing sound files.
		/// </summary>
		/// <remarks>
		/// Empty when all sound files are available.
		/// Refreshed together with <see cref="SoundFilesAvailable"/>.
		/// </remarks>
		public string[] MissingSoundFiles { get; set; } = [];

		/// <summary>
		/// Gets or sets whether the in-game debug menu is enabled.
		/// </summary>
		public bool DebugMenuEnabled { get; set; } = Settings.Instance.DebugMenu;

		/// <summary>
		/// Gets or sets whether fullscreen mode is enabled.
		/// </summary>
		public bool FullScreenEnabled { get; set; } = Settings.Instance.FullScreen;

		/// <summary>
		/// Gets or sets whether fast river movement is enabled.
		/// </summary>
		public bool RiverFastMovementEnabled { get; set; } = Settings.Instance.RiverFastMovement;

		/// <summary>
		/// Gets or sets whether smart pathfinding for goto is enabled.
		/// </summary>
		public bool PathFindingEnabled { get; set; } = Settings.Instance.PathFinding;

		/// <summary>
		/// Gets or sets whether smart pathfinding for computer players is enabled.
		/// </summary>
		public bool ComputerPlayerPathFindingEnabled { get; set; } = Settings.Instance.ComputerPlayerPathFinding;

		/// <summary>
		/// Gets or sets whether the auto settlers cheat is enabled.
		/// </summary>
		public bool AutoSettlersEnabled { get; set; } = Settings.Instance.AutoSettlers;

		/// <summary>
		/// Gets or sets whether sea units ignore the city movement penalty.
		/// </summary>
		public bool CanalCityEnabled { get; set; } = Settings.Instance.CanalCity;

		/// <summary>
		/// Gets or sets whether obsolete buildings are removed automatically.
		/// </summary>
		public bool RemoveObsoleteBuildingsEnabled { get; set; } = Settings.Instance.RemoveObsoleteBuildings;

		/// <summary>
		/// Gets or sets whether Deity difficulty is available.
		/// </summary>
		public bool DeityEnabled { get; set; } = Settings.Instance.DeityEnabled;

		/// <summary>
		/// Gets or sets the screen aspect ratio selected in the startup wizard.
		/// </summary>
		/// <remarks>
		/// Initialised from <see cref="Settings.Instance"/> and kept in sync by page context checks.
		/// </remarks>
		public AspectRatio ScreenAspectRatio { get; set; } = Settings.Instance.AspectRatio;

		/// <summary>
		/// Advances to the next wizard page, up to the last page.
		/// </summary>
		public void MoveNext()
		{
			if (PageIndex < LastPageIndex)
			{
				PageIndex++;
			}
		}

		/// <summary>
		/// Returns to the previous wizard page, down to page zero.
		/// </summary>
		public void MoveBack()
		{
			if (PageIndex > 0)
			{
				PageIndex--;
			}
		}

		/// <summary>
		/// Opens the temporary game patches page and remembers the previous page index.
		/// </summary>
		public void OpenGamePatchesPage()
		{
			_gamePatchesReturnPageIndex = PageIndex;
			RiverFastMovementEnabled = Settings.Instance.RiverFastMovement;
			PageIndex = GamePatchesPageIndex;
		}

		/// <summary>
		/// Returns from the temporary game patches page to the page that opened it.
		/// </summary>
		public bool CloseGamePatchesPage()
		{
			if (_gamePatchesReturnPageIndex < 0)
			{
				return false;
			}

			PageIndex = _gamePatchesReturnPageIndex;
			_gamePatchesReturnPageIndex = -1;
			return true;
		}
	}
}
