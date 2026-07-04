using System.Linq;
using System.Collections.Generic;
using CivOne.Tiles;

namespace CivOne.UnitTests
{

    sealed class MockedMap : IMap
    {
        private readonly List<ICityOnContinent> _continentCities = new();

		public ITile this[int x, int y] => throw new System.NotImplementedException();

		public ITile[,] this[int x, int y, int width, int height] => throw new System.NotImplementedException();

		public int TerrainMasterWord => throw new System.NotImplementedException();

		public int Width => throw new System.NotImplementedException();

		public int Height => throw new System.NotImplementedException();

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
