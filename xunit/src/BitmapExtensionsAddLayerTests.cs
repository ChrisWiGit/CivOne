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
	/// <summary>
	/// Verifies that the row-based <see cref="BitmapExtensions.AddLayer(IBitmap, Bytemap, int, int, bool)"/>
	/// produces the same result as the per-pixel implementation it replaced.
	/// </summary>
	/// <remarks>
	/// The reference implementation below is the original loop, kept verbatim so clipping and
	/// transparency behaviour can be compared directly instead of being restated as expectations.
	/// </remarks>
	public class BitmapExtensionsAddLayerTests
	{
		/// <summary>
		/// The per-pixel implementation that <see cref="BitmapExtensions"/> used before the rewrite.
		/// </summary>
		/// <param name="target">The bitmap that receives the layer.</param>
		/// <param name="layer">The layer to draw.</param>
		/// <param name="left">The horizontal offset of the layer.</param>
		/// <param name="top">The vertical offset of the layer.</param>
		private static void AddLayerReference(Bytemap target, Bytemap layer, int left, int top)
		{
			for (int yy = 0; yy < layer.Height; yy++)
			{
				if (top + yy >= target.Height) break;
				if (top + yy < 0) continue;
				for (int xx = 0; xx < layer.Width; xx++)
				{
					if (left + xx >= target.Width) break;
					if (layer[xx, yy] == 0 || left + xx < 0) continue;
					target[left + xx, top + yy] = layer[xx, yy];
				}
			}
		}

		/// <summary>
		/// Creates a bitmap filled with a repeating non-zero pattern that also contains transparent
		/// pixels, so both the copied and the skipped case are covered.
		/// </summary>
		/// <param name="width">The bitmap width.</param>
		/// <param name="height">The bitmap height.</param>
		/// <param name="seed">Offset applied to the generated pixel values.</param>
		/// <returns>A new bitmap. The caller owns it.</returns>
		private static Bytemap CreatePattern(int width, int height, int seed)
		{
			Bytemap output = new(width, height);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					output[x, y] = (byte)(((x * 7) + (y * 13) + seed) % 5);
				}
			}
			return output;
		}

		public static TheoryData<int, int> Offsets =>
			new()
			{
				{ 0, 0 },
				{ 3, 2 },
				{ -4, -3 },
				{ -20, 0 },
				{ 0, -20 },
				{ 14, 9 },
				{ 40, 40 },
				{ -1, 11 },
				{ 11, -1 },
			};

		[Theory]
		[MemberData(nameof(Offsets))]
		public void MatchesPerPixelImplementation(int left, int top)
		{
			using Bytemap layer = CreatePattern(9, 7, 1);
			using Bytemap expected = CreatePattern(16, 12, 2);
			using Picture actual = new(16, 12);

			for (int y = 0; y < 12; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					actual.Bitmap[x, y] = expected[x, y];
				}
			}

			AddLayerReference(expected, layer, left, top);
			actual.AddLayer(layer, left, top);

			for (int y = 0; y < 12; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					Assert.Equal(expected[x, y], actual.Bitmap[x, y]);
				}
			}
		}

		[Fact]
		public void LeavesTargetUnchangedWhenFullyOutsideBounds()
		{
			using Bytemap layer = CreatePattern(4, 4, 1);
			using Picture target = new(8, 8);
			target.Bitmap[0, 0] = 42;

			target.AddLayer(layer, 100, 100);

			Assert.Equal(42, target.Bitmap[0, 0]);
		}

		[Fact]
		public void DisposesLayerWhenRequested()
		{
			using Bytemap layer = CreatePattern(4, 4, 1);
			using Picture target = new(8, 8);

			target.AddLayer(layer, 0, 0, dispose: true);

			Assert.True(layer.IsDisposed);
		}
	}
}
