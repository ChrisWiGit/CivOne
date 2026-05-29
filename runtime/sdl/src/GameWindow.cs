// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.IO;

namespace CivOne
{
	internal partial class GameWindow : SDL.Window
	{
		private readonly Runtime _runtime;
		private readonly IDebounceService _debounceService;

		private static string GetMcpTitleSuffix(Runtime runtime)
		{
			if (runtime?.Settings == null || !runtime.Settings.McpEnabled)
				return string.Empty;

			return runtime.Settings.McpNoAuth
				? " [MCP is running without Auth]"
				: " [MCP is running]";
		}

		private static string GetDebugTitleSuffix(Runtime runtime)
		{
			if (runtime?.Settings == null || !runtime.Settings.Get<bool>("debug"))
				return string.Empty;

			return $" [PID {Environment.ProcessId}]";
		}

		private static string ApplyMcpTitleState(Runtime runtime, string title)
			=> $"{title}{GetMcpTitleSuffix(runtime)}{GetDebugTitleSuffix(runtime)}";

		private static Settings Settings => Settings.Instance;

		private int _mouseX = -1, _mouseY = -1;

		/// <summary>
		/// True when layer bitmaps may have changed and need to be re-uploaded as SDL textures.
		/// Set by layer-changing events (runtime updates, screen refreshes, window resize); cleared
		/// after <see cref="Runtime.InvokeDraw"/> has been called and the texture cache rebuilt.
		/// </summary>
		private bool _hasUpdate = true;

		/// <summary>
		/// True when only the cursor position changed and no layer rebuild is required. Allows
		/// re-rendering using the cached layer textures so cursor motion is not bottlenecked by
		/// per-frame bytemap uploads (critical for fullscreen at high display resolutions).
		/// </summary>
		private bool _cursorMoved = true;
		/// <summary>
		/// Cached rendering/window settings and corresponding dirty flags used to avoid
		/// expensive per-frame synchronization work in the update loop.
		/// </summary>
		/// <remarks>
		/// These fields are necessary because window/canvas state changes can originate from
		/// multiple sources (settings updates, SDL window events, fullscreen transitions, and
		/// active-screen canvas mode changes).
		/// By keeping the last observed values and explicit dirty flags, the window only
		/// recalculates canvas size and publishes window state when a relevant input changed,
		/// which reduces unnecessary work and prevents redundant resize/sync churn.
		/// </remarks>
		private bool _lastFullscreen = Settings.FullScreen;

		private int _lastScale = Settings.Scale;
		private int _lastWindowWidth = Settings.WindowWidth;
		private int _lastWindowHeight = Settings.WindowHeight;
		private Size? _pendingWindowSizeCandidate;
		private Point _lastWindowPosition = Settings.WindowPosition;
		private Point? _pendingWindowPositionCandidate;
		private bool _lastWindowMaximized = Settings.WindowMaximized;
		private AspectRatio _lastAspectRatio = Settings.AspectRatio;
		private int _lastExpandWidth = Settings.ExpandWidth;
		private int _lastExpandHeight = Settings.ExpandHeight;
		private bool _windowStateDirty = true;
		private bool _canvasSizeDirty = true;
		private bool _lastFullWindowCanvasRequested = RuntimeHandler.IsFullWindowCanvasRequested;
		private static readonly TimeSpan WindowPersistDebounce = TimeSpan.FromSeconds(1);

		private void Load(object sender, EventArgs args)
		{
			UpdateCanvasSizeIfNeeded();
			UpdateWindowSizeState();
			_runtime.InvokeInitialize();
		}

