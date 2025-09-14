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
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.IO;
using CivOne.Units;
using CivOne.UserInterface;
using CivOne.Wonders;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class CityChooseProduction : BaseScreen
	{
		private readonly City _city;

		private readonly IProduction[] _availableProduction;
		private readonly int _fontId = 0;
		private readonly List<IProduction[]> _pages = new List<IProduction[]>();

		private bool _update = true;
		private int _menuHeight;
		private int _page = 0;

		private Menu _menu;

		private void MenuCancel(object sender, EventArgs args)
		{
			CloseMenus();
			Destroy();
		}

		private void ProductionChoice(object sender, EventArgs args)
		{
			if (_pages.Count > 1 && ((sender as MenuItem<int>).Value == _pages[_page].Length))
			{
				CloseMenus();
				_page++;
				if (_page >= _pages.Count) _page = 0;
				_menuHeight = Resources.GetFontHeight(1) * (_pages[_page].Length + 1);
				_update = true;
				return;
			}
			_city.SetProduction(_pages[_page].ToArray()[(sender as MenuItem<int>).Value]);
			MenuCancel(sender, args);
		}

		private void ProductionContext(object sender, EventArgs args)
		{
			if (_pages.Count > 1 && ((sender as MenuItem<int>).Value == _pages[_page].Length))
			{
				ProductionChoice(sender, args);
				return;
			}
			ICivilopedia page = (_pages[_page][(sender as MenuItem<int>).Value] as ICivilopedia);
			Common.AddScreen(new Civilopedia(page, icon: false));
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (_update)
			{
				_update = false;

				this.Clear();

				// CW: Refactoring idea would be to remove all calculations from here into a separate method called only once
				//    and store the results to be used here for drawing.

				List<string> menuItems = new List<string>();
				string menuHeaderText = $"What shall we build in {_city.Name}?";
				int itemWidth = Resources.GetTextSize(_fontId, menuHeaderText).Width;
				foreach (IProduction production in _pages[_page])
				{
					string menuText = string.Empty;
					if (production is IUnit)
					{
						IUnit unit = (production as IUnit);
						int turns = ((int)unit.Price * 10) - _city.Shields;
						if (_city.ShieldIncome > 1)
							turns = (int)Math.Ceiling((double)turns / _city.ShieldIncome);
						if (turns < 1) turns = 1;
						menuText = $"{unit.Name} ({turns} turns, ADM:{unit.Attack}/{unit.Defense}/{unit.Move})";
						if (Resources.GetTextSize(_fontId, menuText).Width > itemWidth) itemWidth = Resources.GetTextSize(_fontId, menuText).Width;
					}
					if (production is IBuilding)
					{
						IBuilding building = (production as IBuilding);
						int turns = ((int)building.Price * 10) - _city.Shields;
						if (_city.ShieldIncome > 1)
							turns = (int)Math.Ceiling((double)turns / _city.ShieldIncome);
						if (turns < 1) turns = 1;
						menuText = $"{building.Name} ({turns} turns)";
						if (Resources.GetTextSize(_fontId, menuText).Width > itemWidth) itemWidth = Resources.GetTextSize(_fontId, menuText).Width;
					}
					if (production is IWonder)
					{
						IWonder wonder = (production as IWonder);
						int turns = ((int)wonder.Price * 10) - _city.Shields;
						if (_city.ShieldIncome > 1)
							turns = (int)Math.Ceiling((double)turns / _city.ShieldIncome);
						if (turns < 1) turns = 1;
						menuText = $"{wonder.Name} ({turns} turns)";
						if (Game.WonderObsolete(wonder)) menuText = $"*{menuText}";
						if (Resources.GetTextSize(_fontId, menuText).Width > itemWidth) itemWidth = Resources.GetTextSize(_fontId, menuText).Width;
					}
					menuItems.Add(menuText);
				}
				if (_pages.Count > 1)
				{
					menuItems.Add("More...");
				}
				itemWidth += 10;

				int width = itemWidth + 14;
				int height = _menuHeight + 10 + Resources.GetFontHeight(_fontId);

				using Picture menuGfx = new(width, height);

				menuGfx.Tile(Pattern.PanelGrey)
					.DrawRectangle3D()
					.DrawText(menuHeaderText, _fontId, 15, 4, 4)
					.DrawText($"(Help available)", 1, 10, width, height - Resources.GetFontHeight(1), TextAlign.Right);

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
			if (_menu != null)
			{
				// The menu does not have to be recreated if it already exists
				// Otherwise Selection is lost when resizing the screen
				return;
			}
			_menu = new Menu(Palette, background)
			{
				X = 83,
				Y = 12 + Resources.GetFontHeight(_fontId),
				MenuWidth = itemWidth,
				ActiveColour = 11,
				TextColour = 5,
				FontId = _fontId
			};

			int i = 0;
			foreach (string item in menuItems)
			{
				_menu.Items.Add(item, i++)
					.OnSelect(ProductionChoice)
					.OnContext(ProductionContext)
					.OnHelp(ProductionContext);
			}
			_menu.MenuWidth += 10;
			_menu.MissClick += MenuCancel;
			_menu.Cancel += MenuCancel;

			AddMenu(_menu);
		}

		protected override void Resize(int width, int height)
		{
			_update = true;
			_menu.Refresh();
			base.Resize(width, height);
		}

		public CityChooseProduction(City city) : base(MouseCursor.Pointer)
		{
			_city = city;

			Palette = Common.DefaultPalette;

			_availableProduction = _city.AvailableProduction.ToArray();
			_menuHeight = Resources.GetFontHeight(0) * _availableProduction.Length;
			if (_menuHeight > 170)
			{
				_fontId = 1;
				_menuHeight = Resources.GetFontHeight(1) * _availableProduction.Length;
				if (_menuHeight > 170)
				{
					_pages.Add(_availableProduction.Where(p => (p is IUnit)).Take(28).ToArray());
					if (_availableProduction.Count(p => (p is IBuilding || p is IWonder)) > 28)
					{
						_pages.Add(_availableProduction.Where(p => (p is IBuilding)).Take(28).ToArray());
						_pages.Add(_availableProduction.Where(p => (p is IWonder)).Take(28).ToArray());
					}
					else
					{
						_pages.Add(_availableProduction.Where(p => (p is IBuilding || p is IWonder)).Take(28).ToArray());
					}
					_menuHeight = Resources.GetFontHeight(1) * (_pages[0].Length + 1);
					return;
				}
			}
			_pages.Add(_availableProduction);
		}
	}
}