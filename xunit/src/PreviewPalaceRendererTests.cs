using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Screens.PalaceAssets;
using Xunit;

namespace CivOne.UnitTests
{
	public class PreviewPalaceResourcesDelegateTests
	{
		[Fact]
		public void GetPreviewPartWhenLevelAndPartProvidedReturnsExpectedSpriteSlice()
		{
			// Arrange
			using Picture sp257 = CreateSp257WithPreviewMarkers();
			var testee = new PreviewPalaceResourcesWrapper(
					name => {
						Assert.Equal("SP257", name);
						return sp257;
					});

			// Act
			Picture actual = testee.GetPreviewPart(PreviewPalacePart.WallRight, level: 3, PalaceStyle.Medieval);

			// Assert
			Assert.Equal(7, actual.Width);
			Assert.Equal(14, actual.Height);
			AssertPictureFilled(actual, ExpectedMarker(3, PreviewPalacePart.WallRight));
		}

		[Fact]
		public void GetPreviewPartWhenCalledTwiceReturnsCachedInstance()
		{
			// Arrange
			using Picture sp257 = CreateSp257WithPreviewMarkers();
			var testee = new PreviewPalaceResourcesWrapper(
					name => {
						Assert.Equal("SP257", name);
						return sp257;
					});

			// Act
			Picture first = testee.GetPreviewPart(PreviewPalacePart.Center, level: 2, PalaceStyle.Medieval);
			Picture second = testee.GetPreviewPart(PreviewPalacePart.Center, level: 2, PalaceStyle.Medieval);

			// Assert
			Assert.Same(first, second);
		}

		private static Picture CreateSp257WithPreviewMarkers()
		{
			// Picture must cover x up to 160 + (4-1)*35 + 4*7 + 7 = 294, y up to 32+13 = 45
			Picture result = new(300, 50);
			for (int level = 1; level <= 4; level++)
			{
				for (int part = 0; part < 5; part++)
				{
					int xStart = 160 + ((level - 1) * 35) + (part * 7);
					byte marker = ExpectedMarker(level, (PreviewPalacePart)part);
					// Medieval: y = 1..14 inclusive (PREVIEW_H = 14)
					for (int y = 1; y <= 14; y++)
					{
						for (int x = xStart; x < xStart + 7; x++)
						{
							result[x, y] = marker;
						}
					}
				}
			}
			return result;
		}

		private static byte ExpectedMarker(int level, PreviewPalacePart part)
			=> (byte)((level * 10) + (int)part + 1);

		private static void AssertPictureFilled(Picture picture, byte expected)
		{
			for (int y = 0; y < picture.Height; y++)
			{
				for (int x = 0; x < picture.Width; x++)
				{
					Assert.Equal(expected, picture[x, y]);
				}
			}
		}
	}

	public class PreviewPalaceRendererTests
	{
		private static readonly PalaceStyle[] Medieval = [.. System.Linq.Enumerable.Repeat(PalaceStyle.Medieval, 9)];

		[Fact]
		public void RenderPalaceWhenNoSlotVisibleReturnsOneByOneBitmap()
		{
			using Picture sp257 = CreateSp257WithPreviewMarkers();
			var testee = new PreviewPalaceRenderer(CreateResources(sp257));
			var palace = MiniPalaceLayout.FromPalace(new PalaceData());

			IBitmap actual = testee.RenderPalace(palace);

			Assert.Equal(1, actual.Width());
			Assert.Equal(1, actual.Height());
		}

		[Fact]
		public void RenderPalaceUsesTheFullNineSlotGrid()
		{
			using Picture sp257 = CreateSp257WithPreviewMarkers();
			var testee = new PreviewPalaceRenderer(CreateResources(sp257));
			var palace = new MiniPalaceLayout([-1, -1, -1, 0, 2, 0, -1, -1, -1], Medieval);

			IBitmap actual = testee.RenderPalace(palace);

			Assert.Equal(63, actual.Width());
			Assert.Equal(13, actual.Height());
		}

		[Fact]
		public void RenderPalaceDrawsVisibleSlotsAtTheirGridPositionAndLeavesGaps()
		{
			using Picture sp257 = CreateSp257WithPreviewMarkers();
			var testee = new PreviewPalaceRenderer(CreateResources(sp257));
			var palace = new MiniPalaceLayout([-1, 2, 0, 3, 1, 4, -1, -1, -1], Medieval);

			IBitmap actual = testee.RenderPalace(palace);

			AssertBlockEmpty(actual, slot: 0);
			AssertBlockFilled(actual, slot: 1, expected: ExpectedMarker(2, PreviewPalacePart.Left));
			AssertBlockEmpty(actual, slot: 2);
			AssertBlockFilled(actual, slot: 3, expected: ExpectedMarker(3, PreviewPalacePart.WallLeft));
			AssertBlockFilled(actual, slot: 4, expected: ExpectedMarker(1, PreviewPalacePart.Center));
			AssertBlockFilled(actual, slot: 5, expected: ExpectedMarker(4, PreviewPalacePart.Right));
			AssertBlockEmpty(actual, slot: 6);
		}

		[Fact]
		public void GetMaxPalaceHeightUsesTheTallestVisibleSlot()
		{
			using Picture sp257 = CreateSp257WithPreviewMarkers();
			var testee = new PreviewPalaceRenderer(CreateResources(sp257));
			PalaceStyle[] styles = [.. Medieval];
			styles[5] = PalaceStyle.Islamic;
			var palace = new MiniPalaceLayout([-1, -1, -1, 1, 0, 4, -1, -1, -1], styles);

			Assert.Equal(14, testee.GetMaxPalaceHeight(palace));
			Assert.Equal(0, testee.GetMaxPalaceHeight(MiniPalaceLayout.Empty));
		}

		private static PreviewPalaceResourcesWrapper CreateResources(Picture sp257)
			=> new(name =>
			{
				Assert.Equal("SP257", name);
				return sp257;
			});

		private static Picture CreateSp257WithPreviewMarkers()
		{
			// Picture must cover x up to 160 + (4-1)*35 + 4*7 + 7 = 294, y up to 32+13 = 45
			Picture result = new(300, 50);
			for (int level = 1; level <= 4; level++)
			{
				for (int part = 0; part < 5; part++)
				{
					int xStart = 160 + ((level - 1) * 35) + (part * 7);
					byte marker = ExpectedMarker(level, (PreviewPalacePart)part);
					// Medieval: y = 1..14 inclusive (PREVIEW_H = 14)
					for (int y = 1; y <= 14; y++)
					{
						for (int x = xStart; x < xStart + 7; x++)
						{
							result[x, y] = marker;
						}
					}
				}
			}
			return result;
		}

		// All parts are rendered at y = -1, so the top sprite row is clipped and all 13 rows carry the marker.
		private static void AssertBlockFilled(IBitmap bitmap, int slot, byte expected)
		{
			int xStart = slot * 7;
			for (int y = 0; y < bitmap.Height(); y++)
			{
				for (int x = xStart; x < xStart + 7; x++)
				{
					Assert.Equal(expected, bitmap.Bitmap[x, y]);
				}
			}
		}

		private static void AssertBlockEmpty(IBitmap bitmap, int slot) => AssertBlockFilled(bitmap, slot, 0);

		private static byte ExpectedMarker(int level, PreviewPalacePart part)
			=> (byte)((level * 10) + (int)part + 1);
	}
}
