using CivOne.Graphics;

namespace CivOne.Screens.PalaceAssets
{
	/// <summary>
	/// Draws the mini palace of the side bar and the Historian.
	/// </summary>
	public interface IPreviewPalaceRenderer
	{
		/// <summary>
		/// Draws the mini palace.
		/// </summary>
		/// <param name="palace">The 9 slot layout.</param>
		/// <returns>
		/// A bitmap of <see cref="MiniPalaceLayout.Width"/> pixels, or 1×1 when no slot is drawn.
		/// </returns>
		IBitmap RenderPalace(MiniPalaceLayout palace);

		/// <summary>
		/// Gets the visible height of the tallest drawn part.
		/// </summary>
		/// <param name="palace">The 9 slot layout.</param>
		/// <returns>The height in pixels; 0 when no slot is drawn.</returns>
		int GetMaxPalaceHeight(MiniPalaceLayout palace);
	}
}
