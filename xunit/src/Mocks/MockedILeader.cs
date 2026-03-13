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

namespace CivOne.UnitTests
{
	public class MockedILeader : ILeader
	{
		public string Name { get; set; } = "TestLeader";


		public AggressionLevel Aggression { get; set; } = AggressionLevel.Normal;
		public DevelopmentLevel Development { get; set; } = DevelopmentLevel.Normal;
		public MilitarismLevel Militarism { get; set; } = MilitarismLevel.Normal;

		public IBitmap PortraitSmall => new MockedIBitmap([Colour.Black], new byte[,] { { 0 } });
		public IBitmap GetPortrait(FaceState state = FaceState.Neutral)
		{
			return new MockedIBitmap([Colour.Black], new byte[,] { { 0,0 } });
		}
	}

}