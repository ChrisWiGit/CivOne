using System.Diagnostics.CodeAnalysis;

namespace CivOne.Graphics
{
	/// <summary>
	/// Expands a palette to a fixed number of entries by repeating its colours.
	/// <br/>
	/// A screen rendered in <see cref="Enums.GraphicsMode.Graphics16"/> mode exposes a 16-entry
	/// palette, but the layers stacked behind it (icons, panels, leader portraits, ...) always use
	/// pixel values that index a 256-entry palette, regardless of the current graphics mode. Using
	/// the 16-entry palette directly as the render palette for the whole layer stack throws an
	/// <see cref="System.IndexOutOfRangeException"/> as soon as one of those layers references an
	/// index of 16 or higher. Repeating the short palette mirrors how <c>Common.GetPalette256</c>
	/// already derives its fallback entries from the 16-colour palette.
	/// </summary>
	internal sealed class PaletteExpansionDelegate
	{
		/// <summary>
		/// Returns <paramref name="palette"/> unchanged if it already has at least <paramref name="length"/>
		/// entries, otherwise returns a new palette of that length with the source colours repeated.
		/// </summary>
		/// <param name="palette">The palette to expand.</param>
		/// <param name="length">The minimum number of entries the returned palette must have.</param>
		/// <returns>A palette with at least <paramref name="length"/> entries.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public Palette Expand(Palette palette, int length = 256)
		{
			if (palette.Length >= length)
			{
				return palette;
			}

			Palette expanded = new(length);
			for (int i = 0; i < length; i++)
			{
				expanded[i] = palette[i % palette.Length];
			}
			return expanded;
		}
	}
}
