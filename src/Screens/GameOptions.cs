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
using CivOne.Screens.Options;
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

		private void MenuAutoSaveOnQuit(object? _, EventArgs __)
		{
			Settings.Instance.AutoSaveOnQuit = !Settings.Instance.AutoSaveOnQuit;
			Update();
		}

		private void MenuConfirmExit(object? _, EventArgs __)
		{
			Settings.Instance.ConfirmExit = !Settings.Instance.ConfirmExit;
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

		private const int MenuFontId = 0;
		private const int MenuIndent = 2;
		private const int MenuBoxLeft = 25;
		private const int MenuBoxTop = 17;
		private const int TitleLeft = 4;
		private const int TitleTop = 4;
		private const byte TitleColourValue = 15;
		private const byte BorderColour = 5;

		/// <summary>
		/// Extra pixel added to each side of the black border, to compensate rounding errors while resizing.
		/// </summary>
		private const int BorderTolerance = 2;

		private readonly GameOptionsMenuLayoutDelegate _layoutDelegate = new();
		private GameOptionsMenuEntry[] _entries = [];

		/// <summary>
		/// Builds all menu entries.
		/// Add a new option by adding a single entry here; the menu size follows automatically.
		/// </summary>
		private GameOptionsMenuEntry[] BuildEntries() =>
		[
			new(Option(Game.InstantAdvice, Translate("Instant Advice")), MenuInstantAdvice),
			new(Option(Game.AutoSave, Translate("AutoSave")), MenuAutoSave, Common.AllowSaveGame),
			new(Option(Settings.Instance.AutoSaveOnQuit, Translate("AutoSave on Quit")), MenuAutoSaveOnQuit, Common.AllowSaveGame),
			new(Option(Settings.Instance.ConfirmExit, Translate("Confirm Quit")), MenuConfirmExit),
			new(Option(Game.EndOfTurn, Translate("End of Turn")), MenuEndOfTurn),
			new(Option(Game.Animations, Translate("Animations")), MenuAnimations),
			new(Option(Game.Sound, Translate("Sound")), MenuSound),
			new(Option(Game.EnemyMoves, Translate("Enemy Moves")), MenuEnemyMoves),
			new(Option(Game.CivilopediaText, Translate("Civilopedia Text")), MenuCivilopediaText),
			new(Option(Game.Palace, Translate("Palace")), MenuPalace),
			new(Option(false, Translate("Change language...")), MenuChangeLanguage)
		];

		/// <summary>
		/// Prefixes a menu text with the check mark of a toggled option, or with a space when it is off.
		/// </summary>
		private static string Option(bool active, string text) => $"{(active ? '^' : ' ')}{text}";

		private GameOptionsMenuLayout RefreshLayout()
		{
			_entries = BuildEntries();
			return _layoutDelegate.Calculate(MenuFontId, MenuIndent, Translate("Options:"), [.. _entries.Select(entry => entry.Text)]);
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			GameOptionsMenuLayout layout = RefreshLayout();

			Picture menuGfx = new(layout.MenuBoxWidth, layout.MenuBoxHeight);
			menuGfx
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.DrawText(Translate("Options:"), MenuFontId, TitleColourValue, TitleLeft, TitleTop);

			IBitmap menuBackground = menuGfx[layout.MenuOffsetX, layout.MenuOffsetY, layout.MenuWidth, _entries.Length * layout.FontHeight]
				.ColourReplace((7, 11), (22, 3));

			DrawBorder(layout);
			this.AddLayer(menuGfx, MenuBoxLeft, MenuBoxTop);

			CreateMenu(menuBackground, layout);

			return true;
		}

		private void DrawBorder(GameOptionsMenuLayout layout) => this.FillRectangle(
			MenuBoxLeft - 1,
			MenuBoxTop - 1,
			layout.MenuBoxWidth + BorderTolerance,
			layout.MenuBoxHeight + BorderTolerance,
			colour: BorderColour);

		private void CreateMenu(IBitmap menuBackground, GameOptionsMenuLayout layout)
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
				X = MenuBoxLeft + layout.MenuOffsetX,
				Y = MenuBoxTop + layout.MenuOffsetY,
				MenuWidth = layout.MenuWidth,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = MenuFontId,
				Indent = MenuIndent
			};
			menu.MissClick += MenuCancel;
			menu.Cancel += MenuCancel;

			foreach (GameOptionsMenuEntry entry in _entries)
			{
				menu.Items.Add(entry.Text).SetEnabled(entry.Enabled).OnSelect(entry.OnSelect);
			}

			AddMenu(menu);
		}

		public GameOptions() : base(MouseCursor.Pointer)
		{
			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			this.AddLayer(ScreenServiceFactory.CreateQueryService().LastScreen!, 0, 0);
			DrawBorder(RefreshLayout());
		}
	}
}