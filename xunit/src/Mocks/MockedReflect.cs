using System.Collections.Generic;
using CivOne.Tiles;
using CivOne.Units;
using System;
using CivOne.UserInterface;

namespace CivOne.UnitTests
{
    public class MockedReflect : IReflect
    {
        public IEnumerable<IProduction> GetProduction() => [new MockProduction()];
    }

    internal sealed class MockProduction : IProduction
    {
        public byte Price { get; } = 1;
        public short BuyPrice { get; } = 50;
        public byte ProductionId { get; } = 1;
    }
}