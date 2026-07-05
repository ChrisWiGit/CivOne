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
	internal class Search : BaseScreen
	{
		private readonly Input _input;

		private bool _done;
		private bool _showUnknownCityMessage;
		private string _unknownCityText = string.Empty;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		public void Close()
		{
			_done = true;
			_input.Close();
			Destroy();
		}

		public City? City { get; private set; }

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

		private void Search_Accept(object? sender, EventArgs args)
		{
			Input? input = sender as Input;
			City = Game.GetCities().FirstOrDefault(x => x.Name.StartsWith(_input.Text, StringComparison.CurrentCultureIgnoreCase) && Human.Visible(x.X, x.Y));
			if (City == null)
			{
				_showUnknownCityMessage = true;
				_unknownCityText = _input.Text;
				_done = true;
				RenderDialog();
				input?.Close();
				return;
			}
			_done = true;
			Accept?.Invoke(this, EventArgs.Empty);
			input?.Close();
			Close();
		}

		private void Search_Cancel(object? sender, EventArgs args)
		{
			Input? input = sender as Input;
			_done = true;
			Cancel?.Invoke(this, EventArgs.Empty);
			input?.Close();
			Close();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				RenderDialog();
				return true;
			}

			if (!_done && !Common.HasScreenType<Input>())
			{
				Common.AddScreen(_input);
			}
			return false;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_input.X = OffsetX + 68;
			_input.Y = OffsetY + 90;
			RenderDialog();
		}

		private void RenderDialog()
		{
			Bitmap.Clear();
			if (_showUnknownCityMessage)
			{
				this.FillRectangle(OffsetX + 64, OffsetY + 78, 225, 25, 5)
					.FillRectangle(OffsetX + 65, OffsetY + 79, 223, 23, 15)
					.DrawText(Translate("City not found."), 0, 5, OffsetX + 66, OffsetY + 80)
					.FillRectangle(OffsetX + 66, OffsetY + 88, 137, 14, 5)
					.FillRectangle(OffsetX + 67, OffsetY + 89, 135, 12, 15)
					.DrawText(_unknownCityText, 0, 5, OffsetX + 68, OffsetY + 91);
				return;
			}

			this.FillRectangle(OffsetX + 64, OffsetY + 78, 225, 25, 5)
				.FillRectangle(OffsetX + 65, OffsetY + 79, 223, 23, 15)
				.DrawText(Translate("Where in the heck is ... (city name)"), 0, 5, OffsetX + 66, OffsetY + 80)
				.FillRectangle(OffsetX + 66, OffsetY + 88, 137, 14, 5)
				.FillRectangle(OffsetX + 67, OffsetY + 89, 135, 12, 15);
		}

		public Search()
		{
			Palette = Common.Screens.Last().OriginalColours;

			_input = new Input(Palette, string.Empty, 0, 5, 11, OffsetX + 68, OffsetY + 90, 133, 10, 16);
			_input.Accept += Search_Accept;
			_input.Cancel += Search_Cancel;
			RenderDialog();
		}
	}
}