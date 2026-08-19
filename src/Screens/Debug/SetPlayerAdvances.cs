// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Services;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal class SetPlayerAdvances : BaseScreen
	{
		private readonly AdvanceManagementService _advanceService;

		private CivSelectMenuDelegate _civSelectDelegate;
		private GridMenuDelegate? _gridDelegate;
		private IAdvance[]? _advances;

		private Player? _selectedPlayer;

		public string? Value { get; }

		public event EventHandler? Cancel;

		private void DrawPlayerMenuDialog()
		{
			_civSelectDelegate.Draw(this, CanvasHeight);
		}

		private void RenderAdvancesGrid()
		{
			Player? selectedPlayer = _selectedPlayer;
			if (selectedPlayer == null)
				return;

			if (_gridDelegate == null)
				CreateGridDelegate();
			if (_gridDelegate == null)
				return;

			_gridDelegate.Draw(this, TranslateFormatted("Set Advances: {0} (Help: Alt+H)", selectedPlayer.TribeNamePlural), CanvasHeight);
		}

		private void CreateGridDelegate()
		{
			Player? selectedPlayer = _selectedPlayer;
			if (selectedPlayer == null)
				return;

			IAdvance[] advances = _advanceService.GetAllAdvances();
			_advances = advances;
			string[] labels = [.. advances.Select(a => a.TranslatedName)];
			_gridDelegate = new GridMenuDelegate(
				labels,
				GridMenuDelegate.SelectionMode.CheckUncheck,
				isChecked: i => selectedPlayer.HasAdvance(advances[i]));
			_gridDelegate.ItemChecked += OnItemChecked;
			_gridDelegate.Cancelled += OnGridCancelled;
		}

		private void OnItemChecked(int index)
		{
			Player? selectedPlayer = _selectedPlayer;
			IAdvance[]? advances = _advances;
			if (selectedPlayer == null || advances == null)
				return;
			if (index < 0 || index >= advances.Length)
				return;

			_advanceService.ToggleAdvance(Game.PlayerNumber(selectedPlayer), advances[index]);
			Refresh();
		}

		private void OnGridCancelled(object? _, EventArgs __)
		{
			_selectedPlayer = null;
			_advances = null;
			_gridDelegate = null;
			_civSelectDelegate = CreateCivSelectDelegate();
			Refresh();
		}

		private CivSelectMenuDelegate CreateCivSelectDelegate()
		{
			var delegate_ = new CivSelectMenuDelegate(Translate("Set Player Advances..."));
			delegate_.PlayerSelected += OnCivSelected;
			delegate_.Cancelled += OnCancel;
			return delegate_;
		}

		private void OnCivSelected(Player player)
		{
			_selectedPlayer = player;
			_gridDelegate = null;
			CloseMenus();
			Refresh();
		}

		private void OnCancel(object? _, EventArgs args)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			Destroy();
		}

		private bool TryOpenCivilopediaForSelectedAdvance()
		{
			if (_gridDelegate == null || _advances == null) return false;
			int idx = _gridDelegate.SelectedIndex;
			if (idx < 0 || idx >= _advances.Length) return false;
			Common.AddScreen(new Civilopedia(_advances[idx]));
			return true;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			// Draw the appropriate dialog based on state
			if (RefreshNeeded())
			{
				if (_selectedPlayer == null)
					DrawPlayerMenuDialog();
				else
					RenderAdvancesGrid();
			}

			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_selectedPlayer == null)
			{
				bool civHandled = _civSelectDelegate.KeyDown(args);
				if (civHandled) Refresh();
				return civHandled;
			}

			if (_gridDelegate == null) return false;

			if (args.Alt && args.Key == Key.Character && (args.KeyChar == 'h' || args.KeyChar == 'H'))
				return TryOpenCivilopediaForSelectedAdvance();

			bool handled = _gridDelegate.KeyDown(args);
			if (handled) Refresh();
			return handled;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_selectedPlayer == null)
			{
				bool civHandled = _civSelectDelegate.MouseDown(args.X, args.Y);
				if (civHandled) Refresh();
				return civHandled;
			}

			if (_gridDelegate == null) return false;
			return _gridDelegate.MouseDown(args.X, args.Y);
		}

		public SetPlayerAdvances() : base(MouseCursor.Pointer)
		{
			_advanceService = new AdvanceManagementService();
			Palette = Common.Screens.LastOrDefault()?.OriginalColours ?? Common.DefaultPalette;

			_civSelectDelegate = CreateCivSelectDelegate();

			DrawPlayerMenuDialog();
		}
	}
}