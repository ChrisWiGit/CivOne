using System.Collections.Generic;

namespace CivOne.Persistence.Factories
{
    public interface ICityNameCatalog
    {
        IEnumerable<string> GetAllCityNames();
    }
}
