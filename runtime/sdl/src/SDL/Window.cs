// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Graphics;
using CivOne.IO;
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// ReSharper disable InconsistentNaming

namespace CivOne
{
	#pragma warning disable S101 // Types should be named in PascalCase - but these are named to match SDL as a name.
	internal static partial class SDL
	{
		internal abstract partial class Window : IDisposable
		{
			private readonly IntPtr _handle, _renderer;

			private bool _running = true;
			private bool _redraw;
			private void Log(string message) => OnLog?.Invoke(message);
			private bool _paused;
			private string _sdlTitle;
			private string _title;

			protected event Action<string>? OnLog;

			protected Texture CreateTexture(IBitmap? bitmap) => new(_renderer, bitmap?.Palette, bitmap?.Bitmap);
			protected Texture CreateTexture(Palette? palette, Bytemap? bytemap) => new(_renderer, palette, bytemap);

			/// <summary>
			/// Creates an empty streaming texture for the render-loop layer cache.
			/// Caller is responsible for refilling it via <see cref="Texture.UpdateFrom"/>.
			/// </summary>
			protected Texture CreateLayerTexture(int width, int height) => new(_renderer, width, height);

			protected void Clear(Color color)
			{
				_redraw = true;

				var result = SDL_SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
				if (result != 0)
				{
					Log($"SDL_SetRenderDrawColor failed: {GetSdlErrorMessage()}");
				}
				_ = SDL_RenderClear(_renderer);
			}

			protected void StopRunning()
			{
				_running = false;
			}

			/// <summary>
			/// Reinterprets SDL_Event as the requested sub-struct type (e.g., SDL_WindowEvent, SDL_KeyboardEvent).
			/// Uses Unsafe.As for zero-allocation casting instead of AllocHGlobal + StructureToPtr + FreeHGlobal roundtrip.
			/// Safe because SDL_Event is an unsafe struct union with LayoutKind.Sequential,
			/// and all sub-types have compatible field layouts (EventType/Type at offset 0).
			/// Performance note: eliminates allocation per SDL event; prior version allocated ~64 bytes on heap per event.
			/// </summary>
			private static T CastToStruct<T>(SDL_Event source) where T : struct
				=> Unsafe.As<SDL_Event, T>(ref source);

			private void HandleEvent(SDL_Event sdlEvent)
			{
				switch (sdlEvent.SDL_EventType)
				{
					case SDL_EventType.SDL_WINDOWEVENT:
						HandleEventWindow(CastToStruct<SDL_WindowEvent>(sdlEvent));
						break;
					case SDL_EventType.SDL_KEYDOWN:
					case SDL_EventType.SDL_KEYUP:
						HandleEventKeyboard(CastToStruct<SDL_KeyboardEvent>(sdlEvent));
						break;
					case SDL_EventType.SDL_MOUSEWHEEL:
						HandleMouseWheel(CastToStruct<SDL_MouseWheelEvent>(sdlEvent));
						break;
				}
			}

			private bool _pixelScale;
			protected bool PixelScale
			{
				get => _pixelScale;
				set
				{
					if (!SDL_SetHint("SDL_RENDER_SCALE_QUALITY", value ? "1" : "0"))
						return;
					_pixelScale = value;
				}
			}

			protected bool Paused
			{
				get => _paused;
				set
				{
					if (_paused == value) return;
					_paused = value;
					UpdateTitle();
				}
			}

			private void UpdateTitle()
			{
				string title = Title;
				if (_paused)
				{
					title += " (Paused)";
				}
				if (title == _sdlTitle) return;
				_sdlTitle = title;
				SDL_SetWindowTitle(_handle, _sdlTitle);
			}

			private static bool HitDebugKeys(SDL_Event sdlEvent, SDL_Scancode scancode)
			{
				if (sdlEvent.SDL_EventType != SDL_EventType.SDL_KEYDOWN) return false;

				SDL_KeyboardEvent keyboardEvent = CastToStruct<SDL_KeyboardEvent>(sdlEvent);
				if (keyboardEvent.KeySym.Scancode == scancode &&
					(keyboardEvent.KeySym.Modifier & (SDL_KMOD.KMOD_LSHIFT | SDL_KMOD.KMOD_RSHIFT)) != 0 &&
					(keyboardEvent.KeySym.Modifier & (SDL_KMOD.KMOD_LCTRL | SDL_KMOD.KMOD_RCTRL)) != 0)
				{
					return true;
				}
				return false;
			}

