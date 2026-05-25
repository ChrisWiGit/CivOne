// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.IO;
using CivOne.Units;
using CivOne.UserInterface;
using CivOne.Wonders;

namespace CivOne.Screens
{
	public enum ProductionFilterMode
	{
		All = 0,
		Units = 1,
		Buildings = 2,
		Wonders = 3
	}

	[ScreenResizeable]
	internal class CityChooseProduction : BaseScreen
	{
		private const int MaxItemsPerPage = 28;

		/// <summary>
		/// This is a convenience to remember the last used filter mode across multiple openings of the production chooser, so that players don't have to reselect their preferred filter every time. It is static because it should be shared across all instances of CityChooseProduction.
		/// However, it is not persisted and will reset to All when the game is restarted. 
		/// This is a design choice to keep the implementation simple, as persisting this preference would require additional code to save and load it with the game state, and it's not critical enough to justify that complexity. If players want to reset it, they can simply restart the game.
		/// </summary>
		private static ProductionFilterMode _lastUsedFilterMode = ProductionFilterMode.All;

		private readonly City _city;

		private readonly IProduction[] _availableProduction;
		private readonly int _fontId = 0;
		private readonly List<IProduction[]> _pages = new List<IProduction[]>();

		private bool _update = true;
		private int _menuHeight;
		private int _page = 0;

		private ProductionFilterMenu _menu;
		private ProductionFilterMode _filterMode = ProductionFilterMode.All;

		private static void SetLastUsedFilterMode(ProductionFilterMode filterMode)
		{
			_lastUsedFilterMode = filterMode;
		}

		private void SetFilterMode(ProductionFilterMode filterMode, bool remember = true)
		{
			_filterMode = filterMode;
			if (remember)
			{
				SetLastUsedFilterMode(filterMode);
			}
		}

		private void MenuCancel(object sender, EventArgs args)
		{
			CloseCurrentMenu();
			Destroy();
		}

		private void CloseCurrentMenu()
		{
			CloseMenus();
			_menu = null;
		}

		private IProduction[] GetFilteredProduction()
		{
			return _filterMode switch
			{
				ProductionFilterMode.Units => _availableProduction.Where(p => p is IUnit).ToArray(),
				ProductionFilterMode.Buildings => _availableProduction.Where(p => p is IBuilding).ToArray(),
				ProductionFilterMode.Wonders => _availableProduction.Where(p => p is IWonder).ToArray(),
				_ => _availableProduction
			};
		}

		private bool FilterHasEntries(ProductionFilterMode filterMode)
		{
			return filterMode switch
			{
				ProductionFilterMode.Units => _availableProduction.Any(p => p is IUnit),
				ProductionFilterMode.Buildings => _availableProduction.Any(p => p is IBuilding),
				ProductionFilterMode.Wonders => _availableProduction.Any(p => p is IWonder),
				_ => _availableProduction.Length > 0
			};
		}

		private ProductionFilterMode GetNextFilterWithEntries()
		{
			for (int i = 1; i <= Enum.GetValues<ProductionFilterMode>().Length; i++)
			{
				var nextMode = (ProductionFilterMode)(((int)_filterMode + i) % Enum.GetValues<ProductionFilterMode>().Length);
				if (FilterHasEntries(nextMode))
				{
					return nextMode;
				}
			}

			return _filterMode;
		}

		private void CycleFilter()
		{
			SetFilterMode(GetNextFilterWithEntries());
			_page = 0;
			CloseCurrentMenu();
			BuildPages();
			UpdateMenuHeight();
			_update = true;
		}

		private string GetFilterModeText()
		{
			return _filterMode switch
			{
				ProductionFilterMode.Units => Translate("Units"),
				ProductionFilterMode.Buildings => Translate("Buildings"),
				ProductionFilterMode.Wonders => Translate("Wonders"),
				_ => string.Empty
			};
		}

