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

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class GameOptions : BaseScreen
	{
		private Menu _menu;

		private void MenuCancel(object sender, EventArgs args)
		{
			Destroy();
		}

		private void MenuAnimations(object sender, EventArgs args)
		{
			Game.Animations = !Game.Animations;
			Update();
		}

		private void MenuSound(object sender, EventArgs args)
		{
			Game.Sound = !Game.Sound;
			Update();
		}

		private void MenuEnemyMoves(object sender, EventArgs args)
		{
			Game.EnemyMoves = !Game.EnemyMoves;
			Update();
		}

		private void MenuCivilopediaText(object sender, EventArgs args)
		{
			Game.CivilopediaText = !Game.CivilopediaText;
			Update();
		}

		private void MenuInstantAdvice(object sender, EventArgs args)
		{
			Game.InstantAdvice = !Game.InstantAdvice;
			Update();
		}

		private void MenuAutoSave(object sender, EventArgs args)
		{
			Game.AutoSave = !Game.AutoSave;
			Update();
		}

		private void MenuEndOfTurn(object sender, EventArgs args)
		{
			Game.EndOfTurn = !Game.EndOfTurn;
			Update();
		}

		private void MenuPalace(object sender, EventArgs args)
		{
			Game.Palace = !Game.Palace;
			Update();
		}

		private void Update()
		{
			CloseMenus();
			_menu = null;
			Refresh();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			int menuBoxWidth = 103;
			int menuBoxHeight = 79;

			Picture menuGfx = new Picture(103, 79)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.DrawText("Options:", 0, 15, 4, 4)
				.As<Picture>();

			IBitmap menuBackground = menuGfx[2, 11, menuBoxWidth, menuBoxHeight]
				.ColourReplace((7, 11), (22, 3));

			this.FillRectangle(24, 16, menuBoxWidth + 2, menuBoxHeight + 2, colour: 5); // produces black border, +2 because of round errors when resizing
			this.AddLayer(menuGfx, 25, 17);

			CreateMenu(menuBackground);

			return true;
		}

		private void CreateMenu(IBitmap menuBackground)
		{
			if (_menu != null)
			{
				// The menu does not have to be recreated if it already exists
				// Otherwise Selection is lost when resizing the screen
				return;
			}
			_menu = new Menu(Palette, menuBackground)
			{
				X = 27,
				Y = 28,
				MenuWidth = 99,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = 0,
				Indent = 2
			};
			_menu.MissClick += MenuCancel;
			_menu.Cancel += MenuCancel;

			_menu.Items.Add($"{(Game.InstantAdvice ? '^' : ' ')}Instant Advice").OnSelect(MenuInstantAdvice);
			_menu.Items.Add($"{(Game.AutoSave ? '^' : ' ')}AutoSave").SetEnabled(Common.AllowSaveGame).OnSelect(MenuAutoSave);
			_menu.Items.Add($"{(Game.EndOfTurn ? '^' : ' ')}End of Turn").OnSelect(MenuEndOfTurn);
			_menu.Items.Add($"{(Game.Animations ? '^' : ' ')}Animations").OnSelect(MenuAnimations);
			_menu.Items.Add($"{(Game.Sound ? '^' : ' ')}Sound").OnSelect(MenuSound);
			_menu.Items.Add($"{(Game.EnemyMoves ? '^' : ' ')}Enemy Moves").OnSelect(MenuEnemyMoves);
			_menu.Items.Add($"{(Game.CivilopediaText ? '^' : ' ')}Civilopedia Text").OnSelect(MenuCivilopediaText);
			_menu.Items.Add($"{(Game.Palace ? '^' : ' ')}Palace").OnSelect(MenuPalace);

			AddMenu(_menu);
		}

		public GameOptions() : base(MouseCursor.Pointer)
		{
			Palette = Common.DefaultPalette;
			this.AddLayer(Common.Screens.Last(), 0, 0)
				.FillRectangle(24, 16, 105, 81, 5);
		}
	}
}