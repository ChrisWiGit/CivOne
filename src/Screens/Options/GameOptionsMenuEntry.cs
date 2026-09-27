using CivOne.Events;

namespace CivOne.Screens.Options
{
	/// <summary>
	/// Describes a single entry of the game options menu.
	/// </summary>
	/// <param name="Text">Text shown in the menu, including the leading check mark or space.</param>
	/// <param name="OnSelect">Action invoked when the entry is selected.</param>
	/// <param name="Enabled">Whether the entry can be selected.</param>
	internal sealed record GameOptionsMenuEntry(string Text, MenuItemEventAction<int> OnSelect, bool Enabled = true);
}
