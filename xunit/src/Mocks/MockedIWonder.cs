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
	sealed partial class MockedIWonder: IWonder
	{
		public MockedIWonder()
		{
			Id = 1;
			TranslatedName = "Mocked Wonder";
			Name = "Mocked Wonder";
			Type = Wonder.Pyramids;
			Price = 100;
			BuyPrice = 20;
			RequiredTech = null;
			ObsoleteTech = null;
			Icon = null;
			SmallIcon = null;
			PageCount = 0;
			ProductionId = 10;
		}

		public byte Id { get; set; }

		public IAdvance? RequiredTech { get; set; }

		public IAdvance? ObsoleteTech { get; set; }

		public IBitmap? SmallIcon { get; set; }

		public Wonder Type { get; set; }

		public string TranslatedName { get; set; }

		public IBitmap? Icon { get; set; }

		public byte PageCount { get; set; }

		public byte Price { get; set; }

		public short BuyPrice { get; set; }

		public byte ProductionId { get; set; }

		public string Name { get; set; } = "Mocked Wonder";

		public Picture DrawPage(byte pageNumber)
		{
			throw new NotImplementedException();
		}

		public string FormatWorldWonder(City city)
		{
			throw new NotImplementedException();
		}
	}
}