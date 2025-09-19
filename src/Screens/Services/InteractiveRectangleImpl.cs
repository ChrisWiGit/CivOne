using System;
using System.Drawing;
using CivOne.Graphics;

namespace CivOne.Screens.Services
{
	public class InteractiveRectangleImpl(
		IBitmap bitmap,
		Rectangle hitBox,
		int drawOffsetX = 0,
		int drawOffsetY = 0) : IInteractiveRectangle
	{
		private readonly IBitmap _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
		private readonly Rectangle _hitBox = hitBox;
		private Rectangle _drawBox = new Rectangle(
				hitBox.X + drawOffsetX,
				hitBox.Y + drawOffsetY,
				hitBox.Width,
				hitBox.Height);

		public bool Contains(Point p)
		{
			return _hitBox.Contains(p);
		}

		public void DrawRectangle(byte color)
		{
			_bitmap.DrawRectangle(_drawBox, color);
		}

		public static IInteractiveRectangle Build(
			IBitmap bitmap,
			Rectangle hitBox,
			int drawOffsetX = 0,
			int drawOffsetY = 0)
		{
			return new InteractiveRectangleImpl(bitmap, hitBox, drawOffsetX, drawOffsetY);
		}
	}
}