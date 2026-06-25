// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens
{
	/// <summary>
	/// Custom Menu subclass for production selection that handles Tab key for filtering
	/// </summary>
	internal class ProductionFilterMenu : Menu
	{
		public event System.EventHandler? TabPressed;

		public ProductionFilterMenu(Palette palette, IBitmap? background = null) 
			: base(palette, background)
		{
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args.Key == Key.Tab)
			{
				TabPressed?.Invoke(this, System.EventArgs.Empty);
				return true;
			}
			return base.KeyDown(args);
		}
	}
}
