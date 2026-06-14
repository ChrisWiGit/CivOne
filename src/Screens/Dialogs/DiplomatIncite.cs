// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Buildings;
using CivOne.Graphics;
using CivOne.Services;
using CivOne.Services.Random;
using CivOne.Tasks;
using CivOne.Units;
using CivOne.UserInterface;

namespace CivOne.Screens.Dialogs
{
	internal class DiplomatIncite : BaseDialog
	{
		private const int FONT_ID = 0;

		private readonly City _cityToIncite;
		private readonly Diplomat _diplomat;
		private readonly IDiplomatInciteService _service;

		private readonly bool _canIncite;
		private Menu? _menu;

		private void DontIncite(object? _, EventArgs __)
		{
			Cancel();
		}

		private void Incite(object? _, EventArgs __)
		{
			_service.InciteRevolt(_cityToIncite, _diplomat);
			Cancel();
		}

		protected override void FirstUpdate()
		{
			CreateMenu();
			base.FirstUpdate();
		}

		private void CreateMenu()
		{
			if (_menu is not null || !_canIncite)
			{
				return;
			}

			int choices = 2;
			_menu = new Menu(Palette, Selection(45, 5 + (3 * Resources.GetFontHeight(FONT_ID)), 130, (2 * Resources.GetFontHeight(FONT_ID)) + (choices * Resources.GetFontHeight(FONT_ID)) + 9))
			{
				X = 143,
				Y = 110,
				CenterTo320Coordinates = true,
				MenuWidth = 130,
				ActiveColour = 11,
				TextColour = 5,
				FontId = FONT_ID
			};

			_menu.Items.Add(Translate("Forget It.")).OnSelect(DontIncite);
			_menu.Items.Add(Translate("Incite revolt")).OnSelect(Incite);
			AddMenu(_menu);
		}

		internal DiplomatIncite(City cityToIncite, Diplomat diplomat, IDiplomatInciteService service) : base(100, 80, 180, 56)
		{
			_cityToIncite = cityToIncite ?? throw new ArgumentNullException(nameof(cityToIncite));
			_diplomat = diplomat ?? throw new ArgumentNullException(nameof(diplomat));
			_service = service ?? throw new ArgumentNullException(nameof(service));

			IBitmap spyPortrait = Icons.Spy;
			using Palette palette = Common.DefaultPalette.Merge(spyPortrait.Palette, 144);
			Palette = palette;

			DialogBox.AddLayer(spyPortrait, 2, 2);

			int inciteCost = Diplomat.InciteCost(cityToIncite);
			_canIncite = Diplomat.CanIncite(cityToIncite, diplomat.Player.Gold);

			DialogBox.DrawText(Translate("Spies Report"), 0, 15, 45, 5);
			DialogBox.DrawText(TranslateFormatted("Dissidents in {0}", _cityToIncite.Name), 0, 15, 45, 5 + Resources.GetFontHeight(FONT_ID));
			DialogBox.DrawText(TranslateFormatted("will revolt for ${0}", inciteCost), 0, 15, 45, 5 + (2 * Resources.GetFontHeight(FONT_ID)));
		}
	}

	internal static class DiplomatInciteDialogFactory
	{
		public static IDiplomatInciteService CreateService()
		{
			return new DiplomatInciteService(TranslationServiceFactory.GetCurrent());
		}

		public static IScreen CreateDialog(City cityToIncite, Diplomat diplomat)
		{
			return new DiplomatIncite(cityToIncite, diplomat, CreateService());
		}

		public static IScreen CreateDialog(City cityToIncite, Diplomat diplomat, IDiplomatInciteService service)
		{
			return new DiplomatIncite(cityToIncite, diplomat, service);
		}
	}

	internal interface IDiplomatInciteService
	{
		void InciteRevolt(City cityToIncite, Diplomat diplomat);
	}

	internal class DiplomatInciteService(ITranslationService translationService) : IDiplomatInciteService
	{
		private static Player Human => Game.Instance.HumanPlayer;
		private readonly ITranslationService _t = translationService ?? throw new ArgumentNullException(nameof(translationService));

		public void InciteRevolt(City cityToIncite, Diplomat diplomat)
		{
			Player previousOwner = cityToIncite.CityOwnerPlayer;
			byte newOwner = diplomat.Owner;
			Player newOwnerPlayer = diplomat.Player;

			var msg = Message.General(_t.TranslateFormattedArray("{0} rebel!\nCivil War in\n{1}.\n{2} influence\nsuspected.", 
						previousOwner.TribeNamePlural, cityToIncite.Name, newOwnerPlayer.TribeName));

			int plundered = 0;
			string[] lines = _t.TranslateFormattedArray("{0} capture\n{1}. {2} gold\npieces plundered.", newOwnerPlayer.TribeNamePlural, cityToIncite.Name, plundered);

			Show captureCity = Show.CaptureCity(cityToIncite, lines);
			void capture_done(object? _, EventArgs __)
			{
				Game.Instance.DisbandUnit(diplomat);
				cityToIncite.CityOwnerPlayerIndex = newOwner;
				cityToIncite.TechStolen = false;

				foreach (var unit in cityToIncite.Units)
				{
					unit.Owner = newOwner;
				}

				var random = RandomServiceFactory.Create();
				foreach (IBuilding building in cityToIncite.Buildings.Where(b => random.Hit(50)).ToList())
				{
					cityToIncite.RemoveBuilding(building);
				}

				newOwnerPlayer.Gold -= (short)Diplomat.InciteCost(cityToIncite);
				newOwnerPlayer.Gold += (short)plundered;
				previousOwner.HandleExtinction();
				// Fix #181 When inciting an enemy's last city, the messages are in the wrong order #181
				GameTask.Insert(msg);

				if (Human == cityToIncite.CityOwnerPlayerIndex || Human == newOwner)
				{
					GameTask.Insert(Tasks.Show.CityManager(cityToIncite));
				}
			}
			captureCity.Done += capture_done;

			if (Human == cityToIncite.CityOwnerPlayerIndex || Human == diplomat.Owner)
			{
				GameTask.Insert(captureCity);
			}
			else
			{
				capture_done(null, EventArgs.Empty);
			}
		}
	}
}