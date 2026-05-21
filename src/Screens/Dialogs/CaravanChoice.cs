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
		private readonly ICaravanChoiceService _service;
		private Menu _menu;

		private void KeepMoving(object sender, EventArgs args)
		{
			_service.KeepMoving(_unit, _city);
			Cancel();
		}

		private void EstablishTradeRoute(object sender, EventArgs args)
		{
			_service.EstablishTradeRoute(_unit, _city);
			Cancel();
		}

		private void HelpBuildWonder(object sender, EventArgs args)
		{
			_service.HelpBuildWonder(_unit, _city);
			Cancel();
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

			_menu.Items.Add(Translate("Keep moving")).OnSelect(KeepMoving);
			_menu.Items.Add(Translate("Establish trade route"))
				.SetEnabled(_service.CanEstablishTradeRoute(_unit, _city))
				.OnSelect(EstablishTradeRoute);

			if (_city.IsBuildingWonder)
			{
				_menu.Items.Add(Translate("Help build WONDER.")).OnSelect(HelpBuildWonder);
			}

			AddMenu(_menu);
		}

		private static int DialogHeight(City city)
		{
			int choices = 2;
			if (city.CurrentProduction is IWonder) choices++;
			return (choices * Resources.GetFontHeight(FONT_ID)) + 17;
		}

		internal CaravanChoice(Caravan unit, City city, ICaravanChoiceService service) : base(100, 80, 136, DialogHeight(city))
		{
			_city = city;
			_unit = unit;
			_service = service ?? throw new ArgumentNullException(nameof(service));

			DialogBox.DrawText(Translate("Will you?"), 0, 15, 5, 5);
		}
	}

	internal static class CaravanChoiceDialogFactory
	{
		public static ICaravanChoiceService CreateService()
		{
			return new CaravanChoiceService();
		}

		public static IScreen CreateDialog(Caravan unit, City city)
		{
			return new CaravanChoice(unit, city, CreateService());
		}

		public static IScreen CreateDialog(Caravan unit, City city, ICaravanChoiceService service)
		{
			return new CaravanChoice(unit, city, service);
		}
	}

	internal interface ICaravanChoiceService
	{
		void KeepMoving(Caravan unit, City city);
		void EstablishTradeRoute(Caravan unit, City city);
		void HelpBuildWonder(Caravan unit, City city);
		bool CanEstablishTradeRoute(Caravan unit, City city);
	}

	internal class CaravanChoiceService : ICaravanChoiceService
	{
		public void KeepMoving(Caravan unit, City city)
		{
			unit.KeepMoving(city);
		}

		public void EstablishTradeRoute(Caravan unit, City city)
		{
			unit.EstablishTradeRoute(city);
		}

		public void HelpBuildWonder(Caravan unit, City city)
		{
			unit.HelpBuildWonder(city);
		}

		public bool CanEstablishTradeRoute(Caravan unit, City city)
		{
			return (unit.Home == null) || (unit.Home.Tile.DistanceTo(city) >= 10);
		}
	}
}