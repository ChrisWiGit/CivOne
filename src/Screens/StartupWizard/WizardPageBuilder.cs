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
using System.Linq;
using CivOne.Enums;
using CivOne.IO;
using CivOne.Services;
using CivOne.Services.Translation;

namespace CivOne.Screens.StartupWizard
{
	internal sealed class WizardPageBuilder(Func<ITranslationService> translationServiceAccessor, IReadOnlyList<TranslationLanguageInfo> availableLanguages) : IWizardPageBuilder
	{
		private static readonly char HotkeyContinue = 'c';
		private static readonly char HotkeyBack = 'b';
		private readonly Func<ITranslationService> _translationServiceAccessor = translationServiceAccessor ?? throw new ArgumentNullException(nameof(translationServiceAccessor));
		private readonly IReadOnlyList<TranslationLanguageInfo> _availableLanguages = availableLanguages ?? throw new ArgumentNullException(nameof(availableLanguages));

		public WizardPage Build(WizardState state)
		{
			ArgumentNullException.ThrowIfNull(state);

			return state.PageIndex switch
			{
				0 => BuildLanguagePage(state),
				1 => BuildWelcomePage(),
				2 => BuildDataFolderPage(state),
				3 => BuildAspectRatioPage(state),
				4 => BuildSoundPage(state),
				5 => BuildMoreSettingsPage(state),
				6 => BuildFinalPage(),
				_ => BuildLanguagePage(state)
			};
		}

		private WizardPage BuildFinalPage()
		{
			return new WizardPage
			{
				Title = T("Startup Wizard: Ready"),
				Lines =
				[
					T("A list of changes is available below."),
					T("Forum and Discord links are included too."),
					T("Click any link or start the game now.")
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = T("Start game now"), Action = WizardEntryAction.Finish, Hotkey = HotkeyContinue },
					new WizardEntry { Number = 2, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
				],
				Links =
				[
					(T("Changes:"), "https://github.com/ChrisWiGit/CivOne/blob/master/CHANGES.md"),
					(T("Forum:"), "https://forums.civfanatics.com"),
					(T("Discord:"), "https://discord.gg/kfaFcTnCX")
				],
				EntriesYOffset = 3
			};
		}

		private WizardPage BuildWelcomePage()
		{
			return new WizardPage
			{
				Title = T("Startup Wizard"),
				Lines =
				[
					T("Welcome to CivOne."),
					T("This screen appears because startup setup still needs attention."),
					T("Use number keys or click with the mouse to choose entries."),
					T("Links in the header can also be clicked.")
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = HotkeyContinue },
					new WizardEntry { Number = 2, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
				]
			};
		}

		private String UpperCaseFirstLetter(String s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return string.Empty;
			}
			return char.ToUpper(s[0]) + s.Substring(1);
		}

		private WizardPage BuildLanguagePage(WizardState state)
		{
			List<WizardEntry> entries = [];
			int number = 1;

			entries.Add(new WizardEntry
			{
				Number = number++,
				Text = T("Original (default)"),
				Action = WizardEntryAction.SelectLanguage,
				Value = string.Empty
			});

			foreach (string languagePostfix in _availableLanguages.Select(language => language.Postfix))
			{
				entries.Add(new WizardEntry
				{
					Number = number++,
					Text = UpperCaseFirstLetter(T(languagePostfix)),
					Action = WizardEntryAction.SelectLanguage,
					Value = languagePostfix
				});
			}

			entries.Add(new WizardEntry
			{
				Number = number,
				Text = ContinueText(),
				Action = WizardEntryAction.Continue,
				Hotkey = 'c'
			});

			if (state.PageIndex > 0)
			{
				entries.Add(new WizardEntry
				{
					Number = number + 1,
					Text = BackText(),
					Action = WizardEntryAction.Back,
					Hotkey = 'b'
				});
			}

			string activeLanguage = string.IsNullOrEmpty(state.SelectedLanguagePostfix)
				? T("Identity")
				: T(state.SelectedLanguagePostfix);

			return new WizardPage
			{
				Title = T("Startup Wizard: Language"),
				Lines =
				[
					T("Select startup language."),
					TF("Current: {0}", activeLanguage)
				],
				Entries = entries
			};
		}

		private WizardPage BuildDataFolderPage(WizardState state)
		{
			bool hasDataFiles = FileSystem.DataFilesExist();
			string dataState = hasDataFiles
				? T("Data files available.")
				: T("Data files still missing.");

			string selectedPath = T("No folder selected.");
			if (hasDataFiles)
			{
				selectedPath = Settings.Instance.DataDirectory;
			}
			else if (!string.IsNullOrWhiteSpace(state.DataFolder))
			{
				selectedPath = state.DataFolder;
			}

			return new WizardPage
			{
				Title = T("Startup Wizard: Data Files"),
				Lines =
				[
					T("Pick DOS Civilization data folder."),
					selectedPath,
					dataState
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = T("Browse data folder"), Action = WizardEntryAction.BrowseDataFolder },
					new WizardEntry { Number = 2, Text = ContinueText(), Action = WizardEntryAction.Continue, 
							// disable continue if data files are not available.
							Enabled = hasDataFiles, Hotkey = HotkeyContinue },
					new WizardEntry { Number = 3, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
				]
			};
		}

