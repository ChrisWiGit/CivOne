using System.Linq;
using System.Collections.Generic;
using CivOne.Wonders;
using System;
using CivOne.Governments;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Graphics;
using CivOne.Enums;

namespace CivOne.UnitTests
{
	partial class MockedIBuilding : IBuilding
	{
        public MockedIBuilding()
        {
            Id = 1;
            Name = "Mocked Building";
            Type = Building.Library;
            Price = 10;
            BuyPrice = 20;
            Maintenance = 1;
            RequiredTech = null;
            Icon = null;
            SmallIcon = null;
            PageCount = 0;
            ProductionId = 1;
        }
        
		public byte Id { get; set; }

		public IAdvance RequiredTech { get; set; }

		public byte Maintenance { get; set; }

		public IBitmap SmallIcon { get; set; }

		public short SellPrice { get; set; }

		public Building Type { get; set; }

		public string Name { get; set; }

		public IBitmap Icon { get; set; }

		public byte PageCount { get; set; }

		public byte Price { get; set; }

		public short BuyPrice { get; set; }

		public byte ProductionId { get; set; }

		public Picture DrawPage(byte pageNumber)
		{
			throw new NotImplementedException();
		}
	}
}