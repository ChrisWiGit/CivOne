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