using System;
using System.Linq;

namespace CivOne.Screens.Debug
{
	internal class CityGridMenuDelegate : GridMenuDelegate
	{
		private readonly City[] _cities;

		public event Action<City>? CitySelected;

		private static string[] GetLabels(City[] cities, Func<City, string>? labelSelector)
		{
			Func<City, string> selector = labelSelector ?? (city => city.Name);
			return [.. cities.Select(selector)];
		}

			public CityGridMenuDelegate(City[] cities, Func<City, string>? labelSelector = null)
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