using System;
using System.IO;

namespace CivOne.Graphics.ImageFormats
{
	/// <summary>
	/// Decodes images by picking a decoder from the data itself.
	/// <br/>
	/// PNG is the only format handled here. The existing readers for the game's own formats
	/// (<see cref="GifFile"/>, <see cref="PicFile"/>) stay where they are; they can be folded in
	/// later without any caller noticing, which is the reason the format is chosen from the file
	/// contents rather than from a file extension.
	/// </summary>
	internal sealed class ImageDecoderService : IImageDecoderService
	{
		private readonly PngDecoderDelegate _png = new();
		private readonly DecodedImageToBitmapDelegate _toBitmap = new();

		/// <inheritdoc/>
		public bool CanDecode(ReadOnlySpan<byte> data) => _png.CanDecode(data);

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">The data is in a format this service cannot read.</exception>
		public DecodedImage Decode(ReadOnlySpan<byte> data)
		{
			if (!_png.CanDecode(data))
			{
				throw new NotSupportedException("The data is not in a supported image format.");
			}
			return _png.Decode(data);
		}

		/// <inheritdoc/>
		public DecodedImage Decode(Stream stream)
		{
			ArgumentNullException.ThrowIfNull(stream);

			using MemoryStream buffer = new();
			stream.CopyTo(buffer);
			return Decode(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
		}

		/// <inheritdoc/>
		public IBitmap DecodeBitmap(Stream stream) => _toBitmap.ToBitmap(Decode(stream));

		/// <inheritdoc/>
		public IBitmap DecodeBitmap(Stream stream, Palette targetPalette) => _toBitmap.ToBitmap(Decode(stream), targetPalette);
	}
}
