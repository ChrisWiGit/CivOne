using System;
using System.Drawing;
using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Screens.Services
{
	/// <summary>
	/// Implementation of an interactive button.
	/// See <see cref="IInteractiveButton"/> for details.
	/// </summary>
	/// <param name="bitmap">The bitmap on which the button will be drawn and where mouse clicks will be checked.</param>
	/// <param name="buttonDrawer">The button drawer to use for drawing the button.</param>
	/// <param name="bounds">The rectangle defining the button's bounds.</param>
	public class InteractiveButtonImpl(
		IBitmap bitmap,
		IButtonDrawer buttonDrawer,
		Rectangle bounds) : IInteractiveButton
	{
		//CW: Usually you would use a factory method to create instances of this class with the dependencies already injected.
		//e.g. new InteractiveButtonFactory(myBitmap, myButtonDrawer) and use it like this everywhere in code: factory.Create(bounds);
		private readonly IBitmap _bitmap = bitmap;
		private readonly IButtonDrawer _buttonDrawer = buttonDrawer;
		private readonly Rectangle _bounds = new(bounds.X, bounds.Y, bounds.Width, bounds.Height);

		public Rectangle Bounds => _bounds;

		public static readonly IInteractiveButton Empty = new EmptyInteractiveButtonImpl();

		public bool Contains(Point p)
		{
			return _bounds.Contains(p);
		}

		public void DrawRectangle(byte color)
		{
			_bitmap.DrawRectangle(_bounds, color);
		}

		public static IInteractiveButton Build(
			IBitmap bitmap,
			IButtonDrawer buttonDrawer,
			Rectangle hitBox)
		{
			return new InteractiveButtonImpl(bitmap, buttonDrawer, hitBox);
		}

		public void DrawButton(string text, byte fontId, byte colour, byte colourDark)
		{
			_buttonDrawer.DrawButton(text, fontId, colour, colourDark, _bounds.X, _bounds.Y, _bounds.Width, _bounds.Height);
		}

		private class EmptyInteractiveButtonImpl : IInteractiveButton
		{
			public Rectangle Bounds => Rectangle.Empty;

			public EmptyInteractiveButtonImpl()
			{
			}

			public bool Contains(Point p)
			{
				return false;
			}

			public void DrawRectangle(byte color)
			{
				// Do nothing
			}

			public void DrawButton(string text, byte fontId, byte colour, byte colourDark)
			{
				// Do nothing
			}
		}
	}
}