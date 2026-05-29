using System;
using System.Drawing;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens
{
	/// <summary>
	/// Provides modal text input behavior for screens that need a small inline dialog.
	/// </summary>
	/// <remarks>
	/// Constructor input values:
	/// <list type="bullet">
	/// <item>
	/// <description><paramref name="title"/> defines the dialog caption.</description>
	/// </item>
	/// <item>
	/// <description><paramref name="maxLength"/> limits the accepted text length and is optional. The default value is <c>80</c>.</description>
	/// </item>
	/// </list>
	/// Runtime input values:
	/// <list type="bullet">
	/// <item>
	/// <description><see cref="Open(string)"/> accepts an optional initial text value. If omitted, the dialog starts empty.</description>
	/// </item>
	/// <item>
	/// <description>User input supports normal printable keyboard characters, numpad digits, space, cursor navigation, delete, backspace, enter, and escape.</description>
	/// </item>
	/// </list>
	/// Open the delegate with <see cref="Open(string)"/>, forward mouse and keyboard events to
	/// <see cref="MouseDown(ScreenEventArgs)"/> and <see cref="KeyDown(KeyboardEventArgs)"/>, and call
	/// <see cref="Draw(IBitmap, uint, int, int)"/> during the screen render pass while <see cref="Active"/> is <see langword="true"/>.
	/// Subscribe to <see cref="Accepted"/> to receive the trimmed input value and to <see cref="Cancelled"/> to react to dismissal.
	/// <example>
	/// <code>
	/// var inputDialog = new InputDialogDelegate("City name", maxLength: 32);
	/// inputDialog.Accepted += cityName =&gt; RenameCity(cityName);
	/// inputDialog.Cancelled += (_, _) =&gt; CloseCityRenameMode();
	///
	/// inputDialog.Open("Rome");
	///
	/// // Inside the screen event loop:
	/// inputDialog.KeyDown(args);
	/// inputDialog.MouseDown(args);
	///
	/// // Inside the render pass:
	/// inputDialog.Draw(target, gameTick, width, height);
	/// </code>
	/// </example>
	/// </remarks>
	internal sealed class InputDialogDelegate : BaseInstance
	{
		private const int FontId = 0;
		private const int TextColour = 5;
		private const int DialogBorderColour = 11;
		private const int DialogInnerColour = 15;
		private const int FieldBorderColour = 5;
		private const int FieldInnerColour = 15;
		private const int ButtonWidth = 52;
		private const int ButtonHeight = 10;

		private readonly string _title;
		private readonly int _maxLength;

		private bool _active;
		private string _text = string.Empty;
		private int _cursorPosition;
		private int _viewStart;

		private Rectangle _fieldRect;
		private Rectangle _okRect;
		private Rectangle _cancelRect;

		public event Action<string>? Accepted;
		public event EventHandler? Cancelled;

		public bool Active => _active;

		public string Text => _text;

		/// <summary>
		/// Opens the dialog and optionally pre-fills the input field.
		/// </summary>
		/// <param name="initialText">
		/// The initial text to show in the field.
		/// This parameter is optional.
		/// When omitted, the dialog starts with an empty value.
		/// </param>
		public void Open(string initialText = "")
		{
			_text = (initialText ?? string.Empty).Trim();
			if (_text.Length > _maxLength)
			{
				_text = _text[.._maxLength];
			}

			_cursorPosition = _text.Length;
			_viewStart = 0;
			_active = true;
		}

		public void Close()
		{
			_active = false;
		}

		private static bool TryGetNumpadChar(KeyboardEventArgs args, out char character)
		{
			character = args.Key switch
			{
				Key.NumPad0 => '0',
				Key.NumPad1 => '1',
				Key.NumPad2 => '2',
				Key.NumPad3 => '3',
				Key.NumPad4 => '4',
				Key.NumPad5 => '5',
				Key.NumPad6 => '6',
				Key.NumPad7 => '7',
				Key.NumPad8 => '8',
				Key.NumPad9 => '9',
				_ => '\0'
			};

			return character != '\0';
		}

		/// <summary>
		/// Resolves the printable character represented by the keyboard event.
		/// </summary>
		/// <param name="args">
		/// The keyboard event to translate.
		/// </param>
		/// <returns>
		/// The character that should be inserted into the input field.
		/// Numpad digits, shifted number-row symbols, and the minus key are normalized to the expected text value.
		/// </returns>
		private static char ResolveCharacter(KeyboardEventArgs args)
		{
			if (TryGetNumpadChar(args, out char numpadChar))
			{
				return numpadChar;
			}

			char c = args.KeyChar;
			if (!args.Shift)
			{
				c = char.ToLowerInvariant(c);
			}

			if (args.Key == Key.Minus)
			{
				c = '-';
			}

			if (args.Shift && c >= '0' && c <= '9')
			{
				c = c switch
				{
					'6' => '^',
					'7' => '&',
					'8' => '*',
					'9' => '(',
					'0' => ')',
					_ => (char)(c - 16)
				};
			}

			return c;
		}

		private bool InsertCharacter(char character)
		{
			if (char.IsControl(character) || char.IsSurrogate(character))
			{
				return false;
			}

			if (_text.Length >= _maxLength)
			{
				return false;
			}

			if (_cursorPosition < 0)
			{
				_cursorPosition = 0;
			}

			if (_cursorPosition > _text.Length)
			{
				_cursorPosition = _text.Length;
			}

			_text = _text.Insert(_cursorPosition, character.ToString());
			_cursorPosition++;
			EnsureCursorVisible();
			return true;
		}

		private int GetFieldInnerWidth()
		{
			return Math.Max(1, _fieldRect.Width - 4);
		}

		private int GetTextWidth(int start, int endExclusive)
		{
			if (endExclusive <= start)
			{
				return 0;
			}

			string segment = _text[start..Math.Min(endExclusive, _text.Length)];
			return Resources.GetTextSize(FontId, segment).Width;
		}

		private void EnsureCursorVisible()
		{
			if (_cursorPosition < _viewStart)
			{
				_viewStart = _cursorPosition;
			}

			int innerWidth = GetFieldInnerWidth();
			while (_viewStart < _cursorPosition && GetTextWidth(_viewStart, _cursorPosition) > innerWidth)
			{
				_viewStart++;
			}

			if (_viewStart > _text.Length)
			{
				_viewStart = _text.Length;
			}
		}

		private int GetVisibleEndIndex()
		{
			int innerWidth = GetFieldInnerWidth();
			int width = 0;
			for (int i = _viewStart; i < _text.Length; i++)
			{
				int charWidth = Math.Max(1, Resources.GetLetterSize(FontId, _text[i]).Width + 1);
				if (width + charWidth > innerWidth)
				{
					return i;
				}
				width += charWidth;
			}
			return _text.Length;
		}

		private int GetCursorX()
		{
			string visibleText = _text[_viewStart..Math.Min(_cursorPosition, _text.Length)];
			return _fieldRect.X + 2 + Resources.GetTextSize(FontId, visibleText).Width;
		}

		private int GetCursorY()
		{
			int fontHeight = Resources.GetFontHeight(FontId);
			return _fieldRect.Y + Math.Max(1, (_fieldRect.Height - fontHeight) / 2);
		}

		private int GetCursorHeight()
		{
			int fontHeight = Resources.GetFontHeight(FontId);
			return Math.Max(1, Math.Min(_fieldRect.Height - 2, fontHeight + 1));
		}

		private void Accept()
		{
			_active = false;
			Accepted?.Invoke(_text.Trim());
		}

		private void Cancel()
		{
			_active = false;
			Cancelled?.Invoke(this, EventArgs.Empty);
		}

		private void MoveCursorToMouse(ScreenEventArgs args)
		{
			int relativeX = Math.Max(0, args.X - (_fieldRect.X + 2));
			int x = 0;
			int position = _viewStart;

			for (int i = _viewStart; i < _text.Length; i++)
			{
				int charWidth = Resources.GetLetterSize(FontId, _text[i]).Width + 1;
				if (x + (charWidth / 2) >= relativeX)
				{
					position = i;
					_cursorPosition = position;
					EnsureCursorVisible();
					return;
				}

				x += charWidth;
				position = i + 1;
			}

			_cursorPosition = position;
			EnsureCursorVisible();
		}

		public bool MouseDown(ScreenEventArgs args)
		{
			if (!_active)
			{
				return false;
			}

			if (_okRect.Contains(args.Location))
			{
				Accept();
				return true;
			}

			if (_cancelRect.Contains(args.Location))
			{
				Cancel();
				return true;
			}

			if (_fieldRect.Contains(args.Location))
			{
				MoveCursorToMouse(args);
				return true;
			}

			return false;
		}

		public bool KeyDown(KeyboardEventArgs args)
		{
			if (!_active)
			{
				return false;
			}

			switch (args.Key)
			{
					case Key.Space:
						return InsertCharacter(' ');
				case Key.Left:
					if (_cursorPosition > 0)
					{
						_cursorPosition--;
					}
					EnsureCursorVisible();
					return true;
				case Key.Right:
					if (_cursorPosition < _text.Length)
					{
						_cursorPosition++;
					}
					EnsureCursorVisible();
					return true;
				case Key.Home:
					_cursorPosition = 0;
					EnsureCursorVisible();
					return true;
				case Key.End:
					_cursorPosition = _text.Length;
					EnsureCursorVisible();
					return true;
				case Key.Delete:
					if (_cursorPosition < _text.Length)
					{
						_text = _text.Remove(_cursorPosition, 1);
						EnsureCursorVisible();
					}
					return true;
				case Key.Backspace:
					if (_cursorPosition > 0)
					{
						_text = _text.Remove(_cursorPosition - 1, 1);
						_cursorPosition--;
						EnsureCursorVisible();
					}
					return true;
				case Key.Enter:
					Accept();
					return true;
				case Key.Escape:
					Cancel();
					return true;
				default:
					if (args[Key.Character] || TryGetNumpadChar(args, out _))
					{
						char character = ResolveCharacter(args);
						return InsertCharacter(character);
					}
					return false;
			}
		}

		private static void DrawButton(IBitmap target, Rectangle rect, string text)
		{
			target.FillRectangle(rect.X, rect.Y, rect.Width, rect.Height, 11)
				.FillRectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2, 15)
				.DrawText(text, FontId, TextColour, rect.X + (rect.Width / 2), rect.Y + 2, TextAlign.Center);
		}

		public void Draw(IBitmap target, uint gameTick, int width, int height)
		{
			if (!_active)
			{
				return;
			}

			int offsetX = Math.Max(0, (width - 320) / 2);
			int offsetY = Math.Max(0, (height - 200) / 2);
			int titleWidth = Resources.GetTextSize(FontId, _title).Width;
			int dialogWidth = Math.Max(210, titleWidth + 20);
			int dialogHeight = 54;

			int dialogX = offsetX + ((320 - dialogWidth) / 2);
			int dialogY = offsetY + ((200 - dialogHeight) / 2);

			_fieldRect = new Rectangle(dialogX + 8, dialogY + 16, dialogWidth - 16, 14);
			_okRect = new Rectangle(dialogX + 28, dialogY + 36, ButtonWidth, ButtonHeight);
			_cancelRect = new Rectangle(dialogX + dialogWidth - 28 - ButtonWidth, dialogY + 36, ButtonWidth, ButtonHeight);

			target.FillRectangle(dialogX - 1, dialogY - 1, dialogWidth + 2, dialogHeight + 2, FieldBorderColour)
				.FillRectangle(dialogX, dialogY, dialogWidth, dialogHeight, DialogBorderColour)
				.FillRectangle(dialogX + 1, dialogY + 1, dialogWidth - 2, dialogHeight - 2, DialogInnerColour)
				.DrawText(_title, FontId, TextColour, dialogX + 8, dialogY + 3)
				.FillRectangle(_fieldRect.X, _fieldRect.Y, _fieldRect.Width, _fieldRect.Height, FieldBorderColour)
				.FillRectangle(_fieldRect.X + 1, _fieldRect.Y + 1, _fieldRect.Width - 2, _fieldRect.Height - 2, FieldInnerColour);

			EnsureCursorVisible();
			int visibleEnd = GetVisibleEndIndex();
			if (visibleEnd > _viewStart)
			{
				string visibleText = _text[_viewStart..visibleEnd];
				target.DrawText(visibleText, FontId, TextColour, _fieldRect.X + 2, _fieldRect.Y + 3);
			}

			int cursorX = Math.Clamp(GetCursorX(), _fieldRect.X + 2, _fieldRect.Right - 2);
			target.FillRectangle(cursorX, GetCursorY(), 1, GetCursorHeight(), 11);

			DrawButton(target, _okRect, Translate("OK"));
			DrawButton(target, _cancelRect, Translate("Cancel"));
		}

		/// <summary>
		/// Initializes a new input dialog delegate.
		/// </summary>
		/// <param name="title">
		/// The dialog title.
		/// If the value is null, empty, or whitespace, the localized fallback title <c>Input</c> is used.
		/// </param>
		/// <param name="maxLength">
		/// The maximum allowed text length.
		/// This parameter is optional.
		/// The default value is <c>80</c>.
		/// </param>
		public InputDialogDelegate(string title, int maxLength = 80)
		{
			_title = string.IsNullOrWhiteSpace(title) ? Translate("Input") : title;
			_maxLength = Math.Max(1, maxLength);
		}
	}
}