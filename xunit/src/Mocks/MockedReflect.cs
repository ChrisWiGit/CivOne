using System.Collections.Generic;
using CivOne.Tiles;
using CivOne.Units;
using System;
using CivOne.UserInterface;

namespace CivOne.UnitTests
{
    public class MockedReflect : IReflect
    {
        public IEnumerable<IProduction> GetProduction() => [new Militia()];
    }
}