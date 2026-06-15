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
using CivOne.Tasks;

namespace CivOne.Screens.Dialogs
{
	internal class Revolution : BaseDialog
	{
		private Menu? _menu;

		private void MenuRevolution(object sender, EventArgs args)
		{
			Human.Revolt();
			GameTask.Enqueue(Message.Newspaper(null,
				TranslateFormattedArray("The {0} are\nrevolting! Citizens\ndemand new govt.", Human.Civilization.NamePlural)));
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

			_menu = new Menu(Palette, Selection(3, 12, 228, 16))
			{
				X = 67,
				Y = 92,
				CenterTo320Coordinates = true,
				MenuWidth = 227,
				ActiveColour = 11,
				TextColour = 5,
				FontId = 0,
				Indent = 2
			};
			int i = 0;
			string[] choices = [Translate("_No thanks."), Translate("_Yes, we need a new government.")];
			foreach (string choice in choices)
			{
				_menu.Items.Add(choice, i++);
			}
			_menu.Items[0].Selected += Cancel;
			_menu.Items[1].Selected += MenuRevolution;

			_menu.MissClick += Cancel;
			_menu.Cancel += Cancel;
			AddMenu(_menu);
		}

		public Revolution() : base(64, 80, 231, 31)
		{
			DialogBox.DrawText(Translate("Are you sure you want a REVOLUTION?"), 0, 15, 5, 5);
		}
	}
}