using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Services.Browser;
using CivOne.Services.Fonts;

namespace CivOne.Screens.Debug
{
	/// <summary>
	/// Debug screen showing the real bitmap fonts stored in <c>FONTS.CV</c>.
	/// <br/>
	/// The screen lists every character code of the selected font, renders typed text in that font and
	/// reports the ASCII code of the last key.
	/// This makes it possible to find out which font maps a given code to a special glyph, for example
	/// <c>$</c> to a currency symbol.
	/// <br/>
	/// The screen requests the full window as its canvas, because the large fonts in <c>FONTS.CV</c>
	/// need more than 320x200 pixels to show all of their characters at original size.
	/// It is modal so that the enlarged canvas does not reach the screens below, whose layout (the game
	/// map in particular) is only validated up to <see cref="Settings.MaxScreenWidth"/> x
	/// <see cref="Settings.MaxScreenHeight"/>.
	/// </summary>
	[Modal]
	[ScreenResizeable]
	internal class FontViewerScreen : BaseScreen
	{
		private const int SideMargin = 4;
		private const int RowGap = 2;
		private const byte TextColour = 15;
		private const byte LabelColour = 14;
		private const byte GlyphColour = 15;
		private const byte EmptyGlyphMarkColour = 8;
		private const int LabelFontId = 3;

		private readonly IFontInspectionService _fontInspection;
		private readonly IBrowserService? _browser;
		private readonly FontGlyphZoomDelegate _zoomDelegate = new(minFactor: -4, maxFactor: 4);
		private readonly StringBuilder _input = new();

		private int _fontId;
		private char? _lastChar;
		private string? _copyStatus;
		private int _hoverCode = -1;
		private bool _hasUpdate = true;

		/// <inheritdoc />
		public override bool UseFullWindowCanvas => true;

		/// <summary>
		/// Resolved lazily, so constructing the screen does not initialise the clipboard service.
		/// </summary>
		private IBrowserService Browser => _browser ?? BrowserServiceFactory.Instance;

		private static int LabelHeight => Math.Max(6, Resources.GetFontHeight(LabelFontId));

		/// <summary>
		/// Gets the width of the column holding the row code labels.
		/// It follows the label font, so a larger font does not push the labels into the grid.
		/// </summary>
		private static int CodeLabelWidth => Math.Max(8, Resources.GetTextSize(LabelFontId, "000").Width + 4);

		// Rows anchored to the bottom edge, so the whole canvas height is used. Each row keeps a gap of
		// RowGap pixels, otherwise descenders of a tall label font touch the line below.
		private int EditHintRowY => Height - LabelHeight - 2;
		private int InsertHintRowY => EditHintRowY - LabelHeight - RowGap;
		private int KeyRowY => InsertHintRowY - LabelHeight - RowGap;
		private int StatusRowY => KeyRowY - LabelHeight - RowGap;
		/// <summary>
		/// Gets the height of the input row.
		/// <br/>
		/// An empty input shows a placeholder in the label font, so the row is as tall as that font and
		/// not as tall as the scaled game font, which can be only a few pixels high at negative zoom
		/// levels.
		/// </summary>
		private int InputContentHeight => _input.Length == 0
			? LabelHeight
			: _zoomDelegate.ScaleLength(Resources.GetFontHeight(_fontId));

		private int InputTextY => Math.Max(GridTop + LabelHeight + 3, StatusRowY - 3 - InputContentHeight);
		private int InputLabelY => Math.Max(GridTop + 2, InputTextY - LabelHeight - 1);

		// Rows anchored to the top edge.
		private static int TitleRowY => 3;
		private static int FontInfoRowY => TitleRowY + LabelHeight + 1;
		private static int GridTop => FontInfoRowY + LabelHeight + 3;
		private int GridBottom => Math.Max(GridTop + 1, InputLabelY - 2);

