// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.IO;

namespace CivOne.Graphics
{
	public struct Colour(byte alpha, byte red, byte green, byte blue)
	{
		public byte A { get; private set; } = alpha;
		public byte R { get; private set; } = red;
		public byte G { get; private set; } = green;
		public byte B { get; private set; } = blue;

		public override readonly bool Equals(object? obj) => (obj is Colour) && obj.GetHashCode() == GetHashCode();
		public override readonly int GetHashCode() => (R << 16) + (G << 8) + B;
		public static bool operator ==(Colour a, Colour b) => a.Equals(b);
		public static bool operator !=(Colour a, Colour b) => !a.Equals(b);

		public static Colour Transparent => new(0, 0, 0) { A = 0 };
		public static Colour Black => new(0, 0, 0);

		public Colour(byte red, byte green, byte blue) : this((byte)255, red, green, blue)
		{
		}

		public Colour(int alpha, int red, int green, int blue) : this((byte)alpha, (byte)red, (byte)green, (byte)blue)
		{
		}

		public Colour(int red, int green, int blue) : this((byte)red, (byte)green, (byte)blue)
		{
		}
	}
}