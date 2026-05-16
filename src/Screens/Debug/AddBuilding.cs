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
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Tasks;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
    internal class AddBuilding : BaseScreen
    {
        private readonly City[] _cities = [.. Game.GetCities().OrderBy(x => x.Name)];
        private int OffsetX => Math.Max(0, (Width - 320) / 2);
        private int OffsetY => Math.Max(0, (Height - 200) / 2);

        private CivSelectMenuDelegate _playerSelect;

        private City[] _playerCities = [];
        private IBuilding[] _buildings;
        private CityGridMenuDelegate _citySelect;
        private GridMenuDelegate _buildingSelect;

        private Player _selectedPlayer;
        private City _selectedCity;

        private void EnsurePlayerSelectDelegate()
        {
            if (_playerSelect != null && _menus.Contains(_playerSelect.Menu)) return;
            _playerSelect = CreatePlayerSelectDelegate();
        }

        private void DrawPlayerMenuDialog()
        {
            EnsurePlayerSelectDelegate();
            _playerSelect.DrawDialog(this, OffsetX, OffsetY);
        }

        private CivSelectMenuDelegate CreatePlayerSelectDelegate()
        {
            Palette palette = Palette
                ?? Common.Screens.LastOrDefault()?.OriginalColours
                ?? Common.DefaultPalette;

            CivSelectMenuDelegate delegate_ = new(palette, "Add building...");
            delegate_.PlayerSelected += OnPlayerSelected;
            delegate_.Cancelled += Cancel;
            return delegate_;
        }

        private void OnPlayerSelected(Player player)
        {
            _selectedPlayer = player;
            _playerCities = [.. _cities.Where(c => c.Owner == Game.PlayerNumber(player))];

            if (_playerCities.Length == 0)
            {
                _selectedPlayer = null;
                _citySelect = null;
                Refresh();
                return;
            }

            _citySelect = null;
            Refresh();
        }

        private void CreateCityGrid()
        {
            Palette = Common.Screens[Common.Screens.Count() - 1].OriginalColours;
            _citySelect = new CityGridMenuDelegate(_playerCities);
            _citySelect.CitySelected += OnCitySelected;
            _citySelect.Cancelled += Cancel;
        }

        private void DrawCityMenuDialog()
        {
            if (_citySelect == null)
            {
                CreateCityGrid();
            }

            _citySelect.Draw(this, "Select city...", CanvasHeight);
        }

        private void OnCitySelected(City city)
        {
            _selectedCity = city;

            _buildings = [.. Reflect.GetBuildings().Where(b => b is not Palace).OrderBy(b => b.Name)];
            _buildingSelect = null;

            Refresh();
        }

        private void CreateBuildingGrid()
        {
            Palette = Common.Screens[Common.Screens.Length - 1].OriginalColours;

            string[] labels = [.. _buildings.Select(x => x.Name)];
            _buildingSelect = new GridMenuDelegate(
                labels,
                GridMenuDelegate.SelectionMode.CheckUncheck,
                i => _selectedCity.HasBuilding(_buildings[i]),
                fontId: 0);
            _buildingSelect.ItemChecked += OnBuildingToggled;
            _buildingSelect.Cancelled += OnBuildingSelectionCancelled;
        }

        private void OnBuildingSelectionCancelled(object sender, EventArgs args)
        {
            _selectedCity = null;
            _buildingSelect = null;
            Refresh();
        }

        private void DrawBuildingSelection()
        {
            if (_buildingSelect == null)
            {
                CreateBuildingGrid();
            }
            _buildingSelect.Draw(this, "Toggle buildings...", CanvasHeight);
        }

        private void OnBuildingToggled(int index)
        {
            if (index < 0 || index >= _buildings.Length) return;

            IBuilding building = _buildings[index];
            if (_selectedCity.HasBuilding(building))
            {
                _selectedCity.RemoveBuilding(building);
            }
            else
            {
                _selectedCity.AddBuilding(building);
            }

            Refresh();
        }

        private void Cancel(object sender, EventArgs args)
        {
            if (sender is Input input)
                input.Close();
            Destroy();
        }

        private bool IsBuildingSelectionActive => _selectedPlayer != null && _selectedCity != null;

        private void HandleRefreshNeededState()
        {
            if (_selectedPlayer == null)
            {
                DrawPlayerMenuDialog();
                if (!_menus.Contains(_playerSelect.Menu))
                {
                    AddMenu(_playerSelect.Menu);
                }
                return;
            }

            CloseMenus();

            if (_selectedCity == null)
            {
                DrawCityMenuDialog();
                return;
            }

            if (IsBuildingSelectionActive)
            {
                DrawBuildingSelection();
            }
        }

        protected override bool HasUpdate(uint gameTick)
        {
            if (RefreshNeeded())
            {
                HandleRefreshNeededState();
                return true;
            }

            if (_cities.Length == 0)
            {
                Destroy();
                return false;
            }

            if (_selectedPlayer == null)
            {
                if (!_menus.Contains(_playerSelect.Menu))
                {
                    DrawPlayerMenuDialog();
                    AddMenu(_playerSelect.Menu);
                }
                return false;
            }

            if (_selectedCity == null)
            {
                DrawCityMenuDialog();
                return false;
            }

            if (IsBuildingSelectionActive)
            {
                DrawBuildingSelection();
                return false;
            }
            return false;
        }

        public override bool KeyDown(KeyboardEventArgs args)
		{
            if (_selectedPlayer != null && _selectedCity == null && _citySelect != null)
            {
                bool handled = _citySelect.KeyDown(args);
                if (handled) Refresh();
                return handled;
            }

            if (IsBuildingSelectionActive && _buildingSelect != null)
			{
				bool handled = _buildingSelect.KeyDown(args);
				if (handled) Refresh();
				return handled;
			}

			if (args.Key == Key.Escape)
			{
				Destroy();
				return true;
			}

			return false;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
            if (_selectedPlayer != null && _selectedCity == null && _citySelect != null)
            {
                bool handled = _citySelect.MouseDown(args.X, args.Y);
                if (handled) Refresh();
                return handled;
            }

            if (IsBuildingSelectionActive && _buildingSelect != null)
			{
				bool handled = _buildingSelect.MouseDown(args.X, args.Y);
				if (handled) Refresh();
				return handled;
			}

			return false;
		}


        public AddBuilding() : base(MouseCursor.Pointer)
        {
            Palette = Common.Screens.LastOrDefault()?.OriginalColours ?? Common.DefaultPalette;

            if (_cities.Length == 0)
            {
                GameTask.Enqueue(Message.General($"There are no cities yet."));
                return;
            }

            _playerSelect = CreatePlayerSelectDelegate();
            DrawPlayerMenuDialog();
        }

    }
}
