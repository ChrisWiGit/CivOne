// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class WindowTitle : BaseScreen
	{
		private readonly Input _input;

		private bool _done;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		public void Close()
		{
			_done = true;
			_input.Close();
			Destroy();
		}

		public event EventHandler? Accept, Cancel;

		public override bool MouseDown(ScreenEventArgs args)
		{
			Close();
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			Close();
			return true;
		}

		private void Input_Accept(object? sender, EventArgs args)
		{
			_done = true;
			Settings.WindowTitle = _input.Text;
			Accept?.Invoke(this, EventArgs.Empty);
			((Input?)sender)?.Close();
			Close();
		}

		private void Input_Cancel(object? sender, EventArgs args)
		{
			_done = true;
			Cancel?.Invoke(this, EventArgs.Empty);
			((Input?)sender)?.Close();
			Close();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_done && !Common.HasScreenType<Input>())
			{
				Common.AddScreen(_input);
			}
			return false;
		}

		private void DrawDialog()
		{
			this.FillRectangle(OffsetX + 64, OffsetY + 78, 225, 25, 5)
				.FillRectangle(OffsetX + 65, OffsetY + 79, 223, 23, 15)
				.DrawText(Translate("Set window title..."), 0, 5, OffsetX + 66, OffsetY + 80)
				.FillRectangle(OffsetX + 66, OffsetY + 88, 221, 14, 5)
				.FillRectangle(OffsetX + 67, OffsetY + 89, 219, 12, 15);
		}

		protected override void Resize(int width, int height)
		{
			// BaseScreen.Resize replaces the bitmap with an empty one, so the dialog has to be drawn
			// again, centred on the new canvas together with its input field.
			base.Resize(width, height);
			_input.X = OffsetX + 68;
			_input.Y = OffsetY + 90;
			DrawDialog();
		}

		public WindowTitle()
		{
			Palette = Common.Screens.Last().OriginalColours;

			DrawDialog();

			_input = new Input(Palette, Settings.WindowTitle, 0, 5, 11, OffsetX + 68, OffsetY + 90, 133, 10, 32);
			_input.Accept += Input_Accept;
			_input.Cancel += Input_Cancel;
		}
	}
}