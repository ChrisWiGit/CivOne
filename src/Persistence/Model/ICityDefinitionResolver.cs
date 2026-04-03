using System.Collections.Generic;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Wonders;

namespace CivOne.Persistence.Model
{
	public interface ICityDefinitionResolver
	{
		IBuilding[] ResolveBuildings(IEnumerable<Building> buildingTypes);
		IWonder[] ResolveWonders(IEnumerable<Wonder> wonderTypes);
	}
}
