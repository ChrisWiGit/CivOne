using System;
using System.Threading;
using System.Threading.Tasks;
using CivOne.IO.Text;
using CivOne.Screens.StartupWizard;
using CivOne.Services;
using CivOne.Services.Browser;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class WizardActionHandlerTests
	{
		private sealed class StubTranslationService : ITranslationService
		{
			public string Translate(string key) => key;

			public string TranslateFormatted(string key, params object[] args) => string.Format(System.Globalization.CultureInfo.InvariantCulture, key, args);

			public string[] TranslateArray(string key) => [key];

			public string[] TranslateFormattedArray(string key, params object[] args) => [string.Format(System.Globalization.CultureInfo.InvariantCulture, key, args)];
		}

		private sealed class StubBrowserService : IBrowserService
		{
			public bool TryOpenUrl(string url, out string? errorMessage)
			{
				errorMessage = null;
				return true;
			}

			public bool TryCopyToClipboard(string text, out string? errorMessage)
			{
				errorMessage = null;
				return true;
			}
		}

		private sealed class StubLanguageValidationService(OriginalTextLanguageValidationResult result) : IOriginalTextLanguageValidationService
		{
			private readonly OriginalTextLanguageValidationResult _result = result;

			public OriginalTextLanguageValidationResult Validate(string dataDirectory)
			{
				return _result;
			}
		}

		[Fact]
		public void ExecuteWhenLanguageValidationFailsShowsWarningDialogAndKeepsSuccessStatusMessage()
		{
			// Arrange
			MockRuntime runtime = new(new RuntimeSettings());
			try
			{
				string[]? warningDialogLines = null;
				WizardState state = new(string.Empty);
				WizardEntry entry = new()
				{
					Number = 1,
					Text = "Browse",
					Action = WizardEntryAction.BrowseDataFolder
				};
				OriginalTextLanguageValidationResult validationResult = OriginalTextLanguageValidationResult.Create(
				[
					new OriginalTextLanguageValidationFileResult("ERROR.TXT", 5, 5, 2, 3, 3, 1, false)
				]);
				var _testee = CreateActionHandler(validationResult, lines => warningDialogLines = lines);

				// Act
				_testee.Execute(entry, state);
				bool completed = SpinWait.SpinUntil(() => !state.IsDataFilesCopyInProgress, TimeSpan.FromSeconds(2));

				// Assert
				Assert.True(completed);
				Assert.Equal("Data files copied successfully.", state.StatusMessage);
				Assert.NotNull(warningDialogLines);
				Assert.Equal(2, warningDialogLines.Length);
				Assert.Equal("One or more original text files were not recognized as English.", warningDialogLines[0]);
				Assert.Equal("If you choose Original language, some texts may stay in the imported original language while other texts stay in CivOne language.", warningDialogLines[1]);
			}
			finally
			{
				runtime.Dispose();
				RuntimeHandler.Wipe();
			}
		}

		[Fact]
		public void ExecuteWhenLanguageValidationSucceedsSetsSuccessStatusMessage()
		{
			// Arrange
			MockRuntime runtime = new(new RuntimeSettings());
			try
			{
				string[]? warningDialogLines = null;
				WizardState state = new(string.Empty);
				WizardEntry entry = new()
				{
					Number = 1,
					Text = "Browse",
					Action = WizardEntryAction.BrowseDataFolder
				};
				OriginalTextLanguageValidationResult validationResult = OriginalTextLanguageValidationResult.Create(
				[
					new OriginalTextLanguageValidationFileResult("ERROR.TXT", 5, 5, 5, 0, 3, 1, true)
				]);
				var _testee = CreateActionHandler(validationResult, lines => warningDialogLines = lines);

				// Act
				_testee.Execute(entry, state);
				bool completed = SpinWait.SpinUntil(() => !state.IsDataFilesCopyInProgress, TimeSpan.FromSeconds(2));

				// Assert
				Assert.True(completed);
				Assert.Equal("Data files copied successfully.", state.StatusMessage);
				Assert.Null(warningDialogLines);
			}
			finally
			{
				runtime.Dispose();
				RuntimeHandler.Wipe();
			}
		}

		private static WizardActionHandler CreateActionHandler(OriginalTextLanguageValidationResult validationResult, Action<string[]>? showWarningDialog)
		{
			return new WizardActionHandler(
				translationServiceAccessor: () => new StubTranslationService(),
				browserService: new StubBrowserService(),
				originalTextLanguageValidationService: new StubLanguageValidationService(validationResult),
				storageDirectory: "/tmp/civone",
				browseFolder: _ => "/tmp/civ_orig",
				log: _ => { },
				showSetupScreen: () => { },
				dispatchToMainThread: action => action(),
				requestRefresh: () => { },
				showWarningDialog: showWarningDialog,
				copyDataFiles: _ => true,
				dataFilesExist: () => true,
				copyDelayAsync: () => Task.CompletedTask);
		}
	}
}