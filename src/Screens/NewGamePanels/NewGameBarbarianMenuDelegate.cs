using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Services;
using CivOne.UserInterface;

namespace CivOne.Screens.NewGamePanels
{
	/// <summary>
	/// Builds and drives the barbarian menu reached from the competition menu.
	/// The menu offers the barbarian sources a game can use and writes the choice straight into the
	/// global setting, because the classic SVE save format has no room to store it per game.
	/// </summary>
	internal class NewGameBarbarianMenuDelegate
	{
		private readonly INewGameMenuHost _host;
		private readonly Action _returnToCompetitionMenu;
		private readonly Func<BarbarianActivity>? _readActivity;
		private readonly Action<BarbarianActivity>? _writeActivity;
		private readonly ITranslationService? _translationService;

		/// <summary>
		/// Creates the barbarian menu delegate.
		/// </summary>
		/// <param name="host">The screen that owns and shows the menus.</param>
		/// <param name="returnToCompetitionMenu">Called to reopen the competition menu this menu was entered from.</param>
		/// <param name="readActivity">Reads the current setting. Falls back to the global setting.</param>
		/// <param name="writeActivity">Stores a picked setting. Falls back to the global setting.</param>
		/// <param name="translationService">Translation service for the menu texts. Falls back to the active service.</param>
		public NewGameBarbarianMenuDelegate(
			INewGameMenuHost host,
			Action returnToCompetitionMenu,
			Func<BarbarianActivity>? readActivity = null,
			Action<BarbarianActivity>? writeActivity = null,
			ITranslationService? translationService = null)
		{
			ArgumentNullException.ThrowIfNull(host);
			ArgumentNullException.ThrowIfNull(returnToCompetitionMenu);

			_host = host;
			_returnToCompetitionMenu = returnToCompetitionMenu;
			_readActivity = readActivity;
			_writeActivity = writeActivity;
			_translationService = translationService;
		}

		/// <summary>
		/// The screen that owns and shows the menus.
		/// </summary>
		protected INewGameMenuHost Host => _host;

		/// <summary>
		/// Translation service for the menu texts.
		/// </summary>
		protected virtual ITranslationService Translation => _translationService ?? TranslationServiceFactory.GetCurrent();

		/// <summary>
		/// The barbarian sources currently selected.
		/// </summary>
		public virtual BarbarianActivity Current => _readActivity?.Invoke() ?? Settings.Instance.BarbarianActivity;

		/// <summary>
		/// The barbarian sources offered by the menu, in menu order.
		/// </summary>
		public virtual BarbarianActivity[] Options =>
		[
			BarbarianActivity.None,
			BarbarianActivity.Villages,
			BarbarianActivity.LandRaids,
			BarbarianActivity.SeaRaids,
			BarbarianActivity.Raids,
			BarbarianActivity.Villages | BarbarianActivity.LandRaids,
			BarbarianActivity.Villages | BarbarianActivity.SeaRaids,
			BarbarianActivity.VillagesAndRaids
		];

		/// <summary>
		/// The text of the entry that leads here from the competition menu, including the current value.
		/// </summary>
		public virtual string MenuEntryText => Translation.TranslateFormatted("Barbarians: {0}", Current.ToShortText());

		/// <summary>
		/// Shows the barbarian menu with the current value preselected.
		/// </summary>
		public virtual void ShowMenu()
		{
			Menu menu = Host.CreateNewGameMenu(Translation.Translate("Barbarians..."));
			menu.Hints = [
				Translation.Translate("Esc: Back"),
				Translation.Translate("Changes the global setting")
			];

			foreach (BarbarianActivity activity in Options)
			{
				BarbarianActivity value = activity;
				menu.Items.Add(value.ToText(), (int)value)
					.SetActive(() => Current == value)
					.OnSelect(ItemSelected);
			}

			menu.Cancel += MenuCancel;
			Host.ShowMenu(menu);
		}

		/// <summary>
		/// Stores a picked setting and returns to the competition menu.
		/// </summary>
		/// <param name="activity">The picked barbarian sources.</param>
		public virtual void Select(BarbarianActivity activity)
		{
			if (_writeActivity != null)
			{
				_writeActivity(activity);
			}
			else
			{
				Settings.Instance.BarbarianActivity = activity;
			}

			Host.CloseOpenMenus();
			_returnToCompetitionMenu();
		}

		/// <summary>
		/// Closes the menu without changing the setting and returns to the competition menu.
		/// </summary>
		public virtual void CancelMenu()
		{
			Host.CloseOpenMenus();
			_returnToCompetitionMenu();
		}

		private void MenuCancel(object? sender, EventArgs args)
		{
			CancelMenu();
		}

		private void ItemSelected(object sender, MenuItemEventArgs<int> args)
		{
			ArgumentNullException.ThrowIfNull(args);

			BarbarianActivity activity = (BarbarianActivity)args.Value;
			// Enum.IsDefined does not accept flag combinations, so check against the known flags instead.
			if ((activity & ~BarbarianActivity.VillagesAndRaids) != 0)
			{
				return;
			}

			Select(activity);
		}
	}
}
