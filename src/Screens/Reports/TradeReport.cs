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
using CivOne.Concepts;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens.Services;

namespace CivOne.Screens.Reports
{
	internal class TradeReport : BaseReport
	{
		private const char LUXURIES = '\\';
		private const char GOLD = '$';
		private const char SCIENCE = '~';

		private readonly City[] _cities;

		private bool _update = true;
		private int _page = 0;

        private int PAGE_SIZE = 17;

		private void DrawCityTrade()
		{
			int totalIncome = _cities.Sum(c => c.GetRealTotalIncome());
			int totalScience = _cities.Sum(c => c.GetRealTotalScience());

			this.DrawText("City Trade", 0, 15, 8, 32);

			int yy = 40;
			for (int i = (_page++ * PAGE_SIZE); i < _cities.Length && i < (_page * PAGE_SIZE); i++)
			{
				City city = _cities[i];

				CitizenTypes citizenTypes = city.GetCitizenTypes();

				var Luxuries = citizenTypes.InDisorder ? 0 : city.Luxuries;
				// Calling GetRealTotalIncome would call GetCitizenTypes twice, so don't use. 
				var Taxes = citizenTypes.InDisorder ? 0 : city.TotalIncome;
				var Science = citizenTypes.InDisorder ? 0 : city.Science;

				this.DrawText(city.Name, 0, 5, 16, yy + 1)
					.DrawText(city.Name, 0, 15, 16, yy)
					.DrawText($"{Luxuries}{LUXURIES}/{Taxes}{GOLD}/{Science}{SCIENCE}", 0, 10, 86, yy);

				yy += Resources.GetFontHeight(0);
			}
			
			if ((_page * PAGE_SIZE) >= _cities.Length)
			{
				yy += 4;
				this.DrawText($"Total Income: {totalIncome}$", 0, 10, 8, yy);
				yy += Resources.GetFontHeight(0);
				if (totalScience > 0 && yy <= 188)
				{
					this.DrawText($"Discoveries: {(int)Math.Ceiling((double)Human.ScienceCost / totalScience)} turns", 0, 10, 8, yy);
				}
			}
		}
		
		private void DrawMaintenanceCost()
		{
			int totalCost = _cities.Sum(c => c.TotalMaintenance);

			this.DrawText("Maintenance Cost", 0, 15, 160, 32);

			int yy = 40;
			foreach (Building entry in Enum.GetValues(typeof(Building)))
			{
				int count = _cities.SelectMany(c => c.Buildings).Count(b => b.Id == (int)entry);
				if (count == 0) continue;

				IBuilding building = _cities.SelectMany(c => c.Buildings).First(b => b.Id == (int)entry);
				if (building.Maintenance == 0) continue;

				this.DrawText($"{count} {building.Name}, {building.Maintenance * count}$", 0, 14, 160, yy);
				yy += Resources.GetFontHeight(0);
			}

			yy += 4;
			this.DrawText($"Total Cost: {totalCost}$", 0, 14, 160, yy);
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;

			this.FillRectangle(0, 32, 320, 168, 2);
			DrawCityTrade();
			if ((_page * PAGE_SIZE) >= _cities.Length)
			{
				DrawMaintenanceCost();
			}

			this.AddLayer(Portrait[(int)Advisor.Domestic], 278, 2);

			_update = false;
			return true;
		}

		private bool NextPage()
		{
			if ((_page * PAGE_SIZE) < _cities.Length)
			{
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

		public TradeReport() : base("TRADE REPORT", 2)
		{
			_cities = Game.GetCities().Where(c => Human == c.Owner && c.Size > 0).ToArray();
		}
	}
}