using System;
using System.IO;

namespace CivOne.Graphics.ImageFormats
{
	/// <summary>
	/// Decodes image files into something the engine can draw.
	/// <br/>
	/// Image data reaches the game from two directions: as a file on disk, and as a stream out of a
	/// resource embedded in an assembly or packed into an archive. Both are first-class here, so a
	/// caller that loads images never has to know how to decode one.
	/// </summary>
	internal interface IImageDecoderService
	{
		/// <summary>
		/// Checks whether the data is in a format this service can read.
		/// </summary>
		/// <param name="data">The file contents, or at least its first few bytes.</param>
		/// <returns><see langword="true"/> when the data can be decoded.</returns>
		bool CanDecode(ReadOnlySpan<byte> data);

		/// <summary>
		/// Decodes image data that is already in memory.
		/// </summary>
		/// <param name="data">The file contents.</param>
		/// <returns>The decoded image.</returns>
		DecodedImage Decode(ReadOnlySpan<byte> data);

		/// <summary>
		/// Decodes image data from a stream, for example an embedded resource or an archive entry.
		/// The stream is read to its end but not disposed, so the caller keeps control over it.
		/// </summary>
		/// <param name="stream">The stream to read the image from.</param>
		/// <returns>The decoded image.</returns>
		DecodedImage Decode(Stream stream);

		/// <summary>
		/// Decodes an image file from disk.
		/// </summary>
		/// <param name="filePath">The path of the file to read.</param>
		/// <returns>The decoded image.</returns>
		DecodedImage DecodeFile(string filePath);

		/// <summary>
		/// Decodes a stream straight into an engine bitmap.
		/// <br/>
		/// Only works for images that carry their own palette, which is the case for indexed PNG
		/// files.
		/// </summary>
		/// <param name="stream">The stream to read the image from.</param>
		/// <returns>A new bitmap that the caller owns and has to dispose.</returns>
		IBitmap DecodeBitmap(Stream stream);

		/// <summary>
		/// Decodes a stream into an engine bitmap that uses the given palette.
		/// <br/>
		/// Works for any image: colours the palette does not contain are replaced by their closest
		/// match. Use this for images that were not authored against the palette they are drawn with.
		/// </summary>
		/// <param name="stream">The stream to read the image from.</param>
		/// <param name="targetPalette">The palette the result has to use.</param>
		/// <returns>A new bitmap that the caller owns and has to dispose.</returns>
		IBitmap DecodeBitmap(Stream stream, Palette targetPalette);
	}
}
