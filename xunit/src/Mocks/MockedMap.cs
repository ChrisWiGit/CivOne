using System.Linq;
using System.Collections.Generic;

namespace CivOne.UnitTests
{

    class MockedMap : IMap
    {
        private readonly List<ICityOnContinent> _continentCities = new();
        public IEnumerable<ICityOnContinent> ContinentCities(int continentId)
        {
            return [.. _continentCities.Where(city => city.ContinentId == continentId)];
        }

        public MockedMap ReturnContinentCitiesValues(params ICityOnContinent[] values)
        {
            _continentCities.RemoveAll(_ => true);
            _continentCities.AddRange(values);

            return this;
        }
    }
}
