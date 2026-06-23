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

		private void ConfirmOverwrite(object sender, EventArgs args)
		{
			_overwriteService.ConfirmOverwrite(_source, _destination, _filename);
			Destroy();
		}

		protected override IMenu? CreateManagedMenu()
		{
			Menu menu = new Menu(Palette, Selection(3, 20, 160, 16))
			{
				X = 73,
				Y = 100,
				CenterTo320Coordinates = true,
				MenuWidth = 160,
				ActiveColour = 11,
				TextColour = 5,
				FontId = 0
			};
			string[] choices = [Translate("No, keep existing"), Translate("Yes, overwrite")];
			foreach (string choice in choices)
			{
				menu.Items.Add(choice);
			}
			menu.Items[0].Selected += Cancel;
			menu.Items[1].Selected += ConfirmOverwrite;

			menu.MissClick += Cancel;
			menu.Cancel += Cancel;
			return menu;
		}

		public OverwritePlugin(string source, string destination, IPluginOverwriteService overwriteService) : base(70, 80, 164, 39)
		{
			_overwriteService = overwriteService ?? throw new ArgumentNullException(nameof(overwriteService));
			_source = source;
			_destination = destination;
			_filename = Path.GetFileName(destination);

			DialogBox.DrawText(Translate("Overwrite existing plugin"), 0, 15, 5, 5);
			DialogBox.DrawText(TranslateFormatted("file {0}?", _filename), 0, 15, 5, 13);
		}
	}

	internal static class OverwritePluginDialogFactory
	{
		public static IPluginOverwriteService CreateService()
		{
			return new PluginOverwriteService();
		}

		public static IScreen CreateDialog(string source, string destination)
		{
			return new OverwritePlugin(source, destination, CreateService());
		}

		public static IScreen CreateDialog(string source, string destination, IPluginOverwriteService overwriteService)
		{
			return new OverwritePlugin(source, destination, overwriteService);
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
			Plugin? plugin = Reflect.Plugins().FirstOrDefault(x => x.Filename == filename && !x.Deleted);
			plugin?.Delete();
			File.Copy(source, destination);
			Reflect.LoadPlugin(destination);
		}
	}
}