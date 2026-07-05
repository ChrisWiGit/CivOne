// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Globalization;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens.PalaceAssets;

namespace CivOne.Screens
{
	/// <summary>
	/// Victory screen displayed when player achieves conquest victory.
	/// </summary>
	internal class VictoryScreen : BaseScreen
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

		private void DrawVictoryText(string text)
		{
			string[] lines = text.Split('^');
			const int baseY = 8;
			const int lineSpacing = 14;

			for (int i = 0; i < lines.Length; i++)
			{
				int y = baseY + (i * lineSpacing);
				this.DrawText(lines[i].Trim(), 5, 8, 159, y + 1, TextAlign.Center)
					.DrawText(lines[i].Trim(), 5, 9, 159, y, TextAlign.Center);
			}
		}

		/// <summary>
		/// Updates the screen, scrolling through victory text lines.
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

			this.AddLayer(_background);

			DrawVictoryText(_textLines[_currentLine]);

			if (!IsLastLine)
			{
				_currentLine++;
			}
			return true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VictoryScreen"/>.
		/// </summary>
		public VictoryScreen()
		{
			_background = PalaceView.CreatePicture(PalaceSpriteProviderFactory.GetInstance(), Human.Palace);
			Palette = _background.Palette;
			this.AddLayer(_background);

			PlaySound("win");

			_textLines =
			[
				Translate("A glorious victory is yours."),
				Translate("Your palace rises above all nations."),
				Translate("Your people celebrate in every city."),
				Translate("Bards sing forever of"),
				Translate("$US^ Conqueror of the World!"),
			];

			for (int i = 0; i < _textLines.Length; i++)
				_textLines[i] = _textLines[i].
					Replace("$RPLC1", Human.LatestAdvance, System.StringComparison.InvariantCulture).
					Replace("$US", Human.LeaderName.ToUpper(CultureInfo.CurrentCulture), System.StringComparison.InvariantCulture);
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_background?.Dispose();
			base.Dispose(disposing);
		}
	}
}
