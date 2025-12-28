// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

// Author: Kevin Routley : July, 2019

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