		private void Update(object sender, EventArgs args)
		{
			UpdateEventArgs updateArgs = UpdateEventArgs.Empty;
			_runtime.InvokeUpdate(ref updateArgs);
			_hasUpdate = (_hasUpdate || updateArgs.HasUpdate);

			ApplyFullscreenSettingChanges();
			ApplyScaleSettingChanges();
			ApplyCanvasSettingChanges();
			TrackFullWindowCanvasModeChanges();

			if (_windowStateDirty || HasWindowedSettingsChanges())
			{
				SyncWindowedStateWithSettings();
			}

			bool canvasSizeUpdated = UpdateCanvasSizeIfNeeded();
			if (_windowStateDirty || canvasSizeUpdated)
			{
				UpdateWindowSizeState();
			}

			_debounceService.ExecuteDueCallbacks();

			if (_runtime.SignalQuit)
			{
				_debounceService.FlushPendingCallbacks();
				StopRunning();
			}
		}

		private void ApplyFullscreenSettingChanges()
		{
			if (_lastFullscreen == Settings.FullScreen)
			{
				return;
			}

			_lastFullscreen = Settings.FullScreen;
			if (_lastFullscreen)
			{
				PersistDisplayResolutionAsWindowSize();
			}

			Fullscreen = _lastFullscreen;
			_windowStateDirty = true;
			_canvasSizeDirty = true;
			_hasUpdate = true;
		}

		private void PersistDisplayResolutionAsWindowSize()
		{
			Size displaySize = GetDisplaySize();
			if (displaySize.Width <= 0 || displaySize.Height <= 0)
			{
				return;
			}

			_lastWindowWidth = displaySize.Width;
			_lastWindowHeight = displaySize.Height;
			_pendingWindowSizeCandidate = null;
			Settings.WindowWidth = displaySize.Width;
			Settings.WindowHeight = displaySize.Height;
		}

		private void ApplyScaleSettingChanges()
		{
			if (_lastScale == Settings.Scale)
			{
				return;
			}

			ResetWindowScale();
			_lastScale = Settings.Scale;
			_windowStateDirty = true;
			_canvasSizeDirty = true;
			_hasUpdate = true;
		}

		private void ApplyCanvasSettingChanges()
		{
			if (_lastAspectRatio == Settings.AspectRatio
				&& _lastExpandWidth == Settings.ExpandWidth
				&& _lastExpandHeight == Settings.ExpandHeight)
			{
				return;
			}

			_lastAspectRatio = Settings.AspectRatio;
			_lastExpandWidth = Settings.ExpandWidth;
			_lastExpandHeight = Settings.ExpandHeight;
			_canvasSizeDirty = true;
			_hasUpdate = true;
		}

		private void TrackFullWindowCanvasModeChanges()
		{
			bool isFullWindowCanvasRequested = RuntimeHandler.IsFullWindowCanvasRequested;
			if (_lastFullWindowCanvasRequested == isFullWindowCanvasRequested)
			{
				return;
			}

			_lastFullWindowCanvasRequested = isFullWindowCanvasRequested;
			_canvasSizeDirty = true;
			_hasUpdate = true;
		}

		private bool HasWindowedSettingsChanges()
		{
			if (Settings.FullScreen)
			{
				return false;
			}

			return Settings.WindowMaximized != _lastWindowMaximized
				|| Settings.WindowWidth != _lastWindowWidth
				|| Settings.WindowHeight != _lastWindowHeight
				|| Settings.WindowPosition != _lastWindowPosition;
		}

		private bool UpdateCanvasSizeIfNeeded()
		{
			if (!_canvasSizeDirty)
			{
				return false;
			}

			Runtime.CanvasSize = SetCanvasSize();
			_canvasSizeDirty = false;
			return true;
		}

		private void UpdateWindowSizeState()
		{
			Runtime.WindowSize = ClientRectangle;
			_windowStateDirty = false;
		}

		private void SyncWindowedStateWithSettings()
		{
			if (Settings.FullScreen)
			{
				_windowStateDirty = false;
				return;
			}

			SyncMaximizedState();
			if (_lastWindowMaximized)
			{
				return;
			}

			SyncWindowSize();
			PersistWindowPosition();
		}

