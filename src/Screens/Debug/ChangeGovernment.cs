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
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Governments;
using CivOne.Tasks;

namespace CivOne.Screens.Debug
{
	/// <summary>
	/// Debug screen that assigns a government to any living civilization.
	/// </summary>
	/// <remarks>
	/// Both steps use the paging grid dialogs, so the player list stays usable with the maximum of
	/// 32 players instead of running off the bottom of a fixed-size dialog box.
	/// </remarks>
	[ScreenResizeable]
	internal class DebugChangeGovernment : BaseScreen
	{
		private readonly Player[] _livingPlayers;
		private readonly IGovernment[] _governments;

		private readonly CivSelectMenuDelegate? _playerSelect;
		private GridMenuDelegate? _governmentSelect;

		private Player? _selectedPlayer;

		private void OnPlayerSelected(Player player)
		{
			_selectedPlayer = player;
			_governmentSelect = null;
			Refresh();
		}

		private void CreateGovernmentGrid()
		{
			Player? selectedPlayer = _selectedPlayer;
			if (selectedPlayer == null)
			{
				return;
			}

			string[] labels = [.. _governments.Select(government => government.TranslatedName)];
			_governmentSelect = new GridMenuDelegate(
				labels,
				GridMenuDelegate.SelectionMode.Select,
				isChecked: i => _governments[i].Id == selectedPlayer.Government.Id,
				fontId: 0);
			_governmentSelect.ItemSelected += OnGovernmentSelected;
			_governmentSelect.Cancelled += OnGovernmentCancelled;
		}

		private void OnGovernmentSelected(int index)
		{
			Player? selectedPlayer = _selectedPlayer;
			if (selectedPlayer == null || index < 0 || index >= _governments.Length)
			{
				return;
			}

			IGovernment government = _governments[index];
			if (government.Id == selectedPlayer.Government.Id)
			{
				Destroy();
				return;
			}

			selectedPlayer.Government = government;
			GameTask.Enqueue(Message.NewGoverment(null,
				$"{selectedPlayer.TribeName} government",
				$"changed to {government.TranslatedName}!"));
			Destroy();
		}

		private void OnGovernmentCancelled(object? _, EventArgs __)
		{
			_selectedPlayer = null;
			_governmentSelect = null;
			Refresh();
		}

		private void OnCancel(object? _, EventArgs __) => Destroy();

		private void DrawDialog()
		{
			if (_playerSelect == null)
			{
				return;
			}

			if (_selectedPlayer == null)
			{
				_playerSelect.Draw(this, CanvasHeight);
				return;
			}

			if (_governmentSelect == null)
			{
				CreateGovernmentGrid();
			}
			_governmentSelect?.Draw(this, TranslateFormatted("Government: {0}", _selectedPlayer.TribeNamePlural), CanvasHeight);
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (RefreshNeeded())
			{
				DrawDialog();
				return true;
			}
			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			bool handled = ActiveDelegateKeyDown(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		private bool ActiveDelegateKeyDown(KeyboardEventArgs args)
		{
			if (_selectedPlayer == null)
			{
				return _playerSelect?.KeyDown(args) ?? false;
			}
			return _governmentSelect?.KeyDown(args) ?? false;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			bool handled = ActiveDelegateMouseDown(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		private bool ActiveDelegateMouseDown(ScreenEventArgs args)
		{
			if (_selectedPlayer == null)
			{
				return _playerSelect?.MouseDown(args.X, args.Y) ?? false;
			}
			return _governmentSelect?.MouseDown(args.X, args.Y) ?? false;
		}

		private static bool IsLivingCivilization(Player player)
		{
			if (player == null || player.Civilization is Barbarian)
			{
				return false;
			}

			return !player.IsDestroyed;
		}

		public DebugChangeGovernment() : base(MouseCursor.Pointer)
		{
			Palette = Common.Screens.LastOrDefault()?.OriginalColours ?? Common.DefaultPalette;

			_livingPlayers = [.. Game.Players.Where(IsLivingCivilization)];
			_governments = [.. Reflect.GetGovernments()];

			if (_livingPlayers.Length == 0)
			{
				GameTask.Enqueue(Message.General("No living civilization available."));
				Destroy();
				return;
			}

			_playerSelect = new CivSelectMenuDelegate(_livingPlayers, Translate("Debug government change"));
			_playerSelect.PlayerSelected += OnPlayerSelected;
			_playerSelect.Cancelled += OnCancel;

			DrawDialog();
		}
	}
}
