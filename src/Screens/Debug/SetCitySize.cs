// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Globalization;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Services.Screen;
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

		private CityGridMenuDelegate? _citySelect;

		private City? _selectedCity;
		private Input? ActiveInput => Inputs.OfType<Input>().FirstOrDefault();

		public string? Value { get; private set; }

		public event EventHandler? Accept, Cancel;

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

			if (ActiveInput is Input input)
			{
				input.X = 90 + ox;
				input.Y = 97 + oy;
			}
		}

		private void DrawCityMenuDialog()
		{
			if (_citySelect == null)
			{
				Palette = Common.Screens[^1].OriginalColours;
				_citySelect = new CityGridMenuDelegate(
					_cities,
					city => $"{city.Name} ({city.CityOwnerPlayer.TribeName})");
				_citySelect.CitySelected += OnCitySelected;
				_citySelect.Cancelled += CitySize_Cancel;
			}

			_citySelect.Draw(this, Translate("Set City Size..."), CanvasHeight);
		}

		private void CitySizeSet_Accept(object? sender, EventArgs args)
		{
			if (sender is not Input input)
				return;
			Value = input.Text;

			if (!byte.TryParse(Value, out byte citySize) || citySize < 1 || citySize > 99)
			{
				GameTask.Enqueue(Message.Error(Translate("-- DEBUG: Set City Size --"), TranslateFormattedArray("The value {0} is invalid or out of range.\nPlease enter a value between 1 and 99.", Value)));
			}
			else if (_selectedCity != null)
			{
				_selectedCity.Size = citySize;
				GameTask.Enqueue(Message.General(TranslateFormatted("{0} size set to {1}.", _selectedCity.Name, citySize)));
			}

			Accept?.Invoke(this, EventArgs.Empty);

			input.Close();
			Destroy();
		}

		private void OnCitySelected(City city)
		{
			Palette = Common.Screens[^1].OriginalColours;
			_selectedCity = city;

			EnsureManagedInput();
			Refresh();
		}

		protected override IScreen? CreateManagedInput()
		{
			if (_selectedCity == null)
			{
				return null;
			}

			Input input = new(Palette, _selectedCity.Size.ToString(CultureInfo.InvariantCulture), 0, 5, 11, 90 + OffsetX, 97 + OffsetY, 101, 10, 3);
			input.Accept += CitySizeSet_Accept;
			input.Cancel += CitySize_Cancel;
			return input;
		}

		private void CitySize_Cancel(object? sender, EventArgs args)
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

			if (_selectedCity == null && TopScreen.GetType() != typeof(Menu))
			{
				DrawCityMenuDialog();
				return false;
			}
			else if (_selectedCity != null && !HasInput && TopScreen.GetType() != typeof(Input))
			{
				EnsureManagedInput();
			}
			return false;
		}

		private IScreen TopScreen => _screenQuery.TopScreen!;

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

		private readonly IScreenQueryService _screenQuery;

		public SetCitySize(IScreenQueryService screenQuery) : base(MouseCursor.Pointer)
		{
			_screenQuery = screenQuery;
			if (_cities.Length == 0)
			{
				GameTask.Enqueue(Message.General(Translate("There are no cities yet.")));
				return;
			}

			DrawCityMenuDialog();
		}
	}
}