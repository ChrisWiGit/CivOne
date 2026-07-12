// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Persistence.Game;
using CivOne.Services.Screen;
using CivOne.Tasks;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class ChangeHumanPlayer : BaseScreen
	{
		private readonly CivSelectMenuDelegate _civSelectDelegate;

		private Player? _selectedPlayer;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private void DrawDialog()
		{
			_civSelectDelegate.DrawDialog(this, OffsetX, OffsetY);
		}

		public event EventHandler? Accept, Cancel;

		private void ChangePlayer_Accept(Player player)
		{
			if (player.Civilization.Id == 0)
			{
				GameTask.Enqueue(Message.General(Translate("Barbarians cannot be selected as the human player.")));
				return;
			}

			_selectedPlayer = player;

			if (_selectedPlayer != Game.HumanPlayer)
			{
				SwapHumanAndSelectedPlayerRuntimeAiState(Game.HumanPlayer, _selectedPlayer);
				Game.HumanPlayer = _selectedPlayer;
				Game.EndTurn(3);
			}

			Accept?.Invoke(this, EventArgs.Empty);
			Destroy();
		}

		private static void SwapHumanAndSelectedPlayerRuntimeAiState(Player previousHumanPlayer, Player newHumanPlayer)
		{
			ArgumentNullException.ThrowIfNull(previousHumanPlayer);
			ArgumentNullException.ThrowIfNull(newHumanPlayer);

			Guid? previousHumanAiId = previousHumanPlayer.AiId;
			Guid? newHumanAiId = newHumanPlayer.AiId;

			((IPlayerRestorable)previousHumanPlayer).AiId = newHumanAiId;
			((IPlayerRestorable)newHumanPlayer).AiId = previousHumanAiId;

			byte previousHumanHandicap = previousHumanPlayer.Handicap;
			previousHumanPlayer.Handicap = newHumanPlayer.Handicap;
			newHumanPlayer.Handicap = previousHumanHandicap;
		}

		private void ChangePlayer_Cancel(object? sender, EventArgs args)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				DrawDialog();
			}

			if (_selectedPlayer == null && !_screenQueryService.HasTopScreen<Menu>())
			{
				AddMenu(_civSelectDelegate.Menu);
				return false;
			}
			return false;
		}

		private readonly IScreenQueryService _screenQueryService;
		public ChangeHumanPlayer() : base(MouseCursor.Pointer)
		{
			_screenQueryService = ScreenServiceFactory.CreateQueryService();
			Palette = Common.Screens[^1].OriginalColours;
			_civSelectDelegate = new CivSelectMenuDelegate(Palette, Translate("Change Human Player..."), player => player.Civilization.Id != 0);
			_civSelectDelegate.PlayerSelected += ChangePlayer_Accept;
			_civSelectDelegate.Cancelled += ChangePlayer_Cancel;

			DrawDialog();
		}
	}
}