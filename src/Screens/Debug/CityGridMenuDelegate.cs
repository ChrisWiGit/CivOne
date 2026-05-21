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

		private static string[] GetLabels(City[] cities, Func<City, string> labelSelector)
		{
			Func<City, string> selector = labelSelector ?? (city => city.Name);
			return [.. cities.Select(selector)];
		}

			public CityGridMenuDelegate(City[] cities, Func<City, string> labelSelector = null)
			: base(GetLabels(cities, labelSelector), SelectionMode.Select, fontId: 0)
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