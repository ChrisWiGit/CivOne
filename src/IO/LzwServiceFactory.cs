namespace CivOne.IO
{
	/// <summary>
	/// Provides the active LZW codec implementation selected by user settings.
	/// </summary>
	public static class LzwServiceFactory
	{
		private static readonly ILzwCodec OriginalCodec = new LZW();
		private static readonly ILzwCodec CorrectedCodec = new LZWCorrected();

		/// <summary>
		/// Gets the configured LZW codec implementation.
		/// </summary>
		/// <returns>
		/// Returns the original or corrected codec, depending on <see cref="Settings.LzwCodecMode"/>.
		/// </returns>
		public static ILzwCodec Codec => Settings.Instance.LzwCodecMode == Settings.LzwCodecType.Corrected
			? CorrectedCodec
			: OriginalCodec;
	}
}