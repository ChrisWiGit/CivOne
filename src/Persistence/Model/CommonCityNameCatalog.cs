using System.Collections.Generic;

namespace CivOne.Persistence.Model
{
    /// <summary>
    /// Production implementation that returns all city names from all civilizations
    /// via <see cref="Common.AllCityNames"/>. Index alignment with Common.AllCityNames
    /// is required so that NameId values match Game.CityNameId() offsets.
    /// </summary>
    public class CommonCityNameCatalog : ICityNameCatalog
    {
        public IEnumerable<string> GetAllCityNames() => Common.AllCityNames;
    }
}