		private void SyncMaximizedState()
		{
			if (_lastWindowMaximized != Settings.WindowMaximized)
			{
				_lastWindowMaximized = Settings.WindowMaximized;
				Maximized = _lastWindowMaximized;
			}

			bool isWindowMaximized = Maximized;
			if (isWindowMaximized == _lastWindowMaximized)
			{
				return;
			}

			_lastWindowMaximized = isWindowMaximized;
			Settings.WindowMaximized = isWindowMaximized;
		}

		private void SyncWindowSize()
		{
			int configuredWidth = Settings.WindowWidth;
			int configuredHeight = Settings.WindowHeight;
			int currentWidth = Width;
			int currentHeight = Height;

			if (ShouldApplyConfiguredWindowSize(configuredWidth, configuredHeight, currentWidth, currentHeight))
			{
				ApplyConfiguredWindowSize(configuredWidth, configuredHeight);
				return;
			}

			PersistCurrentWindowSize(currentWidth, currentHeight);
		}

		private bool ShouldApplyConfiguredWindowSize(int configuredWidth, int configuredHeight, int currentWidth, int currentHeight)
		{
			if (configuredWidth <= 0 || configuredHeight <= 0)
			{
				return false;
			}

			bool configuredSizeChanged = configuredWidth != _lastWindowWidth || configuredHeight != _lastWindowHeight;
			bool windowAlreadyHasConfiguredSize = configuredWidth == currentWidth && configuredHeight == currentHeight;
			return configuredSizeChanged && !windowAlreadyHasConfiguredSize;
		}

		private void ApplyConfiguredWindowSize(int configuredWidth, int configuredHeight)
		{
			SetWindowSize(configuredWidth, configuredHeight);
			_lastWindowWidth = configuredWidth;
			_lastWindowHeight = configuredHeight;
			_pendingWindowSizeCandidate = null;
			_debounceService.Cancel(GameDebounceKeys.WindowSize);
			_canvasSizeDirty = true;
			_hasUpdate = true;
		}

		private void PersistCurrentWindowSize(int currentWidth, int currentHeight)
		{
			if (currentWidth == _lastWindowWidth && currentHeight == _lastWindowHeight)
			{
				_pendingWindowSizeCandidate = null;
				return;
			}

			Size currentSize = new Size(currentWidth, currentHeight);
			if (_pendingWindowSizeCandidate.HasValue && _pendingWindowSizeCandidate.Value == currentSize)
			{
				return;
			}

			_pendingWindowSizeCandidate = currentSize;
			_canvasSizeDirty = true;
			_debounceService.Debounce(GameDebounceKeys.WindowSize, WindowPersistDebounce, () => PersistWindowSize(currentSize));
		}

		private void PersistWindowSize(Size size)
		{
			if (size.Width == _lastWindowWidth && size.Height == _lastWindowHeight)
			{
				return;
			}

			_lastWindowWidth = size.Width;
			_lastWindowHeight = size.Height;
			_pendingWindowSizeCandidate = null;
			Settings.WindowWidth = size.Width;
			Settings.WindowHeight = size.Height;

#if DEBUG
			_runtime.Log($"[Debounce] Persisted window size: {size.Width}x{size.Height}");
#endif
		}

		private void PersistWindowPosition()
		{
			Point currentPosition = new Point(PositionX, PositionY);
			if (currentPosition == _lastWindowPosition)
			{
				_pendingWindowPositionCandidate = null;
				return;
			}

			if (_pendingWindowPositionCandidate.HasValue && _pendingWindowPositionCandidate.Value == currentPosition)
			{
				return;
			}

			_pendingWindowPositionCandidate = currentPosition;
			_debounceService.Debounce(GameDebounceKeys.WindowPosition, WindowPersistDebounce, () => PersistWindowPositionValue(currentPosition));
		}

		private void PersistWindowPositionValue(Point position)
		{
			if (position == _lastWindowPosition)
			{
				return;
			}

			_lastWindowPosition = position;
			_pendingWindowPositionCandidate = null;
			Settings.WindowPosition = position;

#if DEBUG
			_runtime.Log($"[Debounce] Persisted window position: X={position.X}, Y={position.Y}");
#endif
		}

