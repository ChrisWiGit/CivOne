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
using CivOne.Services.EndGame;

namespace CivOne.Screens.Dialogs
{
	internal class ConfirmRetire : BaseDialog
	{
		private static void MenuRetire(object sender, EventArgs args)
		{
			_ = EndGameServiceFactory.CreateDefault().HandleRetireAsync();
		}

		protected override void FirstUpdate()
		{
			Menu menu = new(Palette, Selection(3, 12, 228, 16))
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

			int index = 0;
			foreach (string choice in new[] { Translate("_Keep playing."), Translate("_Yes, retire.") })
			{
				menu.Items.Add(choice, index++);
			}

			menu.Items[0].Selected += Cancel;
			menu.Items[1].Selected += MenuRetire;
			menu.MissClick += Cancel;
			menu.Cancel += Cancel;
			AddMenu(menu);
		}

		public ConfirmRetire() : base(64, 80, 231, 31)
		{
			DialogBox.DrawText(Translate("Are you sure you want to RETIRE?"), 0, 15, 5, 5);
		}
	}
}