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
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Screens.StartupWizard.DosFont;
using CivOne.Services;
using CivOne.Services.Browser;
using CivOne.Services.Screen;

namespace CivOne.Screens.StartupWizard
{
	/// <summary>
	/// DOS style startup wizard shown before normal startup when setup is required.
	/// Orchestrates input handling, page building, action execution, and rendering.
	/// </summary>
	[Break]
	[ScreenResizeable]
	public sealed class WizardScreen : BaseScreen
	{
		private const int TargetRows = 25;
		private const int TargetCols = 80;

		private readonly WizardState _state;
		private readonly IWizardPageBuilder _pageBuilder;
		private readonly IWizardActionHandler _actionHandler;
		private readonly WizardRenderingDelegate _renderingDelegate;
		private readonly WizardMouseMarkerDelegate _mouseMarkerDelegate;
		private readonly WizardRenderingContext _renderingContext;

		private int _mouseX = -1;
		private int _mouseY = -1;

		/// <inheritdoc />
		public override bool UseFullWindowCanvas => true;

		/// <summary>
		/// Initializes the startup wizard screen with default dependencies.
		/// </summary>
		public WizardScreen() : base(MouseCursor.None)
		{
			Palette = Common.GetPalette256;
			_state = new WizardState(TranslationServiceFactory.ActiveLanguagePostfix);
			if (!Settings.Instance.FullScreen)
			{
				Settings.Instance.FullScreen = true;
			}
			_state.FullScreenEnabled = Settings.Instance.FullScreen;
			_pageBuilder = new WizardPageBuilder(
				translationServiceAccessor: TranslationServiceFactory.GetCurrent,
				availableLanguages: TranslationServiceFactory.GetAvailableLanguages(Runtime.StorageDirectory, message => Log(message)));
			_actionHandler = new WizardActionHandler(
				translationServiceAccessor: TranslationServiceFactory.GetCurrent,
				browserService: BrowserServiceFactory.Instance,
				storageDirectory: Runtime.StorageDirectory,
				browseFolder: Runtime.BrowseFolder,
				log: message => Log(message),
				showSetupScreen: () =>
				{
					Setup setup = new();
					setup.Closed += (sender, args) => Refresh();
					ScreenServiceFactory.CreateCommandService().AddScreen(setup);
				});
			_renderingDelegate = new WizardRenderingDelegate(this, Translate);
			_mouseMarkerDelegate = new WizardMouseMarkerDelegate(this);
			_renderingContext = new WizardRenderingContext();
			Refresh();
		}

		/// <summary>
		/// Internal constructor for testing and dependency injection.
		/// </summary>
		internal WizardScreen(WizardState engine, IWizardPageBuilder pageBuilder, IWizardActionHandler actionHandler) : base(MouseCursor.None)
		{
			Palette = Common.GetPalette256;
			_state = engine;
			_pageBuilder = pageBuilder;
			_actionHandler = actionHandler;
			_renderingDelegate = new WizardRenderingDelegate(this, Translate);
			_mouseMarkerDelegate = new WizardMouseMarkerDelegate(this);
			_renderingContext = new WizardRenderingContext();
			Refresh();
		}

		/// <summary>
		/// Handles per-frame update and redraw.
		/// </summary>
		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			RenderCurrentPage();
			return true;
		}

