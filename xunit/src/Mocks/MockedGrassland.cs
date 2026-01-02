using CivOne.Tiles;
using CivOne.Units;
using System;

namespace CivOne.UnitTests
{

    class MockedGrassland : Grassland, ITile
    {
        private IUnit[] _units = Array.Empty<IUnit>();


        public MockedGrassland()
        {
        }

        public MockedGrassland WithUnits(params IUnit[] units)
        {
            _units = units;
            return this;
        }

        public override IUnit[] Units => _units;
    }
}
