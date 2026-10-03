using System;
using System.IO;
using System.Reflection;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.IO;

namespace CivOne.Screens.StartupWizard
{
	/// <summary>
	/// Loads the program icon drawn in the wizard header and blits it onto a target bitmap.
	/// <br/>
	/// The icon ships as a 256 pixel RGBA image embedded into this assembly, so it is found no matter
	/// where the game is started from, and it stays sharp at every window scale. Scaling happens on
	/// the true-colour image and not on palette indices: every target pixel is the average of the
	/// source pixels it covers, which is what keeps the result smooth instead of blocky.
	/// <br/>
	/// The engine draws with palette indices and has no alpha channel, so the scaled image is
	/// composited onto the solid header colour first and only then mapped onto the game palette. That
	/// turns the soft edges of the icon into real antialiasing against the header instead of a hard
	/// cut-out.
	/// </summary>
	/// <param name="decoder">
	/// The decoder that reads the embedded icon.
	/// Left out, it is created on first use.
	/// </param>
	internal sealed class WizardHeaderIconDelegate(IImageDecoderService? decoder = null)
	{
		/// <summary>
		/// Logical name of the embedded icon, as declared in <c>CivOne.csproj</c>.
		/// </summary>
		private const string IconResourceName = "CivOne.Resources.WizardIcon.png";

		/// <summary>
		/// Source pixels below this alpha value contribute nothing and stay unpainted.
		/// </summary>
		private const int MinimumOpacity = 8;

		private readonly PaletteMapperDelegate _mapper = new();

		private IImageDecoderService? _decoder = decoder;

		/// <summary>
		/// The image decoder, resolved only when the icon is actually read.
		/// </summary>
		private IImageDecoderService Decoder => _decoder ??= ImageDecoderServiceFactory.Create();

		private DecodedImage? _source;
		private bool _sourceLoaded;

		private byte[]? _scaledIndices;
		private bool[]? _scaledPainted;
		private int _scaledWidth;
		private int _scaledHeight;
		private byte _scaledBackground;

		/// <summary>
		/// Gets the natural size of the icon in pixels, or <see langword="null"/> when it could not be
		/// loaded.
		/// </summary>
		/// <returns>The icon size, or <see langword="null"/>.</returns>
		public (int Width, int Height)? GetSize()
		{
			DecodedImage? source = GetSource();
			return source == null ? null : (source.Width, source.Height);
		}

		/// <summary>
		/// Draws the icon scaled into the given rectangle.
		/// <br/>
		/// The result is cached for as long as the size and the background colour stay the same, so a
		/// redraw of the header costs nothing but the copy.
		/// </summary>
		/// <param name="target">The bitmap to draw onto.</param>
		/// <param name="targetX">Left edge of the destination rectangle, in pixels.</param>
		/// <param name="targetY">Top edge of the destination rectangle, in pixels.</param>
		/// <param name="targetWidth">Width of the destination rectangle, in pixels.</param>
		/// <param name="targetHeight">Height of the destination rectangle, in pixels.</param>
		/// <param name="backgroundColour">Palette index of the surface the icon is drawn onto.</param>
		public void Draw(Bytemap target, int targetX, int targetY, int targetWidth, int targetHeight, byte backgroundColour)
		{
			ArgumentNullException.ThrowIfNull(target);

			if (targetWidth <= 0 || targetHeight <= 0)
			{
				return;
			}

			if (!TryGetScaled(targetWidth, targetHeight, backgroundColour, out byte[] indices, out bool[] painted))
			{
				return;
			}

			for (int y = 0; y < targetHeight; y++)
			{
				int py = targetY + y;
				if (py < 0 || py >= target.Height)
				{
					continue;
				}

				int row = y * targetWidth;
				for (int x = 0; x < targetWidth; x++)
				{
					int px = targetX + x;
					if (px < 0 || px >= target.Width || !painted[row + x])
					{
						continue;
					}

					target[px, py] = indices[row + x];
				}
			}
		}

		private bool TryGetScaled(int width, int height, byte backgroundColour, out byte[] indices, out bool[] painted)
		{
			if (_scaledIndices != null && _scaledPainted != null
				&& _scaledWidth == width && _scaledHeight == height && _scaledBackground == backgroundColour)
			{
				indices = _scaledIndices;
				painted = _scaledPainted;
				return true;
			}

			indices = [];
			painted = [];

			DecodedImage? source = GetSource();
			if (source == null)
			{
				return false;
			}

			Palette palette = Common.GetPalette256;
			Colour background = palette[backgroundColour];
			Colour[] scaled = Resample(source, width, height);
			painted = new bool[scaled.Length];

			for (int i = 0; i < scaled.Length; i++)
			{
				Colour colour = scaled[i];
				painted[i] = colour.A >= MinimumOpacity;
				scaled[i] = Blend(colour, background);
			}

			indices = _mapper.MapPixels(scaled, palette);

			_scaledIndices = indices;
			_scaledPainted = painted;
			_scaledWidth = width;
			_scaledHeight = height;
			_scaledBackground = backgroundColour;
			return true;
		}

		/// <summary>
		/// Scales the image by averaging, for every target pixel, the source pixels it covers.
		/// <br/>
		/// Colours are weighted by their own alpha, so a transparent source pixel does not pull the
		/// colour of an edge towards whatever happens to sit behind the icon in the source file.
		/// </summary>
		private static Colour[] Resample(DecodedImage source, int width, int height)
		{
			ReadOnlySpan<Colour> pixels = source.Pixels;
			Colour[] result = new Colour[width * height];

			for (int y = 0; y < height; y++)
			{
				int sourceTop = y * source.Height / height;
				int sourceBottom = Math.Max(sourceTop + 1, (y + 1) * source.Height / height);

				for (int x = 0; x < width; x++)
				{
					int sourceLeft = x * source.Width / width;
					int sourceRight = Math.Max(sourceLeft + 1, (x + 1) * source.Width / width);

					long red = 0;
					long green = 0;
					long blue = 0;
					long alpha = 0;
					int count = 0;

					for (int sy = sourceTop; sy < sourceBottom; sy++)
					{
						int row = sy * source.Width;
						for (int sx = sourceLeft; sx < sourceRight; sx++)
						{
							Colour colour = pixels[row + sx];
							red += colour.R * colour.A;
							green += colour.G * colour.A;
							blue += colour.B * colour.A;
							alpha += colour.A;
							count++;
						}
					}

					result[(y * width) + x] = alpha == 0
						? new Colour(0, 0, 0, 0)
						: new Colour((byte)(alpha / count), (byte)(red / alpha), (byte)(green / alpha), (byte)(blue / alpha));
				}
			}

			return result;
		}

		/// <summary>
		/// Composites a colour onto an opaque background.
		/// </summary>
		private static Colour Blend(Colour colour, Colour background)
		{
			if (colour.A >= byte.MaxValue)
			{
				return colour;
			}

			int alpha = colour.A;
			int inverse = byte.MaxValue - alpha;
			return new Colour(
				byte.MaxValue,
				(byte)(((colour.R * alpha) + (background.R * inverse)) / byte.MaxValue),
				(byte)(((colour.G * alpha) + (background.G * inverse)) / byte.MaxValue),
				(byte)(((colour.B * alpha) + (background.B * inverse)) / byte.MaxValue));
		}

		private DecodedImage? GetSource()
		{
			if (_sourceLoaded)
			{
				return _source;
			}

			_sourceLoaded = true;

			using Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResourceName);
			if (resource == null)
			{
				return null;
			}

			_source = Decoder.Decode(resource);
			return _source;
		}
	}
}
