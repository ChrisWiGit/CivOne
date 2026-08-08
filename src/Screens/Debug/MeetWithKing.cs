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
using CivOne.Events;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class MeetWithKing : BaseScreen
	{
		private readonly CivSelectMenuDelegate _civSelect;

		private Player? _selectedPlayer;

		public event EventHandler? Accept, Cancel;

		private void DrawDialog()
		{
			_civSelect.Draw(this, CanvasHeight);
		}

		private void MeetKingAccept(Player player)
		{
			_selectedPlayer = player;

			if (_selectedPlayer != Game.HumanPlayer)
			{
				Common.AddScreen(new King(_selectedPlayer));
			}

			Accept?.Invoke(this, EventArgs.Empty);
			Destroy();
		}

		private void MeetKingCancel(object? sender, EventArgs args)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			if (sender is Input input)
			{
				input.Close();
			}
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

			bool handled = _civSelect.KeyDown(args);
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

			bool handled = _civSelect.MouseDown(args.X, args.Y);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public MeetWithKing() : base(MouseCursor.Pointer)
		{
			Palette = Common.Screens[^1].OriginalColours;
			Player[] players = [.. Game.Players.Where(p => p != 0 && p != Human)];

			// Leader and tribe name together are much wider than a tribe name alone, so the grid has to size
			// itself from the labels instead of using a fixed dialog width.
			_civSelect = new CivSelectMenuDelegate(players, Translate("Meet With King"), player => $"{player.LeaderName} ({player.TribeName})");
			_civSelect.PlayerSelected += MeetKingAccept;
			_civSelect.Cancelled += MeetKingCancel;

			DrawDialog();
		}
	}
}