		/// <summary>
		/// Handles keyboard activation for numbered entries and navigation.
		/// </summary>
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args[Key.Escape])
			{
				// ESC intentionally disabled in startup wizard.
				return true;
			}

			if (args[Key.Character] && TryActivateHotkey(args.KeyChar))
			{
				return true;
			}

			if (args[Key.Character] && char.IsDigit(args.KeyChar) && args.KeyChar != '0')
			{
				ActivateEntry(args.KeyChar - '0');
				return true;
			}

			int numPadNumber = args.Key switch
			{
				Key.NumPad1 => 1,
				Key.NumPad2 => 2,
				Key.NumPad3 => 3,
				Key.NumPad4 => 4,
				Key.NumPad5 => 5,
				Key.NumPad6 => 6,
				Key.NumPad7 => 7,
				Key.NumPad8 => 8,
				Key.NumPad9 => 9,
				_ => 0
			};

			if (numPadNumber > 0)
			{
				ActivateEntry(numPadNumber);
				return true;
			}

			return false;
		}

		private bool TryActivateHotkey(char keyChar)
		{
			char normalizedKey = char.ToLowerInvariant(keyChar);
			WizardEntry entry = _pageBuilder.Build(_state).Entries.FirstOrDefault(x => x.Enabled && x.Hotkey.HasValue && char.ToLowerInvariant(x.Hotkey.Value) == normalizedKey);
			if (entry == null)
			{
				return false;
			}

			ActivateEntry(entry.Number);
			return true;
		}

		/// <summary>
		/// Handles mouse activation for numbered entry rows and links.
		/// </summary>
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (args.Buttons != MouseButton.Left)
			{
				return false;
			}

			// Check links first
			foreach ((string url, Rectangle area) in _renderingContext.LinkAreas)
			{
				if (!area.Contains(args.X, args.Y))
				{
					continue;
				}

				ApplyActionResult(_actionHandler.OpenUrl(url, _state));
				return true;
			}

			// Then check entries
			foreach ((int number, Rectangle area) in _renderingContext.EntryHitAreas)
			{
				if (!area.Contains(args.X, args.Y))
				{
					continue;
				}

				ActivateEntry(number);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Tracks mouse position and refreshes the DOS inversion marker.
		/// </summary>
		public override bool MouseMove(ScreenEventArgs args)
		{
			if (_mouseX == args.X && _mouseY == args.Y)
			{
				return false;
			}

			_mouseX = args.X;
			_mouseY = args.Y;
			Refresh();
			return true;
		}

		private void ActivateEntry(int number)
		{
			WizardPage page = _pageBuilder.Build(_state);
			WizardEntry entry = page.Entries.FirstOrDefault(x => x.Number == number);
			if (entry == null || !entry.Enabled)
			{
				return;
			}

			ApplyActionResult(_actionHandler.Execute(entry, _state));
		}

		private void ApplyActionResult(WizardActionResult actionResult)
		{
			if (actionResult.ShouldClose)
			{
				Destroy();
				return;
			}

			if (actionResult.ShouldRefresh)
			{
				Refresh();
			}
		}

		private void RenderCurrentPage()
		{
			_renderingContext.EntryHitAreas.Clear();
			_renderingContext.LinkAreas.Clear();
			_renderingContext.GlyphAreas.Clear();

			WizardPage page = _pageBuilder.Build(_state);
			int baseWidth = TargetCols * ModernDos8X16.GlyphWidth;
			int baseHeight = TargetRows * ModernDos8X16.GlyphHeight;

			Size window = new(CanvasWidth, CanvasHeight);
			float scaleByWidth = (float)window.Width / baseWidth;
			float scaleByHeight = (float)window.Height / baseHeight;
			float scale = Math.Max(1.0f, Math.Min(scaleByWidth, scaleByHeight));
			float boxWidth = baseWidth * scale;
			float boxHeight = baseHeight * scale;
			int boxX = Math.Max(0, (int)((window.Width - boxWidth) / 2));
			int boxY = Math.Max(0, (int)((window.Height - boxHeight) / 2));

			_renderingContext.Box = new Rectangle(boxX, boxY, (int)boxWidth, (int)boxHeight);
			_renderingContext.Cols = TargetCols;
			_renderingContext.Rows = TargetRows;
			_renderingContext.Scale = scale;
			_renderingContext.StatusMessage = _state.StatusMessage;

			_renderingDelegate.Render(_state, page, _renderingContext);
			_renderingDelegate.DrawPageContent(page, _renderingContext);
			_mouseMarkerDelegate.DrawMarker(_mouseX, _mouseY, _renderingContext);
		}
	}
}
