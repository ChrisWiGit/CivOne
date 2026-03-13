namespace CivOne.Persistence.Model
{
    using System;
	using System.Drawing;
	using CivOne.Persistence.Model.Attributes;
	using YamlDotNet.Serialization;

    public class MapLocation
    {
        // [Doc("The X coordinate of the location.")]
        public readonly uint X;

        // [Doc("The Y coordinate of the location.")]
        public readonly uint Y;

        public MapLocation() : this(0, 0)
		{
			
		}

        public MapLocation(uint x, uint y)
        {
            X = x;
            Y = y;
        }

        public MapLocation(MapLocation other)
        {
            ArgumentNullException.ThrowIfNull(other);

            X = other.X;
            Y = other.Y;
        }

        public MapLocation(Point point)
        {
            X = (uint)point.X;
            Y = (uint)point.Y;
        }

        public MapLocation MoveTo(uint addX, uint addY) => new(X + addX, Y + addY);
        public MapLocation MoveX(uint addX) => new(X + addX, Y);
        public MapLocation MoveY(uint addY) => new(X, Y + addY);

        public static MapLocation operator +(MapLocation left, MapLocation right) => new(left.X + right.X, left.Y + right.Y);
        public static MapLocation operator -(MapLocation left, MapLocation right) => new(left.X - right.X, left.Y - right.Y);

        public override bool Equals(object obj)
        {
            if (obj is not MapLocation other) return false;
            return X == other.X && Y == other.Y;
        }

        public static bool operator ==(MapLocation left, MapLocation right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }   
        public static bool operator !=(MapLocation left, MapLocation right) => !(left == right);

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}
	}
}