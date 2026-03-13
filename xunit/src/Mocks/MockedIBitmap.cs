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
using CivOne.IO;

namespace CivOne.UnitTests
{
	public class MockedIBitmap : IBitmap	
	{
		public MockedIBitmap(IEnumerable<Colour> palette, byte[,] bitmap)
		{
			Palette = palette.ToArray();
			Bitmap = new Bytemap(bitmap);
		}
		public Palette Palette { get; set; }
		public Bytemap Bitmap { get; set; }

		public void Dispose()
		{
		}
	}
}