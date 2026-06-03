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
		private const char HotkeyContinue = 'c';
		private const char HotkeyBack = 'b';
		private const int SoundPageIndex = 4;
		private int _lastBuiltPageIndex = -1;
		private readonly Func<ITranslationService> _translationServiceAccessor = translationServiceAccessor ?? throw new ArgumentNullException(nameof(translationServiceAccessor));
		private readonly IReadOnlyList<TranslationLanguageInfo> _availableLanguages = availableLanguages ?? throw new ArgumentNullException(nameof(availableLanguages));

		public WizardPage Build(WizardState state)
		{
			ArgumentNullException.ThrowIfNull(state);

			if (_lastBuiltPageIndex != state.PageIndex)
			{
				if (state.PageIndex == SoundPageIndex)
				{
					RefreshSoundAvailability(state);
				}

				_lastBuiltPageIndex = state.PageIndex;
			}

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
					(T("Changes:"), ProjectPublicLinks.Changes),
					(T("Forum:"), ProjectPublicLinks.Forum),
					(T("Discord:"), ProjectPublicLinks.Discord)
				],
				EntriesYOffset = 0
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

		// hotkeygenerator, yield 1-9, a-z excluding c and b
		private static IEnumerable<char> GenerateHotkeys(string excludedChars = "cb")
		{
			for (char c = '1'; c <= '9'; c++)
			{
				yield return c;
			}

			for (char c = 'a'; c <= 'z'; c++)
			{
				if (excludedChars.Contains(c))
				{
					continue;
				}

				yield return c;
			}
		}

		private bool shouldShowDebugLanguages;
		private WizardPage BuildLanguagePage(WizardState state)
		{
			InitLanguagePageEntries(state, out List<WizardEntry> entries, out string activeLanguage, shouldShowDebugLanguages);

			return new WizardPage
			{
				Title = T("Startup Wizard: Language"),
				Lines =
				[
					$"Language,Sprache,Idioma,Langue,Linguagem: {activeLanguage}"
				],
				Entries = entries,
				EntriesMaxCount = 8,
				EntriesYOffset = 0,
				OnKeyPress = (args, page) =>
				{
					if (args.Key == Key.F12)
					{
						shouldShowDebugLanguages = !shouldShowDebugLanguages;
					}
				}
			};
		}

		private void InitLanguagePageEntries(WizardState state, out List<WizardEntry> entries, out string activeLanguage, bool addDebugLanguages = false)
		{
			entries = [];
			int number = 1;
			const char NEXT_HOTKEY = 'c';
			const char BACK_HOTKEY = 'b';
			bool hasBackButton = state.PageIndex > 0;
			var hotkeys = GenerateHotkeys($"{NEXT_HOTKEY}{(hasBackButton ? BACK_HOTKEY.ToString() : string.Empty)}").GetEnumerator();

			entries.Add(new WizardEntry
			{
				Number = number++,
				Text = T("Original (default)"),
				Action = WizardEntryAction.SelectLanguage,
				Value = string.Empty,
				Hotkey = hotkeys.MoveNext() ? hotkeys.Current : null
			});

			foreach (TranslationLanguageInfo language in _availableLanguages)
			{
				entries.Add(new WizardEntry
				{
					Number = number++,
					Text = TranslationServiceFactory.GetLanguageDisplayName(language, T),
					Action = WizardEntryAction.SelectLanguage,
					Value = language.Postfix,
					Hotkey = hotkeys.MoveNext() ? hotkeys.Current : null
				});
			}

			if (addDebugLanguages)
			{
				number = AddDebugLanguages(entries, number, hotkeys);
			}

			entries.Add(new WizardEntry
			{
				Number = number,
				Text = ContinueText(),
				Action = WizardEntryAction.Continue,
				Hotkey = NEXT_HOTKEY,
				KeepAlwaysLastPosition = true
			});

			if (hasBackButton)
			{
				entries.Add(new WizardEntry
				{
					Number = number + 1,
					Text = BackText(),
					Action = WizardEntryAction.Back,
					Hotkey = BACK_HOTKEY,
				});
			}

			activeLanguage = string.IsNullOrEmpty(state.SelectedLanguagePostfix)
				? T("Original (English)")
				: TranslationServiceFactory.GetLanguageDisplayName(state.SelectedLanguagePostfix, _availableLanguages, T);
		}

		private static int AddDebugLanguages(List<WizardEntry> entries, int number, IEnumerator<char> hotkeys)
		{
			var LanguageNames = new[]
			{
				// Fantasy Names
				"Elvish",
				"Dwarvish",
				"Orcish",
				"Draconic",
				"Celestial",
				"Undercommon",
				"Primordial",
				"High Speech",
				"Ancient Tongue",
				"Fey Speech",
				"Giantish",
				"Lulabese",
				"Zanzibarian",
				"Qwertyish"
			};

			Enumerable.Range(0, LanguageNames.Length).ToList().ForEach(i =>
				entries.Add(new WizardEntry
				{
					Number = number++,
					Text = $"{LanguageNames[i]} ({number})",
					Action = WizardEntryAction.None,
					Hotkey = hotkeys.MoveNext() ? hotkeys.Current : null
				}));
			return number;
		}

		private WizardPage BuildDataFolderPage(WizardState state)
		{
			bool hasDataFiles = FileSystem.DataFilesExist();
			string dataState = state.IsDataFilesCopyInProgress
				? T("Copying data files in background...")
				: hasDataFiles
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
					T("Pick DOS Civilization data folder to copy data from."),
					selectedPath,
					dataState
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = T("Browse data folder"), Action = WizardEntryAction.BrowseDataFolder, Enabled = !state.IsDataFilesCopyInProgress },
					new WizardEntry { Number = 2, Text = ContinueText(), Action = WizardEntryAction.Continue, 
							// disable continue if data files are not available.
							Enabled = hasDataFiles, Hotkey = HotkeyContinue },
					new WizardEntry { Number = 3, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
				],
				HasContextChanged = CreateDataFolderContextChanged(state)
			};
		}

		private static Func<bool> CreateDataFolderContextChanged(WizardState state)
		{
			bool previousHasDataFiles = FileSystem.DataFilesExist();
			bool previousCopyInProgress = state.IsDataFilesCopyInProgress;
			string previousDataFolder = state.DataFolder;
			string previousStatusMessage = state.StatusMessage;

			return () =>
			{
				bool currentHasDataFiles = FileSystem.DataFilesExist();
				bool currentCopyInProgress = state.IsDataFilesCopyInProgress;
				string currentDataFolder = state.DataFolder;
				string currentStatusMessage = state.StatusMessage;

				if (previousHasDataFiles == currentHasDataFiles
					&& previousCopyInProgress == currentCopyInProgress
					&& string.Equals(previousDataFolder, currentDataFolder, StringComparison.Ordinal)
					&& string.Equals(previousStatusMessage, currentStatusMessage, StringComparison.Ordinal))
				{
					return false;
				}

				previousHasDataFiles = currentHasDataFiles;
				previousCopyInProgress = currentCopyInProgress;
				previousDataFolder = currentDataFolder;
				previousStatusMessage = currentStatusMessage;
				return true;
			};
		}

		private WizardPage BuildSoundPage(WizardState state)
		{
			string soundState = state.SoundEnabled ? T("On") : T("Off");
			bool hasAnySoundFiles = state.SoundFilesAvailable == true;
			bool hasMissingSoundFiles = state.MissingSoundFiles.Length > 0;
			(string Label, string Url)[] links = hasMissingSoundFiles
				? [(T("Download sounds:"), ProjectPublicLinks.CivWinSoundtrackMod)]
				: [];
			string soundFilesState;
			if (!hasAnySoundFiles)
			{
				soundFilesState = T("You can download free sound files from the link below.");
			}
			else if (hasMissingSoundFiles)
			{
				soundFilesState = T("Some sound files are missing.");
			}
			else
			{
				soundFilesState = T("Sound files available.");
			}

			List<string> lines =
			[
				soundFilesState
			];

			string missingSummary = FormatMissingSoundFiles(state.MissingSoundFiles);
			if (hasAnySoundFiles && hasMissingSoundFiles && !string.IsNullOrEmpty(missingSummary))
			{
				lines.Add(missingSummary);
				lines.Add(T("You can still play with sound."));
			}

			return new WizardPage
			{
				Title = T("Startup Wizard: Sound"),
				Lines = [.. lines],
				Entries =
				[
					new WizardEntry { Number = 1, Text = TF("Toggle sound: {0}", soundState), Action = WizardEntryAction.ToggleSound, Enabled = hasAnySoundFiles || state.SoundEnabled },
					new WizardEntry { Number = 2, Text = T("Browse sound folder"), Action = WizardEntryAction.BrowseSoundFolder },
					new WizardEntry { Number = 3, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = HotkeyContinue },
					new WizardEntry { Number = 4, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
				],
				Links = links,
				EntriesYOffset = 0
			};
		}

		private void RefreshSoundAvailability(WizardState state)
		{
			state.MissingSoundFiles = FileSystem.GetMissingSoundFiles();
			bool hasAnySoundFiles = FileSystem.HasAnySoundFiles();
			state.SoundFilesAvailable = hasAnySoundFiles;
			if (hasAnySoundFiles)
			{
				return;
			}

			if (!state.SoundEnabled)
			{
				return;
			}

			state.SoundEnabled = false;
			Settings.Instance.Sound = GameOption.Off;
			if (string.IsNullOrEmpty(state.StatusMessage))
			{
				state.StatusMessage = T("Sound files missing. Sound disabled.");
			}
		}

		private string FormatMissingSoundFiles(string[] missingFiles)
		{
			if (missingFiles == null || missingFiles.Length == 0)
			{
				return string.Empty;
			}

			int shownCount = Math.Min(3, missingFiles.Length);
			string shownFiles = string.Join(", ", missingFiles[..shownCount]);
			int remainingCount = missingFiles.Length - shownCount;
			return remainingCount > 0
				? TF("Missing sound files: {0} (+{1} more)", shownFiles, remainingCount)
				: TF("Missing sound files: {0}", shownFiles);
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
					new WizardEntry { Number = 2, Text = T("Open CivOne Profile folder..."), Action = WizardEntryAction.OpenProfileFolder },
					new WizardEntry { Number = 3, Text = T("Show more settings"), Action = WizardEntryAction.OpenSetupScreen },
					new WizardEntry { Number = 4, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = HotkeyContinue },
					new WizardEntry { Number = 5, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
				],
				EntriesYOffset = 0,
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
				EntriesYOffset = 0,
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
