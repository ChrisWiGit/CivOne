using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Persistence.Game;
using CivOne.Tasks;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class ChangeHumanPlayer : BaseScreen
	{
		private readonly CivSelectMenuDelegate _civSelectDelegate;

		private Player? _selectedPlayer;

		private void DrawDialog()
		{
			_civSelectDelegate.Draw(this, CanvasHeight);
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

		/// <summary>
		/// The players the human may switch to.
		/// Barbarians are left out, they cannot be played.
		/// </summary>
		private static Player[] SelectablePlayers() => [.. Game.Players.Where(player => player.Civilization.Id != 0)];

		private void ChangePlayer_Cancel(object? sender, EventArgs args)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded() && _selectedPlayer == null)
			{
				DrawDialog();
				return true;
			}
			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_selectedPlayer != null)
			{
				return false;
			}

			bool handled = _civSelectDelegate.KeyDown(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_selectedPlayer != null)
			{
				return false;
			}

			bool handled = _civSelectDelegate.MouseDown(args.X, args.Y);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public ChangeHumanPlayer() : base(MouseCursor.Pointer)
		{
			Palette = Common.Screens[^1].OriginalColours;
			_civSelectDelegate = new CivSelectMenuDelegate(SelectablePlayers(), Translate("Change Human Player..."));
			_civSelectDelegate.PlayerSelected += ChangePlayer_Accept;
			_civSelectDelegate.Cancelled += ChangePlayer_Cancel;

			DrawDialog();
		}
	}
}