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
using System.Reflection.PortableExecutable;
using CivOne.Enums;
using CivOne.IO;
using CivOne.Services;
using CivOne.Services.Translation;

namespace CivOne.Screens.StartupWizard
{
	internal sealed class WizardPageBuilder(Func<ITranslationService> translationServiceAccessor, IReadOnlyList<TranslationLanguageInfo> availableLanguages) : IWizardPageBuilder
	{
		private static char HotkeyContinue = 'c';
		private static char HotkeyBack = 'b';
		private readonly Func<ITranslationService> _translationServiceAccessor = translationServiceAccessor ?? throw new ArgumentNullException(nameof(translationServiceAccessor));
		private readonly IReadOnlyList<TranslationLanguageInfo> _availableLanguages = availableLanguages ?? throw new ArgumentNullException(nameof(availableLanguages));

		public WizardPage Build(WizardState engine)
		{
			ArgumentNullException.ThrowIfNull(engine);

			return engine.PageIndex switch
			{
				0 => BuildLanguagePage(engine),
				1 => BuildWelcomePage(),
				2 => BuildDataFolderPage(engine),
				3 => BuildAspectRatioPage(engine),
				4 => BuildSoundPage(engine),
				5 => BuildFinalPage(),
				_ => BuildLanguagePage(engine)
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

		private WizardPage BuildLanguagePage(WizardState engine)
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
					Text = T(languagePostfix),
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

			if (engine.PageIndex > 0)
			{
				entries.Add(new WizardEntry
				{
					Number = number + 1,
					Text = BackText(),
					Action = WizardEntryAction.Back,
					Hotkey = 'b'
				});
			}

			string activeLanguage = string.IsNullOrEmpty(engine.SelectedLanguagePostfix)
				? T("Identity")
				: T(engine.SelectedLanguagePostfix);

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

		private WizardPage BuildDataFolderPage(WizardState engine)
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
			else if (!string.IsNullOrWhiteSpace(engine.DataFolder))
			{
				selectedPath = engine.DataFolder;
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

		private WizardPage BuildSoundPage(WizardState engine)
		{
			string soundState = engine.SoundEnabled ? T("On") : T("Off");
			return new WizardPage
			{
				Title = T("Startup Wizard: Sound"),
				Lines =
				[
					TF("Sound: {0}", soundState),
					T("Save choice and start game.")
				],
				Entries =
				[
					new WizardEntry { Number = 1, Text = T("Toggle sound"), Action = WizardEntryAction.ToggleSound },
					new WizardEntry { Number = 2, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = HotkeyContinue },
					new WizardEntry { Number = 3, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
				]
			};
		}

		private WizardPage BuildAspectRatioPage(WizardState engine)
		{
			List<WizardEntry> entries =
			[
				CreateAspectRatioEntry(1, AspectRatio.Auto, T("Auto - stretch image, may distort")),
				CreateAspectRatioEntry(2, AspectRatio.Fixed, T("Fixed - keep ratio, may add black borders")),
				CreateAspectRatioEntry(3, AspectRatio.Scaled, T("Scaled - fit resolution, may look blurry")),
				CreateAspectRatioEntry(4, AspectRatio.ScaledFixed, T("ScaledFixed - keep ratio, blur and borders possible")),
				CreateAspectRatioEntry(5, AspectRatio.Expand, T("Expand (default) - fill screen, borders possible")),
				new WizardEntry { Number = 6, Text = ContinueText(), Action = WizardEntryAction.Continue, Hotkey = HotkeyContinue },
				new WizardEntry { Number = 7, Text = BackText(), Action = WizardEntryAction.Back, Hotkey = HotkeyBack }
			];


			return new WizardPage
			{
				Title = T("Startup Wizard: Screen Aspect Ratio"),
				Lines =
				[
					T("Choose screen aspect ratio."),
					TF("Current: {0}", engine.ScreenAspectRatio.ToText())
				],
				Entries = entries,
				EntriesYOffset = 3
			};
			
		}

		private WizardEntry CreateAspectRatioEntry(int number, AspectRatio aspectRatio, string explanation) => new()
		{
			Number = number,
			Text = explanation,
			Action = WizardEntryAction.SelectAspectRatio,
			Value = aspectRatio.ToString(),
			Hotkey = null
		};

		private string ContinueText() => T("Continue");
		private string BackText() => T("Back");

		private string T(string key) => _translationServiceAccessor().Translate(key);

		private string TF(string key, params object[] args) => _translationServiceAccessor().TranslateFormatted(key, args);
	}
}
