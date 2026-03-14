using System.Linq;
using System.Collections.Generic;
using CivOne.Wonders;
using System;
using CivOne.Governments;
using CivOne.Advances;
using CivOne.Persistence.Model;
using CivOne.Civilizations;
using CivOne.Graphics;

namespace CivOne.UnitTests
{
	public class MockedIAdvance : IAdvance
	{
		public byte Id { get; set; } = 42;

		public IAdvance[] RequiredTechs => throw new NotImplementedException();

		public Palette OriginalColours => throw new NotImplementedException();

		public string Name { get; set; } = "Mock Advance";

		public IBitmap Icon => throw new NotImplementedException();

		public byte PageCount => throw new NotImplementedException();

		public Picture DrawPage(byte pageNumber)
		{
			throw new NotImplementedException();
		}

		public bool Is<T>() where T : IAdvance
		{
			throw new NotImplementedException();
		}

		public bool Not<T>() where T : IAdvance
		{
			throw new NotImplementedException();
		}

		public bool Requires(byte id)
		{
			throw new NotImplementedException();
		}
	}
}