// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Buildings;
using CivOne.Graphics;
using CivOne.Services;

namespace CivOne.Screens.Dialogs
{
	internal class ConfirmSell : BaseDialog
	{
		public IBuilding Building { get; private set; }

		public event EventHandler Sell;

		private Menu _menu;

		private void MenuYes(object sender, EventArgs args)
		{
			if (Sell != null)
				Sell(this, args);
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

			_menu = new Menu(Palette, Selection(3, 20, TextWidth + 5, 20))
			{
				X = 131,
				Y = 100,
				CenterTo320Coordinates = true,
				MenuWidth = TextWidth + 5,
				ActiveColour = 11,
				TextColour = 5,
				FontId = 0
			};
			int i = 0;
			string[] choices = [Translate("No."), Translate("Yes.")];
			foreach (string choice in choices)
			{
				_menu.Items.Add(choice, i++);
			}
			_menu.Items[0].Selected += Cancel;
			_menu.Items[1].Selected += MenuYes;

			_menu.MissClick += Cancel;
			_menu.Cancel += Cancel;
			AddMenu(_menu);
		}

		private static string[] MessageLines(IBuilding building)
		{
			return TranslationServiceFactory.GetCurrent()
				.TranslateFormattedArray("Do you want to sell\nyour {0} for {1}$?", building.TranslatedName, building.SellPrice);
		}

		public ConfirmSell(IBuilding building) : base(128, 80, 9, 23, MessageLines(building))
		{
			Building = building;
			
			for (int i = 0; i < TextLines.Length; i++)
			{
				DialogBox.AddLayer(TextLines[i], 5, (TextLines[i].Height * i) + 5);
			}
		}
	}
}