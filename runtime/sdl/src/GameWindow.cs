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
using CivOne.Graphics;
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
		private bool _settingsFullscreen = Settings.FullScreen;

		private int _settingsScale = Settings.Scale;
		private int _settingsWindowWidth = Settings.WindowWidth;
		private int _settingsWindowHeight = Settings.WindowHeight;
		private Size? _pendingWindowSizeCandidate;
		private Point _settingsWindowPosition = Settings.WindowPosition;
		private Point? _pendingWindowPositionCandidate;
		private bool _settingsWindowMaximized = Settings.WindowMaximized;
		private static readonly TimeSpan WindowPersistDebounce = TimeSpan.FromSeconds(1);

		private void Load(object sender, EventArgs args)
		{
			Runtime.CanvasSize = SetCanvasSize();
			Runtime.WindowSize = ClientRectangle;
			_runtime.InvokeInitialize();
		}

		private void Update(object sender, EventArgs args)
		{
			UpdateEventArgs updateArgs = UpdateEventArgs.Empty;
			_runtime.InvokeUpdate(ref updateArgs);
			_hasUpdate = (_hasUpdate || updateArgs.HasUpdate);

			ApplyFullscreenSettingChanges();
			ApplyScaleSettingChanges();
			SyncWindowedStateWithSettings();
			_debounceService.ExecuteDueCallbacks();
			
			Runtime.CanvasSize = SetCanvasSize();
			Runtime.WindowSize = ClientRectangle;
			if (_runtime.SignalQuit)
			{
				_debounceService.FlushPendingCallbacks();
				StopRunning();
			}
		}

		private void ApplyFullscreenSettingChanges()
		{
			if (_settingsFullscreen == Settings.FullScreen)
			{
				return;
			}

			_settingsFullscreen = Settings.FullScreen;
			if (_settingsFullscreen)
			{
				PersistDisplayResolutionAsWindowSize();
			}

			Fullscreen = _settingsFullscreen;
		}

		private void PersistDisplayResolutionAsWindowSize()
		{
			Size displaySize = GetDisplaySize();
			if (displaySize.Width <= 0 || displaySize.Height <= 0)
			{
				return;
			}

			_settingsWindowWidth = displaySize.Width;
			_settingsWindowHeight = displaySize.Height;
			_pendingWindowSizeCandidate = null;
			Settings.WindowWidth = displaySize.Width;
			Settings.WindowHeight = displaySize.Height;
		}

		private void ApplyScaleSettingChanges()
		{
			if (_settingsScale == Settings.Scale)
			{
				return;
			}

			ResetWindowScale();
			_settingsScale = Settings.Scale;
		}

		private void SyncWindowedStateWithSettings()
		{
			if (Settings.FullScreen)
			{
				return;
			}

			SyncMaximizedState();
			if (_settingsWindowMaximized)
			{
				return;
			}

			SyncWindowSize();
			PersistWindowPosition();
		}

		private void SyncMaximizedState()
		{
			if (_settingsWindowMaximized != Settings.WindowMaximized)
			{
				_settingsWindowMaximized = Settings.WindowMaximized;
				Maximized = _settingsWindowMaximized;
			}

			bool isWindowMaximized = Maximized;
			if (isWindowMaximized == _settingsWindowMaximized)
			{
				return;
			}

			_settingsWindowMaximized = isWindowMaximized;
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

			bool configuredSizeChanged = configuredWidth != _settingsWindowWidth || configuredHeight != _settingsWindowHeight;
			bool windowAlreadyHasConfiguredSize = configuredWidth == currentWidth && configuredHeight == currentHeight;
			return configuredSizeChanged && !windowAlreadyHasConfiguredSize;
		}

		private void ApplyConfiguredWindowSize(int configuredWidth, int configuredHeight)
		{
			SetWindowSize(configuredWidth, configuredHeight);
			_settingsWindowWidth = configuredWidth;
			_settingsWindowHeight = configuredHeight;
			_pendingWindowSizeCandidate = null;
			_debounceService.Cancel(GameDebounceKeys.WindowSize);
		}

		private void PersistCurrentWindowSize(int currentWidth, int currentHeight)
		{
			if (currentWidth == _settingsWindowWidth && currentHeight == _settingsWindowHeight)
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
			_debounceService.Debounce(GameDebounceKeys.WindowSize, WindowPersistDebounce, () => PersistWindowSize(currentSize));
		}

		private void PersistWindowSize(Size size)
		{
			if (size.Width == _settingsWindowWidth && size.Height == _settingsWindowHeight)
			{
				return;
			}

			_settingsWindowWidth = size.Width;
			_settingsWindowHeight = size.Height;
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
			if (currentPosition == _settingsWindowPosition)
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
			if (position == _settingsWindowPosition)
			{
				return;
			}

			_settingsWindowPosition = position;
			_pendingWindowPositionCandidate = null;
			Settings.WindowPosition = position;

#if DEBUG
			_runtime.Log($"[Debounce] Persisted window position: X={position.X}, Y={position.Y}");
#endif
		}

		private void Draw(object sender, EventArgs args)
		{
			if (_cursorDirty)
			{
				_cursorDirty = false;
				ApplyCursorUpdate();
			}
			if (!_hasUpdate && !_cursorMoved) return;
			if (_hasUpdate)
			{
				_runtime.InvokeDraw();
				_hasUpdate = false;
				InvalidateLayerTextureCache();
			}
			_cursorMoved = false;

			Render();
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
			_settingsWindowPosition = new Point(x, y);
			_pendingWindowPositionCandidate = null;
			Settings.WindowPosition = _settingsWindowPosition;

			Maximized = Settings.WindowMaximized;
			_settingsWindowMaximized = Settings.WindowMaximized;
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
				OnKeyDown -= KeyDown;
				OnKeyUp -= KeyUp;
				OnMouseMove -= MouseMove;
				OnMouseDown -= MouseDown;
				OnMouseUp -= MouseUp;

				CursorTexture?.Dispose();
				CursorTexture = null;

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
			OnWindowResize += (s, a) => _hasUpdate = true;
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