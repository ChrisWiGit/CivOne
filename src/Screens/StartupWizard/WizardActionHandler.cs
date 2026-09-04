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
using System.Linq;
using System.Threading.Tasks;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Services;
using CivOne.Services.Browser;
using CivOne.Services.Translation;
using CivOne.Sound;
using CivOne.Sound.Cvl;
using CivOne.Sound.Playback;

namespace CivOne.Screens.StartupWizard
{
	internal sealed class WizardActionHandler(
		Func<ITranslationService> translationServiceAccessor,
		IBrowserService browserService,
		string storageDirectory,
		Func<string, string?> browseFolder,
		Action<string> log,
		Action showSetupScreen,
		Action<Action> dispatchToMainThread,
		Action requestRefresh) : IWizardActionHandler
	{
		private readonly Func<ITranslationService> _translationServiceAccessor = translationServiceAccessor ?? throw new ArgumentNullException(nameof(translationServiceAccessor));
		private readonly IBrowserService _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
		private readonly string _storageDirectory = storageDirectory ?? string.Empty;
		private readonly Func<string, string?> _browseFolder = browseFolder ?? throw new ArgumentNullException(nameof(browseFolder));
		private readonly Action<string> _log = log ?? (_ => { });
		private readonly Action _showSetupScreen = showSetupScreen ?? (() => { });
		private readonly Action<Action> _dispatchToMainThread = dispatchToMainThread ?? throw new ArgumentNullException(nameof(dispatchToMainThread));
		private readonly Action _requestRefresh = requestRefresh ?? (() => { });

		public WizardActionResult Execute(WizardEntry entry, WizardState engine)
		{
			ArgumentNullException.ThrowIfNull(entry);
			ArgumentNullException.ThrowIfNull(engine);

			if (!entry.Enabled)
			{
				return new WizardActionResult(ShouldRefresh: false);
			}

			switch (entry.Action)
			{
				case WizardEntryAction.SelectLanguage:
						ApplyLanguage(entry.Value ?? string.Empty, engine);
					return new WizardActionResult(ShouldRefresh: true);
					case WizardEntryAction.SelectAspectRatio:
						ApplyAspectRatio(entry.Value, engine);
						return new WizardActionResult(ShouldRefresh: true);
					case WizardEntryAction.ToggleDebugMenu:
						ToggleDebugMenu(engine);
						return new WizardActionResult(ShouldRefresh: true);
					case WizardEntryAction.ToggleTerrainEditorMenu:
						ToggleTerrainEditorMenu(engine);
						return new WizardActionResult(ShouldRefresh: true);
					case WizardEntryAction.OpenGamePatchesScreen:
						HandleOpenGamePatchesScreen(engine);
						return new WizardActionResult(ShouldRefresh: true);
					case WizardEntryAction.OpenSetupScreen:
						_showSetupScreen();
						return new WizardActionResult(ShouldRefresh: false);
					case WizardEntryAction.OpenProfileFolder:
						HandleOpenProfileFolder(engine);
						return new WizardActionResult(ShouldRefresh: true);
					case WizardEntryAction.SelectFullScreen:
						ApplyFullScreen(entry.Value, engine);
						return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.BrowseDataFolder:
						HandleBrowseDataFolder(engine);
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.BrowseSoundFolder:
						HandleBrowseSoundFolder(engine);
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.OpenSoundPackScreen:
						// Opening the page is the earliest point at which the wizard knows a pack is
						// of interest. Preparing it here means the test sound, and the music right
						// after the wizard, do not have to wait for it.
						SoundPlaybackStrategyProvider.WarmUp();
						engine.OpenSoundPackPage();
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.SelectSoundPack:
						SelectSoundPack(entry.Value, engine);
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.ToggleTestSound:
						ToggleTestSound(engine);
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.Continue:
						if (engine.PageIndex == 3)
						{
							Settings.Instance.AspectRatio = engine.ScreenAspectRatio;
						}
						if (engine.PageIndex == 4)
						{
							StopTestSound(engine);
						}
						engine.MoveNext();
						engine.StatusMessage = string.Empty;
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.Back:
						if (engine.CloseGamePatchesPage() || engine.CloseSoundPackPage())
						{
							engine.StatusMessage = string.Empty;
							return new WizardActionResult(ShouldRefresh: true);
						}
						if (engine.PageIndex == 4)
						{
							StopTestSound(engine);
						}
						engine.MoveBack();
						engine.StatusMessage = string.Empty;
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.ToggleSound:
						if (!CanEnableSound(engine))
						{
							return new WizardActionResult(ShouldRefresh: true);
						}
						engine.SoundEnabled = !engine.SoundEnabled;
						Settings.Instance.Sound = engine.SoundEnabled ? GameOption.On : GameOption.Off;
						engine.StatusMessage = engine.SoundEnabled
						? T("Sound enabled.")
						: T("Sound disabled.");
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.ToggleRiverFastMovement:
						ToggleRiverFastMovement(engine);
						return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.TogglePathFinding:
						TogglePathFinding(engine);
						return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.ToggleComputerPlayerPathFinding:
						ToggleComputerPlayerPathFinding(engine);
						return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.ToggleAutoSettlers:
						ToggleAutoSettlers(engine);
						return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.ToggleCanalCity:
						ToggleCanalCity(engine);
						return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.ToggleRemoveObsoleteBuildings:
						ToggleRemoveObsoleteBuildings(engine);
						return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.ToggleDeityEnabled:
						ToggleDeityEnabled(engine);
						return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.ToggleExtendedGlobalWarming:
						ToggleExtendedGlobalWarming(engine);
						return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.Finish:
					return new WizardActionResult(ShouldRefresh: false, ShouldClose: true);
				default:
					return new WizardActionResult(ShouldRefresh: false);
			}
		}

		public WizardActionResult OpenUrl(string url, WizardState engine)
		{
			ArgumentNullException.ThrowIfNull(engine);

			if (string.IsNullOrWhiteSpace(url))
			{
				return new WizardActionResult(ShouldRefresh: false);
			}

			if (!_browserService.TryOpenUrl(url, out _))
			{
				engine.StatusMessage = _browserService.TryCopyToClipboard(url, out _)
					? T("Link copied to clipboard.")
					: T("Could not open URL.");
			}
			else
			{
				engine.StatusMessage = T("Opened URL in browser.");
			}

			return new WizardActionResult(ShouldRefresh: true);
		}

		private void ApplyLanguage(string postfix, WizardState state)
		{
			if (string.IsNullOrEmpty(postfix))
			{
				Settings.Instance.LanguagePostfix = string.Empty;
				TranslationServiceFactory.UseIdentity();
				state.SelectedLanguagePostfix = string.Empty;
				state.StatusMessage = T("Language switched to Identity.");
				return;
			}

			if (!TranslationServiceFactory.TryUseLanguage(_storageDirectory, postfix, out string? error, _log))
			{
				state.StatusMessage = TF("Could not load language '{0}'.", postfix);
				_log($"Could not activate language '{postfix}': {error}");
				return;
			}

			Settings.Instance.LanguagePostfix = postfix;
			state.SelectedLanguagePostfix = postfix;
			state.StatusMessage = T(postfix);
		}

		private void HandleBrowseDataFolder(WizardState engine)
		{
			if (engine.IsDataFilesCopyInProgress)
			{
				engine.StatusMessage = T("Data file copy is already running.");
				return;
			}

			string? path = _browseFolder(T("Location of Civilization data files"));
			if (path == null)
			{
				engine.StatusMessage = T("Folder selection cancelled.");
				return;
			}

			string copyRunningMessage = T("Copying data files...");
			string copyFailedMessage = T("Copying data files failed.");
			string copySucceededMessage = T("Data files copied successfully.");

			engine.DataFolder = path;
			engine.IsDataFilesCopyInProgress = true;
			engine.StatusMessage = copyRunningMessage;

			_ = Task.Run(() => CopyDataFilesInBackgroundAsync(engine, path, copyFailedMessage, copySucceededMessage));
		}

		private async Task CopyDataFilesInBackgroundAsync(WizardState engine, string path, string copyFailedMessage, string copySucceededMessage)
		{
			try
			{
				// Show output even if copying is done in an instant.
				await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

				if (!FileSystem.CopyDataFiles(path) || !FileSystem.DataFilesExist())
				{
					_dispatchToMainThread(() =>
					{
						engine.StatusMessage = copyFailedMessage;
						_requestRefresh();
					});
					return;
				}

				_dispatchToMainThread(() =>
				{
					Resources.ClearInstance();
					engine.StatusMessage = copySucceededMessage;
					_requestRefresh();
				});
			}
			catch (IOException exception)
			{
				_log($"Copying data files failed for '{path}': {exception.Message}");
				_dispatchToMainThread(() =>
				{
					engine.StatusMessage = copyFailedMessage;
					_requestRefresh();
				});
			}
			finally
			{
				_dispatchToMainThread(() =>
				{
					engine.IsDataFilesCopyInProgress = false;
					_requestRefresh();
				});
			}
		}

		private void HandleBrowseSoundFolder(WizardState state)
		{
			string? path = _browseFolder(T("Location of Civilization for Windows sound files"));
			if (string.IsNullOrWhiteSpace(path))
			{
				state.StatusMessage = T("Folder selection cancelled.");
				return;
			}

			try
			{
				if (!FileSystem.CopySoundFiles(path, out string[] missingFiles))
				{
					RefreshSoundAvailability(state);
					state.StatusMessage = T("No usable sound files found in selected folder.");
					return;
				}

				RefreshSoundAvailability(state);
				if (state.SoundFilesAvailable == true)
				{
					state.SoundEnabled = true;
					Settings.Instance.Sound = GameOption.On;
				}
				state.StatusMessage = missingFiles.Length == 0
					? T("Sound files copied successfully.")
					: TF("Sound files copied with missing files: {0}", FormatMissingList(missingFiles));
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
			{
				_log($"Copying sound files failed for '{path}': {ex.Message}");
				RefreshSoundAvailability(state);
				state.StatusMessage = T("No usable sound files found in selected folder.");
			}
		}

		private void SelectSoundPack(string? value, WizardState state)
		{
			StopTestSound(state);

			Settings.Instance.SoundPack = value ?? string.Empty;
			state.SoundPackId = Settings.Instance.SoundPack;
			state.SoundEnabled = Settings.Instance.Sound != GameOption.Off;

			// Also covers picking the pack that was already selected, which changes no setting and
			// would therefore not be noticed anywhere else.
			SoundPlaybackStrategyProvider.WarmUp();

			IReadOnlyList<SoundPackSummary> packs = SoundPackCatalog.GetAvailablePacks(Settings.Instance.SoundsDirectory);
			state.StatusMessage = TF("Sound pack set to {0}.", SoundPackDisplayName(state.SoundPackId, packs));

			state.CloseSoundPackPage();
		}

		private void ToggleTestSound(WizardState state)
		{
			if (state.IsTestSoundPlaying)
			{
				StopTestSound(state);
				state.StatusMessage = T("Test sound stopped.");
				return;
			}

			string soundPackId = state.SoundPackId;
			bool isWave = string.Equals(soundPackId, SoundPlaybackStrategyConstants.WaveSoundPack, StringComparison.OrdinalIgnoreCase);

			SoundPlaybackStrategyProvider.Abort();

			if (isWave)
			{
				bool playedWave = SoundPlaybackStrategyProvider.Current.PlaySound(SoundNames.MusicTitle);
				state.IsTestSoundPlaying = playedWave;
				state.StatusMessage = playedWave
					? T("Playing test sound.")
					: T("Test sound could not be played.");
				return;
			}

			string packId = soundPackId;
			SoundPackIndexEntry? tune = LoadSoundPackIndex(packId)?.Tunes.FirstOrDefault();
			if (tune == null)
			{
				state.StatusMessage = T("No test tune available for this sound pack.");
				return;
			}

			bool playedTune = SoundPlaybackStrategyProvider.PlayTune(packId, tune);
			state.IsTestSoundPlaying = playedTune;
			state.StatusMessage = playedTune
				? TF("Playing test tune: {0}", tune.Title)
				: TF("Test tune could not be played: {0}", tune.Title);
		}

		private static void StopTestSound(WizardState state)
		{
			if (!state.IsTestSoundPlaying)
			{
				return;
			}

			SoundPlaybackStrategyProvider.Abort();
			state.IsTestSoundPlaying = false;
		}

		private SoundPackIndex? LoadSoundPackIndex(string packId)
		{
			string indexPath = Path.Combine(Settings.Instance.SoundsDirectory, packId, SoundPackIndex.FileName);
			if (!File.Exists(indexPath))
			{
				return null;
			}

			try
			{
				return SoundPackIndexJson.Load(indexPath);
			}
			catch (InvalidOperationException ex)
			{
				_log($"Could not load sound pack index '{indexPath}': {ex.Message}");
				return null;
			}
		}

		private string SoundPackDisplayName(string soundPackId, IReadOnlyList<SoundPackSummary> packs)
		{
			if (string.Equals(soundPackId, SoundPlaybackStrategyConstants.NoSoundPack, StringComparison.OrdinalIgnoreCase)) return T("None");
			if (string.Equals(soundPackId, SoundPlaybackStrategyConstants.WaveSoundPack, StringComparison.OrdinalIgnoreCase)) return T("Wave files");
			foreach (SoundPackSummary pack in packs)
			{
				if (string.Equals(pack.PackId, soundPackId, StringComparison.OrdinalIgnoreCase)) return pack.DisplayName;
			}
			return soundPackId;
		}

		private void HandleOpenProfileFolder(WizardState state)
		{
			if (string.IsNullOrWhiteSpace(_storageDirectory))
			{
				state.StatusMessage = T("Profile folder unavailable.");
				return;
			}

			if (RuntimeHandler.Runtime.TryOpenUrl(_storageDirectory, out string? errorMessage))
			{
				state.StatusMessage = T("Opened CivOne profile folder.");
				return;
			}

			_log($"Could not open profile folder '{_storageDirectory}': {errorMessage}");
			state.StatusMessage = T("Could not open profile folder.");
		}

		private bool CanEnableSound(WizardState state)
		{
			if (state.SoundEnabled)
			{
				return true;
			}

			RefreshSoundAvailability(state);
			if (state.SoundFilesAvailable == true)
			{
				return true;
			}

			state.SoundEnabled = false;
			Settings.Instance.Sound = GameOption.Off;
			state.StatusMessage = T("Sound files missing. Select sound folder first.");
			return false;
		}

		private static void RefreshSoundAvailability(WizardState state)
		{
			state.MissingSoundFiles = FileSystem.GetMissingSoundFiles();
			bool hasAnySoundFiles = FileSystem.HasAnySoundFiles();
			state.SoundFilesAvailable = hasAnySoundFiles;
			if (hasAnySoundFiles)
			{
				return;
			}

			state.SoundEnabled = false;
			Settings.Instance.Sound = GameOption.Off;
		}

		private static string FormatMissingList(string[] missingFiles)
		{
			if (missingFiles == null || missingFiles.Length == 0)
			{
				return string.Empty;
			}

			int shownCount = Math.Min(3, missingFiles.Length);
			string shownFiles = string.Join(", ", missingFiles[..shownCount]);
			int remainingCount = missingFiles.Length - shownCount;
			return remainingCount > 0
				? $"{shownFiles} (+{remainingCount})"
				: shownFiles;
		}

		private void ApplyAspectRatio(string? value, WizardState state)
		{
			if (!Enum.TryParse(value, ignoreCase: true, out AspectRatio aspectRatio))
			{
				return;
			}

			state.ScreenAspectRatio = aspectRatio;
			Settings.Instance.AspectRatio = aspectRatio;
			state.StatusMessage = TF("Aspect ratio set to {0}.", aspectRatio.ToText());
		}

		private void ToggleDebugMenu(WizardState state)
		{
			state.DebugMenuEnabled = !state.DebugMenuEnabled;
			Settings.Instance.DebugMenu = state.DebugMenuEnabled;
			state.StatusMessage = state.DebugMenuEnabled
				? T("Debug menu enabled. Press F12 in game to open it.")
				: T("Debug menu disabled.");
		}

		private void ToggleTerrainEditorMenu(WizardState state)
		{
			state.TerrainEditorMenuEnabled = !state.TerrainEditorMenuEnabled;
			Settings.Instance.TerrainEditorMenu = state.TerrainEditorMenuEnabled;
			state.StatusMessage = state.TerrainEditorMenuEnabled
				? T("Terrain editor menu enabled.")
				: T("Terrain editor menu disabled.");
		}

		private static void HandleOpenGamePatchesScreen(WizardState state)
		{
			state.OpenGamePatchesPage();
		}

		private void ToggleRiverFastMovement(WizardState state)
		{
			state.RiverFastMovementEnabled = !state.RiverFastMovementEnabled;
			Settings.Instance.RiverFastMovement = state.RiverFastMovementEnabled;
			state.StatusMessage = state.RiverFastMovementEnabled
				? T("Fast river movement enabled.")
				: T("Fast river movement disabled.");
		}

		private void TogglePathFinding(WizardState state)
		{
			state.PathFindingEnabled = !state.PathFindingEnabled;
			Settings.Instance.PathFinding = state.PathFindingEnabled;
			state.StatusMessage = state.PathFindingEnabled
				? T("Smart goto pathfinding enabled.")
				: T("Smart goto pathfinding disabled.");
		}

		private void ToggleComputerPlayerPathFinding(WizardState state)
		{
			state.ComputerPlayerPathFindingEnabled = !state.ComputerPlayerPathFindingEnabled;
			Settings.Instance.ComputerPlayerPathFinding = state.ComputerPlayerPathFindingEnabled;
			state.StatusMessage = state.ComputerPlayerPathFindingEnabled
				? T("Smart computer player pathfinding enabled.")
				: T("Smart computer player pathfinding disabled.");
		}

		private void ToggleAutoSettlers(WizardState state)
		{
			state.AutoSettlersEnabled = !state.AutoSettlersEnabled;
			Settings.Instance.AutoSettlers = state.AutoSettlersEnabled;
			state.StatusMessage = state.AutoSettlersEnabled
				? T("Auto settlers cheat enabled.")
				: T("Auto settlers cheat disabled.");
		}

		private void ToggleCanalCity(WizardState state)
		{
			state.CanalCityEnabled = !state.CanalCityEnabled;
			Settings.Instance.CanalCity = state.CanalCityEnabled;
			state.StatusMessage = state.CanalCityEnabled
				? T("City movement penalty for sea units disabled.")
				: T("City movement penalty for sea units enabled.");
		}

		private void ToggleRemoveObsoleteBuildings(WizardState state)
		{
			state.RemoveObsoleteBuildingsEnabled = !state.RemoveObsoleteBuildingsEnabled;
			Settings.Instance.RemoveObsoleteBuildings = state.RemoveObsoleteBuildingsEnabled;
			state.StatusMessage = state.RemoveObsoleteBuildingsEnabled
				? T("Obsolete buildings like barracks are removed when obsolete.")
				: T("Obsolete buildings like barracks are kept.");
		}

		private void ToggleDeityEnabled(WizardState state)
		{
			state.DeityEnabled = !state.DeityEnabled;
			Settings.Instance.DeityEnabled = state.DeityEnabled;
			state.StatusMessage = state.DeityEnabled
				? T("Deity difficulty enabled.")
				: T("Deity difficulty disabled.");
		}

		private void ToggleExtendedGlobalWarming(WizardState state)
		{
			state.ExtendedGlobalWarming = !state.ExtendedGlobalWarming;
			if (state.ExtendedGlobalWarming)
			{
				Settings.Instance.GlobalWarmingFeatureFlags |= Settings.GlobalWarmingFeatureFlag.SeaLevelRise;
			}
			else
			{
				Settings.Instance.GlobalWarmingFeatureFlags &= ~Settings.GlobalWarmingFeatureFlag.SeaLevelRise;
			}
			state.StatusMessage = state.ExtendedGlobalWarming
				? T("Allow coastal tiles to turn into ocean.")
				: T("Original global warming effects.");
		}

		private void ApplyFullScreen(string? value, WizardState state)
		{
			if (!bool.TryParse(value, out bool fullScreen))
			{
				return;
			}

			state.FullScreenEnabled = fullScreen;
			Settings.Instance.FullScreen = fullScreen;
			state.StatusMessage = fullScreen
				? T("Fullscreen enabled.")
				: T("Fullscreen disabled.");
		}

		private string T(string key) => _translationServiceAccessor().Translate(key);

		private string TF(string key, params object[] args) => _translationServiceAccessor().TranslateFormatted(key, args);
	}
}