using System;

namespace CivOne.Graphics.ImageFormats
{
	/// <summary>
	/// The result of decoding an image file, independent of the file format it came from.
	/// <br/>
	/// This type holds managed arrays only and is deliberately <b>not</b> <see cref="IDisposable"/>.
	/// The engine's <see cref="IBitmap"/> owns unmanaged memory, so it is created separately by
	/// <see cref="DecodedImageToBitmapDelegate"/> and disposed by whoever asked for it.
	/// </summary>
	internal sealed class DecodedImage
	{
		private readonly Colour[] _palette;
		private readonly byte[] _indices;
		private Colour[]? _pixels;

		/// <summary>The image width in pixels.</summary>
		public int Width { get; }

		/// <summary>The image height in pixels.</summary>
		public int Height { get; }

		/// <summary>
		/// <see langword="true"/> when the source file carried its own palette, which is the case for
		/// PNG colour type 3. Such an image converts to an <see cref="IBitmap"/> without any loss.
		/// </summary>
		public bool IsIndexed { get; }

		/// <summary>
		/// The palette the source file carried, or an empty span when <see cref="IsIndexed"/> is
		/// <see langword="false"/>.
		/// </summary>
		public ReadOnlySpan<Colour> SourcePalette => _palette;

		/// <summary>
		/// One palette index per pixel in row-major order, or an empty span when
		/// <see cref="IsIndexed"/> is <see langword="false"/>.
		/// </summary>
		public ReadOnlySpan<byte> Indices => _indices;

		/// <summary>
		/// The image as one <see cref="Colour"/> per pixel in row-major order.
		/// <br/>
		/// For an indexed image this view is built on first access and then cached, so an image that
		/// is only ever used through <see cref="Indices"/> never pays for it.
		/// </summary>
		public ReadOnlySpan<Colour> Pixels => _pixels ??= BuildPixels();

		private DecodedImage(int width, int height, bool isIndexed, Colour[] palette, byte[] indices, Colour[]? pixels)
		{
			Width = width;
			Height = height;
			IsIndexed = isIndexed;
			_palette = palette;
			_indices = indices;
			_pixels = pixels;
		}

		/// <summary>
		/// Creates an image that carries its own palette.
		/// </summary>
		/// <param name="width">The image width in pixels.</param>
		/// <param name="height">The image height in pixels.</param>
		/// <param name="palette">The palette, at most 256 entries.</param>
		/// <param name="indices">One palette index per pixel, row-major.</param>
		/// <returns>The decoded image.</returns>
		public static DecodedImage Indexed(int width, int height, Colour[] palette, byte[] indices)
		{
			ArgumentNullException.ThrowIfNull(palette);
			ArgumentNullException.ThrowIfNull(indices);

			return new DecodedImage(width, height, true, palette, indices, null);
		}

		/// <summary>
		/// Creates an image that has no palette of its own.
		/// </summary>
		/// <param name="width">The image width in pixels.</param>
		/// <param name="height">The image height in pixels.</param>
		/// <param name="pixels">One colour per pixel, row-major.</param>
		/// <returns>The decoded image.</returns>
		public static DecodedImage TrueColour(int width, int height, Colour[] pixels)
		{
			ArgumentNullException.ThrowIfNull(pixels);

			return new DecodedImage(width, height, false, [], [], pixels);
		}

		private Colour[] BuildPixels()
		{
			Colour[] pixels = new Colour[_indices.Length];
			for (int i = 0; i < _indices.Length; i++)
			{
				byte index = _indices[i];
				pixels[i] = index < _palette.Length ? _palette[index] : Colour.Black;
			}
			return pixels;
		}
	}
}
