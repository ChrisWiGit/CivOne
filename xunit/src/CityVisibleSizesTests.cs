using Xunit;

namespace CivOne.src
{
	/// <summary>
	/// Covers <see cref="City.VisibleSizes"/> for player slots beyond the original 8-player range.
	/// The array is indexed by player slot, so it has to hold <see cref="Game.MaxPlayers"/> entries.
	/// </summary>
	public class CityVisibleSizesTests : TestsBase
	{
		private const byte HighPlayerIndex = Game.MaxPlayers - 1;

		private static City CreateCity(byte owner) => new(owner) { X = 10, Y = 10 };

		[Fact]
		public void ArrayHasOneEntryPerMaxPlayer()
		{
			City city = CreateCity(0);

			Assert.Equal(Game.MaxPlayers, city.VisibleSizes.Length);
		}

		[Fact]
		public void SizeSeenByAHighPlayerIndexIsStored()
		{
			City city = CreateCity(0);

			city.VisibleSizes[HighPlayerIndex] = 2;

			Assert.Equal((uint)2, city.VisibleSizes[HighPlayerIndex]);
		}

		[Fact]
		public void AssigningAShorterArrayKeepsItsValuesAndGrowsToMaxPlayers()
		{
			// Saves written before the player limit was raised contain fewer entries.
			City city = CreateCity(0);

			// Index 0 is the city owner here, and the getter always refreshes the owner's own entry.
			city.VisibleSizes = [0, 7, 4];

			Assert.Equal(Game.MaxPlayers, city.VisibleSizes.Length);
			Assert.Equal((uint)7, city.VisibleSizes[1]);
			Assert.Equal((uint)4, city.VisibleSizes[2]);
		}

		[Fact]
		public void AssigningALongerArrayDoesNotOverflow()
		{
			uint[] oversized = new uint[Game.MaxPlayers + 8];
			oversized[1] = 9;

			City city = CreateCity(0);

			city.VisibleSizes = oversized;

			Assert.Equal(Game.MaxPlayers, city.VisibleSizes.Length);
			Assert.Equal((uint)9, city.VisibleSizes[1]);
		}
	}
}
