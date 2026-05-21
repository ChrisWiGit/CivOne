// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.IO;
using System.Linq;
using CivOne.Graphics;

namespace CivOne.Screens.Dialogs
{
	internal class OverwritePlugin : BaseDialog
	{
		private readonly string _source, _destination, _filename;

		private readonly IPluginOverwriteService _overwriteService;
		private Menu _menu;

		private void ConfirmOverwrite(object sender, EventArgs args)
		{
			_overwriteService.ConfirmOverwrite(_source, _destination, _filename);
			Destroy();
		}

		protected override void FirstUpdate()
		{
			CreateMenu();
			base.FirstUpdate();
		}

		private void CreateMenu()
		{
			if (_menu is not null)
			{
				return;
			}

			_menu = new Menu(Palette, Selection(3, 20, 160, 16))
			{
				X = 73,
				Y = 100,
				CenterTo320Coordinates = true,
				MenuWidth = 160,
				ActiveColour = 11,
				TextColour = 5,
				FontId = 0
			};
			foreach (string choice in new [] { "No, keep existing", "Yes, overwrite" })
			{
				_menu.Items.Add(choice);
			}
			_menu.Items[0].Selected += Cancel;
			_menu.Items[1].Selected += ConfirmOverwrite;

			_menu.MissClick += Cancel;
			_menu.Cancel += Cancel;
			AddMenu(_menu);
		}

		public OverwritePlugin(string source, string destination, IPluginOverwriteService overwriteService = null) : base(70, 80, 164, 39)
		{
			_overwriteService = overwriteService ?? new PluginOverwriteService();
			_source = source;
			_destination = destination;
			_filename = Path.GetFileName(destination);

			DialogBox.DrawText("Overwrite existing plugin", 0, 15, 5, 5);
			DialogBox.DrawText($"file {_filename}?", 0, 15, 5, 13);
		}
	}

	interface IPluginOverwriteService
	{
		void ConfirmOverwrite(string source, string destination, string filename);
	}

	internal class PluginOverwriteService : IPluginOverwriteService
	{
		public void ConfirmOverwrite(string source, string destination, string filename)
		{
			Plugin plugin = Reflect.Plugins().FirstOrDefault(x => x.Filename == filename && !x.Deleted);
			plugin?.Delete();
			File.Copy(source, destination);
			Reflect.LoadPlugin(destination);
		}
	}
}