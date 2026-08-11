using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Tasks;

namespace CivOne.Screens.Debug
{
	/// <summary>
	/// Debug screen that picks the civilization whose palace is opened in the palace builder.
	/// </summary>
	/// <remarks>
	/// The palace builder itself always edited the human player's palace.
	/// Selecting a civilization first makes every player's palace reachable; the human player is
	/// preselected, so confirming right away keeps the previous behaviour.
	/// </remarks>
	[ScreenResizeable]
	internal class DebugBuildPalace : BaseScreen
	{
		private readonly CivSelectMenuDelegate _civSelect;

		private bool _playerSelected;

		private void OnPlayerSelected(Player player)
		{
			_playerSelected = true;
			GameTask.Enqueue(Show.BuildPalace(player.Palace, keepOpenUntilEscape: true));
			Destroy();
		}

		private void OnCancel(object? _, EventArgs __) => Destroy();

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded() && !_playerSelected)
			{
				_civSelect.Draw(this, CanvasHeight);
				return true;
			}
			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_playerSelected)
			{
				return false;
			}

			bool handled = _civSelect.KeyDown(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_playerSelected)
			{
				return false;
			}

			bool handled = _civSelect.MouseDown(args.X, args.Y);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public DebugBuildPalace() : base(MouseCursor.Pointer)
		{
			Palette = Common.Screens.LastOrDefault()?.OriginalColours ?? Common.DefaultPalette;

			_civSelect = new CivSelectMenuDelegate(Translate("Build Palace"));
			_civSelect.PlayerSelected += OnPlayerSelected;
			_civSelect.Cancelled += OnCancel;
		}
	}
}
