using System;
using CivOne.Enums;
using Xunit;

namespace CivOne.Persistence.Model
{
	public class TileCodecTest
	{
		private readonly TileCodec _testee;

		public TileCodecTest()
		{
			_testee = new TileCodec();
		}

		[Theory]
		//               Terrain.Type,   Road,  RR,   Irrig., Poll., Fort., Mine, Hut, ExpectedEncoding
		// from YAML.md table
		[InlineData((int)Terrain.Ocean, false, false, false, false, false, false, false, "AK")] // Ocean with no flags = 10
		[InlineData((int)Terrain.Ocean, true, false, false, false, false, false, false, "Aa")] // Ocean with Road = 138
		[InlineData((int)Terrain.Ocean, true, true, false, false, false, false, false, "A6")]  // Ocean with Road + RailRoad = 154
		[InlineData((int)Terrain.Ocean, false, false, false, true, false, false, false, "CK")]      // Ocean with Pollution = 138
		[InlineData((int)Terrain.Plains, false, false, false, false, false, false, false, "AB")]    // Plains untouched = 1
		[InlineData((int)Terrain.Plains, true, false, false, false, false, false, false, "AR")]    // Plains with Road = 17
		[InlineData((int)Terrain.Plains, false, true, false, false, false, false, false, "Ah")]     // Plains with RailRoad = 33
		[InlineData((int)Terrain.Plains, true, true, false, false, false, false, false, "Ax")]     // Plains with Road + RailRoad = 49
		[InlineData((int)Terrain.Plains, false, false, true, false, false, false, false, "BB")]    // Plains with Irrigation = 65
		[InlineData((int)Terrain.Plains, true, false, true, false, false, false, false, "BR")]    // Plains with Road + Irrigation = 81
		[InlineData((int)Terrain.Plains, false, true, true, false, false, false, false, "Bh")]    // Plains with RailRoad + Irrigation = 97
		[InlineData((int)Terrain.Plains, false, false, false, true, false, false, false, "CB")]   // Plains with Pollution = 129
		[InlineData((int)Terrain.Forest, false, false, false, false, false, true, false, "ID")]   // Forest with Mine = 548
		[InlineData((int)Terrain.Hills, false, false, false, false, true, false, false, "EE")]    // Hills with Fortress = 258
		[InlineData((int)Terrain.Hills, true, false, false, false, true, false, false, "EU")]    // Hills with Road + Fortress = 274
		[InlineData((int)Terrain.Mountains, false, false, false, false, true, false, false, "EF")] // Mountain with Fortress = 772
		[InlineData((int)Terrain.Mountains, true, false, false, false, true, false, false, "EV")] // Mountain with Road + Fortress = 788
		[InlineData((int)Terrain.Desert, false, false, true, false, false, false, false, "BA")]   // Desert with Irrigation = 101
		[InlineData((int)Terrain.Desert, true, false, true, false, false, false, false, "BQ")]   // Desert with Road + Irrigation = 117
		[InlineData((int)Terrain.Grassland1, false, false, false, false, false, false, true, "QC")] // Grassland with Hut = 1024
		[InlineData((int)Terrain.Grassland1, true, false, false, false, false, false, false, "AS")] // Grassland with Road = 16
		[InlineData((int)Terrain.Grassland1, true, true, false, false, false, false, false, "Ay")] // Grassland with Road + RailRoad = 48
		[InlineData((int)Terrain.Forest, false, false, false, false, false, false, true, "QD")]  // Forest with Hut = 548
		[InlineData((int)Terrain.Tundra, false, false, false, false, false, false, false, "AG")] // Tundra with no flags = 6
		[InlineData((int)Terrain.Tundra, false, false, false, false, false, false, true, "QG")]  // Tundra with Hut = 1030
		public void EncodeKnownExamples(
			int terrain,
			bool road,
			bool railRoad,
			bool irrigation,
			bool pollution,
			bool fortress,
			bool mine,
			bool hut,
			string expected)
		{
			TileDto tile = new()
			{
				Terrain = (Terrain)terrain,
				Road = road,
				RailRoad = railRoad,
				Irrigation = irrigation,
				Pollution = pollution,
				Fortress = fortress,
				Mine = mine,
				Hut = hut
			};

			string encoded = _testee.Encode(tile);

			Assert.Equal(expected, encoded);
		}

