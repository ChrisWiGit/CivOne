// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens.Reports
{
	[ScreenResizeable]
	internal class CityStatus : BaseReport
	{
		private const char FOOD = '{';
		private const char SHIELD = '|';
		private const char TRADE = '}';
		private const byte FONT_ID = 0;

		private readonly City[] _cities;

		private bool _update = true;
		private int _page = 0;

		private void Render()
		{
			this.Clear(8);
			DrawReportHeader();

			const int MAGIC = 157;
			int fontHeight = Resources.GetFontHeight(FONT_ID);
			int yy = OffsetY + 32;
			int start = _page * 20;
			int end = System.Math.Min(_cities.Length, start + 20);
			for (int i = start; i < end; i++)
			{
				City city = _cities[i];

				string production = (city.CurrentProduction as ICivilopedia).TranslatedName;
				// fire-eggs 20190721 in microprose, longer wonder names are abbreviated
				if (production.Length > 16)
					production = production.Substring(1, 16) + ".";

				int productionWidth = Resources.GetTextSize(1, production).Width;

				this.DrawText(city.Name, FONT_ID, 15, OffsetX + 8, yy)
					.DrawText(TranslateFormatted("{0}-{1}{2} {3}{4} {5}{6}", city.Size, city.FoodTotal, FOOD, city.ShieldTotal, SHIELD, city.TradeTotal, TRADE), FONT_ID, 15, OffsetX + 80, yy)
					.DrawText(production, FONT_ID, 15, OffsetX + MAGIC, yy)
					.DrawText(TranslateFormatted("({0}/{1})", city.Shields, city.CurrentProduction.Price * 10), FONT_ID, 7, OffsetX + MAGIC + productionWidth + 12, yy);
				yy += fontHeight;
			}
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;

			Render();

			_update = false;
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}

		private bool NextPage()
		{
			if (((_page + 1) * 20) < _cities.Length)
			{
				_page++;
				_update = true;
			}
			else
			{
				Destroy();
			}
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			return NextPage();
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			return NextPage();
		}

		public override string Title() => Translate("CITY STATUS");

		public CityStatus() : base(8)
		{
			_cities = Game.GetCities().Where(c => Human == c.CityOwnerPlayerIndex && c.Size > 0).ToArray();
			Render();
		}
	}
}