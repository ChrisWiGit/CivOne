using System.Collections.Generic;
using CivOne.Persistence.Model;

namespace CivOne.Persistence.Factories
{
    /// <summary>
    /// Runtime implementation of <see cref="ICityNameCatalog"/> that returns all city names
    /// from all civilizations via <see cref="Common.AllCityNames"/>.
    /// Index alignment with <see cref="Common.AllCityNames"/> is required so that NameId values
    /// match <c>Game.CityNameId()</c> offsets.
    /// </summary>
    public class RuntimeCityNameCatalog : ICityNameCatalog
    {
        public IEnumerable<string> GetAllCityNames() => Common.AllCityNames;
    }
}
