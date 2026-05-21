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
using CivOne.Civilizations;
using CivOne.src;

namespace CivOne.Screens.Dialogs
{
	internal class DiplomatBribe : BaseDialog
	{
		private const int FONT_ID = 0;

		private readonly IDiplomatBribeService _service;

		private readonly bool _canBribe;
		private Menu _menu;

		private void DontBribe(object sender, EventArgs args)
		{
			Cancel();
		}

		private void Bribe(object sender, EventArgs args)
		{
			_service.BribeUnit();
			Cancel();
		}

		protected override void FirstUpdate()
		{
			CreateMenu();
			base.FirstUpdate();
		}

		private void CreateMenu()
		{
			if (_menu is not null || !_canBribe)
			{
				return;
			}

			int choices = 2;
			_menu = new Menu(Palette, Selection(3, 5 + (3 * Resources.GetFontHeight(FONT_ID)), 130, ((2 * Resources.GetFontHeight(FONT_ID)) + (choices * Resources.GetFontHeight(FONT_ID)) + 9)))
			{
				X = 103,
				Y = 110,
				CenterTo320Coordinates = true,
				MenuWidth = 130,
				ActiveColour = 11,
				TextColour = 5,
				FontId = FONT_ID
			};

			_menu.Items.Add(Translate("Forget It.")).OnSelect(DontBribe);
			_menu.Items.Add(Translate("Pay")).OnSelect(Bribe);
			AddMenu(_menu);
		}

		private static int DialogHeight(bool canBribe)
		{
			int choices = canBribe ? 2 : 0;

			return (choices * Resources.GetFontHeight(FONT_ID)) + 31;
		}

		internal DiplomatBribe(IDiplomatBribeService service, bool canBribe) : base(100, 80, 135, DialogHeight(canBribe))
		{
			_service = service ?? throw new ArgumentNullException(nameof(service));

			var _bribeCost = _service.CalculateBribeCost();
			_canBribe = _service.CanBribe();

			DialogBox.DrawText(TranslateFormatted("{0} {1}", _service.TribeName, _service.UnitName), 0, 15, 5, 5);
			DialogBox.DrawText(TranslateFormatted("will desert for ${0}", _bribeCost), 0, 15, 5, 5 + Resources.GetFontHeight(FONT_ID));
			DialogBox.DrawText(TranslateFormatted("Treasury ${0}", _service.Gold), 0, 15, 5, 5 + (2 * Resources.GetFontHeight(FONT_ID)));
		}
	}

	internal interface IDiplomatBribeService
	{
		void BribeUnit();
		int CalculateBribeCost();

		bool CanBribe();

		string UnitName { get; }
		string TribeName { get; }

		int Gold { get; }
	}

	internal static class DiplomatBribeDialogFactory
	{
		public static IDiplomatBribeService CreateService(BaseUnitLand unitToBribe, Diplomat diplomat)
		{
			return new DiplomatBribeService(unitToBribe, diplomat);
		}
		public static IScreen CreateDialog(BaseUnitLand unitToBribe, Diplomat diplomat)
		{
			IDiplomatBribeService service = CreateService(unitToBribe, diplomat);
			return new DiplomatBribe(service, service.CanBribe());
		}
	}

	internal class DiplomatBribeService(BaseUnitLand _unitToBribe, Diplomat _diplomat) : IDiplomatBribeService
	{
		public string UnitName => _unitToBribe.Name;

		public string TribeName => _unitToBribe.Player.TribeName;

		public int Gold => _diplomat.Player.Gold;

		public int CalculateBribeCost()
		{
			City capital = _unitToBribe.Player.GetCapital();
			int distance = capital == null ? 16 : _unitToBribe.Tile.DistanceTo(capital);
			int cost = (_unitToBribe.Player.Gold + 750) / (distance + 2) * _unitToBribe.Price;
			return (_unitToBribe.Player.Civilization is Barbarian) ? cost / 2 : cost;
		}

		public bool CanBribe()
		{
			return _diplomat.Player.Gold >= CalculateBribeCost();
		}

		public void BribeUnit()
		{
			City capital = _unitToBribe.Player.GetCapital();
			int distance = capital == null ? 16 : _unitToBribe.Tile.DistanceTo(capital);
			int cost = (_unitToBribe.Player.Gold + 750) / (distance + 2) * _unitToBribe.Price;
			int bribeCost = (_unitToBribe.Player.Civilization is Barbarian) ? cost / 2 : cost;

			IUnit newUnit = Game.Instance.CreateUnit(_unitToBribe.Type, _unitToBribe.X, _unitToBribe.Y, _diplomat.Owner);
			Game.Instance.DisbandUnit(_unitToBribe);
			_diplomat.KeepMoving(newUnit);
			_diplomat.Player.Gold -= (short)bribeCost;
			newUnit.SkipTurn();
		}

		
	}
}