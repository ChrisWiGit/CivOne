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
		[InlineData((int)Terrain.Tundra, false, false, false, false, false, false, false, "AG")]
		[InlineData((int)Terrain.Ocean, false, false, false, false, false, false, false, "AK")]
		[InlineData((int)Terrain.Plains, true, false, false, false, false, false, false, "AR")]
		public void Encode_KnownExamples(
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
		public void EncodeDecode_RoundTrip_PreservesAllMappedFields(
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
				LandValue = 123,
				LandScore = 45
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

			Assert.Equal(default, decoded.LandValue);
			Assert.Equal(default, decoded.LandScore);
		}

		[Fact]
		public void Decode_UsesOffsetInsideRow()
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
		public void Decode_RowTooShort_ThrowsArgumentOutOfRangeException(string row, int offset)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => _testee.Decode(row, offset));
		}

		[Theory]
		[InlineData("=A")]
		[InlineData("A=")]
		[InlineData("~~")]
		public void Decode_InvalidCharacters_ThrowsFormatException(string encoded)
		{
			Assert.Throws<FormatException>(() => _testee.Decode(encoded, 0));
		}
	}
}