		private IProduction[] InsertSeparators(IProduction[] production)
		{
			var result = new List<IProduction>();
			bool hasUnits = false, hasBuildings = false, hasWonders = false;

			foreach (var item in production)
			{
				if (item is IUnit && !hasUnits) { result.Add(new ProductionSeparator(Translate("--- Units ---"))); hasUnits = true; }
				else if (item is IBuilding && !hasBuildings) { result.Add(new ProductionSeparator(Translate("--- Buildings ---"))); hasBuildings = true; }
				else if (item is IWonder && !hasWonders) { result.Add(new ProductionSeparator(Translate("--- Wonders ---"))); hasWonders = true; }
				result.Add(item);
			}

			return result.ToArray();
		}

		private void UpdateMenuHeight()
		{
			int additionalItems = _pages.Count > 1 ? 1 : 0;
			_menuHeight = Resources.GetFontHeight(_fontId) * (_pages[_page].Length + additionalItems);
		}

		private void BuildPages()
		{
			_pages.Clear();

			IProduction[] filteredProduction = GetFilteredProduction();
			if (filteredProduction.Length == 0 && _filterMode != ProductionFilterMode.All)
			{
				SetFilterMode(GetNextFilterWithEntries(), remember: false);
				filteredProduction = GetFilteredProduction();
			}
			IProduction[] productionWithSeparators = (_filterMode == ProductionFilterMode.All) 
				? InsertSeparators(filteredProduction) 
				: filteredProduction;

			foreach (IProduction[] page in productionWithSeparators.Chunk(MaxItemsPerPage))
			{
				_pages.Add(page);
			}

			if (_pages.Count == 0)
			{
				_pages.Add([]);
			}
		}

		private void ProductionChoice(object sender, EventArgs args)
		{
			if (_pages.Count > 1 && ((sender as MenuItem<int>).Value == _pages[_page].Length))
			{
				CloseCurrentMenu();
				_page++;
				if (_page >= _pages.Count) _page = 0;
				UpdateMenuHeight();
				_update = true;
				return;
			}
			var selectedProduction = _pages[_page][(sender as MenuItem<int>).Value];
			if (selectedProduction != null && !(selectedProduction is ProductionSeparator))
			{
				_city.SetProduction(selectedProduction);
				MenuCancel(sender, args);
			}
		}

