// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Linq;
using CivOne.Tiles;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Characterization tests that lock the public tile-accessor surface of <see cref="Map"/>.
	///
	/// Loaded fixture: Earth map via <c>earth.yml</c> (provided by <see cref="src.TestsBase"/>).
	///
	/// These tests intentionally only exercise documented, in-range behavior.
	/// Out-of-range X coordinates are intentionally not covered here; the indexer
	/// wraps X via modulo, but these tests focus on the normal in-range access patterns.
	/// </summary>
	public class MapTileAccessorTests : src.TestsBase
	{
		[Fact]
		public void AllTilesReturnsWidthTimesHeightTiles()
		{
			int expected = Map.WIDTH * Map.HEIGHT;

			int actual = Map.Instance.AllTiles().Count();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void AllTilesContainsNoNullTiles()
		{
			Assert.DoesNotContain(null, Map.Instance.AllTiles());
		}

		[Fact]
		public void IndexerWithYBelowZeroReturnsNull()
		{
			ITile actual = Map.Instance[0, -1];

			Assert.Null(actual);
		}

		[Fact]
		public void IndexerWithYAtHeightReturnsNull()
		{
			ITile actual = Map.Instance[0, Map.HEIGHT];

			Assert.Null(actual);
		}

		[Fact]
		public void IndexerReturnsSameInstanceAsTilesArray()
		{
			ITile[,] tiles = Map.Instance.Tiles;

			ITile expected = tiles[12, 7];
			ITile actual = Map.Instance[12, 7];

			Assert.Same(expected, actual);
		}

		[Fact]
		public void BlockIndexerReturnsArrayWithRequestedDimensions()
		{
			ITile[,] actual = Map.Instance[5, 5, 3, 4];

			Assert.Equal(3, actual.GetLength(0));
			Assert.Equal(4, actual.GetLength(1));
		}

		[Fact]
		public void BlockIndexerFillsBlockFromIndexerInRowMajorOrder()
		{
			const int x = 10;
			const int y = 8;
			const int w = 4;
			const int h = 3;

			ITile[,] actual = Map.Instance[x, y, w, h];

			for (int yy = 0; yy < h; yy++)
			{
				for (int xx = 0; xx < w; xx++)
				{
					Assert.Same(Map.Instance[x + xx, y + yy], actual[xx, yy]);
				}
			}
		}

		[Fact]
		public void BlockIndexerWithNegativeWidthTranslatesOriginLeft()
		{
			ITile[,] expected = Map.Instance[7, 9, 3, 2];

			ITile[,] actual = Map.Instance[10, 9, -3, 2];

			Assert.Equal(expected.GetLength(0), actual.GetLength(0));
			Assert.Equal(expected.GetLength(1), actual.GetLength(1));
			for (int yy = 0; yy < expected.GetLength(1); yy++)
			{
				for (int xx = 0; xx < expected.GetLength(0); xx++)
				{
					Assert.Same(expected[xx, yy], actual[xx, yy]);
				}
			}
		}

		[Fact]
		public void BlockIndexerWithNegativeHeightTranslatesOriginUp()
		{
			ITile[,] expected = Map.Instance[5, 6, 2, 3];

			ITile[,] actual = Map.Instance[5, 9, 2, -3];

			for (int yy = 0; yy < expected.GetLength(1); yy++)
			{
				for (int xx = 0; xx < expected.GetLength(0); xx++)
				{
					Assert.Same(expected[xx, yy], actual[xx, yy]);
				}
			}
		}

		[Fact]
		public void QueryMapPartYieldsWidthTimesHeightTiles()
		{
			int expected = 5 * 4;

			int actual = Map.Instance.QueryMapPart(3, 3, 5, 4).Count();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void QueryMapPartYieldsTilesInRowMajorOrderMatchingIndexer()
		{
			const int x = 6;
			const int y = 4;
			const int w = 3;
			const int h = 3;

			List<ITile> actual = [.. Map.Instance.QueryMapPart(x, y, w, h)];

			int i = 0;
			for (int yy = 0; yy < h; yy++)
			{
				for (int xx = 0; xx < w; xx++)
				{
					Assert.Same(Map.Instance[x + xx, y + yy], actual[i]);
					i++;
				}
			}
		}

		[Fact]
		public void ContinentTilesOnlyReturnsTilesWithRequestedContinentId()
		{
			// Pick the most common continent id in the fixture so the assertion has
			// data to iterate. The Earth YAML fixture does not pre-number continents,
			// so this may be id 0.
			int continentId = Map.Instance.AllTiles()
				.GroupBy(t => (int)t.ContinentId)
				.OrderByDescending(g => g.Count())
				.First()
				.Key;

			IEnumerable<ITile> actual = Map.Instance.ContinentTiles(continentId);

			Assert.NotEmpty(actual);
			Assert.All(actual, t => Assert.Equal(continentId, t.ContinentId));
		}

		[Fact]
		public void ContinentTilesAcrossAllContinentsCoverFullMap()
		{
			IEnumerable<int> distinctIds = Map.Instance.AllTiles()
				.Select(t => (int)t.ContinentId)
				.Distinct();

			int expected = Map.WIDTH * Map.HEIGHT;
			int actual = distinctIds.Sum(id => Map.Instance.ContinentTiles(id).Count());

			Assert.Equal(expected, actual);
		}
	}
}
