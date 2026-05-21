// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Wonders;

namespace CivOne.Screens.Reports
{
	[ScreenResizeable]
	[Modal]
	internal class WorldWonders : BaseScreen
	{
		private struct CityWonders
		{
			public City City { get; set; }
			public IWonder Wonder { get; set; }
		}

		private bool _update = true;

		private int _page = 0;

		private readonly CityWonders[] _wonders;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private void Render()
		{
			this.Clear(3);
			this.DrawText(Translate("The Wonders of the World"), 0, 5, OffsetX + 100, OffsetY + 13)
				.DrawText(Translate("The Wonders of the World"), 0, 15, OffsetX + 100, OffsetY + 12);

			this.FillRectangle(OffsetX + 8, OffsetY + 32, 304, 160, 3);

			for (int i = (_page * 7); i < _wonders.Length && i < ((_page + 1) * 7); i++)
			{
				IWonder wonder = _wonders[i].Wonder;
				City city = _wonders[i].City;

				int xx = OffsetX + 8;
				int yy = OffsetY + 32 + (24 * (i % 7));
				int ww = 304;
				int hh = 16;

				byte colour = 12;
				if (city != null && city.Size > 0)
					colour = Common.ColourLight[city.Owner];
				this.FillRectangle(xx, yy, ww, hh, colour)
					.FillRectangle(xx + 1, yy + 1, ww - 2, hh - 2, 3)
					.AddLayer(wonder.SmallIcon, xx + 8, yy + 3)
					.DrawText(wonder.FormatWorldWonder(city), 0, 15, xx + 32, yy + 5);
			}
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;
			Render();

			_update = false;
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (((_page + 1) * 7) >= _wonders.Length)
				Destroy();
			else
			{
				_page++;
				_update = true;
			}
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (((_page + 1) * 7) >= _wonders.Length)
				Destroy();
			else
			{
				_page++;
				_update = true;
			}
			return true;
		}
		
		public WorldWonders()
		{
			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			_wonders = Game.BuiltWonders.OrderBy(w => w.Id).Select(w => new CityWonders()
			{
				Wonder = w,
				City = Game.GetCities().First(c => c.HasWonder(w))
			}).ToArray();
			
			Render();
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}
	}
}