// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Characterization tests for terrain-query helpers on <see cref="Map"/>.
	///
	/// Fixture: Earth map loaded from <c>earth.yml</c> via <see cref="src.TestsBase"/>.
	///
	/// <see cref="Map.TileIsSpecialInternal"/> is exercised because it is the public
	/// internal accessor for the private <c>TileIsSpecial</c>; the private
	/// <c>NearOcean</c> and <c>ModGrid</c> helpers are intentionally left for later
	/// phases (would require additional internal exposure to test directly).
	/// </summary>
	public class MapTerrainQueryTests : src.TestsBase
	{
		[Fact]
		public void TileIsTypeWithSingleMatchingTerrainReturnsTrue()
		{
			ITile tile = Map.Instance.AllTiles().First(t => t.Type == Terrain.Ocean);

			bool actual = Map.TileIsType(tile, Terrain.Ocean);

			Assert.True(actual);
		}

		[Fact]
		public void TileIsTypeWithSingleNonMatchingTerrainReturnsFalse()
		{
			ITile tile = Map.Instance.AllTiles().First(t => t.Type == Terrain.Ocean);

			bool actual = Map.TileIsType(tile, Terrain.Desert);

			Assert.False(actual);
		}

		[Fact]
		public void TileIsTypeWithMultipleTerrainsIncludingMatchReturnsTrue()
		{
			ITile tile = Map.Instance.AllTiles().First(t => t.Type == Terrain.Ocean);

			bool actual = Map.TileIsType(tile, Terrain.Desert, Terrain.Ocean, Terrain.Plains);

			Assert.True(actual);
		}

		[Fact]
		public void TileIsTypeWithNoCandidateTerrainsReturnsFalse()
		{
			ITile tile = Map.Instance.AllTiles().First();

			bool actual = Map.TileIsType(tile);

			Assert.False(actual);
		}

		[Fact]
		public void TileIsSpecialInternalIsDeterministicForSameCoordinate()
		{
			bool first = Map.Instance.TileIsSpecialInternal(20, 15);
			bool second = Map.Instance.TileIsSpecialInternal(20, 15);

			Assert.Equal(first, second);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void TileIsSpecialInternalNearTopPoleReturnsFalse(int y)
		{
			bool actual = Map.Instance.TileIsSpecialInternal(10, y);

			Assert.False(actual);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void TileIsSpecialInternalNearBottomPoleReturnsFalse(int offsetFromHeight)
		{
			int y = Map.HEIGHT - 1 - offsetFromHeight;

			bool actual = Map.Instance.TileIsSpecialInternal(10, y);

			Assert.False(actual);
		}

		[Fact]
		public void TileIsSpecialInternalAtLeastOneTileInMidLatitudesIsSpecial()
		{
			bool anySpecial = false;
			for (int y = 2; y <= Map.HEIGHT - 3 && !anySpecial; y++)
			{
				for (int x = 0; x < Map.WIDTH && !anySpecial; x++)
				{
					if (Map.Instance.TileIsSpecialInternal(x, y)) anySpecial = true;
				}
			}

			Assert.True(anySpecial);
		}
	}
}
