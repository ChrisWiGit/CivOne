// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.IO;
using System.Threading.Tasks;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Services;
using CivOne.Services.Browser;
using CivOne.Services.Translation;

namespace CivOne.Screens.StartupWizard
{
	internal sealed class WizardActionHandler(
		Func<ITranslationService> translationServiceAccessor,
		IBrowserService browserService,
		string storageDirectory,
		Func<string, string> browseFolder,
		Action<string> log,
		Action showSetupScreen,
		Action<Action> dispatchToMainThread,
		Action requestRefresh) : IWizardActionHandler
	{
		private readonly Func<ITranslationService> _translationServiceAccessor = translationServiceAccessor ?? throw new ArgumentNullException(nameof(translationServiceAccessor));
		private readonly IBrowserService _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
		private readonly string _storageDirectory = storageDirectory ?? string.Empty;
		private readonly Func<string, string> _browseFolder = browseFolder ?? throw new ArgumentNullException(nameof(browseFolder));
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
				case WizardEntryAction.Continue:
						if (engine.PageIndex == 3)
						{
							Settings.Instance.AspectRatio = engine.ScreenAspectRatio;
						}
						engine.MoveNext();
						engine.StatusMessage = string.Empty;
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.Back:
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

			if (!TranslationServiceFactory.TryUseLanguage(_storageDirectory, postfix, out string error, _log))
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

			string path = _browseFolder(T("Location of Civilization data files"));
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
			string path = _browseFolder(T("Location of Civilization for Windows sound files"));
			if (path == null)
			{
				state.StatusMessage = T("Folder selection cancelled.");
				return;
			}

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

		private void HandleOpenProfileFolder(WizardState state)
		{
			if (string.IsNullOrWhiteSpace(_storageDirectory))
			{
				state.StatusMessage = T("Profile folder unavailable.");
				return;
			}

			if (RuntimeHandler.Runtime.TryOpenUrl(_storageDirectory, out string errorMessage))
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

		private void ApplyAspectRatio(string value, WizardState state)
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

		private void ApplyFullScreen(string value, WizardState state)
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