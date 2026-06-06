// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.UserInterface
{
	public class MenuItemCollection<T> : IEnumerable<MenuItem<T>>
	{
		private readonly List<MenuItem<T>> _menuItems = [];

		private void HandlePluginActions(object? _, EventArgs args)
		{
			if (Id == null) return;
			foreach (MenuModification mod in Reflect.GetModifications<MenuModification>().Where(x => x.MenuId == Id))
			foreach(MenuItem<T> item in _menuItems.Where(x => x != null))
			{
				(string? Text, string? Shortcut) = mod.ChangeMenuItemText(item.Text, item.Shortcut);
				item.Text = Text;
				item.Shortcut = Shortcut;
			}
		}

		internal string? Id { get; private set; }

		public event EventHandler? ItemsChanged;

		public int Count => _menuItems.Count;

		public void Add(MenuItem<T> menuItem)
		{
			_menuItems.Add(menuItem);
			ItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		public MenuItem<T> Add(string text, T? value = default)
		{
			MenuItem<T> menuItem = MenuItem<T>.Create(text, value);
			_menuItems.Add(menuItem);
			ItemsChanged?.Invoke(this, EventArgs.Empty);
			return menuItem;
		}

		public void AddRange(IEnumerable<MenuItem<T>> menuItems)
		{
			_menuItems.AddRange(menuItems);
			ItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		public void AddRange(params MenuItem<T>[] menuItems) => AddRange(menuItems.ToList());

		public MenuItem<T> InsertAt(int index, string text, T value)
		{
			if (index < 0) index = 0;
			if (_menuItems.Count <= index) index = _menuItems.Count;
			
			MenuItem<T> menuItem = MenuItem<T>.Create(text, value);
			_menuItems.Insert(index, menuItem);
			ItemsChanged?.Invoke(this, EventArgs.Empty);
			return menuItem;
		}

		public void Remove(int index)
		{
			if (index < 0) return;
			if (_menuItems.Count <= index) return;
			_menuItems.RemoveAt(index);
			ItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		public void Remove(T value)
		{
			IEnumerable<MenuItem<T>> items = _menuItems
					.Where(x => x.Value != null)
					.Where(x => x.Value!.Equals(value));
			if (!items.Any()) return;
			_menuItems.RemoveAll(x => items.Contains(x));
			ItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		public IEnumerator<MenuItem<T>> GetEnumerator()
		{
			return _menuItems.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<MenuItem<T>>)_menuItems).GetEnumerator();
		}

		public MenuItem<T> this[int index]
		{
			get
			{
				if (index < 0 || index >= _menuItems.Count)
					throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the menu item collection.");
				return _menuItems[index];
			}
		}

		/// <summary>
		/// Initializes a new instance of the MenuItemCollection class with the specified identifier.
		/// </summary>
		/// <param name="id">The identifier for the menu item collection.</param>
		public MenuItemCollection(string? id)
		{
			Id = id;

			ItemsChanged += HandlePluginActions;
		}
	}
}