using System.Collections.Generic;

namespace CivOne
{
	public interface ICityOnContinent : ICityBasic, ICityBuildings
	{
	}
	public interface IMap
	{
		IEnumerable<ICityOnContinent> ContinentCities(int continentId);
	}
}