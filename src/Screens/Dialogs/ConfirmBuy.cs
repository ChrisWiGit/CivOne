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
using CivOne.Services;

namespace CivOne.Screens.Dialogs
{
	internal class ConfirmBuy : BaseDialog
	{
		public event EventHandler Buy;

		private Menu _menu;

		private static string[] MessageLines(string name, short price, short treasury)
		{
			return TranslationServiceFactory.GetCurrent()
				.TranslateFormattedArray("Cost to complete\n{0}: ${1}\nTreasury: ${2}", name, price, treasury);
		}

		public ConfirmBuy(string name, short price, short treasury)
			: base(100, 80, 9, 23, MessageLines(name, price, treasury))
		{
			for (int i = 0; i < TextLines.Length; i++)
			{
				DialogBox.AddLayer(TextLines[i], 5, (TextLines[i].Height * i) + 5);
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
				return;

			_menu = new Menu(Palette, Selection(3, 28, TextWidth + 5, 20))
			{
				X = 103,
				Y = 108,
				CenterTo320Coordinates = true,
				MenuWidth = TextWidth + 5,
				ActiveColour = 11,
				TextColour = 5,
				FontId = 0
			};

			_menu.Items.Add(Translate("Yes"), 0);
			_menu.Items.Add(Translate("No"), 1);

			_menu.Items[0].Selected += Confirm;
			_menu.Items[1].Selected += Cancel;

			_menu.MissClick += Cancel;
			_menu.Cancel += Cancel;

			AddMenu(_menu);
		}

		private void Confirm(object sender, EventArgs args)
		{
			Buy?.Invoke(this, args);
			Cancel();
		}
	}
}