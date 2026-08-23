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
			_selectedPlayer = player;

			if (_selectedPlayer != Game.HumanPlayer)
			{
				Game.HumanPlayer = _selectedPlayer;
				Game.EndTurn(3);
			}

			Accept?.Invoke(this, EventArgs.Empty);
			Destroy();
		}

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
			Palette = Common.Screens[Common.Screens.Length - 1].OriginalColours;
			_civSelectDelegate = new CivSelectMenuDelegate(Translate("Change Human Player..."));
			_civSelectDelegate.PlayerSelected += ChangePlayer_Accept;
			_civSelectDelegate.Cancelled += ChangePlayer_Cancel;

			DrawDialog();
		}
	}
}