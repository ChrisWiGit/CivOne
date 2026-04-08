using System.Collections.Generic;

namespace CivOne.Persistence.Model
{
    public interface ICityNameCatalog
    {
        IEnumerable<string> GetAllCityNames();
    }
}
