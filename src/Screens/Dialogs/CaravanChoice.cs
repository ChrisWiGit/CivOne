// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Graphics;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.UserInterface;
using CivOne.Wonders;

namespace CivOne.Screens.Dialogs
{
	internal class CaravanChoice : BaseDialog
	{
		private const int FONT_ID = 0;

		private readonly Caravan _unit;
		private readonly City _city;
		private Menu _menu;

		private void KeepMoving(object sender, EventArgs args)
		{
			_unit.KeepMoving(_city);
			Cancel();
		}

		private void EstablishTradeRoute(object sender, EventArgs args)
		{
			_unit.EstablishTradeRoute(_city);
			Cancel();
		}

		private void HelpBuildWonder(object sender, EventArgs args)
		{
			_unit.HelpBuildWonder(_city);
			Cancel();
		}

        public static bool AllowEstablishTradeRoute(Caravan _unit, City _city)
        {
            return (_unit.Home == null) || (_unit.Home.Tile.DistanceTo(_city) >= 10);
        }

        protected override void FirstUpdate()
		{
			CreateMenu();
			base.FirstUpdate();
		}

		private void CreateMenu()
		{
			if (_menu is not null)
			{
				return;
			}

			int choices = _city.IsBuildingWonder ? 3 : 2;

			_menu = new Menu(Palette, Selection(3, 12, 130, (choices * Resources.GetFontHeight(FONT_ID)) + 4))
			{
				X = 103,
				Y = 92,
				CenterTo320Coordinates = true,
				MenuWidth = 130,
				ActiveColour = 11,
				TextColour = 5,
				FontId = FONT_ID
			};

			_menu.Items.Add("Keep moving").OnSelect(KeepMoving);
			_menu.Items.Add("Establish trade route")
				.SetEnabled(AllowEstablishTradeRoute(_unit, _city))
				.OnSelect(EstablishTradeRoute);

			if (_city.IsBuildingWonder)
			{
				_menu.Items.Add("Help build WONDER.").OnSelect(HelpBuildWonder);
			}

			AddMenu(_menu);
		}

		private static int DialogHeight(City city)
		{
			int choices = 2;
			if (city.CurrentProduction is IWonder) choices++;
			return (choices * Resources.GetFontHeight(FONT_ID)) + 17;
		}

		internal CaravanChoice(Caravan unit, City city) : base(100, 80, 136, DialogHeight(unit, city))
		{
			_city = city;
			_unit = unit;

			DialogBox.DrawText($"Will you?", 0, 15, 5, 5);
		}
	}
}