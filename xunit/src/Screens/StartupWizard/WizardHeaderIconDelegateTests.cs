using System;
using System.IO;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;
using CivOne.Screens.StartupWizard;
using Xunit;

namespace CivOne.UnitTests.Screens.StartupWizard
{
	/// <summary>
	/// Verifies how <see cref="WizardHeaderIconDelegate"/> scales the header icon, keeps transparent
	/// pixels unpainted, clips at the edges of the target and caches its result.
	/// <br/>
	/// The image decoder is injected, so these tests run on small synthetic images instead of the
	/// embedded icon and stay independent of its contents.
	/// </summary>
	public class WizardHeaderIconDelegateTests
	{
		private const byte Sentinel = 200;
		private const byte HeaderBackground = 1;

		/// <summary>
		/// A decoder that always returns the same image and counts how often it was asked.
		/// </summary>
		private sealed class StubImageDecoderService(DecodedImage image) : IImageDecoderService
		{
			public int DecodeCalls { get; private set; }

			public bool CanDecode(ReadOnlySpan<byte> data) => true;

			public DecodedImage Decode(ReadOnlySpan<byte> data) => Decode(Stream.Null);

			public DecodedImage Decode(Stream stream)
			{
				DecodeCalls++;
				return image;
			}

			public DecodedImage DecodeFile(string filePath) => Decode(Stream.Null);

			public IBitmap DecodeBitmap(Stream stream) => throw new NotSupportedException();

			public IBitmap DecodeBitmap(Stream stream, Palette targetPalette) => throw new NotSupportedException();
		}

		/// <summary>
		/// Builds a 2x2 image whose diagonal is opaque red and whose other two pixels are fully
		/// transparent.
		/// </summary>
		private static DecodedImage CreateDiagonalImage()
		{
			Colour opaque = new(255, 200, 0, 0);
			Colour transparent = new(0, 0, 0, 0);
			return DecodedImage.TrueColour(2, 2, [opaque, transparent, transparent, opaque]);
		}

		private static Bytemap CreateTarget(int width, int height)
		{
			Bytemap target = new(width, height);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					target[x, y] = Sentinel;
				}
			}
			return target;
		}

		[Fact]
		public void GetSizeReturnsTheSizeOfTheDecodedImage()
		{
			StubImageDecoderService decoder = new(DecodedImage.TrueColour(16, 8, new Colour[16 * 8]));
			WizardHeaderIconDelegate testee = new(decoder);

			(int Width, int Height)? result = testee.GetSize();

			Assert.Equal((16, 8), result);
		}

		[Fact]
		public void DrawLeavesTransparentPixelsUntouched()
		{
			StubImageDecoderService decoder = new(CreateDiagonalImage());
			WizardHeaderIconDelegate testee = new(decoder);
			using Bytemap target = CreateTarget(4, 4);

			// At the natural size every target pixel covers exactly one source pixel.
			testee.Draw(target, 1, 1, 2, 2, HeaderBackground);

			Assert.NotEqual(Sentinel, target[1, 1]);
			Assert.NotEqual(Sentinel, target[2, 2]);
			Assert.Equal(Sentinel, target[2, 1]);
			Assert.Equal(Sentinel, target[1, 2]);
		}

		[Fact]
		public void DrawClipsAtTheTopLeftCornerOfTheTarget()
		{
			StubImageDecoderService decoder = new(CreateDiagonalImage());
			WizardHeaderIconDelegate testee = new(decoder);
			using Bytemap target = CreateTarget(4, 4);

			// Only the lower right source pixel lands inside the target, the rest is clipped away.
			testee.Draw(target, -1, -1, 2, 2, HeaderBackground);

			Assert.NotEqual(Sentinel, target[0, 0]);
			Assert.Equal(Sentinel, target[1, 0]);
			Assert.Equal(Sentinel, target[0, 1]);
		}

		[Fact]
		public void DrawClipsAtTheBottomRightCornerOfTheTarget()
		{
			StubImageDecoderService decoder = new(CreateDiagonalImage());
			WizardHeaderIconDelegate testee = new(decoder);
			using Bytemap target = CreateTarget(4, 4);

			testee.Draw(target, 3, 3, 2, 2, HeaderBackground);

			Assert.NotEqual(Sentinel, target[3, 3]);
		}

		[Fact]
		public void DrawScalesTheImageUp()
		{
			StubImageDecoderService decoder = new(CreateDiagonalImage());
			WizardHeaderIconDelegate testee = new(decoder);
			using Bytemap target = CreateTarget(4, 4);

			// Doubled, each source pixel covers a 2x2 block, so the transparent blocks stay unpainted.
			testee.Draw(target, 0, 0, 4, 4, HeaderBackground);

			Assert.NotEqual(Sentinel, target[0, 0]);
			Assert.NotEqual(Sentinel, target[1, 1]);
			Assert.NotEqual(Sentinel, target[3, 3]);
			Assert.Equal(Sentinel, target[2, 0]);
			Assert.Equal(Sentinel, target[0, 2]);
		}

		[Fact]
		public void DrawBlendsPartlyTransparentPixelsTowardsTheBackground()
		{
			// One opaque black and one fully transparent pixel average to half transparent black,
			// which has to end up closer to the light background than the opaque pixel does.
			Colour[] pixels = [new Colour(255, 0, 0, 0), new Colour(0, 0, 0, 0)];
			StubImageDecoderService decoder = new(DecodedImage.TrueColour(2, 1, pixels));
			WizardHeaderIconDelegate testee = new(decoder);
			using Bytemap target = CreateTarget(2, 1);

			testee.Draw(target, 0, 0, 1, 1, 15);
			byte blended = target[0, 0];

			using Bytemap opaqueTarget = CreateTarget(2, 1);
			StubImageDecoderService opaqueDecoder = new(DecodedImage.TrueColour(1, 1, [new Colour(255, 0, 0, 0)]));
			WizardHeaderIconDelegate opaqueTestee = new(opaqueDecoder);
			opaqueTestee.Draw(opaqueTarget, 0, 0, 1, 1, 15);

			Assert.NotEqual(Sentinel, blended);
			Assert.NotEqual(opaqueTarget[0, 0], blended);
		}

		[Fact]
		public void DrawDoesNothingForAnEmptyRectangle()
		{
			StubImageDecoderService decoder = new(CreateDiagonalImage());
			WizardHeaderIconDelegate testee = new(decoder);
			using Bytemap target = CreateTarget(2, 2);

			testee.Draw(target, 0, 0, 0, 2, HeaderBackground);

			Assert.Equal(Sentinel, target[0, 0]);
			Assert.Equal(0, decoder.DecodeCalls);
		}

		[Fact]
		public void DrawDecodesTheImageOnlyOnce()
		{
			StubImageDecoderService decoder = new(CreateDiagonalImage());
			WizardHeaderIconDelegate testee = new(decoder);
			using Bytemap target = CreateTarget(4, 4);

			testee.GetSize();
			testee.Draw(target, 0, 0, 2, 2, HeaderBackground);
			testee.Draw(target, 0, 0, 4, 4, HeaderBackground);

			Assert.Equal(1, decoder.DecodeCalls);
		}

		[Fact]
		public void DrawRejectsMissingTarget()
		{
			StubImageDecoderService decoder = new(CreateDiagonalImage());
			WizardHeaderIconDelegate testee = new(decoder);

			Assert.Throws<ArgumentNullException>(() => testee.Draw(null!, 0, 0, 2, 2, HeaderBackground));
		}
	}
}
