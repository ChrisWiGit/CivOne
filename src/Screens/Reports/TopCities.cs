// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Wonders;

namespace CivOne.Screens.Reports
{
	[Modal, ScreenResizeable]
	internal class TopCities : BaseScreen
	{
		private readonly City[] _cities;
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			DrawBackground();

			for (int i = 0; i < _cities.Length; i++)
			{
				City city = _cities[i];

				if (city == null || city.Size == 0) continue;
				byte colour = Common.ColourLight[city.Owner];

				int xx = 8;
				int yy = 32 + (32 * i);
				int ww = 304;
				int hh = 26;

				Player owner = Game.GetPlayer(city.Owner);

				this.FillRectangle(xx, yy, ww, hh, colour)
					.FillRectangle(xx + 1, yy + 1, ww - 2, hh - 2, 3);

				int dx = 42;

				ICityCitizenLayoutService layoutService = ICityCitizenLayoutService.Create(city);
				foreach (var info in layoutService.EnumerateCitizens())
				{
					this.AddLayer(Icons.Citizen(info.Citizen), dx + info.X, yy + 10);
				}
				dx += layoutService.Width();

				dx += 16;
				foreach (IWonder wonder in city.Wonders)
				{
					this.AddLayer(wonder.SmallIcon, dx, yy + 11);
					dx += 19;
				}

				this.DrawText($"{i + 1}. {city.Name} ({owner.Civilization.Name})", 0, 15, 160, yy + 3, TextAlign.Center);
			}

			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			Destroy();
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			Destroy();
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			Refresh();
		}
		
		public TopCities()
		{
			Palette = Common.DefaultPalette;

			// I'm not sure about the order of top 5 cities, but this is pretty close
			_cities = Game.GetCities()
							.Where(c => c.Size > 0)
							.OrderByDescending(c => c.Wonders.Length)
							.ThenByDescending(c => c.Size)
							.ThenByDescending(c => c.Citizens.Count(x => x == Citizen.HappyMale || x == Citizen.HappyFemale))
							.ThenByDescending(c => c.Citizens.Count(x => x == Citizen.ContentMale || x == Citizen.ContentFemale))
							.ThenBy(c => c.Citizens.Count(x => x == Citizen.UnhappyMale || x == Citizen.UnhappyFemale))
							.Take(5)
							.ToArray();

			Refresh();
		}

		private void DrawBackground()
		{
			this.Clear(3)
				.DrawText("The Top Five Cities in the World", 0, 5, 80, 13)
				.DrawText("The Top Five Cities in the World", 0, 15, 80, 12);
		}
	}
}