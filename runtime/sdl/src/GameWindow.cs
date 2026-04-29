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

		private static Settings Settings => Settings.Instance;

		private int _mouseX = -1, _mouseY = -1;

		private bool _hasUpdate = true;
		private bool _settingsFullscreen = Settings.FullScreen;

		private int _settingsScale = Settings.Scale;
		private int _settingsWindowWidth = Settings.WindowWidth;
		private int _settingsWindowHeight = Settings.WindowHeight;
		private Point _settingsWindowPosition = Settings.WindowPosition;
		private Point _pendingWindowPosition = Settings.WindowPosition;
		private bool _hasPendingWindowPositionPersist;
		private DateTime _lastWindowPositionChangeUtc = DateTime.MinValue;
		private bool _settingsWindowMaximized = Settings.WindowMaximized;
		private static readonly TimeSpan WindowPositionPersistDebounce = TimeSpan.FromSeconds(1);

		private void Load(object sender, EventArgs args)
		{
			Runtime.CanvasSize = SetCanvasSize();
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
			
			Runtime.CanvasSize = SetCanvasSize();
			if (_runtime.SignalQuit)
			{
				PersistPendingWindowPositionIfNeeded();
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
		}

		private void PersistCurrentWindowSize(int currentWidth, int currentHeight)
		{
			if (currentWidth == _settingsWindowWidth && currentHeight == _settingsWindowHeight)
			{
				return;
			}

			_settingsWindowWidth = currentWidth;
			_settingsWindowHeight = currentHeight;
			Settings.WindowWidth = currentWidth;
			Settings.WindowHeight = currentHeight;
		}

		private void PersistWindowPosition()
		{
			Point currentPosition = new Point(PositionX, PositionY);
			if (currentPosition == _settingsWindowPosition)
			{
				return;
			}

			if (!_hasPendingWindowPositionPersist || currentPosition != _pendingWindowPosition)
			{
				_pendingWindowPosition = currentPosition;
				_lastWindowPositionChangeUtc = DateTime.UtcNow;
				_hasPendingWindowPositionPersist = true;
			}

			if ((DateTime.UtcNow - _lastWindowPositionChangeUtc) < WindowPositionPersistDebounce)
			{
				return;
			}

			PersistPendingWindowPositionIfNeeded();
		}

		private void PersistPendingWindowPositionIfNeeded()
		{
			if (!_hasPendingWindowPositionPersist)
			{
				return;
			}

			_settingsWindowPosition = _pendingWindowPosition;
			Settings.WindowPosition = _pendingWindowPosition;
			_hasPendingWindowPositionPersist = false;
		}

		private void Draw(object sender, EventArgs args)
		{
			if (!_hasUpdate) return;
			_runtime.InvokeDraw();
			_hasUpdate = false;
			
			Render();
		}

		private void CursorChanged(object sender, EventArgs args)
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
			_runtime.InvokeKeyboardUp(args);
		}

		private void MouseMove(object sender, ScreenEventArgs args)
		{
			if (!IsInsideDrawArea(args)) return;
			args = Transform(args);
			if (args.X == _mouseX && args.Y == _mouseY) return;
			_hasUpdate = true;
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
			Settings.WindowPosition = _settingsWindowPosition;

			Maximized = Settings.WindowMaximized;
			_settingsWindowMaximized = Settings.WindowMaximized;
		}

		public GameWindow(Runtime runtime, bool softwareRender) : base("CivOne", InitialWidth, InitialHeight, Settings.FullScreen, softwareRender)
		{
			Icon = Resources.GetWindowIcon();
			RestoreWindowPlacement();

			_runtime = runtime;
			_runtime.CursorChanged += CursorChanged;
			_runtime.SetWindowTitle += (string title) => Title = title;

			OnLog += (message) => _runtime.Log(message);
			OnLoad += Load;
			OnUpdate += Update;
			OnDraw += Draw;
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