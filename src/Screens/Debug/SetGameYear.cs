// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Globalization;
using System.Linq;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Services;
using CivOne.Tasks;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class SetGameYear : BaseScreen
	{
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);
		private Input? ActiveInput => Inputs.OfType<Input>().FirstOrDefault();

		private void DrawDialog()
		{
			int ox = OffsetX;
			int oy = OffsetY;

			this.Clear();
			this.FillRectangle(80 + ox, 80 + oy, 161, 33, 11)
				.FillRectangle(81 + ox, 81 + oy, 159, 31, 15)
				.DrawText(Translate("Set Game Year..."), 0, 5, 88 + ox, 82 + oy)
				.FillRectangle(88 + ox, 95 + oy, 105, 14, 5)
				.FillRectangle(89 + ox, 96 + oy, 103, 12, 15);

			if (ActiveInput is Input input)
			{
				input.X = 90 + ox;
				input.Y = 97 + oy;
			}
		}

		public string? Value { get; private set; }

		public event EventHandler? Accept, Cancel;

		private void GameYear_Accept(object? sender, EventArgs __)
		{
			if (sender is not Input input) return;
			Value = input.Text;

			int gameYear;
			if (!int.TryParse(Value, out gameYear) || gameYear < -4000 || gameYear > 6000)
			{
				GameTask.Enqueue(Message.Error(Translate("-- DEBUG: Set Game Year --"), TranslateFormattedArray("The value {0} is invalid or out of range.\nPlease enter a value between -4000 and\n6000.", Value)));
			}
			else
			{
				Game.GameTurn = _gameCalendarService.YearToTurn(gameYear);
				GameTask.Enqueue(Message.General(TranslateFormatted("Game year set to {0}.", Game.GameYear)));
			}

			Accept?.Invoke(this, EventArgs.Empty);
			input.Close();
			Destroy();
		}

		private void GameYear_Cancel(object? sender, EventArgs __)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			if (sender is Input input) input.Close();
			Destroy();
		}

		protected override IScreen? CreateManagedInput()
		{
			string yearString = _gameCalendarService.TurnToYear(Game.GameTurn).ToString(CultureInfo.InvariantCulture);
			Input input = new(Palette, yearString, 0, 5, 11, 90 + OffsetX, 97 + OffsetY, 101, 10, 5);
			input.Accept += GameYear_Accept;
			input.Cancel += GameYear_Cancel;
			return input;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				DrawDialog();
			}

			if (!HasInput)
			{
				EnsureManagedInput();
			}
			return false;
		}

		private readonly IGameCalendarService _gameCalendarService;
		
		public SetGameYear()
		{
			_gameCalendarService = GameCalendarServiceFactory.GetCurrent();
			Palette = Common.Screens.Last().OriginalColours;

			DrawDialog();
		}
	}
}