using System;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Events;
using CivOne.Services;
using CivOne.Services.Random;
using CivOne.UserInterface;

namespace CivOne.Screens.NewGamePanels
{
	/// <summary>
	/// Builds and drives the "Pick your tribe" menus.
	/// The first menu lists the civilizations of the original game, the extended menu lists the
	/// civilizations added by this project, in pages.
	/// The delegate owns the navigation between both menus and reports a picked civilization, or a
	/// rename request, back to the screen.
	/// </summary>
	internal class NewGameTribeMenuDelegate
	{
		/// <summary>
		/// Number of civilizations shown on one page of the extended menu.
		/// </summary>
		public const int ExtendedMenuPageSize = 13;

		/// <summary>
		/// Vertical shift of the tribe menus relative to the default menu position.
		/// </summary>
		public const int MenuYOffset = -8;

		private const int OpenExtendedValue = -1000;
		private const int ScrollPreviousValue = -1001;
		private const int ScrollNextValue = -1002;
		private const int BackToClassicValue = -1003;

		private readonly INewGameMenuHost _host;
		private readonly NewGameRulesDelegate _rules;
		private readonly Func<ICivilization[]> _availableTribes;
		private readonly Action<int> _tribeSelected;
		private readonly Action _cancelled;
		private readonly Action<int> _renameRequested;
		private readonly ITranslationService? _translationService;
		private readonly IRandomService? _randomService;
		private readonly MenuPagingDelegate _paging;

