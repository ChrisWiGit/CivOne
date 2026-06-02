// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.IO;
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
		private readonly ConcurrentQueue<Action> _pendingMainThreadActions = new();
		private WizardPage? _currentPage;
		private int _entryScrollOffset;
		private int _entryScrollPageIndex = -1;

		private int _mouseX = -1;
		private int _mouseY = -1;

		/// <summary>
		/// Cached pixel snapshot of the rendered page without the mouse marker overlay.
		/// Used to restore the area under the previous marker on cursor moves so the marker
		/// can be updated without re-rendering the entire page bitmap (which is expensive at
		/// full-window canvas sizes in fullscreen mode).
		/// </summary>
		private Bytemap? _pageSnapshot;

		/// <summary>
		/// Pixel rectangle of the glyph cell where the marker was last drawn, used to restore
		/// the underlying page content from the snapshot when the marker moves.
		/// </summary>
		private Rectangle _previousMarkerRect = Rectangle.Empty;

		/// <summary>
		/// When true, the next refresh will only restore and redraw the marker rather than
		/// re-rendering the entire page. Set by mouse moves that only require marker updates.
		/// </summary>
		private bool _markerOnlyRefresh;

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
				browseFolder: caption => Runtime.BrowseFolder(caption),
				log: message => Log(message),
				showSetupScreen: () =>
				{
					Setup setup = new();
					setup.Closed += (sender, args) => Refresh();
					ScreenServiceFactory.CreateCommandService().AddScreen(setup);
				},
				dispatchToMainThread: _pendingMainThreadActions.Enqueue,
				requestRefresh: Refresh);
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
		/// Handles per-frame update and redraw. Supports two refresh modes:
		/// full page render (state changes) and marker-only update (cursor moves) to keep
		/// fullscreen cursor motion responsive on a full-display-resolution canvas.
		/// </summary>
		protected override bool HasUpdate(uint gameTick)
		{
			while (_pendingMainThreadActions.TryDequeue(out Action? action))
			{
				// Execute pending actions that need to run on the main thread, 
				// such as those triggered by page interactions.
				// This is not really the right way to marshal actions back to the main thread, 
				// but it works for our simple use case and avoids adding a dependency 
				// on a more robust threading/synchronization library.
				action();
			}

			if (_currentPage?.HasContextChanged?.Invoke() == true)
			{
				_markerOnlyRefresh = false;
				Refresh();
			}

			if (!RefreshNeeded())
			{
				return false;
			}

			bool wantsMarkerOnly = _markerOnlyRefresh;
			_markerOnlyRefresh = false;

			if (wantsMarkerOnly && CanDoMarkerOnlyRefresh())
			{
				RestorePageRect(_previousMarkerRect);
				DrawCurrentMarker();
				return true;
			}

			RenderCurrentPage();
			CapturePageSnapshot();
			DrawCurrentMarker();
			return true;
		}

		/// <summary>
		/// Handles keyboard activation for numbered entries and navigation.
		/// </summary>
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args[Key.Escape])
			{
				// The wizard has no visible fullscreen/exit entry, so allow ESC to toggle fullscreen.
				bool nextFullScreen = !Settings.Instance.FullScreen;
				Settings.Instance.FullScreen = nextFullScreen;
				_state.FullScreenEnabled = nextFullScreen;
				Refresh();
				return true;
			}

			if (args[Key.Up])
			{
				return ScrollEntries(-1);
			}

			if (args[Key.Down])
			{
				return ScrollEntries(1);
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
			WizardEntry? entry = _pageBuilder
				.Build(_state)
				.Entries
				.FirstOrDefault(x => x.Enabled && IsEntryHotkeyMatch(x, keyChar));
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
		/// <remarks>
		/// The inversion marker is glyph-cell aligned, so a full page refresh is only required
		/// when the cursor crosses into a new cell or changes hover state over a link/menu hit area.
		/// Skipping refreshes for sub-cell motion avoids re-rendering the full-window canvas on
		/// every mouse-move event, which is the main cause of visible cursor lag in fullscreen.
		/// </remarks>
		public override bool MouseMove(ScreenEventArgs args)
		{
			if (_mouseX == args.X && _mouseY == args.Y)
			{
				return false;
			}

			bool shouldRefresh = ShouldRefreshForMouseMove(args.X, args.Y);
			_mouseX = args.X;
			_mouseY = args.Y;
			if (shouldRefresh)
			{
				// Mouse movement only repositions the marker; the underlying page content
				// is unchanged. Request a marker-only refresh so HasUpdate restores the
				// snapshot under the previous marker and redraws the marker at the new
				// position instead of re-rendering the whole page bitmap.
				_markerOnlyRefresh = true;
				Refresh();
			}
			return shouldRefresh;
		}

		/// <summary>
		/// Determines whether a mouse move at the given coordinates requires re-rendering the page.
		/// Returns true when the cursor crosses a glyph-cell boundary, enters/leaves the page box,
		/// or transitions in/out of a link or menu hit area; otherwise false.
		/// </summary>
		private bool ShouldRefreshForMouseMove(int newX, int newY)
		{
			// First move, or rendering context not yet populated → render to be safe.
			if (_mouseX < 0 || _mouseY < 0 || _renderingContext.Scale <= 0f)
			{
				return true;
			}

			int glyphWidth = (int)(ModernDos8X16.GlyphWidth * _renderingContext.Scale);
			int glyphHeight = (int)(ModernDos8X16.GlyphHeight * _renderingContext.Scale);
			if (glyphWidth <= 0 || glyphHeight <= 0)
			{
				return true;
			}

			Rectangle box = _renderingContext.Box;
			bool oldInsideBox = box.Contains(_mouseX, _mouseY);
			bool newInsideBox = box.Contains(newX, newY);
			if (oldInsideBox != newInsideBox)
			{
				return true;
			}

			if (newInsideBox)
			{
				int oldCol = (_mouseX - box.X) / glyphWidth;
				int oldRow = (_mouseY - box.Y) / glyphHeight;
				int newCol = (newX - box.X) / glyphWidth;
				int newRow = (newY - box.Y) / glyphHeight;
				if (oldCol != newCol || oldRow != newRow)
				{
					return true;
				}
			}

			// Hit-area transitions (link/menu) may not align perfectly to cell edges in all layouts,
			// so detect transitions explicitly to keep the marker style in sync.
			if (HitAreaContains(_renderingContext.LinkAreas.Select(l => l.Area), _mouseX, _mouseY)
				!= HitAreaContains(_renderingContext.LinkAreas.Select(l => l.Area), newX, newY))
			{
				return true;
			}
			if (HitAreaContains(_renderingContext.EntryHitAreas.Select(e => e.Area), _mouseX, _mouseY)
				!= HitAreaContains(_renderingContext.EntryHitAreas.Select(e => e.Area), newX, newY))
			{
				return true;
			}

			return false;
		}

		private static bool HitAreaContains(System.Collections.Generic.IEnumerable<Rectangle> areas, int x, int y)
			=> areas.Any(area => area.Contains(x, y));

		private bool ScrollEntries(int delta)
		{
			WizardPage page = _pageBuilder.Build(_state);
			WizardEntry[] fixedEntries = [.. page.Entries.Where(entry => entry.KeepAlwaysLastPosition)];
			WizardEntry[] scrollableEntries = [.. page.Entries.Where(entry => !entry.KeepAlwaysLastPosition)];
			int maxVisibleEntries = page.EntriesMaxCount > 0 ? page.EntriesMaxCount : page.Entries.Count;
			int scrollableVisibleCount = Math.Min(scrollableEntries.Length, Math.Max(0, maxVisibleEntries - fixedEntries.Length));
			if (scrollableVisibleCount <= 0 || scrollableEntries.Length <= scrollableVisibleCount)
			{
				return false;
			}

			ResetEntryScrollOffsetIfPageChanged();
			int maxOffset = Math.Max(0, scrollableEntries.Length - scrollableVisibleCount);
			int nextOffset = Math.Clamp(_entryScrollOffset + delta, 0, maxOffset);
			if (nextOffset == _entryScrollOffset)
			{
				return false;
			}

			_entryScrollOffset = nextOffset;
			Refresh();
			return true;
		}

		private void ActivateEntry(int number)
		{
			if (number == WizardRenderingContext.ScrollUpHitAreaNumber)
			{
				ScrollEntries(-1);
				return;
			}

			if (number == WizardRenderingContext.ScrollDownHitAreaNumber)
			{
				ScrollEntries(1);
				return;
			}

			WizardPage page = _pageBuilder.Build(_state);
			WizardEntry? entry = page.Entries.FirstOrDefault(x => x.Number == number);
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

			_currentPage = _pageBuilder.Build(_state);
			ResetEntryScrollOffsetIfPageChanged();
			ClampEntryScrollOffset(_currentPage);
			WizardPage page = _currentPage;
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
			_renderingContext.EntryScrollOffset = _entryScrollOffset;
			_renderingContext.StatusMessage = _state.StatusMessage;

			_renderingDelegate.Render(_state, page, _renderingContext);

			// Marker is intentionally not drawn here; HasUpdate draws it after snapshotting
			// the marker-free page so cursor moves can restore the page from the snapshot.
		}

		private void ResetEntryScrollOffsetIfPageChanged()
		{
			if (_entryScrollPageIndex == _state.PageIndex)
			{
				return;
			}

			_entryScrollPageIndex = _state.PageIndex;
			_entryScrollOffset = 0;
		}

		private void ClampEntryScrollOffset(WizardPage page)
		{
			WizardEntry[] fixedEntries = [.. page.Entries.Where(entry => entry.KeepAlwaysLastPosition)];
			WizardEntry[] scrollableEntries = [.. page.Entries.Where(entry => !entry.KeepAlwaysLastPosition)];
			int maxVisibleEntries = page.EntriesMaxCount > 0 ? page.EntriesMaxCount : page.Entries.Count;
			int scrollableVisibleCount = Math.Min(scrollableEntries.Length, Math.Max(0, maxVisibleEntries - fixedEntries.Length));
			if (scrollableVisibleCount <= 0)
			{
				_entryScrollOffset = 0;
				return;
			}

			int maxOffset = Math.Max(0, scrollableEntries.Length - scrollableVisibleCount);
			_entryScrollOffset = Math.Clamp(_entryScrollOffset, 0, maxOffset);
		}

		private static bool IsEntryHotkeyMatch(WizardEntry entry, char pressedKey)
		{
			char? expectedHotkey = entry.Hotkey.HasValue
				? char.ToLowerInvariant(entry.Hotkey.Value)
				: GetAutoActivationHotkey(entry.Number);
			if (!expectedHotkey.HasValue)
			{
				return false;
			}

			return char.ToLowerInvariant(pressedKey) == expectedHotkey.Value;
		}

		private static char? GetAutoActivationHotkey(int entryNumber)
		{
			if (entryNumber >= 1 && entryNumber <= 9)
			{
				return (char)('0' + entryNumber);
			}

			int letterIndex = entryNumber - 10;
			if (letterIndex < 0 || letterIndex >= 26)
			{
				return null;
			}

			return (char)('a' + letterIndex);
		}

		/// <summary>
		/// Returns true when a marker-only refresh is safe: a snapshot exists and matches the
		/// current canvas size, and the rendering context has valid scale information.
		/// </summary>
		private bool CanDoMarkerOnlyRefresh()
			=> _pageSnapshot != null
				&& !_pageSnapshot.IsDisposed
				&& _pageSnapshot.Width == Bitmap.Width
				&& _pageSnapshot.Height == Bitmap.Height
				&& _renderingContext.Scale > 0f;

		/// <summary>
		/// Captures the current Bitmap (which must contain the page without the marker) into
		/// the page snapshot for later restoration during marker-only refreshes.
		/// </summary>
		private void CapturePageSnapshot()
		{
			if (_pageSnapshot != null && (_pageSnapshot.Width != Bitmap.Width || _pageSnapshot.Height != Bitmap.Height))
			{
				_pageSnapshot.Dispose();
				_pageSnapshot = null;
			}
			_pageSnapshot?.Dispose();
			_pageSnapshot = Bytemap.Copy(Bitmap);
		}

		/// <summary>
		/// Restores a rectangular region of the Bitmap from the page snapshot. Used to erase
		/// the previous marker before drawing it at a new position.
		/// </summary>
		private void RestorePageRect(Rectangle rect)
		{
			if (rect.IsEmpty || _pageSnapshot == null || _pageSnapshot.IsDisposed) return;
			int xStart = Math.Max(0, rect.X);
			int yStart = Math.Max(0, rect.Y);
			int xEnd = Math.Min(Bitmap.Width, rect.X + rect.Width);
			int yEnd = Math.Min(Bitmap.Height, rect.Y + rect.Height);
			for (int y = yStart; y < yEnd; y++)
			{
				for (int x = xStart; x < xEnd; x++)
				{
					Bitmap[x, y] = _pageSnapshot[x, y];
				}
			}
		}

		/// <summary>
		/// Draws the marker at the current mouse position and records its glyph-cell rectangle
		/// so the next refresh can restore the underlying page content from the snapshot.
		/// </summary>
		private void DrawCurrentMarker()
		{
			_mouseMarkerDelegate.DrawMarker(_mouseX, _mouseY, _renderingContext);
			_previousMarkerRect = ComputeCurrentMarkerRect();
		}

		/// <summary>
		/// Computes the pixel rectangle of the glyph cell currently under the mouse cursor,
		/// or <see cref="Rectangle.Empty"/> if the cursor is outside the page box or rendering
		/// context is not yet populated.
		/// </summary>
		private Rectangle ComputeCurrentMarkerRect()
		{
			if (_renderingContext.Scale <= 0f) return Rectangle.Empty;
			int gw = (int)(ModernDos8X16.GlyphWidth * _renderingContext.Scale);
			int gh = (int)(ModernDos8X16.GlyphHeight * _renderingContext.Scale);
			if (gw <= 0 || gh <= 0) return Rectangle.Empty;
			Rectangle box = _renderingContext.Box;
			if (!box.Contains(_mouseX, _mouseY)) return Rectangle.Empty;
			int col = (_mouseX - box.X) / gw;
			int row = (_mouseY - box.Y) / gh;
			return new Rectangle(box.X + (col * gw), box.Y + (row * gh), gw, gh);
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			_pageSnapshot?.Dispose();
			_pageSnapshot = null;
			base.Dispose();
		}
	}
}