		private void WindowResize(object sender, EventArgs args)
		{
			_windowStateDirty = true;
			_canvasSizeDirty = true;
			_hasUpdate = true;
		}

		private void WindowMoved(object sender, EventArgs args)
		{
			_windowStateDirty = true;
		}

		private void WindowStateChanged(object sender, EventArgs args)
		{
			_windowStateDirty = true;
			_canvasSizeDirty = true;
			_hasUpdate = true;
			_debounceService.Cancel(GameDebounceKeys.WindowPosition);
			_debounceService.Cancel(GameDebounceKeys.WindowSize);
		}

		private void Draw(object sender, EventArgs args)
		{
			bool isFpsOverlayEnabled = RuntimeHandler.CurrentFpsCorner != FpsCorner.Off;

			if (_cursorDirty)
			{
				_cursorDirty = false;
				ApplyCursorUpdate();
			}
			if (!_hasUpdate && !_cursorMoved && !isFpsOverlayEnabled) return;
			bool gameFrameUpdated = _hasUpdate;
			if (gameFrameUpdated)
			{
				// measure game draw time separately so the FPS overlay can show it without including the overhead of texture uploads and rendering, 
				// which is especially important for accurately reflecting performance when running with vsync enabled
				Stopwatch gameWatch = Stopwatch.StartNew();
				_runtime.InvokeDraw();
				gameWatch.Stop();
				_lastGameDrawMs = gameWatch.Elapsed.TotalMilliseconds;
				_hasUpdate = false;
				InvalidateLayerTextureCache();
			}
			_cursorMoved = false;

			// Refresh overlay texture BEFORE render measurement so bitmap building doesn't inflate metrics.
			DrawFpsOverlay(isFpsOverlayEnabled, gameFrameUpdated);

			Stopwatch renderWatch = Stopwatch.StartNew();
			// measure total render time including texture uploads and FPS overlay rendering, 
			// which reflects the actual frame time and is relevant for performance analysis when running with vsync enabled
			Render();
			renderWatch.Stop();
			_lastFrameRenderMs = renderWatch.Elapsed.TotalMilliseconds;
		}

		private void DrawFpsOverlay(bool isFpsOverlayEnabled, bool gameFrameUpdated)
		{
			// clean up old overlay texture if FPS overlay is disabled or was just turned off, to free GPU memory used by the texture
			FpsCorner fpsCorner = isFpsOverlayEnabled ? RuntimeHandler.CurrentFpsCorner : FpsCorner.Off;
			
			if (_fpsOverlayDrawDelegate.TryBuildOverlayBitmap(
				fpsCorner,
				gameFrameUpdated,
				_lastGameDrawMs,
				_lastFrameRenderMs,
				out Bytemap? overlayBitmap,
				out bool shouldClearTexture))
			{
				try
				{
					ReleaseFpsOverlayTexture();
					if (overlayBitmap != null)
					{
						_fpsOverlayTexture = CreateTexture(_runtime.Palette, overlayBitmap);
					}
				}
				finally
				{
					overlayBitmap?.Dispose();
				}
			}
			else if (shouldClearTexture)
			{
				ReleaseFpsOverlayTexture();
			}
		}

		// Called from the game thread via Runtime.Cursor setter — must not touch SDL here.
		private void CursorChanged(object sender, EventArgs args) => _cursorDirty = true;

		// Called on the render thread (from Draw) to apply the pending cursor update.
		private void ApplyCursorUpdate()
		{
			CursorVisible = !(Settings.CursorType != CursorType.Native || _runtime.CurrentCursor == MouseCursor.None);
			CursorTexture?.Dispose();
			CursorTexture = CreateTexture(_runtime.Cursor);
		}

