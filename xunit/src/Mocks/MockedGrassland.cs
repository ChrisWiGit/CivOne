// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

// Author: Kevin Routley : July, 2019

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
