// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
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
		Action<string> log) : IWizardActionHandler
	{
		private readonly Func<ITranslationService> _translationServiceAccessor = translationServiceAccessor ?? throw new ArgumentNullException(nameof(translationServiceAccessor));
		private readonly IBrowserService _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
		private readonly string _storageDirectory = storageDirectory ?? string.Empty;
		private readonly Func<string, string> _browseFolder = browseFolder ?? throw new ArgumentNullException(nameof(browseFolder));
		private readonly Action<string> _log = log ?? (_ => { });

		public WizardActionResult Execute(WizardEntry entry, WizardState state)
		{
			ArgumentNullException.ThrowIfNull(entry);
			ArgumentNullException.ThrowIfNull(state);

			if (!entry.Enabled)
			{
				return new WizardActionResult(ShouldRefresh: false);
			}

			switch (entry.Action)
			{
				case WizardEntryAction.SelectLanguage:
					ApplyLanguage(entry.Value ?? string.Empty, state);
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.BrowseDataFolder:
					HandleBrowseDataFolder(state);
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.Continue:
					state.MoveNext();
					state.StatusMessage = string.Empty;
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.Back:
					state.MoveBack();
					state.StatusMessage = string.Empty;
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.ToggleSound:
					state.SoundEnabled = !state.SoundEnabled;
					Settings.Instance.Sound = state.SoundEnabled ? GameOption.On : GameOption.Off;
					state.StatusMessage = state.SoundEnabled
						? T("Sound enabled.")
						: T("Sound disabled.");
					return new WizardActionResult(ShouldRefresh: true);
				case WizardEntryAction.Finish:
					return new WizardActionResult(ShouldRefresh: false, ShouldClose: true);
				default:
					return new WizardActionResult(ShouldRefresh: false);
			}
		}

		public WizardActionResult OpenUrl(string url, WizardState state)
		{
			ArgumentNullException.ThrowIfNull(state);

			if (string.IsNullOrWhiteSpace(url))
			{
				return new WizardActionResult(ShouldRefresh: false);
			}

			if (!_browserService.TryOpenUrl(url, out _))
			{
				state.StatusMessage = _browserService.TryCopyToClipboard(url, out _)
					? T("Link copied to clipboard.")
					: T("Could not open URL.");
			}
			else
			{
				state.StatusMessage = T("Opened URL in browser.");
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
			string path = _browseFolder(T("Location of Civilization data files"));
			if (path == null)
			{
				engine.StatusMessage = T("Folder selection cancelled.");
				return;
			}

			engine.DataFolder = path;
			if (!FileSystem.CopyDataFiles(path) || !FileSystem.DataFilesExist())
			{
				engine.StatusMessage = T("Copying data files failed.");
				return;
			}

			Resources.ClearInstance();
			engine.StatusMessage = T("Data files copied successfully.");
		}

		private string T(string key) => _translationServiceAccessor().Translate(key);

		private string TF(string key, params object[] args) => _translationServiceAccessor().TranslateFormatted(key, args);
	}
}