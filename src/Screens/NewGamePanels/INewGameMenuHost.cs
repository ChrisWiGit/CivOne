namespace CivOne.Screens.NewGamePanels
{
	/// <summary>
	/// The screen services the new game menu delegates need.
	/// Keeps layout, palette and menu ownership in the screen while the delegates only decide which
	/// entries a menu has and where a selection leads.
	/// </summary>
	internal interface INewGameMenuHost
	{
		/// <summary>
		/// Creates an empty menu with the look and position used by the new game screen.
		/// The caller adds the entries and hands the menu back through <see cref="ShowMenu"/>.
		/// </summary>
		/// <param name="title">Menu title.</param>
		/// <param name="yOffset">Vertical shift relative to the default menu position.</param>
		/// <returns>The created menu.</returns>
		Menu CreateNewGameMenu(string title, int yOffset = 0);

		/// <summary>
		/// Shows a menu created by <see cref="CreateNewGameMenu"/> and transfers its ownership to the screen.
		/// </summary>
		/// <param name="menu">The menu to show.</param>
		void ShowMenu(Menu menu);

		/// <summary>
		/// Closes every menu currently shown by the screen.
		/// </summary>
		void CloseOpenMenus();
	}
}
