using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Services;
using CivOne.UserInterface;

namespace CivOne.Screens.NewGamePanels
{
	/// <summary>
	/// Builds and drives the "Level of Competition" menus.
	/// The first menu offers the opponent counts of the original game, the extended menu offers every
	/// count the game supports, in pages.
	/// The delegate owns the navigation between both menus and reports a picked opponent count back to
	/// the screen.
	/// </summary>
	internal class NewGameCompetitionMenuDelegate
	{
		/// <summary>
		/// Number of opponent counts shown on one page of the extended menu.
		/// </summary>
		public const int ExtendedMenuPageSize = 11;

		private const int OpenExtendedValue = -900;
		private const int OpenBarbarianValue = -903;
		private const int ScrollLessValue = -901;
		private const int ScrollMoreValue = -902;

		private readonly INewGameMenuHost _host;
		private readonly NewGameRulesDelegate _rules;
		private readonly Action<int> _opponentsSelected;
		private readonly Action _cancelled;
		private readonly ITranslationService? _translationService;
		private readonly MenuPagingDelegate _paging;
		private readonly NewGameBarbarianMenuDelegate? _barbarianMenu;

		/// <summary>
		/// Creates the competition menu delegate.
		/// </summary>
		/// <param name="host">The screen that owns and shows the menus.</param>
		/// <param name="rules">The new game rules, used for the offered opponent counts and their texts.</param>
		/// <param name="opponentsSelected">Called with the picked opponent count.</param>
		/// <param name="cancelled">Called when the first menu is cancelled.</param>
		/// <param name="translationService">Translation service for the menu texts. Falls back to the active service.</param>
		/// <param name="paging">Paging state of the extended menu. A default one is used when omitted.</param>
		/// <param name="barbarianMenu">
		/// The barbarian menu reached from this menu.
		/// When omitted, the menu offers no barbarian entry.
		/// </param>
		public NewGameCompetitionMenuDelegate(
			INewGameMenuHost host,
			NewGameRulesDelegate rules,
			Action<int> opponentsSelected,
			Action cancelled,
			ITranslationService? translationService = null,
			MenuPagingDelegate? paging = null,
			NewGameBarbarianMenuDelegate? barbarianMenu = null)
		{
			ArgumentNullException.ThrowIfNull(host);
			ArgumentNullException.ThrowIfNull(rules);
			ArgumentNullException.ThrowIfNull(opponentsSelected);
			ArgumentNullException.ThrowIfNull(cancelled);

			_host = host;
			_rules = rules;
			_opponentsSelected = opponentsSelected;
			_cancelled = cancelled;
			_translationService = translationService;
			_paging = paging ?? new MenuPagingDelegate(ExtendedMenuPageSize);
			_barbarianMenu = barbarianMenu;
		}

		/// <summary>
		/// The screen that owns and shows the menus.
		/// </summary>
		protected INewGameMenuHost Host => _host;

		/// <summary>
		/// The new game rules.
		/// </summary>
		protected NewGameRulesDelegate Rules => _rules;

		/// <summary>
		/// Paging state of the extended menu.
		/// </summary>
		protected MenuPagingDelegate Paging => _paging;

		/// <summary>
		/// Translation service for the menu texts.
		/// </summary>
		protected virtual ITranslationService Translation => _translationService ?? TranslationServiceFactory.GetCurrent();

		/// <summary>
		/// Moves the extended menu back to its first page.
		/// </summary>
		public virtual void Reset()
		{
			Paging.Reset();
		}

		/// <summary>
		/// Shows the menu with the opponent counts of the original game.
		/// </summary>
		public virtual void ShowMenu()
		{
			Menu menu = Host.CreateNewGameMenu(Translation.Translate("Level of Competition..."));
			menu.Hints = [Translation.Translate("Esc: Back")];
			menu.OnCustomKeyDown = MainMenuKeyDown;

			for (int opponents = Rules.MainMenuMaxOpponents; opponents >= Rules.MainMenuMinOpponents; opponents--)
			{
				menu.Items.Add(Rules.FormatCivilizationCount(opponents), opponents).OnSelect(ItemSelected);
			}

			menu.Items.Add(Translation.Translate("More Civilizations..."), OpenExtendedValue).TextColor(3).OnSelect(ItemSelected);
			if (_barbarianMenu != null)
			{
				menu.Items.Add(_barbarianMenu.MenuEntryText, OpenBarbarianValue).TextColor(3).OnSelect(ItemSelected);
			}
			menu.Cancel += MainMenuCancel;
			Host.ShowMenu(menu);
		}

