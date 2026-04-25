using System.Collections.Generic;
using CivOne.Tiles;
using CivOne.Units;
using System;
using CivOne.UserInterface;

namespace CivOne.UnitTests
{

	class MockedProduction : IProduction
	{
        public MockedProduction()
        {
            Price = 0;
            BuyPrice = 0;
            ProductionId = 1;
        }
		public byte Price { get; set; }

		public short BuyPrice { get; set; }

		public byte ProductionId { get; set; }
	}
}