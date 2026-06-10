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
	public class MockedILeader(int id) : ILeader
	{
		public string Name { get; set; } = $"TestLeader{id}";


		public AggressionLevel Aggression { get; set; } = AggressionLevel.Normal;
		public DevelopmentLevel Development { get; set; } = DevelopmentLevel.Normal;
		public MilitarismLevel Militarism { get; set; } = MilitarismLevel.Normal;

		public IBitmap PortraitSmall => new MockedIBitmap([Colour.Black], new byte[,] { { 0 } });

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		public IBitmap GetPortrait(FaceState state = FaceState.Neutral)
		{
			return new MockedIBitmap([Colour.Black], new byte[,] { { 0,0 } });
		}
	}

	// NOTE:
	// CivilizationDtoMapper.FromDto() resolves civilizations by comparing
	// dto.LeaderClassName with civ.Leader.GetType().Name (reflection-based type name matching).
	// Therefore tests need distinct leader *types* (not only different instances/ids),
	// otherwise multiple civilizations would map to the same LeaderClassName and matching
	// becomes ambiguous/non-deterministic in roundtrip tests.
	public sealed class MockedILeader1() : MockedILeader(1);
	public sealed class MockedILeader2() : MockedILeader(2);
	public sealed class MockedILeader3() : MockedILeader(3);

}