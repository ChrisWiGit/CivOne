// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Services;
using CivOne.Units;
using CivOne.UserInterface;

namespace CivOne.Screens.Dialogs
{
	internal class DisbandUnit : BaseDialog
	{
		private readonly Picture[] _textLines;
		private Menu _menu;

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

			int menuWidth = _textLines.Max(b => b.Width) + 5;
			_menu = new Menu(Palette, Selection(45, 28, menuWidth, 10))
			{
				X = 103,
				Y = 100,
				CenterTo320Coordinates = true,
				MenuWidth = menuWidth,
				ActiveColour = 11,
				TextColour = 5,
				FontId = 0
			};
			_menu.Items.Add(Translate("Unit Disbanded.")).OnSelect(Cancel);
			_menu.MissClick += Cancel;
			_menu.Cancel += Cancel;
			AddMenu(_menu);
		}

		private static Picture[] TextPictures(City city, IUnit unit)
		{
			string[] message = TranslationServiceFactory.GetCurrent().TranslateFormattedArray("{0} can't support\n{1}.", city.Name, unit.TranslatedName);
			Picture[] output = new Picture[message.Length];
			for (int i = 0; i < message.Length; i++)
				output[i] = Resources.GetText(message[i], 0, 15);
			return output;
		}

		public DisbandUnit(City city, IUnit unit) : base(58, 72, TextPictures(city, unit).Max(b => b.Width) + 52, 62)
		{
			bool modernGovernment = Human.HasAdvance<Invention>();
			IBitmap governmentPortrait = Icons.GovernmentPortrait(Human.Government, Advisor.Defense, modernGovernment);
			
			using Palette palette = Common.DefaultPalette.Merge(governmentPortrait.Palette, 144);
			Palette = palette;

			DialogBox.AddLayer(governmentPortrait, 2, 2);
			string advisorLabel = Translate("Defense Minister:");
			DialogBox.DrawText(advisorLabel, 0, 15, 47, 4);
			DialogBox.FillRectangle(47, 11, Resources.GetText(advisorLabel, 0, 15).Width + 1, 1, 11);

			_textLines = TextPictures(city, unit);
			for (int i = 0; i < _textLines.Length; i++)
			{
				DialogBox.AddLayer(_textLines[i], 47, (_textLines[i].Height * i) + 13);
			}
		}
	}
}