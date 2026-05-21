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
		private Menu _menu;

		private void DontIncite(object sender, EventArgs args)
		{
			Cancel();
		}

		private void Incite(object sender, EventArgs args)
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
			_menu = new Menu(Palette, Selection(45, 5 + (3 * Resources.GetFontHeight(FONT_ID)), 130, ((2 * Resources.GetFontHeight(FONT_ID)) + (choices * Resources.GetFontHeight(FONT_ID)) + 9)))
			{
				X = 143,
				Y = 110,
				CenterTo320Coordinates = true,
				MenuWidth = 130,
				ActiveColour = 11,
				TextColour = 5,
				FontId = FONT_ID
			};

			_menu.Items.Add("Forget It.").OnSelect(DontIncite);
			_menu.Items.Add("Incite revolt").OnSelect(Incite);
			AddMenu(_menu);
		}

		internal DiplomatIncite(City cityToIncite, Diplomat diplomat, IDiplomatInciteService service = null) : base(100, 80, 180, 56)
		{
			_cityToIncite = cityToIncite ?? throw new ArgumentNullException(nameof(cityToIncite));
			_diplomat = diplomat ?? throw new ArgumentNullException(nameof(diplomat));
			_service = service ?? new DiplomatInciteService();

			IBitmap spyPortrait = Icons.Spy;
			using Palette palette = Common.DefaultPalette.Merge(spyPortrait.Palette, 144);
			Palette = palette;

			DialogBox.AddLayer(spyPortrait, 2, 2);

			var _inciteCost = Diplomat.InciteCost(cityToIncite);
			_canIncite = Diplomat.CanIncite(cityToIncite, diplomat.Player.Gold);

			DialogBox.DrawText("Spies Report", 0, 15, 45, 5);
			DialogBox.DrawText($"Dissidents in {_cityToIncite.Name}", 0, 15, 45, 5 + Resources.GetFontHeight(FONT_ID));
			DialogBox.DrawText($"will revolt for ${_inciteCost}", 0, 15, 45, 5 + (2 * Resources.GetFontHeight(FONT_ID)));
		}
	}

	internal interface IDiplomatInciteService
	{
		void InciteRevolt(City cityToIncite, Diplomat diplomat);
	}

	internal class DiplomatInciteService : IDiplomatInciteService
	{
		private static Player Human => Game.Instance.HumanPlayer;

		public void InciteRevolt(City cityToIncite, Diplomat diplomat)
		{
			Player previousOwner = Game.Instance.GetPlayer(cityToIncite.Owner);
			var newOwner = diplomat.Owner;
			var newPlayer = Game.Instance.GetPlayer(newOwner);

			var msg = Message.General($"{previousOwner.TribeNamePlural} rebel!",
				"Civil War in",
				$"{cityToIncite.Name}.",
				$"{newPlayer.TribeName} influence",
				"suspected.");
			GameTask.Insert(msg);

			int plundered = 0;
			string[] lines = [$"{newPlayer.TribeNamePlural} capture", $"{cityToIncite.Name}. {plundered} gold", "pieces plundered."];

			Show captureCity = Show.CaptureCity(cityToIncite, lines);
			EventHandler capture_done = (s1, a1) =>
			{
				Game.Instance.DisbandUnit(diplomat);
				cityToIncite.Owner = newOwner;
				cityToIncite.TechStolen = false;

				foreach (var unit in cityToIncite.Units)
				{
					unit.Owner = newOwner;
				}

				foreach (IBuilding building in cityToIncite.Buildings.Where(b => Common.Random.Next(0, 1) == 1).ToList())
				{
					cityToIncite.RemoveBuilding(building);
				}

				newPlayer.Gold -= (short)Diplomat.InciteCost(cityToIncite);
				newPlayer.Gold += (short)plundered;
				previousOwner.HandleExtinction();

				if (Human == cityToIncite.Owner || Human == newOwner)
				{
					GameTask.Insert(Tasks.Show.CityManager(cityToIncite));
				}
			};
			captureCity.Done += capture_done;

			if (Human == cityToIncite.Owner || Human == diplomat.Owner)
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