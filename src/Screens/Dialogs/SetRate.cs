// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Services;
using CivOne.UserInterface;

namespace CivOne.Screens.Dialogs
{
	internal class SetRate : BaseDialog
	{
		private const int FONT_ID = 0;

		private readonly bool _luxuries;
		private readonly string[] _menuItems;
		private Menu _menu;

		private void TaxesChoice(object sender, MenuItemEventArgs<int> args)
		{
			Human.TaxesRate = args.Value;
			Cancel();
		}

		private void LuxuriesChoice(object sender, MenuItemEventArgs<int> args)
		{
			Human.LuxuriesRate = args.Value;
			Cancel();
		}

		private string ScreenName
		{
			get
			{
				if (_luxuries)
					return Translate("Luxuries");
				return Translate("Tax");
			}
		}

		private int ItemWidth
		{
			get
			{
				return MenuOptions(_luxuries).Max(x => Resources.GetTextSize(0, x).Width) + 11;
			}
		}

		private MenuItemEventHandler<int> ChoiceMethod
		{
			get
			{
				if (_luxuries)
					return LuxuriesChoice;
				return TaxesChoice;
			}
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

			_menu = new Menu(Palette, Selection(3, 12, ItemWidth, (_menuItems.Length * Resources.GetFontHeight(FONT_ID)) + 4))
			{
				X = 103,
				Y = 92,
				CenterTo320Coordinates = true,
				MenuWidth = ItemWidth,
				ActiveColour = 11,
				TextColour = 5,
				FontId = FONT_ID
			};
			for (int i = 0; i < _menuItems.Length; i++)
			{
				_menu.Items.Add(_menuItems[i], i).OnSelect(ChoiceMethod);
			}

			_menu.MissClick += Cancel;
			_menu.Cancel += Cancel;

			if (_luxuries)
				_menu.ActiveItem = Human.LuxuriesRate;
			else
				_menu.ActiveItem = Human.TaxesRate;
			
			AddMenu(_menu);
		}

		private static IEnumerable<string> MenuOptions(bool luxuries)
		{
			var translation = TranslationServiceFactory.GetCurrent();

			if (luxuries)
			{
				for (int i = 0; i <= (10 - Human.TaxesRate); i++)
				{
					int science = 10 - Human.TaxesRate - i;
					yield return translation.TranslateFormatted("{0}% Luxuries, ({1}% Science)", i * 10, science * 10);
				}
				yield break;
			}

			for (int i = 0; i <= (10 - Human.LuxuriesRate); i++)
			{
				int science = 10 - Human.LuxuriesRate - i;
				yield return translation.TranslateFormatted("{0}% Tax, ({1}% Science)", i * 10, science * 10);
			}
		}

		private static int DialogWidth(bool luxuries)
		{
			return MenuOptions(luxuries).Max(x => Resources.GetTextSize(0, x).Width) + 15;
		}

		private static int DialogHeight(bool luxuries)
		{
			return (MenuOptions(luxuries).Count() * Resources.GetFontHeight(FONT_ID)) + 15;
		}

		public static SetRate Taxes
		{
			get
			{
				return new SetRate(luxuries: false);
			}
		}

		public static SetRate Luxuries
		{
			get
			{
				return new SetRate(luxuries: true);
			}
		}

		private SetRate(bool luxuries) : base(100, 80, DialogWidth(luxuries), DialogHeight(luxuries))
		{
			_luxuries = luxuries;
			_menuItems = MenuOptions(luxuries).ToArray();
			
			DialogBox.DrawText(TranslateFormatted("Select new {0} rate...", ScreenName), 0, 15, 5, 5);
		}
	}
}