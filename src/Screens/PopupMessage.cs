// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class PopupMessage : BaseScreen
	{
		private bool _update = true;
		private readonly byte _colour;
		private readonly string _title;
		private readonly string[] _message;
		private int OffsetX => System.Math.Max(0, (Width - 320) / 2);
		private int OffsetY => System.Math.Max(0, (Height - 200) / 2);

		private void Render()
		{
			this.Clear();

			byte colourLight = (byte)(_colour + 8);
			int lineHeight = Resources.GetFontHeight(1);
			int width = 209;
			int height = ((_message.Length + (_title != null ? 1 : 0)) * lineHeight) + 5;
			int x = 56 + OffsetX;
			int y = 16 + OffsetY;

			this.FillRectangle(x, y, width, 1, colourLight)
				.FillRectangle(x, y + 1, 1, height - 2, colourLight)
				.FillRectangle(x + width - 1, y + 1, 1, height - 2, colourLight)
				.FillRectangle(x, y + height - 1, width, 1, colourLight)
				.FillRectangle(x + 1, y + 1, width - 2, height - 2, _colour);

			int yy = y + 3 - lineHeight;
			if (_title != null)
				this.DrawText(_title, 1, 5, x + (width / 2), (yy += lineHeight), TextAlign.Center);
			for (int i = 0; i < _message.Length; i++)
				this.DrawText(_message[i], 1, 15, x + 8, (yy += lineHeight));
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_update)
			{
				Render();
				_update = false;
				return true;
			}
			return false;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			Destroy();
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			Destroy();
			return true;
		}

		public PopupMessage(byte colour, string title, string[] message) : base(MouseCursor.Pointer)
		{
			_colour = colour;
			_title = title;
			_message = message;

			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			Render();
		}
	}
}