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
using System.Drawing;
using CivOne.Screens.Services;

namespace CivOne.Screens.CityManagerPanels
{
	internal class CityBuildings : BaseScreen
	{
		IInteractiveButton _moreButton = InteractiveButtonImpl.Empty;

		List<Tuple<IInteractiveButton, IBuilding>> _buildingButtons = new();

		private readonly City _city;
		private IProduction[] _improvements;

		public event EventHandler BuildingUpdate;

		private int _page = 0;

		private int _selectedBuilding = -1;

		private readonly int MAX_BUILDINGS = 14;

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

		private void DrawBuilding(IBuilding building, int offset, int currentPage)
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


			_buildingButtons.Add(new(InteractiveButtonImpl.Build(this, this,
				new Rectangle(0, 2 + (6 * offset), Width, Resources.GetFontHeight(1) + 1)), building));
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
			if (!RefreshNeeded())
			{
				return true;
			}
			
			this.Tile(Pattern.PanelBlue);

			_buildingButtons = new();

			int currentPage = _page;

			for (int i = _page * MAX_BUILDINGS; i < _improvements.Length && i < ((_page + 1) * MAX_BUILDINGS); i++)
			{
				if (_improvements[i] is IWonder)
				{
					DrawWonder(_improvements[i] as IWonder, i % MAX_BUILDINGS);
					continue;
				}

				if (_selectedBuilding != i || (_selectedBuilding == i && (gameTick % 4) < 2))
				{
					DrawBuilding(_improvements[i] as IBuilding, i % MAX_BUILDINGS, currentPage);
				}
				continue;
			}

			if (_improvements.Length > MAX_BUILDINGS)
			{
				int buttonHeight = Resources.GetFontHeight(1) + 3;
				const int buttonWidth = 28;
				_moreButton = InteractiveButtonImpl.Build(this, this,
					new Rectangle(
							Width - buttonWidth - 1,
							Height - buttonHeight - 1,
							buttonWidth, buttonHeight));
				_moreButton.DrawButton("More", 1, 9, 1);
			}

			this.DrawRectangle(colour: 1);

			if (_selectedBuilding != -1)
			{
				Refresh();
			}

			return true;
		}

		private void SellBuilding(object sender, EventArgs args)
		{
			_city.SellBuilding((sender as ConfirmSell).Building);
			_page = 0;
			_improvements = GetImprovements.ToArray();

			_cityManager.CloseActiveScreen();
			_selectedBuilding = -1;

			Refresh();

			BuildingUpdate?.Invoke(this, null);
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_city.BuildingSold)
			{
				ShowBuildAlreadySoldDialog();
				return true;
			}

			Rectangle buildingsArea = new(0, 0, Width, Height);

			if (!buildingsArea.Contains(args.Location))
			{
				return false;
			}

			if (_buildingButtons == null)
			{
				return false;
			}

			var hit = _buildingButtons.FirstOrDefault(x => x.Item1.Contains(args.Location));
			if (hit != null)
			{
				ShowSellConfirmation(hit.Item2);
			}

			if (_moreButton.Contains(args.Location))
			{
				_page++;
				if ((_page * MAX_BUILDINGS) > _improvements.Length)
				{
					_page = 0;
				}
				Refresh();

				return true;
			}

			// always return true to prevent closing city manager (usability enhancement)
			return true;
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

				Refresh();

				if (_selectedBuilding != -1)
				{
					_cityManager.CloseActiveScreen();
					_selectedBuilding = -1;

					return true;
				}

				if (_city.BuildingSold)
				{
					ShowBuildAlreadySoldDialog();

					return true;
				}

				_cityManager.SetActiveScreen(this);
				_selectedBuilding = FirstBuildingIndex + (_page * MAX_BUILDINGS);

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
					_page = _selectedBuilding / MAX_BUILDINGS;

					Refresh();
					return true;
				case Key.Down:
					_selectedBuilding++;
					if (_selectedBuilding >= FirstBuildingIndex + BuildingsCount)
					{
						_selectedBuilding = FirstBuildingIndex;
					}

					_page = _selectedBuilding / MAX_BUILDINGS;

					Refresh();
					return true;
				case Key.Escape:
					_cityManager.CloseActiveScreen();
					_selectedBuilding = -1;

					Refresh();
					return true;
				case Key.Enter:
				case Key.Space:
					if (_improvements[_selectedBuilding] is IBuilding)
					{
						ShowSellConfirmation(_selectedBuilding);
						Refresh();
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

			ShowSellConfirmation(_improvements[buildingIndex] as IBuilding);
		}

		protected void ShowSellConfirmation(IBuilding building)
		{
			if (building == null)
			{
				return;
			}

			ConfirmSell confirmSell = new(building);
			confirmSell.Sell += SellBuilding;
			Common.AddScreen(confirmSell);
		}

		public void Resize(int width)
		{
			Bitmap = new Bytemap(width, 97);
			Refresh();
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