		private PointF GetScaleF()
		{
			GetBorders(out int x1, out int y1, out int x2, out int y2);
			float scaleX = (float)(x2 - x1) / CanvasWidth;
			float scaleY = (float)(y2 - y1) / CanvasHeight;
			if (Settings.AspectRatio == AspectRatio.ScaledFixed)
			{
				if (scaleX > scaleY) scaleX = scaleY;
				else scaleY = scaleX;
			}
			return new PointF(scaleX, scaleY);
		}

		private Size InputSize
		{
			get
			{
				Bytemap topLayer = _runtime.Layers?.LastOrDefault();
				if (topLayer != null && topLayer.Width > 0 && topLayer.Height > 0)
				{
					return new Size(topLayer.Width, topLayer.Height);
				}
				return new Size(CanvasWidth, CanvasHeight);
			}
		}

		private static ScreenEventArgs CreateScreenEventArgs(int x, int y, MouseButton buttons)
			=> buttons == MouseButton.None ? new ScreenEventArgs(x, y) : new ScreenEventArgs(x, y, buttons);

		private static int ScaleToRange(int value, int sourceSize, int targetSize)
			=> (int)((float)value * targetSize / sourceSize);

		private static int Clamp(int value, int min, int max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		private ScreenEventArgs Transform(ScreenEventArgs args)
		{
			GetBorders(out int offsetX, out int offsetY, out int x2, out int y2);
			int drawWidth = x2 - offsetX;
			int drawHeight = y2 - offsetY;
			Size inputSize = InputSize;
			int localX = args.X - offsetX;
			int localY = args.Y - offsetY;
			if (drawWidth <= 0 || drawHeight <= 0)
			{
				return CreateScreenEventArgs(0, 0, args.Buttons);
			}

			int x = Clamp(ScaleToRange(localX, drawWidth, inputSize.Width), 0, inputSize.Width - 1);
			int y = Clamp(ScaleToRange(localY, drawHeight, inputSize.Height), 0, inputSize.Height - 1);

			return CreateScreenEventArgs(x, y, args.Buttons);
		}

		private void KeyDown(object sender, KeyboardEventArgs args)
		{
			if (args.Key == Key.None) return;
			if (args.Modifier == KeyModifier.Alt && args.Key == Key.Enter)
			{
				Fullscreen = !Fullscreen;
				Settings.FullScreen = Fullscreen;
				return;
			}
			_runtime.InvokeKeyboardDown(args);
		}

		private void KeyUp(object sender, KeyboardEventArgs args)
		{
			if (args.Key == Key.None) return;
			if (args.Key == Key.Pause)
			{
				Paused = !Paused;
				return;
			}
			_runtime.InvokeKeyboardUp(args);

		}			

			private void MouseMove(object sender, ScreenEventArgs args)
		{
			if (!IsInsideDrawArea(args)) return;
			args = Transform(args);
			if (args.X == _mouseX && args.Y == _mouseY) return;
			// Cursor-only redraw: reuse cached layer textures. If the screen needs a full refresh
			// in response to this move, it will call Refresh() during InvokeMouseMove and the next
			// Update tick will promote this to _hasUpdate.
			_cursorMoved = true;
			_mouseX = args.X;
			_mouseY = args.Y;
			_runtime.InvokeMouseMove(args);
		}

		private void MouseDown(object sender, ScreenEventArgs args)
        {
			if (!IsInsideDrawArea(args)) return;
            args = Transform(args);
            _runtime.InvokeMouseDown(args);
        }

		/// <summary>
		/// Checks if the given mouse event is within the current draw area (i.e. not in letterbox borders).
		/// This may be used to ignore mouse events that occur outside the draw area when the window is larger than the canvas (e.g. due to aspect ratio settings or user resizing).
		/// </summary>
		/// <param name="args">The mouse event arguments.</param>
		private bool IsInsideDrawArea(ScreenEventArgs args)
		{
			GetBorders(out int x1, out int y1, out int x2, out int y2);
			return args.X >= x1 && args.X < x2 && args.Y >= y1 && args.Y < y2;
		}

        private void MouseUp(object sender, ScreenEventArgs args)
		{
            if (!IsInsideDrawArea(args)) return;
            args = Transform(args);
			_runtime.InvokeMouseUp(args);
		}

		private void RestoreWindowPlacement()
		{
			if (Settings.FullScreen)
			{
				return;
			}

			Point position = Settings.WindowPosition;
			int x = position.X;
			int y = position.Y;
			if (x < 0 || y < 0 || !IsPointInAnyDisplay(x, y))
			{
				x = 0;
				y = 0;
			}

			SetWindowPosition(x, y);
			_lastWindowPosition = new Point(x, y);
			_pendingWindowPositionCandidate = null;
			Settings.WindowPosition = _lastWindowPosition;

			Maximized = Settings.WindowMaximized;
			_lastWindowMaximized = Settings.WindowMaximized;
			_windowStateDirty = true;
			_canvasSizeDirty = true;
		}

		// Stored delegate references so they can be removed in Dispose(). Without this, the
		// previous inline-lambda subscriptions kept the entire GameWindow (and its SDL handles,
		// cursor texture, ...) alive forever if the Runtime outlived the window (e.g. on settings
		// changes that recreated the window).
		private readonly Action<string> _setWindowTitleHandler;
		private readonly Action<string> _onLogHandler;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Unsubscribe from runtime events first so a late event firing during teardown
				// cannot reach a half-disposed GameWindow.
				if (_runtime != null)
				{
					_runtime.CursorChanged -= CursorChanged;
					if (_setWindowTitleHandler != null) _runtime.SetWindowTitle -= _setWindowTitleHandler;
					_runtime.PlaySound -= PlaySound;
					_runtime.StopSound -= StopSound;
				}
				if (_onLogHandler != null) OnLog -= _onLogHandler;
				OnLoad -= Load;
				OnUpdate -= Update;
				OnDraw -= Draw;
				OnWindowResize -= WindowResize;
				OnWindowMove -= WindowMoved;
				OnWindowStateChanged -= WindowStateChanged;
				OnKeyDown -= KeyDown;
				OnKeyUp -= KeyUp;
				OnMouseMove -= MouseMove;
				OnMouseDown -= MouseDown;
				OnMouseUp -= MouseUp;

				CursorTexture?.Dispose();
				CursorTexture = null;
				ReleaseFpsOverlayTexture();
				_fpsOverlayDrawDelegate.Dispose();

				DisposeCachedLayerTextures();
			}

