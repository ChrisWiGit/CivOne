using System.Collections.Generic;
using System.Linq;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Wonders;

namespace CivOne.Persistence.Resolver
{
	public class CityDefinitionResolver : ICityDefinitionResolver
	{
		public IBuilding[] ResolveBuildings(IEnumerable<Building> buildingTypes)
		{
			var types = (buildingTypes ?? []).ToHashSet();
			return [.. Reflect.GetBuildings().Where(b => types.Contains(b.Type))];
		}

		public IWonder[] ResolveWonders(IEnumerable<Wonder> wonderTypes)
		{
			var types = (wonderTypes ?? []).ToHashSet();
			return [.. Reflect.GetWonders().Where(w => types.Contains(w.Type))];
		}
	}
}
