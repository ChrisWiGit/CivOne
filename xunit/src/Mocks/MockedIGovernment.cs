using CivOne.Buildings;
using CivOne.Wonders;
using System.Drawing;
using CivOne.Tiles;
using CivOne.Units;
using System;
using System.Linq;
using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Persistence.Model;
using CivOne.Graphics;
using CivOne.Civilizations;
using CivOne.Leaders;
using CivOne.Governments;
using CivOne.Advances;

namespace CivOne.UnitTests
{
	public class MockedIGovernment : IGovernment
	{
		public byte Id { get; set; } = 42; 

		public string NameAdjective { get; set; } = "Mock Adjective";

		public IAdvance RequiredTech => throw new NotImplementedException();

		public int CorruptionMultiplier => throw new NotImplementedException();

		public string TranslatedName { get; set; } = "Mock Government";
		public string Name { get; set; } = "Mock Government";

		public IBitmap Icon => throw new NotImplementedException();

		public byte PageCount => throw new NotImplementedException();

		public Picture DrawPage(byte pageNumber)
		{
			throw new NotImplementedException();
		}
	}

}