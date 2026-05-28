// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne
{
	#pragma warning disable S101 // Types should be named in PascalCase - but these are named to match SDL as a name.
	internal static partial class SDL
	{
		internal abstract partial class Window
		{
			protected event EventHandler OnLoad, OnDraw, OnUpdate;

			/// <summary>
			/// Raised when the SDL window has been resized by the user or the system.
			/// </summary>
			/// <remarks>
			/// Fires for both SDL_WINDOWEVENT_RESIZED and SDL_WINDOWEVENT_SIZE_CHANGED so
			/// hosts can force a redraw after a drag-resize completes.
			/// </remarks>
			protected event EventHandler OnWindowResize;
			/// <summary>
			/// Raised when the SDL window position changes.
			/// </summary>
			/// <remarks>
			/// Used by hosts to persist window placement and trigger deferred sync logic
			/// without polling window coordinates every frame.
			/// </remarks>
			protected event EventHandler OnWindowMove;
			/// <summary>
			/// Raised when the SDL window state changes.
			/// </summary>
			/// <remarks>
			/// Covers state transitions like shown/restored/minimized/maximized.
			/// Hosts can use this to refresh cached window/canvas state after OS-driven
			/// state transitions.
			/// </remarks>
			protected event EventHandler OnWindowStateChanged;

			public event EventHandler OnClose;

			private bool _fullscreen;
			protected bool Fullscreen
			{
				get => _fullscreen;
				set
				{
					if (value == _fullscreen) return;
					_fullscreen = value;
					SDL_SetWindowFullscreen(_handle, _fullscreen ? SDL_WINDOW.FULLSCREEN_DESKTOP : 0);
				}
			}

			private void Close()
			{
				OnClose?.Invoke(this, EventArgs.Empty);
				_running = false;
			}

			private void HandleEventWindow(SDL_WindowEvent windowEvent)
			{
				switch(windowEvent.Event)
				{
					case SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
					case SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN:
						Paused = false;
						_redraw = true;
						OnWindowStateChanged?.Invoke(this, EventArgs.Empty);
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN:
						Paused = true;
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED:
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:
						OnWindowMove?.Invoke(this, EventArgs.Empty);
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
					case SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
						_redraw = true;
						OnWindowResize?.Invoke(this, EventArgs.Empty);
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
						Paused = true;
						OnWindowStateChanged?.Invoke(this, EventArgs.Empty);
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:
						OnWindowStateChanged?.Invoke(this, EventArgs.Empty);
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
						ResetMouseButtonState();
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
						Close();
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_TAKE_FOCUS:
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_HIT_TEST:
						break;
				}
			}
		}
	}
}