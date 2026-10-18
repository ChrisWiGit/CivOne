namespace CivOne.Screens.Options
{
	/// <summary>
	/// Pixel metrics of the game options menu, derived from its entries.
	/// </summary>
	/// <param name="MenuBoxWidth">Width of the grey panel that surrounds the menu.</param>
	/// <param name="MenuBoxHeight">Height of the grey panel that surrounds the menu.</param>
	/// <param name="MenuWidth">Width of a menu row, which is also the width of the selection bar.</param>
	/// <param name="MenuOffsetX">Horizontal distance between the panel and the first menu row.</param>
	/// <param name="MenuOffsetY">Vertical distance between the panel and the first menu row.</param>
	/// <param name="FontHeight">Height of a single menu row.</param>
	internal sealed record GameOptionsMenuLayout(
		int MenuBoxWidth,
		int MenuBoxHeight,
		int MenuWidth,
		int MenuOffsetX,
		int MenuOffsetY,
		int FontHeight);
}
