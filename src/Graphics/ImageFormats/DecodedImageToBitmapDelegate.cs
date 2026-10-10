using System;
using System.Diagnostics.CodeAnalysis;
using CivOne.IO;

namespace CivOne.Graphics.ImageFormats
{
	/// <summary>
	/// Turns a <see cref="DecodedImage"/> into an engine bitmap.
	/// <br/>
	/// The engine stores pictures as palette indices, so an image that already carries its own
	/// palette converts directly and without any loss. A true-colour image first needs a decision
	/// about which palette it should use, which this class does not make.
	/// </summary>
	internal sealed class DecodedImageToBitmapDelegate
	{
		private const int PaletteLength = 256;

		private readonly PaletteMapperDelegate _mapper = new();

		/// <summary>
		/// Converts an image that carries its own palette.
		/// </summary>
		/// <param name="image">The decoded image.</param>
		/// <returns>A new bitmap that the caller owns and has to dispose.</returns>
		/// <exception cref="NotSupportedException">The image has no palette of its own.</exception>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public IBitmap ToBitmap(DecodedImage image)
		{
			ArgumentNullException.ThrowIfNull(image);

			if (!image.IsIndexed)
			{
				throw new NotSupportedException("Only images that carry their own palette can be converted; a true-colour image needs a target palette.");
			}

			// The engine addresses a full 256 entry palette, so short palettes are padded.
			Colour[] colours = new Colour[PaletteLength];
			ReadOnlySpan<Colour> source = image.SourcePalette;
			for (int i = 0; i < PaletteLength; i++)
			{
				colours[i] = i < source.Length ? source[i] : Colour.Black;
			}

			// Picture copies both arguments, so the temporaries are released right away.
			using Palette palette = Palette.ToPalette(colours);
			return CreatePicture(image, image.Indices.ToArray(), palette);
		}

		/// <summary>
		/// Converts an image onto a palette it does not use itself, by replacing every colour with
		/// the closest entry of that palette.
		/// <br/>
		/// This is the lossy conversion and the one plugin artwork needs, because art drawn in an
		/// image editor does not come in the game's palette. An image that carries its own palette is
		/// translated through a lookup table instead of pixel by pixel.
		/// </summary>
		/// <param name="image">The decoded image.</param>
		/// <param name="targetPalette">The palette the result has to use.</param>
		/// <returns>A new bitmap that the caller owns and has to dispose.</returns>
		public IBitmap ToBitmap(DecodedImage image, Palette targetPalette)
		{
			ArgumentNullException.ThrowIfNull(image);
			ArgumentNullException.ThrowIfNull(targetPalette);

			byte[] indices;
			if (image.IsIndexed)
			{
				byte[] table = _mapper.MapPalette(image.SourcePalette, targetPalette);
				ReadOnlySpan<byte> source = image.Indices;
				indices = new byte[source.Length];
				for (int i = 0; i < source.Length; i++)
				{
					indices[i] = source[i] < table.Length ? table[source[i]] : (byte)0;
				}
			}
			else
			{
				indices = _mapper.MapPixels(image.Pixels, targetPalette);
			}

			return CreatePicture(image, indices, targetPalette);
		}

		private static Picture CreatePicture(DecodedImage image, byte[] indices, Palette palette)
		{
			using Bytemap bitmap = new Bytemap(image.Width, image.Height).FromByteArray(indices);
			return new Picture(bitmap, palette);
		}
	}
}
