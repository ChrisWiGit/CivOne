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

		private readonly int _inciteCost;

		private readonly bool _canIncite;
		private Menu _menu;

		private void DontIncite(object sender, EventArgs args)
		{
			Cancel();
		}

		private void Incite(object sender, EventArgs args)
		{
			Player previousOwner = Game.GetPlayer(_cityToIncite.Owner);
            var newOwner = _diplomat.Owner;
            var newPlayer = Game.GetPlayer(newOwner);

            // Initial incite message
            var msg = Message.General($"{previousOwner.TribeNamePlural} rebel!",
                "Civil War in",
                $"{_cityToIncite.Name}.",
                $"{newPlayer.TribeName} influence",
                "suspected.");
            GameTask.Insert(msg);

            int plundered = 0;

            string[] lines = { $"{newPlayer.TribeNamePlural} capture", 
                               $"{_cityToIncite.Name}. {plundered} gold", 
                               "pieces plundered." };

			Show captureCity = Show.CaptureCity(_cityToIncite, lines);
			EventHandler capture_done = (s1, a1) =>
			{
				Game.DisbandUnit(_diplomat);
				_cityToIncite.Owner = newOwner;
				_cityToIncite.TechStolen = false;

                foreach (var unit in _cityToIncite.Units)
                {
                    unit.Owner = newOwner;
                }

				// remove half the buildings at random
				foreach (IBuilding building in _cityToIncite.Buildings.Where(b => Common.Random.Next(0, 1) == 1).ToList())
				{
					_cityToIncite.RemoveBuilding(building);
				}

				newPlayer.Gold -= (short)_inciteCost;
                newPlayer.Gold += (short) plundered;

				previousOwner.HandleExtinction();

				if (Human == _cityToIncite.Owner || Human == newOwner)
				{
					GameTask.Insert(Tasks.Show.CityManager(_cityToIncite));
				}
			};
            captureCity.Done += capture_done;

			if (Human == _cityToIncite.Owner || Human == _diplomat.Owner)
            {
				GameTask.Insert(captureCity);
            }
            else
            {
				capture_done(null, EventArgs.Empty);
            }

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

		internal DiplomatIncite(City cityToIncite, Diplomat diplomat) : base(100, 80, 180, 56)
		{
			_cityToIncite = cityToIncite ?? throw new ArgumentNullException(nameof(cityToIncite));
			_diplomat = diplomat ?? throw new ArgumentNullException(nameof(diplomat));

			IBitmap spyPortrait = Icons.Spy;

			using Palette palette = Common.DefaultPalette.Merge(spyPortrait.Palette, 144);
			Palette = palette; // No transparent colour in spy portrait

			DialogBox.AddLayer(spyPortrait, 2, 2);

			_inciteCost = Diplomat.InciteCost(cityToIncite);
			_canIncite = Diplomat.CanIncite(cityToIncite, diplomat.Player.Gold);

			DialogBox.DrawText($"Spies Report", 0, 15, 45, 5);
			DialogBox.DrawText($"Dissidents in {_cityToIncite.Name}", 0, 15, 45, 5 + Resources.GetFontHeight(FONT_ID));
			DialogBox.DrawText($"will revolt for ${_inciteCost}", 0, 15, 45, 5 + (2 * Resources.GetFontHeight(FONT_ID)));
		}
	}
}