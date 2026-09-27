using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Graphics;

namespace CivOne.Screens.Options
{
	/// <summary>
	/// Calculates the size of the game options menu from its entries.
	/// The width follows the longest entry text, the height the number of entries, so that entries
	/// can be added or removed without adjusting any pixel constant.
	/// </summary>
	internal sealed class GameOptionsMenuLayoutDelegate
	{
		/// <summary>Horizontal distance between the grey panel and the first menu row.</summary>
		private const int MenuOffsetX = 2;

		/// <summary>Vertical space above the first menu row, occupied by the panel title.</summary>
		private const int MenuOffsetY = 11;

		/// <summary>Free space below the last menu row.</summary>
		private const int BottomPadding = 5;

		/// <summary>Free space between the longest entry text and the right edge of the selection bar.</summary>
		private const int TrailingPadding = 6;

		/// <summary>Space the panel adds around the selection bar.</summary>
		private const int BorderWidth = 4;

		/// <summary>Horizontal position of the panel title.</summary>
		private const int TitleIndent = 4;

		/// <summary>Lower bound for the row width, so short translations keep the classic panel size.</summary>
		private const int MinimumMenuWidth = 99;

		private readonly IResourceTextSizeProvider? _textSizeProvider;
		private readonly IResourceFontHeightProvider? _fontHeightProvider;

		private IResourceTextSizeProvider TextSizeProvider => _textSizeProvider ?? Resources.Instance;
		private IResourceFontHeightProvider FontHeightProvider => _fontHeightProvider ?? Resources.Instance;

		/// <summary>
		/// Creates the layout calculation.
		/// </summary>
		/// <param name="textSizeProvider">Text measurement source, resolved from <see cref="Resources"/> when omitted.</param>
		/// <param name="fontHeightProvider">Font height source, resolved from <see cref="Resources"/> when omitted.</param>
		public GameOptionsMenuLayoutDelegate(IResourceTextSizeProvider? textSizeProvider = null, IResourceFontHeightProvider? fontHeightProvider = null)
		{
			_textSizeProvider = textSizeProvider;
			_fontHeightProvider = fontHeightProvider;
		}

		/// <summary>
		/// Calculates the menu metrics for the given title and entry texts.
		/// </summary>
		/// <param name="fontId">Font used for the title and the entries.</param>
		/// <param name="indent">Horizontal text indent inside a menu row.</param>
		/// <param name="title">Panel title, for example "Options:".</param>
		/// <param name="itemTexts">Texts of all menu entries.</param>
		/// <returns>The calculated menu metrics.</returns>
		public GameOptionsMenuLayout Calculate(int fontId, int indent, string title, IReadOnlyList<string> itemTexts)
		{
			ArgumentNullException.ThrowIfNull(title);
			ArgumentNullException.ThrowIfNull(itemTexts);

			int fontHeight = FontHeightProvider.GetFontHeight(fontId);
			int widestItem = itemTexts.Count == 0 ? 0 : itemTexts.Max(text => TextWidth(fontId, text));

			int menuWidth = Math.Max(MinimumMenuWidth, widestItem + indent + TrailingPadding);
			int titleWidth = TextWidth(fontId, title) + (2 * TitleIndent);
			int menuBoxWidth = Math.Max(menuWidth + BorderWidth, titleWidth);
			int menuBoxHeight = MenuOffsetY + (itemTexts.Count * fontHeight) + BottomPadding;

			return new GameOptionsMenuLayout(menuBoxWidth, menuBoxHeight, menuWidth, MenuOffsetX, MenuOffsetY, fontHeight);
		}

		private int TextWidth(int fontId, string text) => TextSizeProvider.GetTextSize(fontId, text ?? string.Empty).Width;
	}
}
