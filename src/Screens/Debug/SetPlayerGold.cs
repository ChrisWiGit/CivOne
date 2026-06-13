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
using CivOne.Graphics.Sprites;
using CivOne.Services.Screen;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class SetPlayerGold : BaseScreen
	{
		private readonly CivSelectMenuDelegate _civSelectDelegate;

		private Input? _input;

		private Player? _selectedPlayer;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private void DrawPlayerSelectDialog()
		{
			_civSelectDelegate.DrawDialog(this, OffsetX, OffsetY);
		}

		private void DrawInputDialog()
		{
			int ox = OffsetX;
			int oy = OffsetY;

			this.Clear();
			this.FillRectangle(80 + ox, 80 + oy, 161, 33, 11)
				.FillRectangle(81 + ox, 81 + oy, 159, 31, 15)
				.DrawText(Translate("Set Player Gold..."), 0, 5, 88 + ox, 82 + oy)
				.FillRectangle(88 + ox, 95 + oy, 105, 14, 5)
				.FillRectangle(89 + ox, 96 + oy, 103, 12, 15);

			if (_input != null)
			{
				_input.X = 90 + ox;
				_input.Y = 97 + oy;
			}
		}

		public string? Value { get; private set; }

		public event EventHandler? Accept, Cancel;

		private void OnPlayerSelected(Player? player)
		{
			_selectedPlayer = player;

			if (_selectedPlayer == null)
			{
				System.Diagnostics.Debug.Assert(false, "Player selection was cancelled or invalid in OnPlayerSelected");
				return;
			}

			_input = new Input(Palette, _selectedPlayer.Gold.ToString(CultureInfo.InvariantCulture), 0, 5, 11, 90 + OffsetX, 97 + OffsetY, 101, 10, 5);
			_input.Accept += PlayerGold_Accept;
			_input.Cancel += PlayerGold_Cancel;

			DrawInputDialog();

			CloseMenus();
		}

		private void PlayerGold_Accept(object? sender, EventArgs args)
		{
			if (sender is not Input input)
			{
				System.Diagnostics.Debug.Assert(false, "Sender is not Input in PlayerGold_Accept");
				return;
			}
			Value = input.Text;

			if (!short.TryParse(input.Text, out short playerGold) || playerGold < 0 || playerGold > 30000)
			{
				GameTask.Enqueue(Message.Error(Translate("-- DEBUG: Set Player Gold --"), TranslateFormattedArray("The value {0} is invalid or out of range.\nPlease enter a value between 0 and\n30000.", Value ?? "(null)")));
			}
			else if (_selectedPlayer != null)
			{
				_selectedPlayer.Gold = playerGold;
				GameTask.Enqueue(Message.General(TranslateFormatted("{0} gold set to {1}$.", _selectedPlayer.TribeName, playerGold)));
			}

			Accept?.Invoke(this, EventArgs.Empty);
			input.Close();
			Destroy();
		}

		private void PlayerGold_Cancel(object? sender, EventArgs args)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			if (sender is Input input)
				input.Close();
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				if (_selectedPlayer == null)
					DrawPlayerSelectDialog();
				else
					DrawInputDialog();
			}

			if (_selectedPlayer == null && !_screenQueryService.HasTopScreen<Menu>())
			{
				AddMenu(_civSelectDelegate.Menu);
				return false;
			}
			else if (_selectedPlayer != null && !_screenQueryService.HasScreenType<Input>())
			{
				Common.AddScreen(_input);
			}
			return false;
		}

		private readonly IScreenQueryService _screenQueryService;

		public SetPlayerGold() : base(MouseCursor.Pointer)
		{
			_screenQueryService = ScreenServiceFactory.CreateQueryService();
			Palette = Common.Screens[^1].OriginalColours;
			_civSelectDelegate = new CivSelectMenuDelegate(Palette, Translate("Set Player Gold..."));
			_civSelectDelegate.PlayerSelected += OnPlayerSelected;
			_civSelectDelegate.Cancelled += PlayerGold_Cancel;

			DrawPlayerSelectDialog();
		}
	}
}