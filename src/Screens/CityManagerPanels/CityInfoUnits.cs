// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Governments;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.IO;
using CivOne.Units;

namespace CivOne.Screens.CityManagerPanels
{
	internal class CityInfoUnits : BaseScreen
	{
		private readonly City _city;

		private readonly ICityManager _cityManager;
		
		private bool _update = true;
		private readonly List<IUnit> Units;

		private readonly int RowCount = 6;
		
		private Point UnitPosition(int index)
		{
			int x = 4 + ((index % RowCount) * 18);
			int y = 0 + (((index - (index % RowCount)) / RowCount) * 16);
			return new Point(x, y);
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update)
			{
				return false;
			}

			Bitmap.Clear();

			for (int i = 0; i < Units.Count; i++)
			{
				Point position = UnitPosition(i);

				if (_selectedUnit == i && (gameTick % 4) < 2)
				{
					this.AddLayer(Units[i].ToBitmap(), position.X, position.Y);
				} else if (_selectedUnit != i)
				{
					this.AddLayer(Units[i].ToBitmap(), position.X, position.Y);
				}
					
				string homeCity = "NON.";
				if (Units[i].Home != null)
				{
					homeCity = Units[i].Home.Name;
					if (homeCity.Length >= 3)
						homeCity = $"{homeCity[..3]}.";
				}
				this.DrawText(homeCity, 1, 5, position.X, position.Y + 16);
			}

			const int FONT_HEIGHT = 6;
			Point tradeCityPosition = new(3, Bitmap.Height - (_city.TradingCities.Length * FONT_HEIGHT) - 1);
			for (int i = 0; i < _city.TradingCities.Length; i++)
			{
				City city = _city.TradingCities[i];

				// TODO: CW: Calculate real trading value
				int trading = 0;
				this.DrawText($"{city.Name}:+{trading}}}", 1, 10, tradeCityPosition.X, tradeCityPosition.Y + (i * FONT_HEIGHT));
			}

			_update = _selectedUnit != -1;

			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			for (int i = 0; i < Units.Count; i++)
			{
				Point position = UnitPosition(i);
				if (new Rectangle(position.X, position.Y + 9, 16, 16).Contains(args.Location))
				{
					ActivateUnit(i);

					return true;
				}
			}

			return false;
		}

		private void ActivateUnit(int index)
		{
			if (index < 0) return;

			Units[index].Sentry = false;
			Units[index].Fortify = false;
			//CW: This allows to activate a recently fortified unit again
			Units[index].Busy = false; 

			Game.ActiveUnit = Units[index];

			_cityManager.CloseActiveScreen();

			_update = true;
		}

		private void SetUnitStatus(int index, bool sentry, bool fortify)
		{
			if (index < 0) return;

			Units[index].Sentry = sentry;
			Units[index].Fortify = fortify;

			_update = true;
		}

		int _selectedUnit = -1;

		private void SelectUp()
		{
			if (_selectedUnit == -1) return;
			_selectedUnit -= RowCount;
			if (_selectedUnit < 0)
			{
				_selectedUnit += RowCount;
				// last row
				int relativX = _selectedUnit % RowCount;
				int lastRow = (Units.Count - 1) / RowCount;
				_selectedUnit = lastRow * RowCount + relativX;
			}
			_update = true;
		}
		private void SelectDown()
		{
			if (_selectedUnit == -1)
			{
				if (Units.Count > 0) _selectedUnit = 0;
			}
			else
			{
				_selectedUnit += RowCount;
				if (_selectedUnit >= Units.Count)
				{
					_selectedUnit %= RowCount;
				}
			}
			_update = true;
		}
		private void SelectLeft()
		{
			if (_selectedUnit == -1) return;
			_selectedUnit--;
			if (_selectedUnit < 0) _selectedUnit = Units.Count - 1;
			_update = true;
		}
		private void SelectRight()
		{
			if (_selectedUnit == -1) return;
			_selectedUnit++;
			if (_selectedUnit >= Units.Count) _selectedUnit = 0;
			_update = true;
		}	

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args.KeyChar == 'A')
			{
				if (Units.Count == 0) return true;
				
				_update = true;

				if (_selectedUnit != -1)
				{
					_selectedUnit = -1;
					_cityManager.CloseActiveScreen();
					return true;
				}
				_selectedUnit = 0;
				_cityManager.SetActiveScreen(this);

				return true;
			}

			if (_selectedUnit == -1) return false;

			// up,down, left, right
			switch (args.Key)
			{
				case Key.Up: SelectUp(); return true;
				case Key.Down: SelectDown(); return true;
				case Key.Left: SelectLeft(); return true;
				case Key.Right: SelectRight(); return true;
			}

			if (args.KeyChar == 'F') 
			{
				SetUnitStatus(_selectedUnit, false, fortify: true);

				return true;
			}
			
			if (args.KeyChar == 'S')
			{
				SetUnitStatus(_selectedUnit, sentry: true, false);

				return true;
			}

			// enter, space
			if (args.Key == Key.Enter || args.Key == Key.Space)
			{
				ActivateUnit(_selectedUnit);

				return true;
			}

			_update = true;
			_cityManager.CloseActiveScreen();
			_selectedUnit = -1;

			return false;
		}

		internal void Resize(int width)
		{
			Bitmap = new Bytemap(width, 38);
			_update = true;
		}

		public CityInfoUnits(City city, ICityManager cityManager, IUnit[] units) : base(133, 92-9)
		{
			_city = city;
			_cityManager = cityManager;
			Units = [.. units];
		}
	}
}