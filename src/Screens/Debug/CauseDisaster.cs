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
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class CauseDisaster : BaseScreen
	{
		private readonly City[] _cities = Game.GetCities().OrderBy(x => x.Name).ToArray();
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private Menu _citySelect;

		private int _index = 0;

		private City _selectedCity = null;

		public string Value { get; private set; }

		public event EventHandler Cancel;

		private void CitiesMenu()
		{
			Palette = Common.Screens.Last().OriginalColours;

			City[] cities = _cities.Skip(_index).Take(15).ToArray();

			bool more = (cities.Length < _cities.Length);

			int fontHeight = Resources.GetFontHeight(0);
			int hh = (fontHeight * (cities.Length + (more ? 2 : 1))) + 5;
			int ww = 136;

			int xx = OffsetX + ((320 - ww) / 2);
			int yy = OffsetY + ((200 - hh) / 2);

			Picture menuGfx = new Picture(ww, hh)
				.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.As<Picture>();
			IBitmap menuBackground = menuGfx[2, 11, ww - 4, hh - 11].ColourReplace((7, 11), (22, 3));

			this.Clear();
			this.FillRectangle(xx - 1, yy - 1, ww + 2, hh + 2, 5)
				.AddLayer(menuGfx, xx, yy)
				.DrawText(Translate("Cause disaster..."), 0, 15, xx + 8, yy + 3);

			_citySelect = new Menu(Palette, menuBackground)
			{
				X = xx + 2,
				Y = yy + 11,
				MenuWidth = ww - 4,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = 0,
				Indent = 8
			};

			foreach (City city in cities)
			{
				_citySelect.Items.Add($"{city.Name} ({Game.GetPlayer(city.Owner).TribeName})").OnSelect(CauseDisaster_Accept);
			}

			if (more)
			{
				_citySelect.Items.Add($" {Translate("---MORE---")}").OnSelect(CauseDisaster_More);
			}

			_citySelect.Cancel += CauseDisaster_Cancel;
			_citySelect.MissClick += CauseDisaster_Cancel;
			_citySelect.ActiveItem = (_citySelect.Items.Count - 1);
		}

		private void CauseDisaster_More(object sender, EventArgs args)
		{
			_index += 15;
			if (_index > _cities.Count()) _index = 0;
			CloseMenus();
		}

		private void CauseDisaster_Accept(object sender, EventArgs args)
		{
			_selectedCity = _cities[_citySelect.ActiveItem + _index];
			_selectedCity.Disaster();
			Destroy();
		}

		private void CauseDisaster_Cancel(object sender, EventArgs args)
		{
			if (Cancel != null)
				Cancel(this, null);
			if (sender is Input)
				((Input)sender)?.Close();
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded() && _selectedCity == null)
			{
				CloseMenus();
				CitiesMenu();
				AddMenu(_citySelect);
				return true;
			}

			if (_cities.Length == 0)
			{
				Destroy();
				return false;
			}

			if (_selectedCity == null && Common.TopScreen.GetType() != typeof(Menu))
			{
				AddMenu(_citySelect);
				return false;
			}

			return false;
		}

		public CauseDisaster() : base(MouseCursor.Pointer)
		{
			if (_cities.Length == 0)
			{
				GameTask.Enqueue(Message.General(Translate("There are no cities yet.")));
				return;
			}

			CitiesMenu();
		}
	}
}