			private bool ProcessPendingEvents()
			{
				while (SDL_PollEvent(out SDL_Event sdlEvent) == 1)
				{
#if DEBUG
					// Split debug hotkeys from normal events: F10/F9 may swallow input,
					// but F12 still runs after HandleEvent to preserve original flow.
					if (HandleDebuggingEvents(sdlEvent))
					{
						continue;
					}
#endif
					HandleEvent(sdlEvent);

					if (!_running)
					{
						return false;
					}

#if DEBUG
					TrapDebugger(sdlEvent);
#endif
				}

				return true;
			}

#if DEBUG
			private static void TrapDebugger(SDL_Event sdlEvent)
			{
				if (HitDebugKeys(sdlEvent, SDL_Scancode.SDL_SCANCODE_F12))
					System.Diagnostics.Debugger.Break();
			}

			private int _eventLoopWaitCounter;

			private bool HandleDebuggingEvents(SDL_Event sdlEvent)
			{
				if (HitDebugKeys(sdlEvent, SDL_Scancode.SDL_SCANCODE_F10))
				{
					_eventLoopWaitCounter += 1;
					Log($"Increased event loop wait counter to {_eventLoopWaitCounter} ms");
					return true;
				}
				else if (_eventLoopWaitCounter > 0 && HitDebugKeys(sdlEvent, SDL_Scancode.SDL_SCANCODE_F9))
				{
					_eventLoopWaitCounter -= 1;
					_eventLoopWaitCounter = Math.Max(0, _eventLoopWaitCounter);
					Log($"Decreased event loop wait counter to {_eventLoopWaitCounter} ms");
					return true;
				}

				if (_eventLoopWaitCounter > 0)
				{
					Wait((uint)_eventLoopWaitCounter);
				}
				return false;
			}
#endif

			public void Run()
			{
				OnLoad?.Invoke(this, EventArgs.Empty);

				while (_running)
				{
					if (!ProcessPendingEvents())
					{
						break;
					}

					if (_paused)
					{
						Wait(100);

						continue;
					}

					OnUpdate?.Invoke(this, EventArgs.Empty);
					OnDraw?.Invoke(this, EventArgs.Empty);

					HandleMouse();
					HandleSound();

					if (!_redraw)
					{
						Wait(1);
						continue;
					}

					SDL_RenderPresent(_renderer);
					_redraw = false;
				}
			}

			public static void Wait(uint time)
			{
				SDL_Delay(time);
			}

			protected int Width
			{
				get
				{
					SDL_GetWindowSize(_handle, out int width, out _);
					return width;
				}
			}

			protected int Height
			{
				get
				{
					SDL_GetWindowSize(_handle, out _, out int height);
					return height;
				}
			}

			protected int PositionX
			{
				get
				{
					SDL_GetWindowPosition(_handle, out int x, out _);
					return x;
				}
			}

			protected int PositionY
			{
				get
				{
					SDL_GetWindowPosition(_handle, out _, out int y);
					return y;
				}
			}

			protected bool Maximized
			{
				get => (SDL_GetWindowFlags(_handle) & (uint)SDL_WINDOW.MAXIMIZED) != 0;
				set
				{
					if (value)
						SDL_MaximizeWindow(_handle);
					else
						SDL_RestoreWindow(_handle);
				}
			}

			public string Title
			{
				get => _title ?? string.Empty;
				set
				{
					string baseTitle = value ?? string.Empty;
					if (baseTitle == _title) return;
					Log($@"Changing window title from ""{_title}"" to ""{baseTitle}""");
					_title = baseTitle;
					UpdateTitle();
				}
			}

