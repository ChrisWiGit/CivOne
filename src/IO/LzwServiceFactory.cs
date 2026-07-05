// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
