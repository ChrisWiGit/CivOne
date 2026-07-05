// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CivOne.Screens
{
	/// <summary>
	/// Provides managed menu lifecycle support for <see cref="BaseScreen"/>.
	/// This partial owns registration, lookup, closure, and disposal of menus created by derived screens.
	/// </summary>
	/// <remarks>
	/// Use this partial when a screen should expose menu overlays through a single, centralized ownership model.
	/// Derived screens typically override <see cref="CreateManagedMenu"/> and trigger lazy creation via <see cref="EnsureManagedMenu"/>.
	/// For the full usage guide and examples, see docs/BaseScreen.ManagedMenus.md.
	/// </remarks>
	public abstract partial class BaseScreen
	{
		private readonly List<IMenu> _menus = [];
		private bool _managedMenuInitialized;

		[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "The Menus property is protected and only intended for use within the BaseScreen class and its derived classes. It is not exposed publicly, and it is used to manage the collection of menus associated with the screen. The List<IMenu> type is appropriate for this internal use case, as it provides the necessary functionality for managing the menus without exposing unnecessary complexity to external consumers.")]
		protected List<IMenu> Menus => _menus;

		protected bool HasMenu => Menus.Count != 0;

		protected TMenu? GetMenu<TMenu>() where TMenu : class, IMenu
		{
			return Menus.OfType<TMenu>().FirstOrDefault();
		}

		/// <summary>
		/// Override to provide a lazily created, single managed menu for this screen.
		/// Return null when no managed menu is needed.
		/// </summary>
		protected virtual IMenu? CreateManagedMenu()
		{
			return null;
		}

		/// <summary>
		/// Creates and adds the managed menu once.
		/// Returns the existing or created menu instance, or null when no menu is provided.
		/// </summary>
		protected IMenu? EnsureManagedMenu()
		{
			if (_managedMenuInitialized)
			{
				return Menus.FirstOrDefault();
			}

			_managedMenuInitialized = true;
			IMenu? menu = CreateManagedMenu();
			if (menu == null)
			{
				return null;
			}

			AddMenu(menu);
			return menu;
		}
		
		protected void AddMenu(IMenu menu)
		{
			Menus.Add(menu);
			Common.AddScreen(menu);
		}

		protected void CloseMenus(string? menuId = null)
		{
			foreach (IMenu menu in Menus)
			{
				if (menuId != null && menu.Id != menuId) continue;
				menu.Close();
			}
			Menus.RemoveAll(x => menuId == null || x.Id == menuId);
		}

		private void DisposeMenus()
		{
			foreach (IMenu menu in Menus.Distinct().ToArray())
			{
				menu.Dispose();
			}

			Menus.Clear();
		}
	}
}