		/// <summary>
		/// Creates the tribe menu delegate.
		/// </summary>
		/// <param name="host">The screen that owns and shows the menus.</param>
		/// <param name="rules">The new game rules, used to tell classic from added civilizations.</param>
		/// <param name="availableTribes">Returns the civilizations that may currently be picked.</param>
		/// <param name="tribeSelected">Called with the index of the picked civilization.</param>
		/// <param name="cancelled">Called when the first menu is cancelled.</param>
		/// <param name="renameRequested">Called with the index of the civilization the player wants to rename.</param>
		/// <param name="translationService">Translation service for the menu texts. Falls back to the active service.</param>
		/// <param name="randomService">Random source used when no civilization is selected on a rename. Falls back to the shared service.</param>
		/// <param name="paging">Paging state of the extended menu. A default one is used when omitted.</param>
		public NewGameTribeMenuDelegate(
			INewGameMenuHost host,
			NewGameRulesDelegate rules,
			Func<ICivilization[]> availableTribes,
			Action<int> tribeSelected,
			Action cancelled,
			Action<int> renameRequested,
			ITranslationService? translationService = null,
			IRandomService? randomService = null,
			MenuPagingDelegate? paging = null)
		{
			ArgumentNullException.ThrowIfNull(host);
			ArgumentNullException.ThrowIfNull(rules);
			ArgumentNullException.ThrowIfNull(availableTribes);
			ArgumentNullException.ThrowIfNull(tribeSelected);
			ArgumentNullException.ThrowIfNull(cancelled);
			ArgumentNullException.ThrowIfNull(renameRequested);

			_host = host;
			_rules = rules;
			_availableTribes = availableTribes;
			_tribeSelected = tribeSelected;
			_cancelled = cancelled;
			_renameRequested = renameRequested;
			_translationService = translationService;
			_randomService = randomService;
			_paging = paging ?? new MenuPagingDelegate(ExtendedMenuPageSize);
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
		/// Random source used when a rename is requested without a civilization being selected.
		/// </summary>
		protected virtual IRandomService RandomService => _randomService ?? RandomServiceFactory.Create();

		/// <summary>
		/// The civilizations that may currently be picked.
		/// </summary>
		protected ICivilization[] AvailableTribes => _availableTribes();

		/// <summary>
		/// Moves the extended menu back to its first page.
		/// </summary>
		public virtual void Reset()
		{
			Paging.Reset();
		}

		/// <summary>
		/// Shows the menu with the civilizations of the original game.
		/// </summary>
		public virtual void ShowMenu()
		{
			Menu menu = CreateTribeMenu(Translation.Translate("Pick your tribe..."));

			ICivilization[] tribes = AvailableTribes;
			for (int i = 0; i < tribes.Length; i++)
			{
				if (Rules.IsExtendedCivilization(tribes[i]))
				{
					continue;
				}

				menu.Items.Add(tribes[i].Name, i).OnSelect(ItemSelected);
			}

			if (tribes.Any(Rules.IsExtendedCivilization))
			{
				menu.Items.Add(Translation.Translate("New Civilizations..."), OpenExtendedValue).OnSelect(ItemSelected);
			}

			menu.Cancel += MainMenuCancel;
			Host.ShowMenu(menu);
		}

		/// <summary>
		/// Shows one page of the civilizations added by this project.
		/// Falls back to <see cref="ShowMenu"/> when there are none.
		/// </summary>
		public virtual void ShowExtendedMenu()
		{
			(int Index, ICivilization Civilization)[] extendedCivilizations = GetExtendedCivilizations();
			if (extendedCivilizations.Length == 0)
			{
				ShowMenu();
				return;
			}

			Menu menu = CreateTribeMenu(Translation.Translate("Pick new civilization..."));
			menu.Items.Add(Translation.Translate("Original Civilizations..."), BackToClassicValue).OnSelect(ItemSelected);

			Paging.ClampOffset(extendedCivilizations.Length);
			if (!Paging.IsFirstPage)
			{
				menu.Items.Add(Translation.Translate("Previous civilizations..."), ScrollPreviousValue).OnSelect(ItemSelected);
			}

			int endExclusive = Paging.PageEndExclusive(extendedCivilizations.Length);
			for (int i = Paging.PageStart; i < endExclusive; i++)
			{
				(int index, ICivilization civilization) = extendedCivilizations[i];
				menu.Items.Add(civilization.Name, index).OnSelect(ItemSelected);
			}

			if (endExclusive < extendedCivilizations.Length)
			{
				menu.Items.Add(Translation.Translate("Next civilizations..."), ScrollNextValue).OnSelect(ItemSelected);
			}

			menu.Cancel += ExtendedMenuCancel;
			Host.ShowMenu(menu);
		}

		private Menu CreateTribeMenu(string title)
		{
			Menu menu = Host.CreateNewGameMenu(title, MenuYOffset);
			menu.Hints = [
				Translation.Translate("Esc: Back"),
				Translation.Translate("R: Rename")
			];
			menu.OnCustomKeyDown = args => RenameKeyDown(menu, args);
			return menu;
		}

		private (int Index, ICivilization Civilization)[] GetExtendedCivilizations()
		{
			return [.. AvailableTribes
				.Select((civilization, index) => (Index: index, Civilization: civilization))
				.Where(entry => Rules.IsExtendedCivilization(entry.Civilization))];
		}

		/// <summary>
		/// Closes the menus and reports that the player left the tribe selection.
		/// </summary>
		public virtual void CancelMenu()
		{
			Host.CloseOpenMenus();
			_cancelled();
		}

		/// <summary>
		/// Closes the extended menu and returns to the menu with the classic civilizations.
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

		private bool RenameKeyDown(Menu menu, KeyboardEventArgs args)
		{
			if (args.KeyChar != 'r' && args.KeyChar != 'R')
			{
				return false;
			}

			int selectedTribeIndex = menu.SelectedItem.Value;
			ICivilization[] tribes = AvailableTribes;
			if (selectedTribeIndex < 0 || selectedTribeIndex >= tribes.Length)
			{
				selectedTribeIndex = RandomService.NextInt(tribes.Length);
			}

			Host.CloseOpenMenus();
			_renameRequested(selectedTribeIndex);
			return true;
		}

		private void ItemSelected(object sender, MenuItemEventArgs<int> args)
		{
			ArgumentNullException.ThrowIfNull(args);

			switch (args.Value)
			{
				case OpenExtendedValue:
					Host.CloseOpenMenus();
					ShowExtendedMenu();
					return;
				case BackToClassicValue:
					Host.CloseOpenMenus();
					ShowMenu();
					return;
				case ScrollPreviousValue:
					Paging.PreviousPage();
					Host.CloseOpenMenus();
					ShowExtendedMenu();
					return;
				case ScrollNextValue:
					Paging.NextPage(GetExtendedCivilizations().Length);
					Host.CloseOpenMenus();
					ShowExtendedMenu();
					return;
				default:
					Host.CloseOpenMenus();
					_tribeSelected(args.Value);
					return;
			}
		}
	}
}
