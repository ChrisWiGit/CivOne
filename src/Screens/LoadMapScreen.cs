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
using System.Globalization;
using System.IO;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Services.Maps;
using CivOne.Sound;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	/// <summary>
	/// Displays a scrollable list of available map files and loads the selected map.
	/// </summary>
	/// <remarks>
	/// The list contains all <c>*.comap</c> and <c>*.map</c> files from
	/// <see cref="ISettings.MapsDirectory"/>, plus an entry that opens a file chooser
	/// so a map outside that directory can be loaded.
	/// Only as many entries as fit on screen are shown; the cursor keys move the
	/// selection and scroll the list.
	/// Selecting a map loads it into <see cref="Map.Instance"/> and starts a new game.
	/// Pressing Escape returns to the Credits screen.
	/// </remarks>
	[Modal, ScreenResizeable]
	internal class LoadMapScreen : BaseScreen
	{
		private const int DialogWidth = 217;
		private const int DialogTop = 24;
		private const int DialogBottom = 16;
		private const int RowHeight = 8;
		private const int WheelRows = 3;
		private const int CenteredMapCountThreshold = 10;

		/// <summary>
		/// A single row of the list: either a map file or the file chooser entry.
		/// </summary>
		/// <param name="Label">Text shown in the list.</param>
		/// <param name="FilePath">Full path of the map file, or <see langword="null"/> for non-file entries.</param>
		/// <param name="Enabled">Whether the entry can be activated.</param>
		private readonly record struct MapEntry(string Label, string? FilePath, bool Enabled);

		private readonly ICustomMapLoaderService _mapLoader;
		private readonly IMapDialogPathProvider? _pathProvider;
		private readonly List<MapEntry> _entries = [];
		private readonly WorldGenerationMusicDelegate _generationMusic = new();

		private int _selectedIndex;
		private int _scrollOffset;
		private int _visibleRows = 1;
		private int _mapFileCount;
		private int _menuY = DialogTop;
		private bool _update = true;

		private IMapDialogPathProvider PathProvider => _pathProvider ?? MapDialogPathProviderFactory.Create();

		private Menu? CurrentMenu => GetMenu<Menu>();

		private int DialogX => Math.Max(0, (Width - DialogWidth) / 2);

		public override MouseCursor Cursor => MouseCursor.Pointer;

		/// <summary>
		/// Initialises the screen and builds the map file list.
		/// </summary>
		/// <param name="mapLoader">Service that discovers and loads map files.</param>
		/// <param name="pathProvider">
		/// Provides the start path of the file chooser.
		/// Resolved from <see cref="MapDialogPathProviderFactory"/> when not supplied.
		/// </param>
		public LoadMapScreen(ICustomMapLoaderService mapLoader, IMapDialogPathProvider? pathProvider = null)
		{
			ArgumentNullException.ThrowIfNull(mapLoader);

			_mapLoader = mapLoader;
			_pathProvider = pathProvider;

			Palette = Resources["LOGO"].Palette;

			BuildEntries();
		}

		private void BuildEntries()
		{
			_entries.Clear();
			_entries.Add(new MapEntry(Translate("Load map from file..."), null, true));

			_mapFileCount = 0;
			foreach (string filePath in _mapLoader.GetMapFiles())
			{
				_entries.Add(new MapEntry(Path.GetFileNameWithoutExtension(filePath), filePath, true));
				_mapFileCount++;
			}

			if (_entries.Count == 1)
			{
				_entries.Add(new MapEntry(Translate("No maps found"), null, false));
			}
		}

		private int CalculateVisibleRows()
		{
			// One row is used by the menu title.
			int rows = ((Height - DialogTop - DialogBottom) / RowHeight) - 1;
			return Math.Clamp(rows, 1, _entries.Count);
		}

		/// <summary>
		/// Determines the menu's vertical position.
		/// </summary>
		/// <remarks>
		/// With <see cref="CenteredMapCountThreshold"/> or fewer map files, the menu is centred
		/// vertically. Beyond that, the centred position drifts up by 1 pixel per additional map
		/// file, but never past the original <see cref="DialogTop"/> position.
		/// </remarks>
		private int CalculateMenuY(int menuHeight)
		{
			int centeredY = Math.Max(0, (Height - menuHeight) / 2);
			if (_mapFileCount <= CenteredMapCountThreshold)
			{
				return centeredY;
			}

			int excess = _mapFileCount - CenteredMapCountThreshold;
			return Math.Max(DialogTop, centeredY - excess);
		}

		private void CreateMenu()
		{
			CloseMenus();

			_visibleRows = CalculateVisibleRows();
			EnsureSelectionVisible();

			int menuHeight = (_visibleRows + 1) * RowHeight;
			_menuY = CalculateMenuY(menuHeight);

			Menu menu = new(Palette)
			{
				Title = Translate("Select Map File..."),
				X = DialogX,
				Y = _menuY,
				MenuWidth = DialogWidth,
				TitleColour = 12,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 8,
				FontId = 0,
				IndentTitle = 2,
				RowHeight = RowHeight
			};

			for (int row = 0; row < _visibleRows; row++)
			{
				menu.Items.Add(string.Empty, row).OnSelect(RowSelected);
			}

			AddMenu(menu);
			UpdateMenuItems();
		}

		private void UpdateMenuItems()
		{
			Menu? menu = CurrentMenu;
			if (menu == null)
			{
				return;
			}

			for (int row = 0; row < menu.Items.Count; row++)
			{
				int index = _scrollOffset + row;
				MenuItem<int> item = menu.Items[row];
				bool hasEntry = index < _entries.Count;
				item.Text = hasEntry ? _entries[index].Label : string.Empty;
				item.Enabled = hasEntry && _entries[index].Enabled;
			}

			menu.ActiveItem = _selectedIndex - _scrollOffset;
			menu.ForceUpdate();
			_update = true;
		}

		private void ClampScroll()
		{
			_scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _entries.Count - _visibleRows));
		}

		private void EnsureSelectionVisible()
		{
			if (_selectedIndex < _scrollOffset)
			{
				_scrollOffset = _selectedIndex;
			}
			else if (_selectedIndex >= _scrollOffset + _visibleRows)
			{
				_scrollOffset = _selectedIndex - _visibleRows + 1;
			}

			ClampScroll();
		}

		private void SetSelection(int index)
		{
			_selectedIndex = Math.Clamp(index, 0, _entries.Count - 1);
			EnsureSelectionVisible();
			UpdateMenuItems();
		}

		private void MoveSelection(int delta)
		{
			int index = _selectedIndex + delta;
			if (index < 0)
			{
				index = _entries.Count - 1;
			}
			else if (index >= _entries.Count)
			{
				index = 0;
			}

			SetSelection(index);
		}

		private bool SelectByCharacter(char inputChar)
		{
			if (!char.IsLetterOrDigit(inputChar))
			{
				return false;
			}

			char key = char.ToLower(inputChar, CultureInfo.CurrentCulture);
			for (int offset = 1; offset <= _entries.Count; offset++)
			{
				int index = (_selectedIndex + offset) % _entries.Count;
				string label = _entries[index].Label;
				if (label.Length > 0 && char.ToLower(label[0], CultureInfo.CurrentCulture) == key)
				{
					SetSelection(index);
					return true;
				}
			}

			return false;
		}

		private void RowSelected(object sender, MenuItemEventArgs<int> args)
		{
			ArgumentNullException.ThrowIfNull(args);

			int index = _scrollOffset + args.Value;
			_selectedIndex = Math.Clamp(index, 0, _entries.Count - 1);
			Activate(index);
		}

		private void Activate(int index)
		{
			if (index < 0 || index >= _entries.Count)
			{
				return;
			}

			MapEntry entry = _entries[index];
			if (!entry.Enabled)
			{
				return;
			}

			if (entry.FilePath == null)
			{
				LoadMapFromBrowser();
				return;
			}

			LoadMap(entry.FilePath);
		}

		private void LoadMapFromBrowser()
		{
			// Built from a translated description plus literal patterns so a translation can
			// never break the glob part of the filter.
			string filter = $"{Translate("CivOne Map")} (*.comap, *.map)|*.comap;*.map";

			string? filePath = Runtime.FileChooser(
				false,
				Translate("Load Map..."),
				PathProvider.EnsureInitialMapFilePath(),
				filter);

			_update = true;
			if (string.IsNullOrEmpty(filePath))
			{
				return;
			}

			PathProvider.SetLastUsedMapPath(filePath);
			LoadMap(filePath);
		}

		private void LoadMap(string filePath)
		{
			Log("LoadMapScreen: Loading map from '{0}'", filePath);

			if (!_mapLoader.LoadMapFile(filePath))
			{
				Log("LoadMapScreen: Failed to load map '{0}'", filePath);
				GameTask.Enqueue(Message.Error(
					Translate("-- Civilization Note --"),
					TranslateFormattedArray("Could not load the map file.\nFile: {0}", Path.GetFileName(filePath))));
				_update = true;
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
				// The loaded map needs no generation, but the intro that follows is the same one a
				// generated world gets, and it comes with the same music.
				_generationMusic.Start();
				Common.AddScreen(new Intro());
			}
		}

		private void BackToCredits()
		{
			var credits = new Credits();
			credits.SkipIntro();
			credits.SkipLogo();
			Common.AddScreen(credits);
			Destroy();
		}

		private void DrawScrollPosition()
		{
			if (_entries.Count <= _visibleRows)
			{
				return;
			}

			int lastVisible = Math.Min(_scrollOffset + _visibleRows, _entries.Count);
			string position = string.Format(
				CultureInfo.CurrentCulture,
				"{0}-{1}/{2}",
				_scrollOffset + 1,
				lastVisible,
				_entries.Count);

			this.DrawText(position, 0, 12, DialogX + DialogWidth - 2, _menuY + 1, TextAlign.Right);
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			CreateMenu();
			_update = true;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			Menu? menu = CurrentMenu;
			if (menu == null)
			{
				CreateMenu();
				menu = CurrentMenu;
			}

			bool menuUpdated = menu != null && menu.Update(gameTick);
			if (!_update && !menuUpdated)
			{
				return false;
			}

			_update = false;
			Bitmap.Clear();
			this.Clear(15);
			DrawScrollPosition();
			if (menu != null)
			{
				this.AddLayer(menu);
			}

			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			ArgumentNullException.ThrowIfNull(args);

			switch (args.Key)
			{
				case Key.Escape:
					BackToCredits();
					return true;
				case Key.Enter:
					Activate(_selectedIndex);
					return true;
				case Key.Up:
				case Key.NumPad8:
					MoveSelection(-1);
					return true;
				case Key.Down:
				case Key.NumPad2:
					MoveSelection(1);
					return true;
				case Key.PageUp:
					SetSelection(_selectedIndex - _visibleRows);
					return true;
				case Key.PageDown:
					SetSelection(_selectedIndex + _visibleRows);
					return true;
				case Key.Home:
					SetSelection(0);
					return true;
				case Key.End:
					SetSelection(_entries.Count - 1);
					return true;
				default:
					return SelectByCharacter(args.KeyChar);
			}
		}

		private void SyncSelectionFromMenu(Menu menu)
		{
			int index = _scrollOffset + menu.ActiveItem;
			_selectedIndex = Math.Clamp(index, 0, _entries.Count - 1);
			_update = true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			Menu? menu = CurrentMenu;
			if (menu == null)
			{
				return false;
			}

			bool handled = menu.MouseDown(args);
			SyncSelectionFromMenu(menu);
			return handled;
		}

		public override bool MouseUp(ScreenEventArgs args)
		{
			Menu? menu = CurrentMenu;
			if (menu == null)
			{
				return false;
			}

			return menu.MouseUp(args);
		}

		public override bool MouseDrag(ScreenEventArgs args)
		{
			Menu? menu = CurrentMenu;
			if (menu == null)
			{
				return false;
			}

			bool handled = menu.MouseDrag(args);
			SyncSelectionFromMenu(menu);
			return handled;
		}

		public override bool MouseMove(ScreenEventArgs args) => false;

		public override bool MouseWheel(ScreenEventArgs args)
		{
			ArgumentNullException.ThrowIfNull(args);

			if (args.WheelDelta == 0)
			{
				return false;
			}

			SetSelection(_selectedIndex - (Math.Sign(args.WheelDelta) * WheelRows));
			return true;
		}
	}
}
