using System.Drawing;

namespace CivOne.Graphics
{
	/// <summary>
	/// Provides the rendered size of a text for a given font.
	/// Used to lay out UI elements based on their actual text width instead of hard-coded pixel values.
	/// </summary>
	public interface IResourceTextSizeProvider
	{
		/// <summary>
		/// Measures the pixel size the given text occupies when rendered with the given font.
		/// </summary>
		/// <param name="font">Font identifier.</param>
		/// <param name="text">Text to measure.</param>
		/// <returns>Width and height of the rendered text in pixels.</returns>
		Size GetTextSize(int font, string text);
	}
}
