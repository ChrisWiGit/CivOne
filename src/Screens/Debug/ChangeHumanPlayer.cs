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
		private readonly Menu<Player> _civSelect;

		private Player _selectedPlayer = null;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);
		private readonly int _menuWidth;
		private readonly int _menuHeight;

		private void DrawDialog()
		{
			int xx = OffsetX + ((320 - _menuWidth) / 2);
			int yy = OffsetY + ((200 - _menuHeight) / 2);

			Picture menuGfx = new Picture(_menuWidth, _menuHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();

			this.Clear();
			this.FillRectangle(xx - 1, yy - 1, _menuWidth + 2, _menuHeight + 2, 5)
				.AddLayer(menuGfx, xx, yy, dispose: true)
				.DrawText("Change Human Player...", 0, 15, xx + 8, yy + 3);

			_civSelect.X = xx + 2;
			_civSelect.Y = yy + 11;
			_civSelect.ForceUpdate();
		}

		public string Value { get; private set; }

		public event EventHandler Accept, Cancel;

		private void ChangePlayer_Accept(object sender, MenuItemEventArgs<Player> args)
		{
			_selectedPlayer = args.Value;

			if (_selectedPlayer != Game.HumanPlayer)
			{
				Game.HumanPlayer = _selectedPlayer;
				Game.EndTurn(3);
			}

			if (Accept != null)
				Accept(this, null);
			Destroy();
		}

		private void ChangePlayer_Cancel(object sender, EventArgs args)
		{
			if (Cancel != null)
				Cancel(this, null);
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
				AddMenu(_civSelect);
				return false;
			}
			return true;
		}

		public ChangeHumanPlayer() : base(MouseCursor.Pointer)
		{
			Palette = Common.Screens.Last().OriginalColours;
			int fontHeight = Resources.GetFontHeight(0);
			_menuHeight = (fontHeight * (Game.Players.Count() + 1)) + 5;
			_menuWidth = 128;

			Picture menuGfx = new Picture(_menuWidth, _menuHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();
			IBitmap menuBackground = menuGfx[2, 11, _menuWidth - 4, _menuHeight - 11].ColourReplace((7, 11), (22, 3));

			_civSelect = new Menu<Player>("ChangeHumanPlayer", Palette, menuBackground)
			{
				X = 0,
				Y = 0,
				MenuWidth = _menuWidth - 4,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = 0,
				Indent = 8
			};

			foreach (Player player in Game.Players)
			{
				_civSelect.Items.Add(player.TribeNamePlural, player).OnSelect(ChangePlayer_Accept);
			}

			_civSelect.Cancel += ChangePlayer_Cancel;
			_civSelect.MissClick += ChangePlayer_Cancel;
			_civSelect.ActiveItem = Game.PlayerNumber(Human);

			DrawDialog();
		}
	}
}