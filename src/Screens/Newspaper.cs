// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Events;
using CivOne.IO;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.src;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class Newspaper : BaseScreen
	{
		private const int PaperWidth = 320;
		private const int PaperHeight = 200;

		private bool _update = true;
		private readonly string[] _message;
		private readonly bool _showGovernment;
		private readonly bool _modernGovernment;
		private readonly string _newsflash;
		private readonly string _shout;
		private readonly string _date;
		private readonly string _name;

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			RenderNewspaper();
			_update = true;
		}

		private void GetPaperLayout(out int paperX, out int paperY)
		{
			paperX = (Width > PaperWidth) ? ((Width - PaperWidth) / 2) : 0;
			paperY = (Height > PaperHeight) ? ((Height - PaperHeight) / 2) : 0;
		}

		private IBitmap[] PrepareGovernmentPortraits(Palette palette)
		{
			IBitmap[] governmentPortraits = new IBitmap[4];
			if (!_showGovernment)
			{
				return governmentPortraits;
			}

			for (int i = 0; i < 4; i++)
			{
				governmentPortraits[i] = Icons.GovernmentPortrait(Human.Government, (Advisor)Enum.Parse(typeof(Advisor), i.ToString()), _modernGovernment);
			}

			for (int i = 144; i < 256; i++)
			{
				palette[i] = governmentPortraits[0].Palette[i];
			}

			return governmentPortraits;
		}

		private void DrawHeader(int paperX, int paperY)
		{
			this.FillRectangle(paperX, paperY, PaperWidth, 100, 15)
				.DrawText(_shout, 2, 5, paperX + 6, paperY + 3)
				.DrawText(_shout, 2, 5, paperX + 272, paperY + 3)
				.DrawText(_newsflash, 1, 5, paperX + 158, paperY + 3, TextAlign.Center)
				.DrawText(_newsflash, 1, 5, paperX + 158, paperY + 3, TextAlign.Center)
				.DrawText(",-.", 4, 5, paperX + 8, paperY + 11)
				.DrawText(",-.", 4, 5, paperX + 268, paperY + 11)
				.DrawText(_name, 4, 5, paperX + 160, paperY + 11, TextAlign.Center)
				.DrawText(_date, 0, 5, paperX + 8, paperY + 28)
				.DrawText("10 cents", 0, 5, paperX + 272, paperY + 28)
				.FillRectangle(paperX + 1, paperY + 1, 318, 1, 5)
				.FillRectangle(paperX + 1, paperY + 2, 1, 33, 5)
				.FillRectangle(paperX + 318, paperY + 2, 1, 33, 5)
				.FillRectangle(paperX, paperY + 35, PaperWidth, 1, 5)
				.FillRectangle(paperX, paperY + 97, PaperWidth, 1, 5);
		}

		private void DrawMessageLines(int paperX, int paperY)
		{
			for (int i = 0; i < _message.Length; i++)
			{
				this.DrawText(_message[i], 3, 5, paperX + 16, paperY + 40 + (i * 17));
			}
		}

		private void DrawGovernmentSection(int paperX, int paperY, IBitmap[] governmentPortraits)
		{
			string[] advisorNames = new string[] { "Defense Minister", "Domestic Advisor", "Foreign Minister", "Science Advisor" };
			this.FillRectangle(paperX, paperY + 100, PaperWidth, 100, 15)
				.DrawText("New Cabinet:", 5, 5, paperX + 106, paperY + 102);
			for (int i = 0; i < 4; i++)
			{
				this.AddLayer(governmentPortraits[i], paperX + 20 + (80 * i), paperY + 118)
					.DrawText(advisorNames[i], 1, 5, paperX + 40 + (80 * i), paperY + ((i % 2) == 0 ? 180 : 186), TextAlign.Center);
			}
		}

		private void DrawStandardSection(int paperX, int paperY)
		{
			for (int xx = paperX; xx < (paperX + PaperWidth); xx += Icons.Newspaper.Width())
			{
				this.AddLayer(Icons.Newspaper, xx, paperY + 100);
			}
			using (IBitmap dialog = new Picture(151, 15)
					.Tile(Pattern.PanelGrey)
					.DrawRectangle3D()
					.DrawText("Press any key to continue.", 0, 15, 4, 4))
			{
				this.FillRectangle(paperX + 80, paperY + 128, 153, 17, 5)
					.AddLayer(dialog, paperX + 81, paperY + 129);
			}
		}

		private void RenderNewspaper()
		{
			GetPaperLayout(out int paperX, out int paperY);

			Palette palette = Common.DefaultPalette;
			IBitmap[] governmentPortraits = PrepareGovernmentPortraits(palette);

			Palette = palette;
			DrawHeader(paperX, paperY);
			DrawMessageLines(paperX, paperY);

			if (_showGovernment)
			{
				DrawGovernmentSection(paperX, paperY, governmentPortraits);
			}
			else
			{
				DrawStandardSection(paperX, paperY);
			}
		}

		public void Close()
		{
			Destroy();
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_update)
			{
				_update = false;
			}
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			Close();
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			Close();
			return true;
		}

		//public Newspaper(bool showGovernment, City city = null, params string[] message)
		public Newspaper(City city, string[] message, bool showGovernment = false)
		{
			_message = message;
			_showGovernment = showGovernment;
			_modernGovernment = Human.HasAdvance<Invention>();

			_newsflash = TextFile.Instance.GetGameText($"KING/NEWS{(char)Common.Random.Next((int)'A', (int)'O')}")[0];
			_shout = (Common.Random.Next(0, 2) == 0) ? "FLASH" : "EXTRA!";
			_date = $"January 1, {Common.YearString(Game.GameTurn)}";
			_name = city == null ? Human.GetCapitalName() : city.Name;

			switch (Common.Random.Next(0, 3))
			{
				case 0: _name = $"The {_name} Times"; break;
				case 1: _name = $"The {_name} Tribune"; break;
				case 2: _name = $"{_name} Weekly"; break;
			}

			RenderNewspaper();
		}
	}
}