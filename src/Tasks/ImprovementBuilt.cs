// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Buildings;
using CivOne.Screens;
using CivOne.Wonders;

namespace CivOne.Tasks
{
	internal class ImprovementBuilt : GameTask
	{
		private readonly City _city;
		private readonly IProduction _improvement;

		private void ClosedCityView(object? _, EventArgs __)
		{
			if (Common.HasScreenType<CityManager>()) return;
			
			CityManager cityManager = new(_city);
			cityManager.Closed += (s, a) => EndTask();
			Common.AddScreen(cityManager);
		}

		public override void Run()
		{
			if (Human != _city.CityOwnerPlayerIndex)
			{
				if (_improvement is ICivilopedia civilopedia)
				{
					Log($"{_city.Name} builds {civilopedia.TranslatedName}.");
				}
				EndTask();
				return;
			}

			IScreen cityView;
			if (!Game.Animations && _improvement is ICivilopedia civilopedia2)
			{
				cityView = new Newspaper(_city, new string[] { $"{_city.Name} builds", $"{civilopedia2.TranslatedName}." }, showGovernment: false);
			}
			else if (_improvement is IBuilding building)
			{
				cityView = new CityView(_city, production: building);
			}
			else if (_improvement is IWonder wonder)
			{
				cityView = new CityView(_city, production: wonder);
			}
			else
			{
				EndTask();
				return;
			}
			cityView.Closed += ClosedCityView;
			Common.AddScreen(cityView);
		}

		public ImprovementBuilt(City city, IBuilding building)
		{
			_city = city;
			_improvement = building;
		}

		public ImprovementBuilt(City city, IWonder wonder)
		{
			_city = city;
			_improvement = wonder;
		}
	}
}