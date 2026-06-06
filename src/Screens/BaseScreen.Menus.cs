// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Linq;

namespace CivOne.Screens
{
	public abstract partial class BaseScreen
	{
		private readonly List<IMenu> _menus = [];

		protected List<IMenu> Menus => _menus;

		protected bool HasMenu => Menus.Count != 0;
		
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
	}
}