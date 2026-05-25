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
		private readonly IAdvanceManagementService _advanceService;
		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private CivSelectMenuDelegate _civSelectDelegate;
		private GridMenuDelegate _gridDelegate;
		private IAdvance[] _advances;

		private Player _selectedPlayer = null;

		public string Value { get; }

		public event EventHandler Cancel;

		private void DrawPlayerMenuDialog()
		{
			_civSelectDelegate.DrawDialog(this, OffsetX, OffsetY);
		}

		private void RenderAdvancesGrid()
		{
			if (_gridDelegate == null)
				CreateGridDelegate();
			_gridDelegate.Draw(this, $"Set Advances: {_selectedPlayer.TribeNamePlural} (Help: Alt+H)", CanvasHeight);
		}

		private void CreateGridDelegate()
		{
			_advances = _advanceService.GetAllAdvances();
			string[] labels = [.. _advances.Select(a => a.TranslatedName)];
			_gridDelegate = new GridMenuDelegate(
				labels,
				GridMenuDelegate.SelectionMode.CheckUncheck,
				isChecked: i => _selectedPlayer.HasAdvance(_advances[i]));
			_gridDelegate.ItemChecked += OnItemChecked;
			_gridDelegate.Cancelled += OnGridCancelled;
		}

		private void OnItemChecked(int index)
		{
			_advanceService.ToggleAdvance(Game.PlayerNumber(_selectedPlayer), _advances[index]);
			Refresh();
		}

		private void OnGridCancelled(object sender, EventArgs args)
		{
			_selectedPlayer = null;
			_gridDelegate = null;
			_civSelectDelegate = CreateCivSelectDelegate();
			Refresh();
		}

		private CivSelectMenuDelegate CreateCivSelectDelegate()
		{
			var delegate_ = new CivSelectMenuDelegate(Palette, "Set Player Advances...");
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

		private void OnCancel(object sender, EventArgs args)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			Destroy();
		}

		private bool TryOpenCivilopediaForSelectedAdvance()
		{
			if (_gridDelegate == null || _advances == null) return false;
			int idx = _gridDelegate.SelectedIndex;
			if (idx < 0) return false;
			Common.AddScreen(new CivOne.Screens.Civilopedia(_advances[idx]));
			return true;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			// Draw the appropriate dialog based on state
			if (_selectedPlayer == null)
			{
				// State 1: Civ selection
				if (RefreshNeeded())
					DrawPlayerMenuDialog();

				if (Common.TopScreen.GetType() != typeof(Menu))
				{
					AddMenu(_civSelectDelegate.Menu);
					return false;
				}
			}
			else
			{
				// State 2: Grid display
				if (RefreshNeeded())
					RenderAdvancesGrid();
			}

			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_selectedPlayer == null) return false;
			if (_gridDelegate == null) return false;

			if (args.Alt && args.Key == Key.Character && (args.KeyChar == 'h' || args.KeyChar == 'H'))
				return TryOpenCivilopediaForSelectedAdvance();

			bool handled = _gridDelegate.KeyDown(args);
			if (handled) Refresh();
			return handled;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_selectedPlayer == null || _gridDelegate == null) return false;
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