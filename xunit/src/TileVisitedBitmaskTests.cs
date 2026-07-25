using CivOne.Tiles;
using Xunit;

namespace CivOne.UnitTests
{
	public class TileVisitedBitmaskTests
	{
		[Fact]
		public void VisitTracksPlayersBeyondTheOriginalEightBitLimit()
		{
			Plains tile = new(0, 0, false);

			tile.Visit(0);
			tile.Visit(8);
			tile.Visit(31);

			Assert.Equal(0x8000_0101u, tile.Visited);
		}

		[Fact]
		public void VisitIsIdempotent()
		{
			Plains tile = new(0, 0, false);

			tile.Visit(31);
			tile.Visit(31);

			Assert.Equal(1u << 31, tile.Visited);
		}

		[Fact]
		public void VisitBeyondMaxPlayersIsANoOp()
		{
			Plains tile = new(0, 0, false);

			tile.Visit(32);

			Assert.Equal(0u, tile.Visited);
		}
	}
}
