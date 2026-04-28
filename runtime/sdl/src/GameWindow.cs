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

			if (_settingsFullscreen != Settings.FullScreen)
			{
				_settingsFullscreen = Settings.FullScreen;
				if (_settingsFullscreen)
				{
					// Save the display resolution so WindowSize knows the screen dimensions
					var displaySize = GetDisplaySize();
					if (displaySize.Width > 0 && displaySize.Height > 0)
					{
						_settingsWindowWidth  = displaySize.Width;
						_settingsWindowHeight = displaySize.Height;
						Settings.WindowWidth  = displaySize.Width;
						Settings.WindowHeight = displaySize.Height;
					}
				}
				Fullscreen = _settingsFullscreen;
			}

			if (_settingsScale != Settings.Scale)
			{
				ResetWindowScale();
				_settingsScale = Settings.Scale;
			}

			if (!Settings.FullScreen)
			{
				int settW = Settings.WindowWidth;
				int settH = Settings.WindowHeight;
				int winW = Width;
				int winH = Height;

				// Settings changed externally (e.g. preset selected in Setup menu) → resize window immediately
				if (settW > 0 && settH > 0 && (settW != _settingsWindowWidth || settH != _settingsWindowHeight) && (settW != winW || settH != winH))
				{
					SetWindowSize(settW, settH);
					_settingsWindowWidth = settW;
					_settingsWindowHeight = settH;
				}
				// Window was resized by user → persist to settings
				else if (winW != _settingsWindowWidth || winH != _settingsWindowHeight)
				{
					_settingsWindowWidth = winW;
					_settingsWindowHeight = winH;
					Settings.WindowWidth = winW;
					Settings.WindowHeight = winH;
				}
			}
			
			Runtime.CanvasSize = SetCanvasSize();
			if (_runtime.SignalQuit) StopRunning();
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

		public GameWindow(Runtime runtime, bool softwareRender) : base("CivOne", InitialWidth, InitialHeight, Settings.FullScreen, softwareRender)
		{
			Icon = Resources.GetWindowIcon();

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