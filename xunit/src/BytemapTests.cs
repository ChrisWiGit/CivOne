// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Graphics;
using CivOne.IO;
using Xunit;

namespace CivOne.UnitTests
{
	public class BytemapTests
	{
		// Source 5×2:
		// Row 0: [1, 2, 3, 4, 5]
		// Row 1: [6, 7, 8, 9, 10]
		private static Bytemap CreateSource5x2()
		{
			var src = new Bytemap(5, 2);
			for (int y = 0; y < 2; y++)
			{
				for (int x = 0; x < 5; x++)
				{
					src[x, y] = (byte)((y * 5) + x + 1);
				}
			}
			return src;
		}

		// Source 4×4:
		// Row 0: [ 1,  2,  3,  4]
		// Row 1: [ 5,  6,  7,  8]
		// Row 2: [ 9, 10, 11, 12]
		// Row 3: [13, 14, 15, 16]
		private static Bytemap CreateSource4x4()
		{
			var src = new Bytemap(4, 4);
			for (int y = 0; y < 4; y++)
			{
				for (int x = 0; x < 4; x++)
				{
					src[x, y] = (byte)((y * 4) + x + 1);
				}
			}
			return src;
		}

		[Fact]
		public void SubBitmapInBoundsPixelsMatchSource()
		{
			// Arrange
			var source = CreateSource4x4();

			// Act – request columns 1–2, all 4 rows (fully in-bounds)
			var actual = source[1, 0, 2, 4];

			// Assert
			Assert.Equal(2, actual.Width);
			Assert.Equal(4, actual.Height);
			Assert.Equal(2, actual[0, 0]);
			Assert.Equal(3, actual[1, 0]);
			Assert.Equal(6, actual[0, 1]);
			Assert.Equal(7, actual[1, 1]);
			Assert.Equal(10, actual[0, 2]);
			Assert.Equal(11, actual[1, 2]);
			Assert.Equal(14, actual[0, 3]);
			Assert.Equal(15, actual[1, 3]);
		}

		[Fact]
		public void SubBitmapClippedLeftPixelsPlacedAtCorrectColumn()
		{
			// Arrange – left=-2 means buffer.Length (3) < output.Width (5)
			// which is the condition that triggers the row-stride bug.
			var source = CreateSource5x2();

			// Act
			var actual = source[-2, 0, 5, 2];

			// Assert – dimensions
			Assert.Equal(5, actual.Width);
			Assert.Equal(2, actual.Height);

			// Left two columns are out-of-bounds and must stay 0
			Assert.Equal(0, actual[0, 0]);
			Assert.Equal(0, actual[1, 0]);
			Assert.Equal(0, actual[0, 1]);
			Assert.Equal(0, actual[1, 1]);

			// Columns 2–4 carry source columns 0–2
			Assert.Equal(1, actual[2, 0]);
			Assert.Equal(2, actual[3, 0]);
			Assert.Equal(3, actual[4, 0]);
			Assert.Equal(6, actual[2, 1]);
			Assert.Equal(7, actual[3, 1]);
			Assert.Equal(8, actual[4, 1]);
		}

		[Fact]
		public void SubBitmapClippedTopAndLeftPixelsPlacedAtCorrectRowAndColumn()
		{
			// Arrange – top=-1 and left=-1 so both dy=1 and dx=1.
			// buffer.Length (3) < output.Width (4) triggers the row-stride bug.
			var source = CreateSource4x4();

			// Act
			var actual = source[-1, -1, 4, 4];

			// Assert – dimensions
			Assert.Equal(4, actual.Width);
			Assert.Equal(4, actual.Height);

			// Row 0 is out-of-bounds and must stay 0
			Assert.Equal(0, actual[0, 0]);
			Assert.Equal(0, actual[1, 0]);
			Assert.Equal(0, actual[2, 0]);
			Assert.Equal(0, actual[3, 0]);

			// Column 0 is out-of-bounds and must stay 0
			Assert.Equal(0, actual[0, 1]);
			Assert.Equal(0, actual[0, 2]);
			Assert.Equal(0, actual[0, 3]);

			// Source row 0 cols 0–2 → output row 1 cols 1–3
			Assert.Equal(1, actual[1, 1]);
			Assert.Equal(2, actual[2, 1]);
			Assert.Equal(3, actual[3, 1]);

			// Source row 1 cols 0–2 → output row 2 cols 1–3
			Assert.Equal(5, actual[1, 2]);
			Assert.Equal(6, actual[2, 2]);
			Assert.Equal(7, actual[3, 2]);

			// Source row 2 cols 0–2 → output row 3 cols 1–3
			Assert.Equal(9, actual[1, 3]);
			Assert.Equal(10, actual[2, 3]);
			Assert.Equal(11, actual[3, 3]);
		}

		[Fact]
		public void DrawLineWhenEndpointsCrossBitmapBoundsDoesNotThrow()
		{
			// Arrange
			var picture = new Picture(16, 16);

			// Act
			var exception = Record.Exception(() =>
				picture
					.DrawLine(6, -1, 8, 5, 77)
					.DrawLine(11, 5, 16, 6, 77));

			// Assert
			Assert.Null(exception);
			Assert.Equal(77, picture.Bitmap[6, 0]);
			Assert.Equal(77, picture.Bitmap[15, 6]);
		}
	}
}