		/// <summary>
		/// Shows one page of every opponent count the game supports.
		/// </summary>
		/// <param name="selectFirstNumberItem">
		/// When <see langword="true"/>, the first opponent count is preselected instead of a navigation entry.
		/// </param>
		public virtual void ShowExtendedMenu(bool selectFirstNumberItem = false)
		{
			Menu menu = Host.CreateNewGameMenu(Translation.Translate("More Civilizations..."));
			menu.Hints = [Translation.Translate("Esc: Back")];
			menu.OnCustomKeyDown = ExtendedMenuKeyDown;

			int totalEntries = Rules.OpponentCount;
			Paging.ClampOffset(totalEntries);

			bool needsPaging = Paging.RequiresPaging(totalEntries);
			if (needsPaging)
			{
				menu.Items.Add(Translation.Translate("Back..."), ScrollLessValue).OnSelect(ItemSelected);
			}

			int startValue = Rules.MinOpponents + Paging.PageStart;
			int endValueExclusive = Rules.MinOpponents + Paging.PageEndExclusive(totalEntries);
			for (int value = startValue; value < endValueExclusive; value++)
			{
				menu.Items.Add(Rules.FormatCivilizationCount(value), value).OnSelect(ItemSelected);
			}

			if (needsPaging && !Paging.IsLastPage(totalEntries))
			{
				menu.Items.Add(Translation.Translate("More..."), ScrollMoreValue).OnSelect(ItemSelected);
			}

			menu.Cancel += ExtendedMenuCancel;
			if (selectFirstNumberItem)
			{
				menu.ActiveItem = needsPaging ? 1 : 0;
			}
			Host.ShowMenu(menu);
		}

		/// <summary>
		/// Closes the menus and reports that the player left the competition selection.
		/// </summary>
		public virtual void CancelMenu()
		{
			Host.CloseOpenMenus();
			_cancelled();
		}

		/// <summary>
		/// Closes the extended menu and returns to the menu with the classic opponent counts.
		/// </summary>
		public virtual void CancelExtendedMenu()
		{
			Host.CloseOpenMenus();
			ShowMenu();
		}

		private void MainMenuCancel(object? sender, EventArgs args)
		{
			CancelMenu();
		}

		private void ExtendedMenuCancel(object? sender, EventArgs args)
		{
			CancelExtendedMenu();
		}

		private bool MainMenuKeyDown(KeyboardEventArgs args)
		{
			switch (args.Key)
			{
				case Key.PageDown:
					Paging.Reset();
					Host.CloseOpenMenus();
					ShowExtendedMenu(selectFirstNumberItem: true);
					return true;
				case Key.PageUp:
					Paging.MoveToLastPage(Rules.OpponentCount);
					Host.CloseOpenMenus();
					ShowExtendedMenu(selectFirstNumberItem: true);
					return true;
				default:
					return false;
			}
		}

		private bool ExtendedMenuKeyDown(KeyboardEventArgs args)
		{
			int totalEntries = Rules.OpponentCount;

			switch (args.Key)
			{
				case Key.PageUp:
					if (!Paging.PreviousPage())
					{
						Host.CloseOpenMenus();
						ShowMenu();
						return true;
					}

					Host.CloseOpenMenus();
					ShowExtendedMenu(selectFirstNumberItem: true);
					return true;
				case Key.PageDown:
					if (!Paging.NextPage(totalEntries))
					{
						Host.CloseOpenMenus();
						ShowMenu();
						return true;
					}

					Host.CloseOpenMenus();
					ShowExtendedMenu(selectFirstNumberItem: true);
					return true;
				default:
					return false;
			}
		}

		private void ItemSelected(object sender, MenuItemEventArgs<int> args)
		{
			ArgumentNullException.ThrowIfNull(args);

			switch (args.Value)
			{
				case OpenExtendedValue:
					Paging.Reset();
					Host.CloseOpenMenus();
					ShowExtendedMenu();
					return;
				case OpenBarbarianValue:
					if (_barbarianMenu == null)
					{
						return;
					}

					Host.CloseOpenMenus();
					_barbarianMenu.ShowMenu();
					return;
				case ScrollLessValue:
					if (!Paging.PreviousPage())
					{
						Host.CloseOpenMenus();
						ShowMenu();
						return;
					}

					Host.CloseOpenMenus();
					ShowExtendedMenu(selectFirstNumberItem: true);
					return;
				case ScrollMoreValue:
					Paging.NextPage(Rules.OpponentCount);
					Host.CloseOpenMenus();
					ShowExtendedMenu(selectFirstNumberItem: true);
					return;
				default:
					Host.CloseOpenMenus();
					_opponentsSelected(args.Value);
					return;
			}
		}
	}
}
