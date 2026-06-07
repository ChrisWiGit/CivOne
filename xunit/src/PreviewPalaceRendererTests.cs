using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Screens.PalaceAssets;
using Xunit;

namespace CivOne.UnitTests
{
	public class PreviewPalaceResourcesDelegateTests
	{
		[Fact]
		public void GetPreviewPart_WhenLevelAndPartProvided_ReturnsExpectedSpriteSlice()
		{
			// Arrange
			Picture sp257 = CreateSp257WithPreviewMarkers();
			var testee = new PreviewPalaceResourcesWrapper(name => name == "SP257" ? sp257 : null);

			// Act
			Picture actual = testee.GetPreviewPart(PreviewPalacePart.WallRight, level: 3, PalaceStyle.Medieval);

			// Assert
			Assert.Equal(7, actual.Width);
			Assert.Equal(14, actual.Height);
			AssertPictureFilled(actual, ExpectedMarker(3, PreviewPalacePart.WallRight));
		}

		[Fact]
		public void GetPreviewPart_WhenCalledTwice_ReturnsCachedInstance()
		{
			// Arrange
			Picture sp257 = CreateSp257WithPreviewMarkers();
			var testee = new PreviewPalaceResourcesWrapper(name => name == "SP257" ? sp257 : null);

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
		[Fact]
		public void RenderPalace_WhenNoPalacePartsActive_ReturnsOneByOneBitmap()
		{
			// Arrange
			Picture sp257 = CreateSp257WithPreviewMarkers();
			var resources = new PreviewPalaceResourcesWrapper(name => name == "SP257" ? sp257 : null);
			var testee = new PreviewPalaceRenderer(resources);
			var palace = new PalaceData();

			// Act
			IBitmap actual = testee.RenderPalace(palace);

			// Assert
			Assert.Equal(1, actual.Width());
			Assert.Equal(1, actual.Height());
		}

		[Fact]
		public void RenderPalace_WhenSomePartsActive_ReturnsBitmapWithExpectedSize()
		{
			// Arrange
			Picture sp257 = CreateSp257WithPreviewMarkers();
			var resources = new PreviewPalaceResourcesWrapper(name => name == "SP257" ? sp257 : null);
			var testee = new PreviewPalaceRenderer(resources);
			var palace = new PalaceData();
			palace.SetPalace(0, style: 1, level: 1);
			palace.SetPalace(3, style: 1, level: 2);
			palace.SetPalace(6, style: 1, level: 4);

			// Act
			IBitmap actual = testee.RenderPalace(palace);

			// Assert
			Assert.Equal(21, actual.Width());
			Assert.Equal(13, actual.Height());
		}

		[Fact]
		public void RenderPalace_WhenWallBuilt_AlwaysRendersAdjacentTower()
		{
			// Arrange
			Picture sp257 = CreateSp257WithPreviewMarkers();
			var resources = new PreviewPalaceResourcesWrapper(name => name == "SP257" ? sp257 : null);
			var testee = new PreviewPalaceRenderer(resources);
			var palace = new PalaceData();
			// Only center and one wall on each side — outer towers (0, 6) remain at level 0
			palace.SetPalace(1, style: 1, level: 2);
			palace.SetPalace(3, style: 1, level: 1);
			palace.SetPalace(5, style: 1, level: 3);

			// Act
			IBitmap actual = testee.RenderPalace(palace);

			// Assert: 5 parts (forced left tower + wall1 + center + wall5 + forced right tower)
			Assert.Equal(35, actual.Width());
			// Forced left tower borrows level/style from index 1
			AssertBlockFilled(actual, blockIndex: 0, expected: ExpectedMarker(2, PreviewPalacePart.Left));
			// Forced right tower borrows level/style from index 5
			AssertBlockFilled(actual, blockIndex: 4, expected: ExpectedMarker(3, PreviewPalacePart.Right));
		}

		[Fact]
		public void RenderPalace_WhenRenderingWallsAndCenter_UsesExpectedPartMapping()
		{
			// Arrange
			Picture sp257 = CreateSp257WithPreviewMarkers();
			var resources = new PreviewPalaceResourcesWrapper(name => name == "SP257" ? sp257 : null);
			var testee = new PreviewPalaceRenderer(resources);
			var palace = new PalaceData();
			palace.SetPalace(1, style: 1, level: 2); // WallLeft → also forces Left tower at index 0
			palace.SetPalace(2, style: 1, level: 4); // WallLeft
			palace.SetPalace(3, style: 1, level: 3); // Center
			palace.SetPalace(5, style: 1, level: 1); // WallRight → also forces Right tower at index 6

			// Act
			IBitmap actual = testee.RenderPalace(palace);

			// 6 parts: forced Left(lv2), WallLeft(lv2), WallLeft(lv4), Center(lv3), WallRight(lv1), forced Right(lv1)
			Assert.Equal(42, actual.Width());
			AssertBlockFilled(actual, blockIndex: 0, expected: ExpectedMarker(2, PreviewPalacePart.Left));
			AssertBlockFilled(actual, blockIndex: 1, expected: ExpectedMarker(2, PreviewPalacePart.WallLeft));
			AssertBlockFilled(actual, blockIndex: 2, expected: ExpectedMarker(4, PreviewPalacePart.WallLeft));
			AssertBlockFilled(actual, blockIndex: 3, expected: ExpectedMarker(3, PreviewPalacePart.Center));
			AssertBlockFilled(actual, blockIndex: 4, expected: ExpectedMarker(1, PreviewPalacePart.WallRight));
			AssertBlockFilled(actual, blockIndex: 5, expected: ExpectedMarker(1, PreviewPalacePart.Right));
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

		// All parts are rendered at baseOffset=-1, so the top sprite row is clipped and all 13
		// canvas rows are filled with the marker. No yStart parameter needed.
		private static void AssertBlockFilled(IBitmap bitmap, int blockIndex, byte expected)
		{
			int xStart = blockIndex * 7;
			for (int y = 0; y < bitmap.Height(); y++)
			{
				for (int x = xStart; x < xStart + 7; x++)
				{
					Assert.Equal(expected, bitmap.Bitmap[x, y]);
				}
			}
		}

		private static byte ExpectedMarker(int level, PreviewPalacePart part)
			=> (byte)((level * 10) + (int)part + 1);
	}
}