			base.Dispose(disposing);
		}

		public GameWindow(Runtime runtime, bool softwareRender, IDebounceService debounceService) : base(ApplyMcpTitleState(runtime, "CivOne"), InitialWidth, InitialHeight, Settings.FullScreen, softwareRender)
		{
			_runtime = runtime;
			_debounceService = debounceService ?? throw new ArgumentNullException(nameof(debounceService));

			SetIcon(Resources.GetWindowIcon());
			RestoreWindowPlacement();

			_setWindowTitleHandler = title => Title = ApplyMcpTitleState(_runtime, title);
			_onLogHandler = (message) => _runtime.Log(message);

			_runtime.CursorChanged += CursorChanged;
			_runtime.SetWindowTitle += _setWindowTitleHandler;

			OnLog += _onLogHandler;
			OnLoad += Load;
			OnUpdate += Update;
			OnDraw += Draw;
			OnWindowResize += WindowResize;
			OnWindowMove += WindowMoved;
			OnWindowStateChanged += WindowStateChanged;
			OnKeyDown += KeyDown;
			OnKeyUp += KeyUp;
			OnMouseMove += MouseMove;
			OnMouseDown += MouseDown;
			OnMouseUp += MouseUp;

			if (!_runtime.Settings.Get<bool>("no-sound"))
			{
				runtime.PlaySound += PlaySound;
				runtime.StopSound += StopSound;
			}
		}
	}
}