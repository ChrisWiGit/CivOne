namespace CivOne.Graphics.ImageFormats
{
	/// <summary>
	/// Provides the image decoder for callers that cannot take it through constructor injection yet.
	/// </summary>
	internal static class ImageDecoderServiceFactory
	{
		private static IImageDecoderService? _decoder;

		/// <summary>
		/// Gets the shared image decoder, created on first use.
		/// </summary>
		public static IImageDecoderService Decoder => _decoder ??= new ImageDecoderService();
	}
}
