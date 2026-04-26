// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

// Author: Kevin Routley : July, 2019

using System;
using System.Linq;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
    internal class AddBuilding : BaseScreen
    {
        private readonly City[] _cities = Game.GetCities().OrderBy(x => x.Name).ToArray();
        private int OffsetX => Math.Max(0, (Width - 320) / 2);
        private int OffsetY => Math.Max(0, (Height - 200) / 2);

        private IBuilding[] _buildings;

        private Menu _citySelect;

        private int _index;

        private City _selectedCity;

        private IBuilding _selectedBldg;

        private Menu _bldgSelect;

        //public event EventHandler Cancel;

        // TODO fire-eggs generalize to eliminate duplication

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
                .DrawText("Add building...", 0, 15, xx + 8, yy + 3);

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
                _citySelect.Items.Add($"{city.Name} ({Game.GetPlayer(city.Owner).TribeName})").OnSelect(Accept);
            }

            if (more)
            {
                _citySelect.Items.Add($" ---MORE---").OnSelect(More);
            }

            _citySelect.Cancel += Cancel;
            _citySelect.MissClick += Cancel;
            _citySelect.ActiveItem = (_citySelect.Items.Count - 1);
        }

        private void More(object sender, EventArgs args)
        {
            _index += 15;
            if (_index > _cities.Count()) _index = 0;
            CloseMenus();
        }

        private void BMore(object sender, EventArgs args)
        {
            _index += 15;
            if (_index > _buildings.Length) _index = 0;
            CloseMenus();
        }

        private void BAccept(object sender, EventArgs args)
        {
            _selectedBldg = _buildings[_bldgSelect.ActiveItem + _index];
            _selectedCity.AddBuilding(_selectedBldg);
            Destroy();
        }

        private void Accept(object sender, EventArgs args)
        {
            _selectedCity = _cities[_citySelect.ActiveItem + _index];

            _index = 0;
            var currBldgs = _selectedCity.Buildings;
            _buildings = Reflect.GetBuildings().Where(b => currBldgs.All(x => x.Id != b.Id) && !(b is Palace)).ToArray();

            CloseMenus();
        }

        private void Cancel(object sender, EventArgs args)
        {
            if (sender is Input)
                ((Input)sender)?.Close();
            Destroy();
        }

        protected override bool HasUpdate(uint gameTick)
        {
            if (RefreshNeeded())
            {
                CloseMenus();
                if (_selectedCity == null)
                {
                    CitiesMenu();
                    AddMenu(_citySelect);
                }
                else if (_selectedBldg == null)
                {
                    BuildingsMenu();
                    AddMenu(_bldgSelect);
                }
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

            if (_selectedCity != null && _selectedBldg == null && Common.TopScreen.GetType() != typeof(Menu))
            {
                BuildingsMenu();
                AddMenu(_bldgSelect);
                return false;
            }
            return true;
        }

        private void BuildingsMenu()
        {
            Palette = Common.Screens.Last().OriginalColours;

            IBuilding[] bldgs = _buildings.Skip(_index).Take(15).ToArray();

            bool more = (bldgs.Length < _buildings.Length);

            int fontHeight = Resources.GetFontHeight(0);
            int hh = (fontHeight * (bldgs.Length + (more ? 2 : 1))) + 5;
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
                .DrawText("Select building...", 0, 15, xx + 8, yy + 3);

            _bldgSelect = new Menu(Palette, menuBackground)
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

            foreach (IBuilding bldg in bldgs)
            {
                _bldgSelect.Items.Add($"{bldg.Name}").OnSelect(BAccept);
            }

            if (more)
            {
                _bldgSelect.Items.Add($" ---MORE---").OnSelect(BMore);
            }

            _bldgSelect.Cancel += Cancel;
            _bldgSelect.MissClick += Cancel;
            _bldgSelect.ActiveItem = (_bldgSelect.Items.Count - 1);
        }


        public AddBuilding() : base(MouseCursor.Pointer)
        {
            if (_cities.Length == 0)
            {
                GameTask.Enqueue(Message.General($"There are no cities yet."));
                return;
            }

            CitiesMenu();
        }

    }
}
