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
        public void DefaultConstructorSetsXAndYToZero()
        {
            _testee = new MapLocation();

            Assert.Equal(0u, _testee.X);
            Assert.Equal(0u, _testee.Y);
        }

        [Fact]
        public void ConstructorWithCoordinatesSetsXAndY()
        {
            Assert.Equal(3u, _testee.X);
            Assert.Equal(5u, _testee.Y);
        }

        [Fact]
        public void CopyConstructorCopiesXAndY()
        {
            var actual = new MapLocation(_testee);

            Assert.Equal(_testee.X, actual.X);
            Assert.Equal(_testee.Y, actual.Y);
        }

        [Fact]
        public void CopyConstructorWithNullThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MapLocation((MapLocation)null));
        }

        [Fact]
        public void ConstructorWithPointSetsXAndY()
        {
            _testee = new MapLocation(new Point(7, 9));

            Assert.Equal(7u, _testee.X);
            Assert.Equal(9u, _testee.Y);
        }

        [Fact]
        public void MoveToReturnsNewLocationWithAddedValues()
        {
            var expected = new MapLocation(5, 8);

            var actual = _testee.MoveTo(2, 3);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MoveXReturnsNewLocationWithAddedX()
        {
            var expected = new MapLocation(7, 5);

            var actual = _testee.MoveX(4);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MoveYReturnsNewLocationWithAddedY()
        {
            var expected = new MapLocation(3, 7);

            var actual = _testee.MoveY(2);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AddOperatorReturnsSumOfCoordinates()
        {
            var expected = new MapLocation(4, 7);

            var actual = _testee + new MapLocation(1, 2);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SubtractOperatorReturnsDifferenceOfCoordinates()
        {
            var expected = new MapLocation(2, 3);

            var actual = _testee - new MapLocation(1, 2);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EqualsWithSameValuesReturnsTrue()
        {
            Assert.True(_testee.Equals(new MapLocation(3, 5)));
        }

        [Fact]
        public void EqualsWithDifferentValuesReturnsFalse()
        {
            Assert.False(_testee.Equals(new MapLocation(1, 1)));
        }

        [Fact]
        public void EqualsWithNullReturnsFalse()
        {
            Assert.False(_testee.Equals(null));
        }

        [Fact]
        public void EqualityOperatorWithEqualInstancesReturnsTrue()
        {
            Assert.True(_testee == new MapLocation(3, 5));
        }

        [Fact]
        public void EqualityOperatorWithDifferentInstancesReturnsFalse()
        {
            Assert.False(_testee == new MapLocation(1, 1));
        }

        [Fact]
        public void InequalityOperatorWithDifferentInstancesReturnsTrue()
        {
            Assert.True(_testee != new MapLocation(1, 1));
        }

        [Fact]
        public void InequalityOperatorWithEqualInstancesReturnsFalse()
        {
            Assert.False(_testee != new MapLocation(3, 5));
        }

        [Fact]
        public void GetHashCodeEqualInstancesReturnsSameHashCode()
        {
            var expected = new MapLocation(3, 5).GetHashCode();

            var actual = _testee.GetHashCode();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetHashCodeDifferentInstancesReturnsDifferentHashCodes()
        {
            var actual = _testee.GetHashCode();
            var expected = new MapLocation(1, 2).GetHashCode();

            Assert.NotEqual(expected, actual);
        }
    }
}