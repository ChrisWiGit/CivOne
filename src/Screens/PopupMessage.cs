// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using System.Collections.Generic;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class PopupMessage : BaseScreen
	{
		private bool _update = true;
		private readonly byte _colour;
		private readonly string _title;
		private readonly string[] _message;
		private int OffsetX => System.Math.Max(0, (Width - 320) / 2);
		private int OffsetY => System.Math.Max(0, (Height - 200) / 2);

		private static string AddWordToLine(string currentLine, string word)
			=> string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";

		private static List<string> SplitLongWord(string word, int font, int maxWidth)
		{
			List<string> output = [];
			string chunk = string.Empty;
			for (int c = 0; c < word.Length; c++)
			{
				string charCandidate = chunk + word[c];
				if (Resources.GetTextSize(font, charCandidate).Width <= maxWidth)
				{
					chunk = charCandidate;
					continue;
				}

				if (!string.IsNullOrEmpty(chunk)) output.Add(chunk);
				chunk = word[c].ToString();
			}
			if (!string.IsNullOrEmpty(chunk)) output.Add(chunk);
			return output;
		}

		private static List<string> WrapLine(string text, int font, int maxWidth)
		{
			List<string> output = [];
			if (string.IsNullOrEmpty(text))
			{
				output.Add(string.Empty);
				return output;
			}

			string[] words = text.Split(' ');
			string currentLine = string.Empty;
			for (int i = 0; i < words.Length; i++)
			{
				string word = words[i];
				string candidate = AddWordToLine(currentLine, word);
				if (Resources.GetTextSize(font, candidate).Width <= maxWidth)
				{
					currentLine = candidate;
					continue;
				}

				if (!string.IsNullOrEmpty(currentLine))
				{
					output.Add(currentLine);
				}

				if (Resources.GetTextSize(font, word).Width <= maxWidth)
				{
					currentLine = word;
					continue;
				}

				List<string> chunks = SplitLongWord(word, font, maxWidth);
				for (int c = 0; c < chunks.Count - 1; c++)
				{
					output.Add(chunks[c]);
				}
				currentLine = chunks.Count > 0 ? chunks[chunks.Count - 1] : string.Empty;
			}

			if (!string.IsNullOrEmpty(currentLine)) output.Add(currentLine);
			return output;
		}

		private void Render()
		{
			this.Clear();

			byte colourLight = (byte)(_colour + 8);
			int lineHeight = Resources.GetFontHeight(1);
			const int horizontalPadding = 10;
			int maxPopupWidth = 320;
			int maxContentWidth = maxPopupWidth - (horizontalPadding * 2);
			List<string> messageLines = [];
			for (int i = 0; i < _message.Length; i++)
			{
				messageLines.AddRange(WrapLine(_message[i], 1, maxContentWidth));
			}
			int maxTextWidth = 0;
			if (_title != null)
			{
				maxTextWidth = System.Math.Max(maxTextWidth, Resources.GetTextSize(1, _title).Width);
			}
			for (int i = 0; i < messageLines.Count; i++)
			{
				maxTextWidth = System.Math.Max(maxTextWidth, Resources.GetTextSize(1, messageLines[i]).Width);
			}
			int width = System.Math.Min(maxPopupWidth, maxTextWidth + (horizontalPadding * 2));
			int lineCount = messageLines.Count + (_title != null ? 1 : 0);
			int height = (lineCount * lineHeight) + 5;
			int x = OffsetX + System.Math.Max(0, (maxPopupWidth - width) / 2);
			int y = 16 + OffsetY;

			this.FillRectangle(x, y, width, 1, colourLight)
				.FillRectangle(x, y + 1, 1, height - 2, colourLight)
				.FillRectangle(x + width - 1, y + 1, 1, height - 2, colourLight)
				.FillRectangle(x, y + height - 1, width, 1, colourLight)
				.FillRectangle(x + 1, y + 1, width - 2, height - 2, _colour);

			int yy = y + 3 - lineHeight;
			if (_title != null)
			{
				yy += lineHeight;
				this.DrawText(_title, 1, 5, x + (width / 2), yy, TextAlign.Center);
			}
			for (int i = 0; i < messageLines.Count; i++)
			{
				yy += lineHeight;
				this.DrawText(messageLines[i], 1, 15, x + horizontalPadding, yy);
			}
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_update)
			{
				Render();
				_update = false;
				return true;
			}
			return false;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			Destroy();
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			Destroy();
			return true;
		}

		public PopupMessage(byte colour, string title, string[] message) : base(MouseCursor.Pointer)
		{
			_colour = colour;
			_title = title;
			_message = message;

			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			Render();
		}
	}
}