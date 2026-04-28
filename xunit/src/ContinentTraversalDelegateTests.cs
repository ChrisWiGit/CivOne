// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using Xunit;

namespace CivOne.UnitTests
{
    public class ContinentTraversalDelegateTests
    {
        private readonly int[,] _aiRelPos = { { -1, 0 }, { 0, -1 }, { 0, 1 }, { 1, 0 } };

        [Fact]
        public void CountContinent_WrapsHorizontallyAcrossMapEdge()
        {
            // Arrange
            const int width = 5;
            const int height = 3;
            bool[,] ocean =
            {
                { true, true, true },
                { true, true, true },
                { true, true, true },
                { true, true, true },
                { true, true, true }
            };
            byte[,] continentIds = new byte[width, height];
            ocean[0, 1] = false;
            ocean[4, 1] = false;

            ContinentTraversalDelegate _testee = CreateTestee(width, height, ocean, continentIds);

            // Act
            ulong actual = _testee.CountContinent(0, 1, ocean: false, continentId: 7);

            // Assert
            Assert.Equal((ulong)2, actual);
            Assert.Equal((byte)7, continentIds[0, 1]);
            Assert.Equal((byte)7, continentIds[4, 1]);
        }

        [Fact]
        public void CountContinent_DoesNotWrapVertically()
        {
            // Arrange
            const int width = 5;
            const int height = 3;
            bool[,] ocean =
            {
                { true, true, true },
                { true, true, true },
                { true, true, true },
                { true, true, true },
                { true, true, true }
            };
            byte[,] continentIds = new byte[width, height];
            ocean[2, 0] = false;
            ocean[2, 2] = false;

            ContinentTraversalDelegate _testee = CreateTestee(width, height, ocean, continentIds);

            // Act
            ulong actual = _testee.CountContinent(2, 0, ocean: false, continentId: 3);

            // Assert
            Assert.Equal((ulong)1, actual);
            Assert.Equal((byte)3, continentIds[2, 0]);
            Assert.Equal((byte)0, continentIds[2, 2]);
        }

        [Fact]
        public void CountContinent_SkipsAlreadyAssignedTiles()
        {
            // Arrange
            const int width = 4;
            const int height = 3;
            bool[,] ocean =
            {
                { true, true, true },
                { true, true, true },
                { true, true, true },
                { true, true, true }
            };
            byte[,] continentIds = new byte[width, height];
            ocean[0, 1] = false;
            ocean[1, 1] = false;
            ocean[2, 1] = false;
            continentIds[1, 1] = 9;

            ContinentTraversalDelegate _testee = CreateTestee(width, height, ocean, continentIds);

            // Act
            ulong actual = _testee.CountContinent(0, 1, ocean: false, continentId: 2);

            // Assert
            Assert.Equal((ulong)1, actual);
            Assert.Equal((byte)2, continentIds[0, 1]);
            Assert.Equal((byte)9, continentIds[1, 1]);
            Assert.Equal((byte)0, continentIds[2, 1]);
        }

        [Fact]
        public void CountContinent_ReturnsZeroForMismatchingStartTileType()
        {
            // Arrange
            const int width = 3;
            const int height = 3;
            bool[,] ocean =
            {
                { true, true, true },
                { true, true, true },
                { true, true, true }
            };
            byte[,] continentIds = new byte[width, height];

            ContinentTraversalDelegate _testee = CreateTestee(width, height, ocean, continentIds);

            // Act
            ulong actual = _testee.CountContinent(1, 1, ocean: false, continentId: 1);

            // Assert
            Assert.Equal((ulong)0, actual);
            Assert.Equal((byte)0, continentIds[1, 1]);
        }

        private ContinentTraversalDelegate CreateTestee(int width, int height, bool[,] ocean, byte[,] continentIds)
            => new ContinentTraversalDelegate(
                width,
                height,
                _aiRelPos,
                (x, y) => ocean[x, y],
                (x, y) => continentIds[x, y],
                (x, y, continentId) => continentIds[x, y] = continentId);
    }
}
