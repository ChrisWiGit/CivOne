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

namespace CivOne.Screens.Debug
{
	internal class CityGridMenuDelegate : GridMenuDelegate
	{
		private readonly City[] _cities;

		public event Action<City> CitySelected;

		public CityGridMenuDelegate(City[] cities)
			: base([.. cities.Select(x => x.Name)], SelectionMode.Select, fontId: 0)
		{
			_cities = cities;
			ItemSelected += OnCitySelected;
		}

		private void OnCitySelected(int index)
		{
			if (index < 0 || index >= _cities.Length) return;
			CitySelected?.Invoke(_cities[index]);
		}
	}
}