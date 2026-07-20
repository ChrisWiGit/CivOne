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
using System.IO;
using System.Linq;
using CivOne.Events;
using CivOne.Screens.Debug;
using CivOne.Services.Maps;

namespace CivOne.Screens
{
	/// <summary>
	/// Displays a list of available map files and loads the selected map.
	/// </summary>
	/// <remarks>
	/// Shows a <see cref="GridMenuDelegate"/> overlay populated with all
	/// <c>*.comap</c> and <c>*.cos</c> files from <see cref="ISettings.MapsDirectory"/>.
	/// Selecting a file loads the map into <see cref="Map.Instance"/> and transitions
	/// to the game intro. Pressing Escape returns to the Credits screen.
	/// </remarks>
	[ScreenResizeable]
	internal class LoadMapScreen : BaseScreen
	{
		private const int MinDialogWidth = 160;

		private readonly ICustomMapLoaderService _mapLoader;
		private readonly IReadOnlyList<string> _mapFiles;
		private readonly GridMenuDelegate _grid;

		private bool _hasUpdate = true;

		/// <summary>
		/// Initialises the screen and builds the map file list.
		/// </summary>
		/// <param name="mapLoader">Service that discovers and loads map files.</param>
		public LoadMapScreen(ICustomMapLoaderService mapLoader)
		{
			_mapLoader = mapLoader;
			_mapFiles = _mapLoader.GetMapFiles();

			Palette = Common.Screens.Length > 0
				? Common.Screens.Last().OriginalColours
				: Resources["SP299"].Palette;

			string[] labels = BuildLabels(_mapFiles);
			_grid = new GridMenuDelegate(labels, GridMenuDelegate.SelectionMode.Select, fontId: 0, allowCancel: true, minDialogWidth: MinDialogWidth);
			_grid.ItemSelected += OnItemSelected;
			_grid.Cancelled += OnCancelled;
		}

		private string[] BuildLabels(IReadOnlyList<string> files)
		{
			if (files.Count == 0)
			{
				return [Translate("No maps found")];
			}

			return [.. files.Select(f => Path.GetFileNameWithoutExtension(f))];
		}

		private void OnItemSelected(int index)
		{
			if (_mapFiles.Count == 0)
			{
				return;
			}

			string filePath = _mapFiles[index];
			Log("LoadMapScreen: Loading map from '{0}'", filePath);

			if (!_mapLoader.LoadMapFile(filePath))
			{
				Log("LoadMapScreen: Failed to load map '{0}'", filePath);
				return;
			}

			StartGame();
		}

		private void StartGame()
		{
			Destroy();
			if (!Runtime.Settings.ShowIntro)
			{
				Common.AddScreen(new NewGame());
			}
			else
			{
				Common.AddScreen(new Intro());
			}
		}

		private void OnCancelled(object? _, EventArgs args)
		{
			BackToCredits();
		}

		private void BackToCredits()
		{
			var credits = new Credits();
			credits.SkipIntro();
			credits.SkipLogo();
			Common.AddScreen(credits);
			Destroy();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_hasUpdate)
			{
				return false;
			}

			_hasUpdate = false;
			_grid.Draw(this, Translate("Load Map"), CanvasHeight);
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			bool handled = _grid.KeyDown(args);
			if (handled)
			{
				_hasUpdate = true;
			}

			return handled;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			bool handled = _grid.MouseDown(args.X, args.Y);
			if (handled)
			{
				_hasUpdate = true;
			}

			return handled;
		}

		public override bool MouseUp(ScreenEventArgs args) => false;

		public override bool MouseDrag(ScreenEventArgs args) => false;

		public override bool MouseMove(ScreenEventArgs args) => false;
	}
}
