// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class CustomizeWorld : BaseScreen
	{
		private int _landMass = -1, _temperature = -1, _climate = -1, _age = -1;
		private bool _hasUpdate = true;

		private bool _closing = false;
		private readonly Picture _background;

		private int OffsetX => ((Width - 320) / 2);
		private int OffsetY => ((Height - 200) / 2);

		private void DrawBackground()
		{
			this.Clear();
			this.AddLayer(_background, OffsetX, OffsetY);
		}
		
		private int GetMenuWidth(string title, string[] items)
		{
			int i = 0;
			Picture[] texts = new Picture[items.Length + 1];
			texts[i++] = Resources.GetText(" " + title, 0, 15);
			foreach (string item in items)
				texts[i++] = Resources.GetText(" " + item, 0, 5);
			return (texts.Select(t => t.Width).Max()) + 6;
		}
		
		private Menu CreateMenu(int y, string title, MenuItemEventHandler<int> setChoice, params string[] menuTexts)
		{
			Menu menu = new Menu(Palette)
			{
				Title = title,
				X = 203,
				Y = y,
				MenuWidth = GetMenuWidth(title, menuTexts),
				TitleColour = 15,
				ActiveColour = 11,
				TextColour = 79,
				DisabledColour = 8,
				FontId = 0,
				CenterTo320Coordinates = true
			};
			
			for (int i = 0; i < menuTexts.Length; i++)
			{
				menu.Items.Add(menuTexts[i], i).OnSelect(setChoice);
			}
			menu.ActiveItem = 1;
			return menu;
		}
		
		private void SetLandMass(object sender, MenuItemEventArgs<int> args)
		{
			Log("Customize World - Land Mass: {0}", _landMass);
			_landMass = args.Value;
			_hasUpdate = true;
		}
		
		private void SetTemperature(object sender, MenuItemEventArgs<int> args)
		{
			Log("Customize World - Temperature: {0}", _temperature);
			_temperature = args.Value;
			_hasUpdate = true;
		}
		
		private void SetClimate(object sender, MenuItemEventArgs<int> args)
		{
			Log("Customize World - Climate: {0}", _climate);
			_climate = args.Value;
			_hasUpdate = true;
		}
		
		private void SetAge(object sender, MenuItemEventArgs<int> args)
		{
			Log("Customize World - Age: {0}", _age);
			_age = args.Value;
			_hasUpdate = true;
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_closing)
			{
				if (!HandleScreenFadeOut())
				{
					Destroy();
					Map.Generate(_landMass, _temperature, _climate, _age);
					if (!Runtime.Settings.ShowIntro)
					{
						Common.AddScreen(new NewGame());
					}
					else
					{
						Common.AddScreen(new Intro());
					}
				}
				return true;
			}
			
			if (!_hasUpdate) return false;
			
			if (_landMass < 0) AddMenu(CreateMenu(6, Translate("LAND MASS:"), SetLandMass, Translate("Small"), Translate("Normal"), Translate("Large")));
			else if (_temperature < 0) AddMenu(CreateMenu(56, Translate("TEMPERATURE:"), SetTemperature, Translate("Cool"), Translate("Temperate"), Translate("Warm")));
			else if (_climate < 0) AddMenu(CreateMenu(106, Translate("CLIMATE:"), SetClimate, Translate("Arid"), Translate("Normal"), Translate("Wet")));
			else if (_age < 0) AddMenu(CreateMenu(156, Translate("AGE:"), SetAge, Translate("3 billion years"), Translate("4 billion years"), Translate("5 billion years")));
			else
			{
				_closing = true;
				foreach (IScreen menu in _menus)
					this.AddLayer(menu);
				CloseMenus();
				return true;
			}
			
			_hasUpdate = false;
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			DrawBackground();
		}
		
		public CustomizeWorld()
		{
			_background = Resources["CUSTOM"];
			Palette = _background.Palette;
			DrawBackground();
		}
	}
}