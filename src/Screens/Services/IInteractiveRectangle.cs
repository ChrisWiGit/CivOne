using System.Drawing;

namespace CivOne.Screens.Services
{
	/// <summary>
	/// An interactive button with Methods to draw and check for mouse interaction.
	/// It also comes with a rectangle defining its bounds to show the hitbox.
	/// In this way a button can easily be drawn and checked for mouse interaction.
	/// Debugging can be done by drawing the hitbox rectangle on the associated bitmap.
	/// <code>
	/// // Example of creating and using an InteractiveButton
	/// IInteractiveButton button = InteractiveButtonImpl.Build(bitmap, new Rectangle(10, 10, 100, 30));
	/// button.DrawButton("Click Me", 1, 5, 8);
	/// if (button.Contains(new Point(50, 20)))
	/// {
	///     // Handle button click
	/// }
	/// button.DrawRectangle(10); // Optional: Draw the button's hitbox
	/// </code>
	/// </summary>
	public interface IInteractiveButton
	{
		/// <summary>
		/// Check if a point is within the bounds of this button.
		/// </summary>
		/// <param name="p">The point to check.</param>
		/// <returns>True if the point is within the bounds; otherwise, false.</returns>
		bool Contains(Point p);

		/// <summary>
		/// Draw the outline of the button's bounds on the associated bitmap.
		/// </summary>
		/// <param name="color">The color to use for the outline.</param>
		/// <returns></returns>
		void DrawRectangle(byte color);


		/// <summary>
		/// Draw the button with specified text and colors.
		/// </summary>
		/// <param name="text">The text to display on the button.</param>
		/// <param name="fontId">The font ID to use for the text.</
		/// <param name="colour">The main color of the button.</param>
		/// <param name="colourDark">The darker color for the button's border.</param>
		/// <returns></returns>
		void DrawButton(string text, byte fontId, byte colour, byte colourDark);


		/// <summary>
		/// The rectangle defining the bounds of the button.
		/// </summary>
		Rectangle Bounds { get; }
	}
}