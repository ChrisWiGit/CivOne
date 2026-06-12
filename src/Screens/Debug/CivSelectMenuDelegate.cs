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
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	internal class CivSelectMenuDelegate : BaseInstance
	{
		private const int MinDialogWidth = 136;

		private readonly int _menuWidth;
		private readonly int _menuHeight;
		private readonly string _title;
		private readonly Menu _menu;

		public event Action<Player>? PlayerSelected;
		public event EventHandler? Cancelled;

		public Menu Menu => _menu;

		public void DrawDialog(IBitmap target, int offsetX, int offsetY)
		{
			int xx = offsetX + ((320 - _menuWidth) / 2);
			int yy = offsetY + ((200 - _menuHeight) / 2);

			Picture menuGfx = new(_menuWidth, _menuHeight);
			menuGfx
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D();

			target.Clear();
			target.FillRectangle(xx - 1, yy - 1, _menuWidth + 2, _menuHeight + 2, 5)
				.AddLayer(menuGfx, xx, yy, dispose: true)
				.DrawText(_title, 0, 15, xx + 8, yy + 3);

			_menu.X = xx + 2;
			_menu.Y = yy + 11;
		}

		private void OnAccept(object? _, EventArgs args)
		{
			var player = Game.GetPlayer((byte)_menu.ActiveItem);
			if (player == null) return;

			PlayerSelected?.Invoke(player);
		}

		private void OnMenuCancel(object? _, EventArgs args)
		{
			Cancelled?.Invoke(this, EventArgs.Empty);
		}

		private Menu CreateMenu(Palette palette)
		{
			Picture menuGfx = new(_menuWidth, _menuHeight);
			menuGfx
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D();

			IBitmap menuBackground = menuGfx[2, 11, _menuWidth - 4, _menuHeight - 11].ColourReplace((7, 11), (22, 3));

			Menu menu = new(palette, menuBackground)
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
				menu.Items.Add(player.TribeNamePlural).OnSelect(OnAccept);

			menu.Cancel += OnMenuCancel;
			menu.MissClick += OnMenuCancel;
			menu.ActiveItem = Game.PlayerNumber(Human);
			return menu;
		}

		// max length player oder title
		private static int CalculateMenuWidth(string title)
		{
			const int padding = 16;
			int maxTextWidth = Game.Players.Max(p => Resources.GetTextSize(0, p.TribeNamePlural).Width);
			int titleWidth = Resources.GetTextSize(0, title).Width;
			return Math.Max(maxTextWidth, titleWidth) + padding;
		}

		public CivSelectMenuDelegate(Palette palette, string title = "Select Civilization...")
		{
			_title = title;
			_menuWidth = Math.Max(CalculateMenuWidth(title), MinDialogWidth);
			_menuHeight = Resources.GetFontHeight(0) * (Game.Players.Count() + 2);
			_menu = CreateMenu(palette);
		}
	}
}
