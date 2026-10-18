using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;
using System;
using CivOne.UserInterface;

namespace CivOne.UnitTests
{
    sealed class MockedUnit : BaseUnit, IUnit
    {
        public override IEnumerable<MenuItem<int>> MenuItems => throw new NotImplementedException();

        public MockedUnit(int x = 1, int y = 1, byte attack = 1)
        {
            X = x;
            Y = y;
            Attack = attack;
        }

        private ICityBasic? _city;

        public MockedUnit WithHome(ICityBasic city)
        {
            _city = city;
            return this;
        }

        /// <summary>
        /// Sets the terrain class of the unit.
        /// War weariness treats air units as away from home even while they stand in their own city.
        /// </summary>
        /// <param name="unitClass">The class to use.</param>
        /// <returns>The same instance, so calls can be chained.</returns>
        public MockedUnit WithCategory(UnitClass unitClass)
        {
            UnitCategory = unitClass;
            return this;
        }

        public bool IsHome(ICityBasic city)
        {
            return _city == city;
        }

        protected override bool ValidMoveTarget(ITile tile)
        {
            throw new NotImplementedException();
        }
    }
}
