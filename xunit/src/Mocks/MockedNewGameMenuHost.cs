using System;
using System.Collections.Generic;
using CivOne.Graphics;
using CivOne.Screens;
using CivOne.Screens.NewGamePanels;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Stands in for the New Game screen while testing the menu delegates.
	/// Creates real menus so entries, values and key handlers can be inspected, but keeps them out of the
	/// screen stack and records how often the delegate closed the open menus.
	/// </summary>
	internal sealed class MockedNewGameMenuHost : INewGameMenuHost, IDisposable
	{
		private readonly List<Menu> _createdMenus = [];
		private readonly List<Menu> _shownMenus = [];

		/// <summary>
		/// Number of times the delegate closed the open menus.
		/// </summary>
		public int CloseCount { get; private set; }

		/// <summary>
		/// Vertical shift the delegate asked for when creating the last menu.
		/// </summary>
		public int LastYOffset { get; private set; }

		/// <summary>
		/// The menu currently shown, or <see langword="null"/> when none is open.
		/// </summary>
		public Menu? CurrentMenu => _shownMenus.Count == 0 ? null : _shownMenus[^1];

		/// <summary>
		/// The menu currently shown.
		/// Fails with an exception when no menu is open, so tests do not need a null check.
		/// </summary>
		public Menu OpenMenu => CurrentMenu ?? throw new InvalidOperationException("No menu is currently shown.");

		/// <summary>
		/// Creates a menu without registering it anywhere.
		/// </summary>
		/// <param name="title">Menu title.</param>
		/// <param name="yOffset">Vertical shift relative to the default menu position.</param>
		/// <returns>The created menu.</returns>
		public Menu CreateNewGameMenu(string title, int yOffset = 0)
		{
			LastYOffset = yOffset;
			Menu menu = new("NewGameMenu", new Palette())
			{
				Title = title
			};
			_createdMenus.Add(menu);
			return menu;
		}

		/// <summary>
		/// Records a menu as shown.
		/// </summary>
		/// <param name="menu">The menu to show.</param>
		public void ShowMenu(Menu menu)
		{
			_shownMenus.Add(menu);
		}

		/// <summary>
		/// Records that every open menu was closed.
		/// </summary>
		public void CloseOpenMenus()
		{
			CloseCount++;
			_shownMenus.Clear();
		}

		/// <summary>
		/// Returns the entry texts of the menu currently shown.
		/// </summary>
		/// <returns>The texts, in menu order.</returns>
		public string[] MenuTexts()
		{
			Menu menu = OpenMenu;
			string[] texts = new string[menu.Items.Count];
			for (int i = 0; i < texts.Length; i++)
			{
				texts[i] = menu.Items[i].Text ?? string.Empty;
			}
			return texts;
		}

		/// <summary>
		/// Returns the entry values of the menu currently shown.
		/// </summary>
		/// <returns>The values, in menu order.</returns>
		public int[] MenuValues()
		{
			Menu menu = OpenMenu;
			int[] values = new int[menu.Items.Count];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = menu.Items[i].Value;
			}
			return values;
		}

		/// <summary>
		/// Selects an entry of the menu currently shown, as a click or the enter key would.
		/// </summary>
		/// <param name="index">Zero-based index of the entry.</param>
		public void SelectItem(int index)
		{
			OpenMenu.Items[index].Select();
		}

		/// <summary>
		/// Selects the last entry of the menu currently shown.
		/// </summary>
		public void SelectLastItem()
		{
			SelectItem(OpenMenu.Items.Count - 1);
		}

		/// <summary>
		/// Disposes every menu this host created.
		/// </summary>
		public void Dispose()
		{
			foreach (Menu menu in _createdMenus)
			{
				menu.Dispose();
			}
			_createdMenus.Clear();
			_shownMenus.Clear();
		}
	}
}
