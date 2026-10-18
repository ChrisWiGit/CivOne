namespace CivOne.Services.Fonts
{
	/// <summary>
	/// Describes a single bitmap font loaded from the <c>FONTS.CV</c> data file.
	/// </summary>
	/// <param name="FontId">Zero-based font index, in the order the fonts appear in the file.</param>
	/// <param name="Height">Glyph height in pixels.</param>
	/// <param name="FirstChar">Lowest ASCII code stored in the font.</param>
	/// <param name="LastChar">Highest ASCII code stored in the font.</param>
	/// <param name="IsInternational">
	/// <see langword="true"/> when the font can render characters beyond the stored ASCII range,
	/// either from glyphs in the file or by simulating them.
	/// </param>
	internal sealed record FontDescription(int FontId, int Height, byte FirstChar, byte LastChar, bool IsInternational);
}