		/// <summary>
		/// Gets the widest unscaled glyph of the selected font, which determines the grid column width.
		/// </summary>
		private int WidestGlyph
		{
			get
			{
				int width = 0;
				foreach (byte code in _fontInspection.GetCharacterCodes(_fontId))
				{
					int letterWidth = Resources.GetTextSize(_fontId, ((char)code).ToString()).Width;
					if (letterWidth > width)
					{
						width = letterWidth;
					}
				}
				return Math.Max(1, width);
			}
		}

		private int CellWidth => Math.Max(2, _zoomDelegate.ScaleLength(WidestGlyph) + 2);

		private int CellHeight => Math.Max(2, _zoomDelegate.ScaleLength(Resources.GetFontHeight(_fontId)) + 2);

		/// <summary>
		/// Gets the number of grid columns that fit next to each other at the current zoom level.
		/// </summary>
		private int GridColumns => Math.Max(1, (Width - CodeLabelWidth - SideMargin) / CellWidth);

		/// <summary>
		/// Gets the number of grid rows that fit above the input area at the current zoom level.
		/// </summary>
		private int GridRows => Math.Max(1, (GridBottom - GridTop) / CellHeight);

		/// <summary>
		/// Gets a palette index that is black and fully opaque.
		/// <br/>
		/// Index 0 is the transparency index, so it cannot be used to paint a solid background.
		/// </summary>
		private byte BackgroundColour
		{
			get
			{
				for (int i = 1; i < Palette.Length; i++)
				{
					Colour colour = Palette[i];
					if (colour.A > 0 && colour.R == 0 && colour.G == 0 && colour.B == 0)
					{
						return (byte)i;
					}
				}
				return 5;
			}
		}

		private static int GridStartX => CodeLabelWidth;

		private void SelectFont(int fontId)
		{
			if (fontId < 0 || fontId >= _fontInspection.FontCount || fontId == _fontId)
			{
				return;
			}

			_fontId = fontId;
			_hoverCode = -1;
			_hasUpdate = true;
			Refresh();
		}

		private void CycleFont(int direction)
		{
			int count = _fontInspection.FontCount;
			if (count <= 0)
			{
				return;
			}

			SelectFont((((_fontId + direction) % count) + count) % count);
		}

		private void ChangeZoom(int delta)
		{
			if (!_zoomDelegate.Change(delta))
			{
				return;
			}

			_hasUpdate = true;
			Refresh();
		}

		private void AppendCharacter(char character)
		{
			if (character == 0)
			{
				return;
			}

			_input.Append(character);
			_lastChar = character;
			_copyStatus = null;
			_hasUpdate = true;
			Refresh();
		}

