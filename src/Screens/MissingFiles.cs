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
using CivOne.IO;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	[Break]
	public class MissingFiles : BaseScreen
	{
		private bool _update = true;

		private bool _success;

		private int _y;

		private Menu? _menu;

		private void Menu_Continue(object sender, EventArgs args)
		{
			Destroy();
		}

		private void Menu_Copy(object sender, EventArgs args)
		{
			string? path = Runtime.BrowseFolder(Translate("Location of Civilization data files"));
			if (path == null)
			{
				// User pressed cancel
				return;
			}

			this.Clear(8)
				.FillRectangle(40, 50, 240, 100, 15);

			if (FileSystem.CopyDataFiles(path))
			{
				_success = true;
				Resources.ClearInstance();

				this.FillRectangle(0, 0, 320, 200, 8)
					.FillRectangle(40, 50, 240, 100, 15);

				this.DrawText(Translate("Succes!"), 1, 2, 160, 54, TextAlign.Center);

				string[] text = TranslateArray("Done copying the data files.\n \nPress any key to start the game...");

				for (int i = 0; i < text.Length; i++)
				{
					this.DrawText(text[i], 1, 5, 44, 66 + (i * 9), TextAlign.Left);
				}
			}
			else
			{
				this.DrawText(Translate("Failed!"), 1, 4, 160, 54, TextAlign.Center);

				string[] text = TranslateArray("Copying the data files has failed.\nPlease make sure you pointed to the correct\ndata folder and try again.\n \nPress any key to close the game...");

				for (int i = 0; i < text.Length; i++)
				{
					this.DrawText(text[i], 1, 5, 44, 66 + (i * 9), TextAlign.Left);
				}
			}

			_update = true;

			CloseMenus();
		}

		private void Menu_Quit(object sender, EventArgs args)
		{
			Runtime.Quit();
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_menu == null)
			{
				_menu = new Menu(Palette)
				{
					X = 44,
					Y = _y,
					MenuWidth = 232,
					ActiveColour = 11,
					TextColour = 5,
					FontId = 1,
					Indent = 4
				};

				_menu.Items.AddRange(
					MenuItem.Create(Translate("Continue without data files (not recommended)")).OnSelect(Menu_Continue),
					MenuItem.Create(Translate("Browse for data files")).OnSelect(Menu_Copy),
					MenuItem.Create(Translate("Quit")).OnSelect(Menu_Quit)
				);
				
				AddMenu(_menu);
				return true;
			}

			if (!_update) return false;
			_update = false;
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (!_success) Runtime.Quit();
			Destroy();
			return true;
		}
		
		public MissingFiles()
		{
			var _text = TranslateArray("One or more data files are missing from the\ndata folder. CivOne works best with the\noriginal Civilization for DOS data files.\n \nWhat do you want to do?");

			Palette = Common.GetPalette256;
			this.Clear(8)
				.FillRectangle(40, 50, 240, 100, 15)
				.DrawText(Translate("Warning!"), 1, 4, 160, 54, TextAlign.Center);

			for (int i = 0; i < _text.Length; i++)
			{
				this.DrawText(_text[i], 1, 5, 44, 66 + (i * 9), TextAlign.Left);
			}

			_y = 75 + (9 * _text.Length);
		}
	}
}