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
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.UserInterface;

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

		public event EventHandler Accept, Cancel;

		private void ChangePlayer_Accept(Player player)
		{
			_selectedPlayer = player;

			if (_selectedPlayer != Game.HumanPlayer)
			{
				Game.HumanPlayer = _selectedPlayer;
				Game.EndTurn(3);
			}

			if (Accept != null)
				Accept(this, EventArgs.Empty);
			Destroy();
		}

		private void ChangePlayer_Cancel(object sender, EventArgs args)
		{
			if (Cancel != null)
				Cancel(this, EventArgs.Empty);
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				DrawDialog();
			}

			if (_selectedPlayer == null && Common.TopScreen.GetType() != typeof(Menu))
			{
				AddMenu(_civSelectDelegate.Menu);
				return false;
			}
			return false;
		}

		public ChangeHumanPlayer() : base(MouseCursor.Pointer)
		{
			Palette = Common.Screens[Common.Screens.Count() - 1].OriginalColours;
			_civSelectDelegate = new CivSelectMenuDelegate(Palette, "Change Human Player...");
			_civSelectDelegate.PlayerSelected += ChangePlayer_Accept;
			_civSelectDelegate.Cancelled += ChangePlayer_Cancel;

			DrawDialog();
		}
	}
}