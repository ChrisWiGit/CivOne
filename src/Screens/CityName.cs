// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class CityName : BaseScreen
	{
		private readonly Input _input;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private void DrawDialog()
		{
			int ox = OffsetX;
			int oy = OffsetY;

			this.Clear();
			this.FillRectangle(80 + ox, 80 + oy, 161, 33, 11)
				.FillRectangle(81 + ox, 81 + oy, 159, 31, 15)
				.DrawText("City Name...", 0, 5, 88 + ox, 82 + oy)
				.FillRectangle(88 + ox, 95 + oy, 105, 14, 5)
				.FillRectangle(89 + ox, 96 + oy, 103, 12, 15);

			_input.X = 90 + ox;
			_input.Y = 97 + oy;
		}

		public int NameId { get; private set; }

		public string Value { get; private set; }

		public event EventHandler Accept, Cancel;

		private void CityName_Accept(object sender, EventArgs args)
		{
			Value = (sender as Input).Text;
			if (Accept != null)
				Accept(this, null);
			if (sender is Input)
				((Input)sender)?.Close();
			Destroy();
		}

		private void CityName_Cancel(object sender, EventArgs args)
		{
			if (Cancel != null)
				Cancel(this, null);
			if (sender is Input)
				((Input)sender)?.Close();
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				DrawDialog();
			}

			if (!Common.HasScreenType<Input>())
			{
				Common.AddScreen(_input);
			}
			return false;
		}

		public CityName(int nameId, string cityName)
		{
			NameId = nameId;
			Palette = Common.DefaultPalette;

			_input = new Input(Palette, cityName, 0, 5, 11, 90 + OffsetX, 97 + OffsetY, 101, 10, 12);
			_input.Accept += CityName_Accept;
			_input.Cancel += CityName_Cancel;

			DrawDialog();
		}
	}
}