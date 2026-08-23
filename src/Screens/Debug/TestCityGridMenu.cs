// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Tasks;

namespace CivOne.Screens.Debug
{
	/// <summary>
	/// Test screen for GridMenuDelegate pagination with 300 test cities.
	/// City names range from 1000000000 to 1000000299.
	/// Tests the ability to handle large lists with pagination.
	/// </summary>
	[ScreenResizeable]
	internal class TestCityGridMenu : BaseScreen
	{
		private GridMenuDelegate? _gridMenu;
		private bool _menuCreated;

		private void CreateTestMenu()
		{
			// Create 300 test city names
			string[] cityNames = new string[300];
			for (int i = 0; i < 300; i++)
			{
				cityNames[i] = $"{1000000000 + i}";
			}

			_gridMenu = new GridMenuDelegate(
				cityNames,
				GridMenuDelegate.SelectionMode.Select,
				fontId: 0,
				defaultSelectedIndex: 220);

			_gridMenu.ItemSelected += OnCitySelected;
			_gridMenu.Cancelled += OnCancelled;
			_menuCreated = true;
		}

		private void OnCitySelected(int index)
		{
			GameTask.Enqueue(Message.General(TranslateFormatted("Selected city at index {0}: {1}", index, 1000000000 + index)));
			Destroy();
		}

		private void OnCancelled(object? _, EventArgs __)
		{
			Destroy();
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_gridMenu != null)
			{
				bool handled = _gridMenu.KeyDown(args);
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
			if (_gridMenu != null)
			{
				bool handled = _gridMenu.MouseDown(args.X, args.Y);
				if (handled) Refresh();
				return handled;
			}

			return false;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_menuCreated)
			{
				CreateTestMenu();
			}

			this.Clear();
			_gridMenu?.Draw(this, "Test City Grid (300 cities)", CanvasHeight);
			return true;
		}

		public TestCityGridMenu() : base(MouseCursor.Pointer)
		{
			Palette = Common.DefaultPalette;
		}
	}
}