			public void SetIcon(IBitmap value)
			{
				ArgumentNullException.ThrowIfNull(value);

				int width = value.Width(), height = value.Height();
				byte[] bytes = new byte[width * height * 4];

				int i = 0;
				for (int yy = 0; yy < height; yy++)
				{
					for (int xx = 0; xx < width; xx++)
					{
						Colour colour = value.Palette[value.Bitmap[xx, yy]];
						bytes[i++] = colour.A;
						bytes[i++] = colour.R;
						bytes[i++] = colour.G;
						bytes[i++] = colour.B;
					}
				}
				IntPtr pixels = Marshal.AllocHGlobal(bytes.Length);
				IntPtr surface = IntPtr.Zero;
				try
				{
					Marshal.Copy(bytes, 0, pixels, bytes.Length);
					surface = SDL_CreateRGBSurfaceFrom(pixels, width, height, 32, width * 4, 0x0000ff00, 0x00ff0000, 0xff000000, 0x000000ff);
					if (surface != IntPtr.Zero)
					{
						SDL_SetWindowIcon(_handle, surface);
					}
				}
				finally
				{
					if (surface != IntPtr.Zero) SDL_FreeSurface(surface);
					Marshal.FreeHGlobal(pixels);
				}
			}

			protected Window(string title, int width, int height, bool fullscreen, bool softwareRender = false)
			{
				_title = title;
				_sdlTitle = string.Empty;

				if (SDL_Init(SDL_INIT.VIDEO | SDL_INIT.AUDIO) < 0)
					throw new InvalidOperationException($"SDL_Init failed: {GetSdlErrorMessage()}");

				SDL_WINDOW flags = SDL_WINDOW.RESIZABLE;

				_fullscreen = fullscreen;
				if (fullscreen)
					flags |= SDL_WINDOW.FULLSCREEN_DESKTOP;

				_handle = SDL_CreateWindow(title, 100, 100, width, height, flags);
				if (_handle == IntPtr.Zero)
					throw new InvalidOperationException($"SDL_CreateWindow failed: {GetSdlErrorMessage()}");

				bool vSyncEnabled = Settings.Instance.VSync;
				SDL_RENDERER_FLAGS rendererFlags = SDL_RENDERER_FLAGS.SDL_RENDERER_ACCELERATED;
				if (vSyncEnabled)
				{
					rendererFlags |= SDL_RENDERER_FLAGS.SDL_RENDERER_PRESENTVSYNC;
				}

				_renderer = softwareRender ? IntPtr.Zero : SDL_CreateRenderer(_handle, -1, rendererFlags);
				if (_renderer == IntPtr.Zero && !softwareRender && vSyncEnabled)
				{
					_renderer = SDL_CreateRenderer(_handle, -1, SDL_RENDERER_FLAGS.SDL_RENDERER_ACCELERATED);
				}
				if (_renderer == IntPtr.Zero)
				{
					_renderer = SDL_CreateRenderer(_handle, -1, SDL_RENDERER_FLAGS.SDL_RENDERER_SOFTWARE);
				}

				// Should be default, just to be sure
				PixelScale = false;

				// Run OS native functions for initialization
				Native.Init(_handle);
				UpdateTitle();
			}

			protected void SetWindowSize(int width, int height)
			{				
				SDL_SetWindowSize(_handle, width, height);
			}

			protected void SetWindowPosition(int x, int y)
			{
				SDL_SetWindowPosition(_handle, x, y);
			}

			/// <summary>Returns the resolution of the display the window is currently on.</summary>
			protected Size GetDisplaySize()
			{
				int idx = SDL_GetWindowDisplayIndex(_handle);
				if (idx < 0) return Size.Empty;
				if (SDL_GetDisplayBounds(idx, out SDL_DisplayRect r) != 0) 
					return Size.Empty;
				return new Size(r.w, r.h);
			}

			protected static bool IsPointInAnyDisplay(int x, int y)
			{
				int displays = SDL_GetNumVideoDisplays();
				if (displays <= 0)
				{
					return true;
				}

				for (int i = 0; i < displays; i++)
				{
					if (SDL_GetDisplayBounds(i, out SDL_DisplayRect r) != 0)
					{
						continue;
					}

					if (x >= r.x && x < (r.x + r.w) && y >= r.y && y < (r.y + r.h))
					{
						return true;
					}
				}

				return false;
			}

			private bool _disposed;

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			~Window() => Dispose(false);

			protected virtual void Dispose(bool disposing)
			{
				if (_disposed) return;
				_disposed = true;

				if (disposing)
				{
					// Ensure active sound is released before SDL audio is shut down.
					StopSound();
				}

				if (_renderer != IntPtr.Zero) SDL_DestroyRenderer(_renderer);
				if (_handle != IntPtr.Zero) SDL_DestroyWindow(_handle);
				SDL_Quit();
			}
		}
	}
}