		/// <summary>
		/// Applies the shift and caps lock state to a letter.
		/// <br/>
		/// The runtime reports every character key in upper case and resolves the top row digits by
		/// physical scancode, so the shifted symbol of a digit key (for example the currency glyph on
		/// Shift+4) never reaches a screen.
		/// Letters can still be cased here, and every other glyph is reachable by clicking it in the
		/// character grid.
		/// </summary>
		/// <param name="character">Character as reported by the runtime.</param>
		/// <param name="shift"><see langword="true"/> when a shift key was held.</param>
		/// <param name="capsLock"><see langword="true"/> when caps lock was active.</param>
		/// <returns>The character to insert.</returns>
		private static char ApplyCase(char character, bool shift, bool capsLock)
		{
			if (!char.IsLetter(character))
			{
				return character;
			}

			return shift ^ capsLock
				? char.ToUpper(character, CultureInfo.CurrentCulture)
				: char.ToLower(character, CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Builds the clipboard text for the current input.
		/// <br/>
		/// Control codes such as 127 (DEL) do have a glyph in <c>FONTS.CV</c> but no printable
		/// representation outside the game, where they end up as replacement characters.
		/// They are written as <c>\xNN</c> with the hexadecimal character code instead, so the code
		/// itself survives the copy.
		/// </summary>
		/// <param name="input">Raw input text.</param>
		/// <param name="escaped">Set to <see langword="true"/> when at least one code was escaped.</param>
		/// <returns>The text to put on the clipboard.</returns>
		private static string BuildClipboardText(string input, out bool escaped)
		{
			escaped = false;
			StringBuilder output = new(input.Length);
			foreach (char character in input)
			{
				if (!char.IsControl(character))
				{
					output.Append(character);
					continue;
				}

				escaped = true;
				output.Append("\\x").Append(((int)character).ToString("X2", CultureInfo.InvariantCulture));
			}

			return output.ToString();
		}

		/// <summary>
		/// Copies the current input to the system clipboard and reports the result in the status row.
		/// </summary>
		private void CopyInputToClipboard()
		{
			if (_input.Length == 0)
			{
				_copyStatus = Translate("Nothing to copy.");
			}
			else
			{
				string clipboardText = BuildClipboardText(_input.ToString(), out bool escaped);
				if (!Browser.TryCopyToClipboard(clipboardText, out string? errorMessage))
				{
					_copyStatus = errorMessage ?? Translate("Could not copy to clipboard.");
				}
				else
				{
					_copyStatus = escaped
						// no font supports \\, so we flip the slash direction
						? Translate("Input copied; control codes as /xNN.")
						: Translate("Input copied to clipboard.");
				}
			}

			_hasUpdate = true;
			Refresh();
		}

		private void DrawGlyph(char character, int x, int y, byte colour)
		{
			using Picture glyph = Resources.GetText(character.ToString(), _fontId, colour);
			if (_zoomDelegate.Factor == 0)
			{
				this.AddLayer(glyph, x, y);
				return;
			}

			using Bytemap scaled = _zoomDelegate.Scale(glyph.Bitmap);
			this.AddLayer(scaled, x, y);
		}

		private void DrawCharacterGrid()
		{
			IReadOnlyList<byte> codes = _fontInspection.GetCharacterCodes(_fontId);
			int cellWidth = CellWidth;
			int cellHeight = CellHeight;
			int columns = GridColumns;
			int visibleRows = GridRows;
			int gridTop = GridTop;

			// The row labels use the fixed label font, so at small zoom levels only every n-th row can be
			// labelled without the labels overlapping.
			int labelRowStep = Math.Max(1, (LabelHeight + cellHeight - 1) / cellHeight);

			for (int i = 0; i < codes.Count; i++)
			{
				int column = i % columns;
				int row = i / columns;
				if (row >= visibleRows)
				{
					int noticeY = Math.Min(gridTop + (visibleRows * cellHeight), GridBottom - LabelHeight);
					this.DrawText(Translate("(zoom out with F9 to see the remaining characters)"), LabelFontId, LabelColour, GridStartX, noticeY);
					break;
				}

				char character = (char)codes[i];
				if (column == 0 && row % labelRowStep == 0)
				{
					this.DrawText(codes[i].ToString(CultureInfo.InvariantCulture), LabelFontId, LabelColour, 2, gridTop + (row * cellHeight));
				}

				int cellX = GridStartX + (column * cellWidth);
				int cellY = gridTop + (row * cellHeight);
				if (_fontInspection.HasVisibleGlyph(_fontId, character))
				{
					DrawGlyph(character, cellX, cellY, GlyphColour);
					continue;
				}

				// The code exists in the font but its glyph is blank; mark it so an empty cell is not
				// mistaken for a missing code.
				this.FillRectangle(cellX, cellY + (cellHeight / 2), Math.Max(1, cellWidth - 3), 1, EmptyGlyphMarkColour);
			}
		}

		private void DrawInputLine()
		{
			this.DrawText(TranslateFormatted("Input in font {0}, zoom level {1}:", _fontId, _zoomDelegate.Factor), LabelFontId, LabelColour, SideMargin, InputLabelY);

			if (_input.Length == 0)
			{
				this.DrawText(Translate("Type characters to render them in this font."), LabelFontId, LabelColour, SideMargin, InputTextY);
				return;
			}

			using Picture text = Resources.GetText(_input.ToString(), _fontId, TextColour);
			if (_zoomDelegate.Factor == 0)
			{
				this.AddLayer(text, SideMargin, InputTextY);
				return;
			}

			using Bytemap zoomed = _zoomDelegate.Scale(text.Bitmap);
			this.AddLayer(zoomed, SideMargin, InputTextY);
		}

		/// <summary>
		/// Draws a hint line, falling back to a shorter text when the full text would run past the right
		/// edge of the canvas.
		/// </summary>
		/// <param name="full">Preferred text.</param>
		/// <param name="compact">Shorter text used when <paramref name="full"/> does not fit.</param>
		/// <param name="y">Screen row to draw at.</param>
		private void DrawHintLine(string full, string compact, int y)
		{
			int available = Width - (2 * SideMargin);
			string text = Resources.GetTextSize(LabelFontId, full).Width <= available ? full : compact;
			this.DrawText(text, LabelFontId, LabelColour, SideMargin, y);
		}

		private void DrawStatusLines()
		{
			if (_copyStatus != null)
			{
				this.DrawText(_copyStatus, LabelFontId, TextColour, SideMargin, StatusRowY);
			}
			else if (_hoverCode >= 0)
			{
				this.DrawText(TranslateFormatted("Hover: '{0}'   ASCII {1}   hex {2}", (char)_hoverCode, _hoverCode, _hoverCode.ToString("X2", CultureInfo.InvariantCulture)), LabelFontId, TextColour, SideMargin, StatusRowY);
			}
			else if (_lastChar.HasValue)
			{
				int code = _lastChar.Value;
				this.DrawText(TranslateFormatted("Last key: '{0}'   ASCII {1}   hex {2}", _lastChar.Value, code, code.ToString("X2", CultureInfo.InvariantCulture)), LabelFontId, TextColour, SideMargin, StatusRowY);
			}
			else
			{
				this.DrawText(Translate("Hover a glyph or type a character to see its ASCII code."), LabelFontId, LabelColour, SideMargin, StatusRowY);
			}

			DrawHintLine(
				TranslateFormatted("F1-F7 font   LEFT/RIGHT cycle font   F9/F10 zoom {0}", _zoomDelegate.Factor),
				TranslateFormatted("F1-F7 font   F9/F10 zoom {0}", _zoomDelegate.Factor),
				KeyRowY);
			DrawHintLine(
				Translate("CLICK insert glyph   SHIFT upper case letters   CTRL+C copy"),
				Translate("CLICK insert   SHIFT upper case   CTRL+C copy"),
				InsertHintRowY);
			DrawHintLine(
				Translate("BACKSPACE delete   DELETE clear   ESC close"),
				Translate("BACKSPACE delete   ESC close"),
				EditHintRowY);
		}

		private void DrawScreen()
		{
			// A dark background keeps the white glyphs readable; the grey panel pattern used by other
			// debug screens has too little contrast against them.
			this.Clear(BackgroundColour);

			if (_fontInspection.FontCount <= 0)
			{
				this.DrawText(Translate("Font Viewer"), LabelFontId, TextColour, SideMargin, TitleRowY)
					.DrawText(Translate("No fonts loaded from FONTS.CV."), LabelFontId, TextColour, SideMargin, FontInfoRowY)
					.DrawText(Translate("ESC: close"), LabelFontId, LabelColour, SideMargin, EditHintRowY);
				return;
			}

			FontDescription? font = _fontInspection.GetFont(_fontId);
			this.DrawText(TranslateFormatted("Font Viewer - FONTS.CV font {0} of {1}", _fontId, _fontInspection.FontCount - 1), LabelFontId, TextColour, SideMargin, TitleRowY);

			if (font != null)
			{
				string international = font.IsInternational ? Translate("yes") : Translate("no");
				this.DrawText(TranslateFormatted("Height {0} px   codes {1}-{2}   international {3}", font.Height, font.FirstChar, font.LastChar, international), LabelFontId, LabelColour, SideMargin, FontInfoRowY);
			}

			DrawCharacterGrid();
			DrawInputLine();
			DrawStatusLines();
		}

		private int GetHoverCode(ScreenEventArgs args)
		{
			IReadOnlyList<byte> codes = _fontInspection.GetCharacterCodes(_fontId);
			if (codes.Count == 0)
			{
				return -1;
			}

			int cellWidth = CellWidth;
			int cellHeight = CellHeight;
			int columns = GridColumns;
			int localX = args.X - GridStartX;
			int localY = args.Y - GridTop;
			if (localX < 0 || localY < 0 || localX >= columns * cellWidth)
			{
				return -1;
			}

			int column = localX / cellWidth;
			int row = localY / cellHeight;
			if (row >= GridRows)
			{
				// Rows below the grid belong to the input area, not to a character cell.
				return -1;
			}

			int index = (row * columns) + column;
			if (index < 0 || index >= codes.Count)
			{
				return -1;
			}

			return codes[index];
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded() && !_hasUpdate)
			{
				return false;
			}

			DrawScreen();
			_hasUpdate = false;
			return true;
		}

		public override bool MouseMove(ScreenEventArgs args)
		{
			int hoverCode = GetHoverCode(args);
			if (hoverCode == _hoverCode)
			{
				return false;
			}

			_hoverCode = hoverCode;
			_hasUpdate = true;
			Refresh();
			return true;
		}

		public override bool MouseDrag(ScreenEventArgs args) => MouseMove(args);

		public override bool MouseDown(ScreenEventArgs args)
		{
			int code = GetHoverCode(args);
			if (code < 0)
			{
				return false;
			}

			AppendCharacter((char)code);
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			switch (args.Key)
			{
				case Key.Escape:
					Destroy();
					return true;
				case Key.F1:
				case Key.F2:
				case Key.F3:
				case Key.F4:
				case Key.F5:
				case Key.F6:
				case Key.F7:
					// There is no 8th font in the original FONTS.CV
					SelectFont(args.Key - Key.F1);
					return true;
				case Key.F9:
					ChangeZoom(-1);
					return true;
				case Key.F10:
					ChangeZoom(1);
					return true;
				case Key.Left:
					CycleFont(-1);
					return true;
				case Key.Right:
					CycleFont(1);
					return true;
				case Key.Space:
					AppendCharacter(' ');
					return true;
				case Key.Backspace:
					if (_input.Length > 0)
					{
						_input.Length--;
						_copyStatus = null;
						_hasUpdate = true;
						Refresh();
					}
					return true;
				case Key.Delete:
					if (_input.Length > 0)
					{
						_input.Clear();
						_copyStatus = null;
						_hasUpdate = true;
						Refresh();
					}
					return true;
				case Key.Character:
					if (args.Control)
					{
						if (char.ToUpperInvariant(args.KeyChar) != 'C')
						{
							return false;
						}

						CopyInputToClipboard();
						return true;
					}

					AppendCharacter(ApplyCase(args.KeyChar, args.Shift, args.CapsLock));
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Creates the screen.
		/// </summary>
		/// <param name="fontInspection">
		/// Service providing the font metadata.
		/// When <see langword="null"/>, the default service is used.
		/// </param>
		/// <param name="browser">
		/// Service used for clipboard access.
		/// When <see langword="null"/>, the default service is used.
		/// </param>
		public FontViewerScreen(IFontInspectionService? fontInspection = null, IBrowserService? browser = null) : base(MouseCursor.Pointer)
		{
			_fontInspection = fontInspection ?? FontInspectionServiceFactory.CreateDefault();
			_browser = browser;
			using Palette defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;
		}
	}
}
