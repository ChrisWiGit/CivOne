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
	public class MockedIBitmap(IEnumerable<Colour> palette, byte[,] bitmap) : IBitmap	
	{
		public Palette Palette { get; set; } = palette.ToArray();
		public Bytemap Bitmap { get; set; } = new Bytemap(bitmap);

		public void Dispose()
		{
			Bitmap.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}