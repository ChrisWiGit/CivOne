namespace CivOne.Graphics.ImageFormats
{
	/// <summary>
	/// Creates the image decoder for callers that have no injected one.
	/// </summary>
	internal static class ImageDecoderServiceFactory
	{
		/// <summary>
		/// Creates an image decoder.
		/// </summary>
		/// <returns>A new decoder service.</returns>
		public static IImageDecoderService Create() => new ImageDecoderService();
	}
}
