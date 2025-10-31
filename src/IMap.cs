using System.Collections.Generic;

namespace CivOne
{
	public interface IMap
	{
		IEnumerable<City> ContinentCities(int continentId);
	}
}