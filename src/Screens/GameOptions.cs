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
using CivOne.Services.Screen;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class GameOptions : BaseScreen
	{
		private void MenuCancel(object? _, EventArgs __)
		{
			Destroy();
		}

		private void MenuAnimations(object? _, EventArgs __)
		{
			Game.Animations = !Game.Animations;
			Update();
		}

		private void MenuSound(object? _, EventArgs __)
		{
			Game.Sound = !Game.Sound;
			Update();
		}

		private void MenuEnemyMoves(object? _, EventArgs __)
		{
			Game.EnemyMoves = !Game.EnemyMoves;
			Update();
		}

		private void MenuCivilopediaText(object? _, EventArgs __)
		{
			Game.CivilopediaText = !Game.CivilopediaText;
			Update();
		}

		private void MenuInstantAdvice(object? _, EventArgs __)
		{
			Game.InstantAdvice = !Game.InstantAdvice;
			Update();
		}

		private void MenuAutoSave(object? _, EventArgs __)
		{
			Game.AutoSave = !Game.AutoSave;
			Update();
		}

		private void MenuEndOfTurn(object? _, EventArgs __)
		{
			Game.EndOfTurn = !Game.EndOfTurn;
			Update();
		}

		private void MenuPalace(object? _, EventArgs __)
		{
			Game.Palace = !Game.Palace;
			Update();
		}

		private void MenuChangeLanguage(object? _, EventArgs __)
		{
			Common.AddScreen(new LanguageScreen());
		}

		private void Update()
		{
			CloseMenus();
			Refresh();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			int menuBoxWidth = 103;
			int menuBoxHeight = 88;

			Picture menuGfx = new(menuBoxWidth, menuBoxHeight);
			menuGfx
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.DrawText(Translate("Options:"), 0, 15, 4, 4);

			IBitmap menuBackground = menuGfx[2, 11, menuBoxWidth, menuBoxHeight]
				.ColourReplace((7, 11), (22, 3));

			this.FillRectangle(24, 16, menuBoxWidth + 2, menuBoxHeight + 2, colour: 5); // produces black border, +2 because of round errors when resizing
			this.AddLayer(menuGfx, 25, 17);

			CreateMenu(menuBackground);

			return true;
		}

		private void CreateMenu(IBitmap menuBackground)
		{
			Menu? menu = GetMenu<Menu>();
			if (menu != null)
			{
				// The menu does not have to be recreated if it already exists
				// Otherwise Selection is lost when resizing the screen
				return;
			}
			menu = new Menu(Palette, menuBackground)
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
			menu.MissClick += MenuCancel;
			menu.Cancel += MenuCancel;

			menu.Items.Add($"{(Game.InstantAdvice ? '^' : ' ')}{Translate("Instant Advice")}").OnSelect(MenuInstantAdvice);
			menu.Items.Add($"{(Game.AutoSave ? '^' : ' ')}{Translate("AutoSave")}").SetEnabled(Common.AllowSaveGame).OnSelect(MenuAutoSave);
			menu.Items.Add($"{(Game.EndOfTurn ? '^' : ' ')}{Translate("End of Turn")}").OnSelect(MenuEndOfTurn);
			menu.Items.Add($"{(Game.Animations ? '^' : ' ')}{Translate("Animations")}").OnSelect(MenuAnimations);
			menu.Items.Add($"{(Game.Sound ? '^' : ' ')}{Translate("Sound")}").OnSelect(MenuSound);
			menu.Items.Add($"{(Game.EnemyMoves ? '^' : ' ')}{Translate("Enemy Moves")}").OnSelect(MenuEnemyMoves);
			menu.Items.Add($"{(Game.CivilopediaText ? '^' : ' ')}{Translate("Civilopedia Text")}").OnSelect(MenuCivilopediaText);
			menu.Items.Add($"{(Game.Palace ? '^' : ' ')}{Translate("Palace")}").OnSelect(MenuPalace);
			menu.Items.Add($" {Translate("Change language...")}").OnSelect(MenuChangeLanguage);

			AddMenu(menu);
		}

		public GameOptions() : base(MouseCursor.Pointer)
		{
			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			this.AddLayer(ScreenServiceFactory.CreateQueryService().LastScreen!, 0, 0)
				.FillRectangle(24, 16, 105, 90, 5);
		}
	}
}