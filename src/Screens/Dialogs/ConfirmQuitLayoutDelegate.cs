using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Graphics;

namespace CivOne.Screens.Dialogs
{
	/// <summary>
	/// Calculates the size and the inner positions of the quit confirmation dialog from its texts.
	/// The width follows the longest question line or menu entry, so translations longer than the
	/// English original still fit without adjusting any pixel constant.
	/// </summary>
	internal sealed class ConfirmQuitLayoutDelegate
	{
		/// <summary>Horizontal position of the question text inside the dialog.</summary>
		private const int TextIndent = 5;

		/// <summary>Vertical position of the first question line inside the dialog.</summary>
		private const int TextTop = 5;

		/// <summary>Horizontal distance between the dialog and the menu rows.</summary>
		private const int MenuOffsetX = 3;

		/// <summary>Text indent inside a menu row, matching the default menu indent.</summary>
		private const int MenuItemIndent = 8;

		/// <summary>Free space between the longest menu entry text and the right edge of the selection bar.</summary>
		private const int TrailingPadding = 4;

		/// <summary>Free space below the last menu row.</summary>
		private const int BottomPadding = 3;

		/// <summary>Horizontal center the dialog keeps, taken from the classic fixed layout.</summary>
		private const int CenterX = 152;

		/// <summary>Vertical center the dialog keeps, taken from the classic fixed layout.</summary>
		private const int CenterY = 99;

		/// <summary>Lower bound for the dialog width, so short translations keep the classic dialog size.</summary>
		private const int MinimumWidth = 104;

		private readonly IResourceTextSizeProvider? _textSizeProvider;
		private readonly IResourceFontHeightProvider? _fontHeightProvider;

		private IResourceTextSizeProvider TextSizeProvider => _textSizeProvider ?? Resources.Instance;
		private IResourceFontHeightProvider FontHeightProvider => _fontHeightProvider ?? Resources.Instance;

		/// <summary>
		/// Creates the layout calculation.
		/// </summary>
		/// <param name="textSizeProvider">Text measurement source, resolved from <see cref="Resources"/> when omitted.</param>
		/// <param name="fontHeightProvider">Font height source, resolved from <see cref="Resources"/> when omitted.</param>
		public ConfirmQuitLayoutDelegate(IResourceTextSizeProvider? textSizeProvider = null, IResourceFontHeightProvider? fontHeightProvider = null)
		{
			_textSizeProvider = textSizeProvider;
			_fontHeightProvider = fontHeightProvider;
		}

		/// <summary>
		/// Calculates the dialog metrics for the given question and menu entries.
		/// </summary>
		/// <param name="fontId">Font used for the question and the menu entries.</param>
		/// <param name="questionLines">Translated question, already split into lines.</param>
		/// <param name="menuItems">Translated menu entry texts.</param>
		/// <returns>The calculated dialog metrics.</returns>
		public ConfirmQuitLayout Calculate(int fontId, IReadOnlyList<string> questionLines, IReadOnlyList<string> menuItems)
		{
			ArgumentNullException.ThrowIfNull(questionLines);
			ArgumentNullException.ThrowIfNull(menuItems);

			int fontHeight = FontHeightProvider.GetFontHeight(fontId);
			int widestQuestionLine = questionLines.Count == 0 ? 0 : questionLines.Max(line => TextWidth(fontId, line));
			int widestMenuItem = menuItems.Count == 0 ? 0 : menuItems.Max(text => TextWidth(fontId, text));

			int questionWidth = widestQuestionLine + (2 * TextIndent);
			int menuWidth = widestMenuItem + MenuItemIndent + TrailingPadding;
			int width = Math.Max(MinimumWidth, Math.Max(questionWidth, menuWidth + (2 * MenuOffsetX)));

			int menuTop = TextTop + (questionLines.Count * fontHeight) - 1;
			int menuHeight = menuItems.Count * fontHeight;
			int height = menuTop + menuHeight + BottomPadding;

			return new ConfirmQuitLayout(
				CenterX - (width / 2),
				CenterY - (height / 2),
				width,
				height,
				TextIndent,
				TextTop,
				MenuOffsetX,
				menuTop,
				width - (2 * MenuOffsetX) + 2,
				menuHeight,
				fontHeight,
				questionLines,
				menuItems);
		}

		private int TextWidth(int fontId, string text) => TextSizeProvider.GetTextSize(fontId, text ?? string.Empty).Width;
	}
}
