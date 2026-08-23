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
	public class MockedIPalace : PalaceData
	{
		public MockedIPalace()
		{
			SetPalace(0, 1, 2);
			SetPalace(1, 2, 3);
			SetPalace(2, 3, 4);
			SetPalace(3, 1, 1);
			SetPalace(4, 2, 2);
			SetPalace(5, 3, 3);
			SetPalace(6, 1, 4);

			SetGarden(0, 1);
			SetGarden(1, 2);
			SetGarden(2, 3);
		}
	}
}