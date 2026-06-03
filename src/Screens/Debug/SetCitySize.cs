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
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class SetCitySize : BaseScreen
	{
		private readonly City[] _cities = Game.GetCities().OrderBy(x => x.Name).ToArray();
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private CityGridMenuDelegate _citySelect;

		private Input _input;

		private City? _selectedCity;

		public string Value { get; private set; }

		public event EventHandler Accept, Cancel;

		private void DrawInputDialog()
		{
			int ox = OffsetX;
			int oy = OffsetY;

			this.Clear();
			this.FillRectangle(80 + ox, 80 + oy, 161, 33, 11)
				.FillRectangle(81 + ox, 81 + oy, 159, 31, 15)
				.DrawText(Translate("Set City Size..."), 0, 5, 88 + ox, 82 + oy)
				.FillRectangle(88 + ox, 95 + oy, 105, 14, 5)
				.FillRectangle(89 + ox, 96 + oy, 103, 12, 15);

			if (_input != null)
			{
				_input.X = 90 + ox;
				_input.Y = 97 + oy;
			}
		}

		private void DrawCityMenuDialog()
		{
			if (_citySelect == null)
			{
				Palette = Common.Screens[Common.Screens.Count() - 1].OriginalColours;
				_citySelect = new CityGridMenuDelegate(
					_cities,
					city => $"{city.Name} ({Game.GetPlayer(city.Owner).TribeName})");
				_citySelect.CitySelected += OnCitySelected;
				_citySelect.Cancelled += CitySize_Cancel;
			}

			_citySelect.Draw(this, Translate("Set City Size..."), CanvasHeight);
		}

		private void CitySizeSet_Accept(object sender, EventArgs args)
		{
			Value = (sender as Input).Text;
			
			byte citySize;
			if (!byte.TryParse(Value, out citySize) || citySize < 1 || citySize > 99)
			{
				GameTask.Enqueue(Message.Error(Translate("-- DEBUG: Set City Size --"), TranslateFormattedArray("The value {0} is invalid or out of range.\nPlease enter a value between 1 and 99.", Value)));
			}
			else
			{
				_selectedCity.Size = citySize;
				GameTask.Enqueue(Message.General(TranslateFormatted("{0} size set to {1}.", _selectedCity.Name, citySize)));
			}

			if (Accept != null)
				Accept(this, EventArgs.Empty);
			if (sender is Input input)
				input.Close();
			Destroy();
		}

		private void OnCitySelected(City city)
		{
			Palette = Common.Screens[Common.Screens.Count() - 1].OriginalColours;
			_selectedCity = city;

			_input = new Input(Palette, _selectedCity.Size.ToString(), 0, 5, 11, 90 + OffsetX, 97 + OffsetY, 101, 10, 3);
			_input.Accept += CitySizeSet_Accept;
			_input.Cancel += CitySize_Cancel;

			Refresh();
		}

		private void CitySize_Cancel(object sender, EventArgs args)
		{
			if (Cancel != null)
				Cancel(this, EventArgs.Empty);
			if (sender is Input input)
				input.Close();
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				if (_selectedCity == null)
				{
					DrawCityMenuDialog();
				}
				else
				{
					DrawInputDialog();
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
				DrawCityMenuDialog();
				return false;
			}
			else if (_selectedCity != null && Common.TopScreen.GetType() != typeof(Input))
			{
				Common.AddScreen(_input);
			}
			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_selectedCity == null && _citySelect != null)
			{
				bool handled = _citySelect.KeyDown(args);
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
			if (_selectedCity == null && _citySelect != null)
			{
				bool handled = _citySelect.MouseDown(args.X, args.Y);
				if (handled) Refresh();
				return handled;
			}

			return false;
		}

		public SetCitySize() : base(MouseCursor.Pointer)
		{
			if (_cities.Length == 0)
			{
				GameTask.Enqueue(Message.General(Translate("There are no cities yet.")));
				return;
			}

			DrawCityMenuDialog();
		}
	}
}