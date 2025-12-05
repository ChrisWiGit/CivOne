// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

// Author: Kevin Routley : July, 2019

using System.Collections.Generic;
using CivOne.Tiles;
using CivOne.Units;
using System;
using CivOne.UserInterface;

namespace CivOne.UnitTests
{

public partial class CityCitizenServiceImplTests
	{
		class MockedUnit : BaseUnit, IUnit
        {
            public override IEnumerable<MenuItem<int>> MenuItems => throw new NotImplementedException();

            public MockedUnit(int x = 1, int y = 1, byte attack = 1)
            {
                X = x;
                Y = y;
                Attack = attack;
            }

            private ICityBasic _city;

            public MockedUnit WithHome(ICityBasic city)
            {
                _city = city;
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
}
