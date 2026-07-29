// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Persistence.Model;
using CivOne.Tiles;
using CivOne.UnitTests;
using Xunit;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Tests for <see cref="FallbackTileScanDelegate"/>, the last-resort search both starting-position services
	/// and <c>Game.PlaceStartingUnits</c> use before giving up on a player.
	/// </summary>
	public class FallbackTileScanDelegateTests
	{
		private static MockedMapEditor BuildOceanMap(int width, int height)
		{
			var map = new MockedMapEditor { Width = width, Height = height };
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					map.SetTile(x, y, new Ocean(x, y, false));
				}
			}
			return map;
		}

		private static StartPositionContext ContextFor(MockedMapEditor map) => new()
		{
			Map = map,
			RandomService = new StubRandomService()
		};

		[Fact]
		public void ReturnsTheFirstFreeLandTile()
		{
			MockedMapEditor map = BuildOceanMap(10, 10);
			map.SetTile(3, 4, new Grassland(3, 4) { ContinentId = 1, LandValue = 0 });
			map.SetTile(7, 6, new Grassland(7, 6) { ContinentId = 1, LandValue = 0 });

			MapLocation? tile = new FallbackTileScanDelegate(ContextFor(map)).FindAnyUsableTile([]);

			Assert.NotNull(tile);
			Assert.Equal(3u, tile.X);
			Assert.Equal(4u, tile.Y);
		}

		[Fact]
		public void SkipsOccupiedTiles()
		{
			MockedMapEditor map = BuildOceanMap(10, 10);
			map.SetTile(3, 4, new Grassland(3, 4) { ContinentId = 1, LandValue = 0 });
			map.SetTile(7, 6, new Grassland(7, 6) { ContinentId = 1, LandValue = 0 });

			MapLocation? tile = new FallbackTileScanDelegate(ContextFor(map)).FindAnyUsableTile([new MapLocation(3, 4)]);

			Assert.NotNull(tile);
			Assert.Equal(7u, tile.X);
			Assert.Equal(6u, tile.Y);
		}

		[Fact]
		public void UsesPolarTilesOnlyWhenNothingElseIsLeft()
		{
			MockedMapEditor map = BuildOceanMap(10, 10);
			map.SetTile(1, 0, new Grassland(1, 0) { ContinentId = 1, LandValue = 0 });
			map.SetTile(5, 5, new Grassland(5, 5) { ContinentId = 1, LandValue = 0 });
			var scan = new FallbackTileScanDelegate(ContextFor(map));

			MapLocation? preferred = scan.FindAnyUsableTile([]);
			Assert.NotNull(preferred);
			Assert.Equal(5u, preferred.X);
			Assert.Equal(5u, preferred.Y);

			MapLocation? polar = scan.FindAnyUsableTile([new MapLocation(5, 5)]);
			Assert.NotNull(polar);
			Assert.Equal(1u, polar.X);
			Assert.Equal(0u, polar.Y);
		}

		[Fact]
		public void ReturnsNullWhenNoLandTileIsLeft()
		{
			MockedMapEditor map = BuildOceanMap(10, 10);

			MapLocation? tile = new FallbackTileScanDelegate(ContextFor(map)).FindAnyUsableTile([]);

			Assert.Null(tile);
		}
	}
}
