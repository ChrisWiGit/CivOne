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
using System.Linq;
using CivOne.Buildings;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens.Dialogs;
using CivOne.Graphics.Sprites;
using CivOne.Wonders;
using CivOne.IO;
using CivOne.Enums;
using CivOne.Tasks;

namespace CivOne.Screens.CityManagerPanels
{
	internal class CityBuildings : BaseScreen
	{
		private readonly City _city;
		private IProduction[] _improvements;

		private bool _update = true;

		public event EventHandler BuildingUpdate;

		private int _page = 0;

		private int _selectedBuilding = -1;

		private void DrawWonder(IWonder wonder, int offset)
		{
			int xx = (offset % 2 == 0) ? 21 : 1;
			int yy = -1 + (6 * offset);
			if (yy < 0)
				this.AddLayer(wonder.SmallIcon.Crop(0, Math.Abs(yy), wonder.SmallIcon.Width(), wonder.SmallIcon.Height() + yy), xx, 0);
			else
				this.AddLayer(wonder.SmallIcon, xx, yy);

			string name = wonder.Name;
			while (Resources.GetTextSize(1, name).Width > 62)
			{
				name = $"{name.Substring(0, name.Length - 2)}.";
			}
			this.DrawText(name, 1, 15, 42, 3 + (6 * offset));
		}

		private void DrawBuilding(IBuilding building, int offset)
		{
			int xx = (offset % 2 == 0) ? 21 : 1;
			int yy = -1 + (6 * offset);
			if (yy < 0)
				this.AddLayer(building.SmallIcon.Crop(0, Math.Abs(yy), building.SmallIcon.Width(), building.SmallIcon.Height() + yy), xx, 0);
			else
				this.AddLayer(building.SmallIcon, xx, yy);

			string name = building.Name;
			while (Resources.GetTextSize(1, name).Width > 54)
			{
				name = $"{name.Substring(0, name.Length - 1)}";
			}
			this.DrawText(name, 1, 15, 42, 3 + (6 * offset))
				.AddLayer(Icons.SellButton, Width - 10, 2 + (6 * offset));
		}

		private IEnumerable<IProduction> GetImprovements
		{
			get
			{
				foreach (IWonder wonder in _city.Wonders)
					yield return wonder;
				foreach (IBuilding building in _city.Buildings)
					yield return building;
			}
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update)
			{
				return true;
			}
			this.Tile(Pattern.PanelBlue);

			for (int i = _page * 14; i < _improvements.Length && i < ((_page + 1) * 14); i++)
			{
				if (_improvements[i] is IWonder)
				{
					DrawWonder(_improvements[i] as IWonder, i % 14);
					continue;
				}

				if (_selectedBuilding != i || (_selectedBuilding == i && (gameTick % 4) < 2))
				{
					DrawBuilding(_improvements[i] as IBuilding, i % 14);
				}
				continue;
			}

			if (_improvements.Length > 14)
			{
				DrawButton("More", 9, 1, 76, 87, 29);
			}

			this.DrawRectangle(colour: 1);

			_update = _selectedBuilding != -1;

			return true;
		}

		private void SellBuilding(object sender, EventArgs args)
		{
			_city.SellBuilding((sender as ConfirmSell).Building);
			_page = 0;
			_improvements = GetImprovements.ToArray();

			_cityManager.CloseActiveScreen();
			_selectedBuilding = -1;

			_update = true;

			BuildingUpdate?.Invoke(this, null);
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (!_city.BuildingSold && args.X > Width - 11 && args.X < Width - 3)
			{
				int yy = 2;
				for (int i = (_page * 14); i < _improvements.Length && i < ((_page + 1) * 14); i++)
				{
					if (args.Y >= yy && args.Y < yy + 8 && _improvements[i] is IBuilding)
					{
						ShowSellConfirmation(i);

						return true;
					}
					yy += 6;
				}
			}

			if (args.X > 75 && args.X < 105 && args.Y > 86 && args.Y < 96)
			{
				_page++;
				if ((_page * 14) > _improvements.Length) _page = 0;
				_update = true;
				return true;
			}
			return false;
		}

		protected static void ShowBuildAlreadySoldDialog()
		{
			// CW: Not like in original game but made more user friendly.
			GameTask.Enqueue(Message.General(
				"You have to wait until next turn",
				"to sell another building."));
		}
		
		protected int FirstBuildingIndex => Array.FindIndex(_improvements, x => x is IBuilding);
		protected int BuildingsCount => _improvements.Count(x => x is IBuilding);

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args.KeyChar == 'S')
			{
				if (_improvements.Length == 0) return true;

				_update = true;

				if (_selectedBuilding != -1)
				{
					_cityManager.CloseActiveScreen();
					_selectedBuilding = -1;
					_update = true;
					return true;
				}

				if (_city.BuildingSold)
				{
					ShowBuildAlreadySoldDialog();

					return true;
				}

				_cityManager.SetActiveScreen(this);
				_selectedBuilding = FirstBuildingIndex;
				_update = true;
				return true;
			}

			if (_selectedBuilding == -1) return false;

			switch (args.Key)
			{
				case Key.Up:
					_selectedBuilding--;
					if (_selectedBuilding < FirstBuildingIndex)
					{
						_selectedBuilding = FirstBuildingIndex + BuildingsCount - 1;
					}
					_update = true;
					return true;
				case Key.Down:
					_selectedBuilding++;
					if (_selectedBuilding >= FirstBuildingIndex + BuildingsCount)
					{
						_selectedBuilding = FirstBuildingIndex;
					}
					_update = true;
					return true;
				case Key.Escape:
					_cityManager.CloseActiveScreen();
					_selectedBuilding = -1;
					_update = true;
					return true;
				case Key.Enter:
				case Key.Space:
					if (_improvements[_selectedBuilding] is IBuilding)
					{
						ShowSellConfirmation(_selectedBuilding);
					}
					return true;
			}


			return base.KeyDown(args);
		}

		protected void ShowSellConfirmation(int buildingIndex)
		{
			if (_improvements[buildingIndex] is not IBuilding)
			{
				return;
			}

			ConfirmSell confirmSell = new(_improvements[buildingIndex] as IBuilding);
			confirmSell.Sell += SellBuilding;
			Common.AddScreen(confirmSell);
		}

		public void Resize(int width)
		{
			Bitmap = new Bytemap(width, 97);
			_update = true;
		}

		public CityBuildings(City city, ICityManager cityManager) : base(108, 97)
		{
			_city = city;
			_cityManager = cityManager;
			_improvements = GetImprovements.ToArray();
		}
		
		private readonly ICityManager _cityManager;
	}
}