		private WizardPage BuildSoundPage(WizardState state)
		{
			string soundState = state.SoundEnabled ? T("On") : T("Off");
			return new WizardPage
			{
				Title = T("Startup Wizard: Sound"),
				Lines =
				[
					TF("Sound: {0}", soundState)
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = T("Toggle sound"), Action = WizardEntryAction.ToggleSound },
					new WizardEntry { Number = 2, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = HotkeyContinue },
					new WizardEntry { Number = 3, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
				]
			};
		}

		private WizardPage BuildMoreSettingsPage(WizardState state)
		{
			string debugMenuEntryText = state.DebugMenuEnabled
				? T("Debug-Menu - Hit ^F12^ to show debug menu in game")
				: T("Debug-Menu - ^off");

			return new WizardPage
			{
				Title = T("Startup Wizard: More Settings"),
				Lines =
				[
					T("Enable debugging, then press F12 in game to open debug menu."),
					T("Open full settings screen for more options, then return here."),
					T("Settings screen can also accessed by Shift+F1 at start or from debug menu.")
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = debugMenuEntryText, Action = WizardEntryAction.ToggleDebugMenu },
					new WizardEntry { Number = 2, Text = T("Show more settings"), Action = WizardEntryAction.OpenSetupScreen },
					new WizardEntry { Number = 3, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = HotkeyContinue },
					new WizardEntry { Number = 4, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
				],
				EntriesYOffset = 1,
				HasContextChanged = () => SyncMoreSettingsState(state)
			};
		}

		private WizardPage BuildAspectRatioPage(WizardState state)
		{
			List<WizardEntry> entries =
			[
				CreateFullScreenEntry(1, true, !state.FullScreenEnabled, TF("Fullscreen: ^{0}^", state.FullScreenEnabled.YesNo())),
				CreateAspectRatioEntry(2, AspectRatio.Auto, T("Auto - stretch image, may distort")),
				CreateAspectRatioEntry(3, AspectRatio.Fixed, T("Fixed - keep ratio, may add black borders")),
				CreateAspectRatioEntry(4, AspectRatio.Scaled, T("Scaled - fit resolution, may look blurry")),
				CreateAspectRatioEntry(5, AspectRatio.ScaledFixed, T("ScaledFixed - keep ratio, blur and borders possible")),
				CreateAspectRatioEntry(6, AspectRatio.Expand, T("Expand ^(default)^ - fill screen, borders possible")),
				new WizardEntry { Number = 7, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = HotkeyContinue },
				new WizardEntry { Number = 8, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
			];


			return new WizardPage
			{
				Title = T("Startup Wizard: Screen Aspect Ratio"),
				Lines =
				[
					TF("Current: {0}", state.ScreenAspectRatio.ToText())
				],
				Entries = entries,
				EntriesYOffset = 2,
				HasContextChanged = () => SyncAspectRatioSettingsState(state)
			};
			
		}

		private static bool SyncAspectRatioSettingsState(WizardState state)
		{
			bool hasChanged = SyncFullScreenState(state);

			AspectRatio aspectRatio = Settings.Instance.AspectRatio;
			if (state.ScreenAspectRatio == aspectRatio)
			{
				return hasChanged;
			}

			state.ScreenAspectRatio = aspectRatio;
			return true;
		}

		private static bool SyncMoreSettingsState(WizardState state)
		{
			bool hasChanged = false;

			bool debugMenuEnabled = Settings.Instance.DebugMenu;
			if (state.DebugMenuEnabled != debugMenuEnabled)
			{
				state.DebugMenuEnabled = debugMenuEnabled;
				hasChanged = true;
			}

			if (SyncAspectRatioSettingsState(state))
			{
				hasChanged = true;
			}

			return hasChanged;
		}

		private static bool SyncFullScreenState(WizardState state)
		{
			bool fullScreenEnabled = Settings.Instance.FullScreen;
			if (state.FullScreenEnabled == fullScreenEnabled)
			{
				return false;
			}

			state.FullScreenEnabled = fullScreenEnabled;
			return true;
		}

		private WizardEntry CreateAspectRatioEntry(int number, AspectRatio aspectRatio, string explanation) => new()
		{
			Number = number,
			Text = explanation,
			Action = WizardEntryAction.SelectAspectRatio,
			Value = aspectRatio.ToString(),
			Hotkey = null
		};

		private WizardEntry CreateFullScreenEntry(int number, bool entryEnabled, bool targetValue, string text) => new()
		{
			Number = number,
			Text = text,
			Action = WizardEntryAction.SelectFullScreen,
			Value = targetValue.ToString(),
			Enabled = entryEnabled,
			Hotkey = 'f'
		};

		private string ContinueText() => T("Continue");
		private string BackText() => T("Back");

		private string T(string key) => _translationServiceAccessor().Translate(key);

		private string TF(string key, params object[] args) => _translationServiceAccessor().TranslateFormatted(key, args);
	}
}
