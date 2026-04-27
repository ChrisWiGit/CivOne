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
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens.Reports
{
	[ScreenResizeable]
	internal class AttitudeSurvey : BaseReport
	{
		private const byte FONT_ID = 0;

		private readonly City[] _cities;

		private bool _update = true;
		private int _page = 0;

		private void Render()
		{
			this.Clear(9);
			DrawReportHeader();
			this.FillRectangle(OffsetX, OffsetY + 28, 320, 172, 9);
			int y = DrawCityRows();
			DrawPopulationSummary(y);
		}

		private int DrawCityRows()
		{
			int y = OffsetY + 32;

			int start = _page * 16;
			int end = start + 16;
			for (int i = start; i < _cities.Length && i < end; i++)
			{
				City city = _cities[i];

				this.DrawText($"{city.Name}:", FONT_ID, 15, OffsetX + 16, y);
				DrawCitizens(city, OffsetX + ((i % 2 == 0) ? 72 : 76), y);
				DrawBuildings(city, y);
				y += 10;
			}

			return y;
		}

		private void DrawPopulationSummary(int y)
		{
			y += 8;
			if (y > OffsetY + 190)
			{
				return;
			}

			Citizen[] allCitizens = [.. Human.Cities.SelectMany(x => x.GetCitizens())];
			int totalCitizens = allCitizens.Length;
			if (totalCitizens == 0)
			{
				return;
			}

			string population = GetPopulationText();
			int happyCitizens = allCitizens.Count(c => c == Citizen.HappyMale || c == Citizen.HappyFemale);
			int unhappyCitizens = allCitizens.Count(c => c == Citizen.UnhappyMale || c == Citizen.UnhappyFemale);
			int contentCitizens = totalCitizens - happyCitizens - unhappyCitizens;

			int happy = (int)Math.Floor((double)(100 / totalCitizens) * happyCitizens);
			int content = (int)Math.Floor((double)(100 / totalCitizens) * contentCitizens);
			int unhappy = (int)Math.Floor((double)(100 / totalCitizens) * unhappyCitizens);

			this.DrawText($"Population: {population} Happy:{happy}% Content:{content}% Unhappy:{unhappy}%", 0, 15, OffsetX + 16, y);
		}

		private string GetPopulationText()
		{
			string population = Common.NumberSeperator(Human.Population);
			if (Human.Population == 0)
			{
				return "00,000";
			}
			return population;
		}

		private void DrawBuilding<T>(City city, ref int x, int y) where T : IBuilding
		{
			IBuilding building;
			if ((building = city.Buildings.FirstOrDefault(b => b is T)) == null) return;

			this.AddLayer(building.SmallIcon, x, y - 1);
			x += 18;
		}

		private void DrawCitizens(City city, int x, int y)
		{
			ICityCitizenLayoutService layoutService = ICityCitizenLayoutService.Create(city);
			foreach (var info in layoutService.EnumerateCitizens())
			{
				this.AddLayer(Icons.Citizen(info.Citizen), info.X + x, y - 4);
			}
		}

		private void DrawBuildings(City city, int y)
		{
			int x = OffsetX + 212;
			this.FillRectangle(x, y - 1, 90, 10, 11);
			DrawBuilding<Temple>(city, ref x, y);
			DrawBuilding<MarketPlace>(city, ref x, y);
			DrawBuilding<Bank>(city, ref x, y);
			DrawBuilding<Cathedral>(city, ref x, y);
			DrawBuilding<Colosseum>(city, ref x, y);
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;
			Render();

			_update = false;
			return true;
		}

		private bool NextPage()
		{
			if (((_page + 1) * 16) < _cities.Length)
			{
				_page++;
				_update = true;
			}
			else
			{
				Destroy();
			}
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			return NextPage();
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			return NextPage();
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}

		public AttitudeSurvey() : base("ATTITUDE SURVEY", 9)
		{
			_cities = Game.GetCities().Where(c => Human == c.Owner && c.Size > 0).ToArray();
		}
	}
}