using System.Drawing;

namespace CivOne
{
	/// <summary>
	/// Describes one predefined map size offered by a map generator.
	/// </summary>
	/// <param name="Label">
	/// Gets the user-facing display label shown in the map size menu (e.g. "Normal (80x50)").
	/// </param>
	/// <param name="Width">
	/// Gets the map width in tiles.
	/// </param>
	/// <param name="Height">
	/// Gets the map height in tiles.
	/// </param>
	public sealed record MapSizePreset(string Label, int Width, int Height)
	{
		/// <summary>
		/// Returns the size as a <see cref="Size"/> value.
		/// </summary>
		public Size ToSize() => new(Width, Height);
	}
}
