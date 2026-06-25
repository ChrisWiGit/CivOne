// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne.Screens
{
	/// <summary>
	/// Defeat screen displayed when player loses the game.
	/// </summary>
	internal class DefeatScreen : BaseScreen
	{
		private readonly Picture _background;
		private readonly string[] _textLines;
		private int _currentLine;
		private int _lineTick;
		private bool _showFirstFrame = true;

		/// <summary>
		/// Handles keyboard input to skip or close the screen.
		/// </summary>
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args.Key == Key.Escape)
			{
				Destroy();
				return true;
			}
			if (IsLastLine && args.Key == Key.Enter)
			{
				Destroy();
				return true;
			}
			return false;
		}

		private bool IsLastLine => _textLines.Length == 0 || _currentLine >= _textLines.Length - 1;

		/// <summary>
		/// Updates the screen, scrolling through defeat text lines.
		/// </summary>
		protected override bool HasUpdate(uint gameTick)
		{
			if (_showFirstFrame)
			{
				_showFirstFrame = false;
			}
			else
			{
				if (gameTick % 10 != 0) return false;
				_lineTick++;

				if (_lineTick % 6 != 0) return false;
			}

			if (_textLines.Length == 0)
			{
				return true;
			}

			this.AddLayer(_background)
				.DrawText(_textLines[_currentLine], 5, 15, 159, 7, TextAlign.Center)
				.DrawText(_textLines[_currentLine], 5, 13, 159, 9, TextAlign.Center)
				.DrawText(_textLines[_currentLine], 5, 14, 159, 8, TextAlign.Center);

			if (!IsLastLine)
			{
				_currentLine++;
			}
			return true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DefeatScreen"/>.
		/// </summary>
		public DefeatScreen()
		{
			_background = Resources["ARCH"];
			Palette = _background.Palette;
			this.AddLayer(_background);

			PlaySound("lose2");

			// Load defeat text
			_textLines = GetGameText("KING/ARCH");
			for (int i = 0; i < _textLines.Length; i++)
				_textLines[i] = _textLines[i]
					.Replace("$RPLC1", Human.LatestAdvance, StringComparison.InvariantCulture)
					.Replace("$US", Human.LeaderName.ToUpperInvariant(), StringComparison.InvariantCulture)
					.Replace("^", "", StringComparison.InvariantCulture);
		}
	}
}