		private void ProductionContext(object sender, EventArgs args)
		{
			if (_pages.Count > 1 && ((sender as MenuItem<int>).Value == _pages[_page].Length))
			{
				ProductionChoice(sender, args);
				return;
			}
			var selectedProduction = _pages[_page][(sender as MenuItem<int>).Value];
			if (selectedProduction != null && !(selectedProduction is ProductionSeparator))
			{
				ICivilopedia page = selectedProduction as ICivilopedia;
				Common.AddScreen(new Civilopedia(page, icon: false));
			}
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (_update)
			{
				_update = false;

				this.Clear();

				// CW: Refactoring idea would be to remove all calculations from here into a separate method called only once
				//    and store the results to be used here for drawing.

				List<string> menuItems = [];
				string filterLabel = _filterMode == ProductionFilterMode.All
					? string.Empty
					: $" [{GetFilterModeText()}]";

				string menuHeaderText = TranslateFormatted("What shall we build in {0}?{1}", _city.Name, filterLabel);
				int itemWidth = Resources.GetTextSize(_fontId, menuHeaderText).Width;
				foreach (IProduction production in _pages[_page])
				{
					string menuText = string.Empty;
					if (production is ProductionSeparator)
					{
						menuText = (production as ProductionSeparator).Text;
					}
					else if (production is IUnit)
					{
						IUnit unit = (production as IUnit);
						int turns = ((int)unit.Price * 10) - _city.Shields;
						if (_city.ShieldIncome > 1)
							turns = (int)Math.Ceiling((double)turns / _city.ShieldIncome);
						if (turns < 1) turns = 1;
						menuText = TranslateFormatted("{0} ({1} turns, ADM:{2}/{3}/{4})", unit.TranslatedName, turns, unit.Attack, unit.Defense, unit.Move);
						if (Resources.GetTextSize(_fontId, menuText).Width > itemWidth) itemWidth = Resources.GetTextSize(_fontId, menuText).Width;
					}
					else if (production is IBuilding)
					{
						IBuilding building = (production as IBuilding);
						int turns = ((int)building.Price * 10) - _city.Shields;
						if (_city.ShieldIncome > 1)
							turns = (int)Math.Ceiling((double)turns / _city.ShieldIncome);
						if (turns < 1) turns = 1;
						menuText = TranslateFormatted("{0} ({1} turns)", building.TranslatedName, turns);
						if (Resources.GetTextSize(_fontId, menuText).Width > itemWidth) itemWidth = Resources.GetTextSize(_fontId, menuText).Width;
					}
					else if (production is IWonder)
					{
						IWonder wonder = (production as IWonder);
						int turns = ((int)wonder.Price * 10) - _city.Shields;
						if (_city.ShieldIncome > 1)
							turns = (int)Math.Ceiling((double)turns / _city.ShieldIncome);
						if (turns < 1) turns = 1;
						menuText = TranslateFormatted("{0} ({1} turns)", wonder.TranslatedName, turns);
						if (Game.WonderObsolete(wonder)) menuText = $"*{menuText}";
						if (Resources.GetTextSize(_fontId, menuText).Width > itemWidth) itemWidth = Resources.GetTextSize(_fontId, menuText).Width;
					}
					menuItems.Add(menuText);
				}
				if (_pages.Count > 1)
				{
					menuItems.Add(Translate("More..."));
				}
				itemWidth += 10;

				int width = itemWidth + 14;
				int height = _menuHeight + 10 + Resources.GetFontHeight(_fontId);

				using Picture menuGfx = new(width, height);

				menuGfx.Tile(Pattern.PanelGrey)
					.DrawRectangle3D()
					.DrawText(menuHeaderText, _fontId, 15, 4, 4)
					.DrawText(Translate("(Help available, Tab=Filter)"), 1, 10, width, height - Resources.GetFontHeight(1), TextAlign.Right);

				this.FillRectangle(80, 8, width + 2, height + 2, 5); // produces black border, +2 because of round errors when resizing
				this.AddLayer(menuGfx, 81, 9);

				using Picture background = menuGfx[2, 3 + Resources.GetFontHeight(_fontId), itemWidth, Resources.GetFontHeight(_fontId) * menuItems.Count + 4];
				background.ColourReplace((7, 11), (22, 3));

				AddMenu(menuItems, itemWidth, background);
			}
			return true;
		}

		private void AddMenu(List<string> menuItems, int itemWidth, Picture background)
		{
			if (_menu != null && HasMenu)
			{
				return;
			}
			_menu = new ProductionFilterMenu(Palette, background)
			{
				X = 83,
				Y = 12 + Resources.GetFontHeight(_fontId),
				MenuWidth = itemWidth,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 8,
				FontId = _fontId
			};

			for (int i = 0; i < menuItems.Count; i++)
			{
				var menuItem = _menu.Items.Add(menuItems[i], i)
					.OnSelect(ProductionChoice)
					.OnContext(ProductionContext)
					.OnHelp(ProductionContext);
			
				// Disable separators
				if (i < _pages[_page].Length && _pages[_page][i] is ProductionSeparator)
				{
					menuItem.Disable();
				}
			}
			_menu.MenuWidth += 10;
			_menu.TabPressed += (s, a) => CycleFilter();
			_menu.MissClick += MenuCancel;
			_menu.Cancel += MenuCancel;

			AddMenu(_menu);
		}

		protected override void Resize(int width, int height)
		{
			_update = true;
			_menu?.Refresh();
			base.Resize(width, height);
		}

		public CityChooseProduction(City city) : base(MouseCursor.Pointer)
		{
			_city = city;

			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			_availableProduction = _city.AvailableProduction.ToArray();
			SetFilterMode(_lastUsedFilterMode, remember: false);
			if (!FilterHasEntries(_filterMode))
			{
				SetFilterMode(GetNextFilterWithEntries(), remember: false);
			}
			_menuHeight = Resources.GetFontHeight(0) * _availableProduction.Length;
			if (_menuHeight > 170)
			{
				_fontId = 1;
				_menuHeight = Resources.GetFontHeight(1) * _availableProduction.Length;
			}
			BuildPages();
			UpdateMenuHeight();
		}
	}
}