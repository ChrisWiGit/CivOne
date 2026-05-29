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
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne.Screens
{
	public abstract partial class BaseScreen : BaseInstance, IScreen
	{
		private readonly MouseCursor _cursor;

		protected bool _doRefresh = true;
		
		protected int CanvasWidth => RuntimeHandler.Instance.CanvasWidth;
		protected int CanvasHeight => RuntimeHandler.Instance.CanvasHeight;
		protected static Size WindowSize => new(RuntimeHandler.WindowWidth, RuntimeHandler.WindowHeight);
		public virtual bool UseFullWindowCanvas => false;
		private bool CanExpand => Common.HasAttribute<ScreenResizeable>(this);
		private bool SizeChanged => (this.Width() != CanvasWidth || this.Height() != CanvasHeight);

		// Last window size observed by Update; used to detect host-window resizes
		// even when the canvas size stays fixed (e.g. AspectRatio != Expand).
		private Size _lastWindowSize = WindowSize;
		private bool WindowSizeChanged => _lastWindowSize != WindowSize;

		protected event ResizeEventHandler OnResize;

		protected void MouseArgsOffset(ref ScreenEventArgs args, int offsetX, int offsetY)
		{
			args = new ScreenEventArgs(args.X - offsetX, args.Y - offsetY, args.Buttons, args.Modifier, args.WheelDelta);
		}

		public event EventHandler Closed;

		protected void HandleClose()
		{
			if (Closed == null)
				return;
			Closed(this, null);
		}

		protected abstract bool HasUpdate(uint gameTick);

		public void Refresh() => _doRefresh = true;

		public bool RefreshNeeded()
		{
			if (!_doRefresh)
			{
				return false;
			}
			_doRefresh = false;
			return true;
		}

		protected virtual void Resize(int width, int height)
		{
			Bitmap = new Bytemap(width, height);
			Refresh();
			OnResize?.Invoke(this, new ResizeEventArgs(width, height));
		}

		public virtual MouseCursor Cursor => _cursor;

		public virtual bool Update(uint gameTick)
		{
			if (CanExpand && SizeChanged)
			{
				// Use capped canvas size here to match SizeChanged checks.
				// Using Runtime.CanvasWidth can trigger perpetual resize/redraw loops in Expand mode.
				Resize(CanvasWidth, CanvasHeight);
				_lastWindowSize = WindowSize;
				HasUpdate(gameTick);
				return true;
			}

			// Notify resize-aware screens when the host window changed size even though
			// the canvas dimensions stayed the same (e.g. AspectRatio != Expand).
			// This lets screens rebuild layout / force a redraw after a window drag.
			if (CanExpand && WindowSizeChanged)
			{
				_lastWindowSize = WindowSize;
				Refresh();
				OnResize?.Invoke(this, new ResizeEventArgs(this.Width(), this.Height()));
				HasUpdate(gameTick);
				return true;
			}

			return HasUpdate(gameTick);
		}
		public virtual bool KeyDown(KeyboardEventArgs args) => false;
		public virtual bool MouseDown(ScreenEventArgs args) => false;
		public virtual bool MouseUp(ScreenEventArgs args) => false;
		public virtual bool MouseWheel(ScreenEventArgs args) => false;
		public virtual bool MouseDrag(ScreenEventArgs args) => false;
		public virtual bool MouseMove(ScreenEventArgs args) => false;

		protected void Destroy()
		{
			CloseMenus();
			HandleClose();
			Common.DestroyScreen(this);
		}

		protected BaseScreen(MouseCursor cursor = MouseCursor.None)
		{
			_cursor = cursor;
			if (CanExpand)
			{
				Bitmap = new Bytemap(CanvasWidth, CanvasHeight);
			}
			else
			{
				Bitmap = new Bytemap(320, 200);
			}
		}

		protected BaseScreen(int width, int height, MouseCursor cursor = MouseCursor.None)
		{
			_cursor = cursor;
			Bitmap = new Bytemap(width, height);
		}
	}
}