		[Theory]
		[InlineData((int)Terrain.None, false, false, false, false, false, false, false)]
		[InlineData((int)Terrain.Desert, true, false, true, false, true, false, true)]
		[InlineData((int)Terrain.Grassland2, false, true, false, true, false, true, false)]
		[InlineData((int)Terrain.River, true, true, true, true, true, true, true)]
		public void EncodeDecodeRoundTripPreservesAllMappedFields(
			int terrain,
			bool road,
			bool railRoad,
			bool irrigation,
			bool pollution,
			bool fortress,
			bool mine,
			bool hut)
		{
			TileDto original = new()
			{
				Terrain = (Terrain)terrain,
				Road = road,
				RailRoad = railRoad,
				Irrigation = irrigation,
				Pollution = pollution,
				Fortress = fortress,
				Mine = mine,
				Hut = hut,
				LandValue = 123
			};

			string encoded = _testee.Encode(original);
			TileDto decoded = _testee.Decode(encoded, 0);

			Assert.Equal(original.Terrain, decoded.Terrain);
			Assert.Equal(original.Road, decoded.Road);
			Assert.Equal(original.RailRoad, decoded.RailRoad);
			Assert.Equal(original.Irrigation, decoded.Irrigation);
			Assert.Equal(original.Pollution, decoded.Pollution);
			Assert.Equal(original.Fortress, decoded.Fortress);
			Assert.Equal(original.Mine, decoded.Mine);
			Assert.Equal(original.Hut, decoded.Hut);
			Assert.Equal(original.Special, decoded.Special);

			Assert.Equal(default, decoded.LandValue);
		}

		[Theory]
		// Desert+Special (Oasis) = terrain 0, bit 11 set → value 2048 → first char index 32 = 'g', second 0 = 'A'
		[InlineData((int)Terrain.Desert, false, "gA")]
		// Desert+Special+Road → value 2048+16 = 2064 → first 2064>>6 = 32 = 'g', second 2064&63 = 16 = 'Q'
		[InlineData((int)Terrain.Desert, true, "gQ")]
		public void EncodeDesertWithSpecialUsesExpectedEncoding(int terrain, bool road, string expected)
		{
			TileDto tile = new()
			{
				Terrain = (Terrain)terrain,
				Road    = road,
				Special = true,
			};

			string actual = _testee.Encode(tile);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData((int)Terrain.Desert, false, false)]
		[InlineData((int)Terrain.Desert, true,  false)]
		[InlineData((int)Terrain.Desert, false, true)]
		[InlineData((int)Terrain.Desert, true,  true)]
		[InlineData((int)Terrain.Plains, false, true)]
		public void EncodeDecodeRoundTripPreservesSpecialFlag(int terrain, bool road, bool special)
		{
			TileDto original = new()
			{
				Terrain = (Terrain)terrain,
				Road    = road,
				Special = special,
			};

			string encoded = _testee.Encode(original);
			TileDto decoded = _testee.Decode(encoded, 0);

			Assert.Equal(original.Special, decoded.Special);
			Assert.Equal(original.Terrain, decoded.Terrain);
			Assert.Equal(original.Road, decoded.Road);
		}

		[Fact]
		public void DecodeUsesOffsetInsideRow()
		{
			const string row = "ZZARQQ";

			TileDto decoded = _testee.Decode(row, 2);

			Assert.Equal(Terrain.Plains, decoded.Terrain);
			Assert.True(decoded.Road);
			Assert.False(decoded.RailRoad);
			Assert.False(decoded.Irrigation);
			Assert.False(decoded.Pollution);
			Assert.False(decoded.Fortress);
			Assert.False(decoded.Mine);
			Assert.False(decoded.Hut);
		}

		[Theory]
		[InlineData("", 0)]
		[InlineData("A", 0)]
		[InlineData("AB", 1)]
		public void DecodeRowTooShortThrowsArgumentOutOfRangeException(string row, int offset)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => _testee.Decode(row, offset));
		}

		[Theory]
		[InlineData("=A")]
		[InlineData("A=")]
		[InlineData("~~")]
		public void DecodeInvalidCharactersThrowsFormatException(string encoded)
		{
			Assert.Throws<FormatException>(() => _testee.Decode(encoded, 0));
		}
	}
}
