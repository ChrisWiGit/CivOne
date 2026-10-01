using System.Collections.Generic;

namespace CivOne.Services.Fonts
{
	/// <summary>
	/// Provides read-only metadata about the bitmap fonts loaded from <c>FONTS.CV</c>.
	/// <br/>
	/// Used by debug tooling to find out which font contains which glyph, for example which font maps
	/// <c>$</c> to a currency symbol instead of a dollar sign.
	/// </summary>
	internal interface IFontInspectionService
	{
		/// <summary>
		/// Gets the number of fonts loaded from the data file.
		/// Zero when no font file was found and the built-in fallback font is used.
		/// </summary>
		int FontCount { get; }

		/// <summary>
		/// Returns the metadata of a single font.
		/// </summary>
		/// <param name="fontId">Zero-based font index.</param>
		/// <returns>
		/// The font metadata, or <see langword="null"/> when <paramref name="fontId"/> is out of range.
		/// </returns>
		FontDescription? GetFont(int fontId);

		/// <summary>
		/// Returns the ASCII codes stored in a font, in ascending order.
		/// </summary>
		/// <param name="fontId">Zero-based font index.</param>
		/// <returns>The stored character codes; empty when the font does not exist.</returns>
		IReadOnlyList<byte> GetCharacterCodes(int fontId);

		/// <summary>
		/// Determines whether a character actually has visible pixels in a font.
		/// <br/>
		/// Characters inside the stored range can still be blank, which is useful to know when hunting
		/// for special glyphs.
		/// </summary>
		/// <param name="fontId">Zero-based font index.</param>
		/// <param name="character">Character to test.</param>
		/// <returns><see langword="true"/> when the glyph contains at least one set pixel.</returns>
		bool HasVisibleGlyph(int fontId, char character);
	}
}
