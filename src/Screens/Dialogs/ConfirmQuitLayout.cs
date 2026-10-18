using System.Collections.Generic;

namespace CivOne.Screens.Dialogs
{
	/// <summary>
	/// Pixel metrics of the quit confirmation dialog, derived from its translated texts.
	/// </summary>
	/// <param name="DialogLeft">Left position of the dialog in the 320x200 coordinate space.</param>
	/// <param name="DialogTop">Top position of the dialog in the 320x200 coordinate space.</param>
	/// <param name="Width">Width of the dialog box, without the black border.</param>
	/// <param name="Height">Height of the dialog box, without the black border.</param>
	/// <param name="TextIndent">Horizontal position of the question text inside the dialog.</param>
	/// <param name="TextTop">Vertical position of the first question line inside the dialog.</param>
	/// <param name="MenuOffsetX">Horizontal distance between the dialog and the menu rows.</param>
	/// <param name="MenuTop">Vertical position of the first menu row inside the dialog.</param>
	/// <param name="MenuWidth">Width of a menu row, which is also the width of the selection bar.</param>
	/// <param name="MenuHeight">Height of all menu rows together.</param>
	/// <param name="FontHeight">Height of a single text line.</param>
	/// <param name="QuestionLines">Translated question, already split into lines.</param>
	/// <param name="MenuItems">Translated menu entry texts.</param>
	internal sealed record ConfirmQuitLayout(
		int DialogLeft,
		int DialogTop,
		int Width,
		int Height,
		int TextIndent,
		int TextTop,
		int MenuOffsetX,
		int MenuTop,
		int MenuWidth,
		int MenuHeight,
		int FontHeight,
		IReadOnlyList<string> QuestionLines,
		IReadOnlyList<string> MenuItems);
}
