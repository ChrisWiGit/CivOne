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
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class MeetWithKing : BaseScreen
	{
		private readonly Menu _civSelect;

		private readonly Player[] _players;
		private readonly int _menuWidth;
		private readonly int _menuHeight;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private Player _selectedPlayer = null;

		public event EventHandler Accept, Cancel;

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
				.DrawText(Translate("Meet With King"), 0, 15, xx + 8, yy + 3);

			_civSelect.X = xx + 2;
			_civSelect.Y = yy + 11;
			_civSelect.ForceUpdate();
		}

		private void MeetKing_Accept(object sender, EventArgs args)
		{
			_selectedPlayer = _players[_civSelect.ActiveItem];

			if (_selectedPlayer != Game.HumanPlayer)
			{
				Common.AddScreen(new King(_selectedPlayer));
			}

			if (Accept != null)
				Accept(this, EventArgs.Empty);
			Destroy();
		}

		private void MeetKing_Cancel(object sender, EventArgs args)
		{
			if (Cancel != null)
				Cancel(this, EventArgs.Empty);
			if (sender is Input input)
				input.Close();
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
			return false;
		}

		public MeetWithKing() : base(MouseCursor.Pointer)
		{
			Palette = Common.Screens[Common.Screens.Count() - 1].OriginalColours;
			_players = Game.Players.Where(p => p != 0 && p != Human).ToArray();

			int fontHeight = Resources.GetFontHeight(0);
			_menuHeight = (fontHeight * (_players.Length + 1)) + 5;
			_menuWidth = 144;

			Picture menuGfx = new Picture(_menuWidth, _menuHeight)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();
			IBitmap menuBackground = menuGfx[2, 11, _menuWidth - 4, _menuHeight - 11].ColourReplace((7, 11), (22, 3));

			_civSelect = new Menu(Palette, menuBackground)
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

			foreach (Player player in _players)
			{
				_civSelect.Items.Add($"{player.LeaderName} ({player.TribeName})").OnSelect(MeetKing_Accept);
			}

			_civSelect.Cancel += MeetKing_Cancel;
			_civSelect.MissClick += MeetKing_Cancel;
			if (_players.Length > 0)
			{
				_civSelect.ActiveItem = 0;
			}

			DrawDialog();
		}
	}
}