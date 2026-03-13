using System;
using System.Drawing;
using Xunit;

namespace CivOne.Persistence.Model
{
    public class MapLocationTest
    {
        private MapLocation _testee;

        public MapLocationTest()
        {
            _testee = new MapLocation(3, 5);
        }

        [Fact]
        public void DefaultConstructor_SetsXAndYToZero()
        {
            _testee = new MapLocation();

            Assert.Equal(0u, _testee.X);
            Assert.Equal(0u, _testee.Y);
        }

        [Fact]
        public void Constructor_WithCoordinates_SetsXAndY()
        {
            Assert.Equal(3u, _testee.X);
            Assert.Equal(5u, _testee.Y);
        }

        [Fact]
        public void CopyConstructor_CopiesXAndY()
        {
            var actual = new MapLocation(_testee);

            Assert.Equal(_testee.X, actual.X);
            Assert.Equal(_testee.Y, actual.Y);
        }

        [Fact]
        public void CopyConstructor_WithNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MapLocation((MapLocation)null));
        }

        [Fact]
        public void Constructor_WithPoint_SetsXAndY()
        {
            _testee = new MapLocation(new Point(7, 9));

            Assert.Equal(7u, _testee.X);
            Assert.Equal(9u, _testee.Y);
        }

        [Fact]
        public void MoveTo_ReturnsNewLocationWithAddedValues()
        {
            var expected = new MapLocation(5, 8);

            var actual = _testee.MoveTo(2, 3);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MoveX_ReturnsNewLocationWithAddedX()
        {
            var expected = new MapLocation(7, 5);

            var actual = _testee.MoveX(4);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MoveY_ReturnsNewLocationWithAddedY()
        {
            var expected = new MapLocation(3, 7);

            var actual = _testee.MoveY(2);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AddOperator_ReturnsSumOfCoordinates()
        {
            var expected = new MapLocation(4, 7);

            var actual = _testee + new MapLocation(1, 2);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SubtractOperator_ReturnsDifferenceOfCoordinates()
        {
            var expected = new MapLocation(2, 3);

            var actual = _testee - new MapLocation(1, 2);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Equals_WithSameValues_ReturnsTrue()
        {
            Assert.True(_testee.Equals(new MapLocation(3, 5)));
        }

        [Fact]
        public void Equals_WithDifferentValues_ReturnsFalse()
        {
            Assert.False(_testee.Equals(new MapLocation(1, 1)));
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            Assert.False(_testee.Equals(null));
        }

        [Fact]
        public void EqualityOperator_WithEqualInstances_ReturnsTrue()
        {
            Assert.True(_testee == new MapLocation(3, 5));
        }

        [Fact]
        public void EqualityOperator_WithDifferentInstances_ReturnsFalse()
        {
            Assert.False(_testee == new MapLocation(1, 1));
        }

        [Fact]
        public void InequalityOperator_WithDifferentInstances_ReturnsTrue()
        {
            Assert.True(_testee != new MapLocation(1, 1));
        }

        [Fact]
        public void InequalityOperator_WithEqualInstances_ReturnsFalse()
        {
            Assert.False(_testee != new MapLocation(3, 5));
        }

        [Fact]
        public void GetHashCode_EqualInstances_ReturnsSameHashCode()
        {
            var expected = new MapLocation(3, 5).GetHashCode();

            var actual = _testee.GetHashCode();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetHashCode_DifferentInstances_ReturnsDifferentHashCodes()
        {
            var actual = _testee.GetHashCode();
            var expected = new MapLocation(1, 2).GetHashCode();

            Assert.NotEqual(expected, actual);
        }
    }
}