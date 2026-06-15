using CivOne.Tiles;
using CivOne.Units;
using System;

namespace CivOne.UnitTests
{

    class MockedGrassland : Grassland, ITile
    {
        private IUnit[] _units = [];


        public MockedGrassland()
        {
        }
        public MockedGrassland(int x, int y) : base(x, y)
        {
        }

        public MockedGrassland WithUnits(params IUnit[] units)
        {
            _units = units;
            return this;
        }

        public override IUnit[] Units => _units;

        public ITile[,] MockedMap { get; set; } = new ITile[1, 1];

        public new ITile this[int relativeX, int relativeY] => MockedMap[X + relativeX, Y + relativeY];
		
    }
}
