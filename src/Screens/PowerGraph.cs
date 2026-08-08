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
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens.Debug;
using CivOne.Services;
using CivOne.Services.Screen;

namespace CivOne.Screens
{
	internal class PowerGraph : BaseScreen
	{
		private const int RowHeight = 8;
		private const int FirstRowY = 12;

		private readonly IPowerGraphSelectionService _selection = PowerGraphSelectionServiceFactory.Current;
		private readonly Player[] _players;
		private readonly int[] _playerNumbers;
		private readonly bool _canSelectCivilizations;

		private GridMenuDelegate? _civSelect;
		private bool _update = true;

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;
			_update = false;

			if (_civSelect != null)
			{
				// The graph keeps the fixed 320x200 layout, so the dialog has to fit into the bitmap and not
				// into a possibly larger canvas: mouse input is scaled to the bitmap as well.
				_civSelect.Draw(this, SelectionTitle, this.Height());
				return true;
			}

			DrawGraph();
			return true;
		}

		private string SelectionTitle => TranslateFormatted("Show Civilizations ({0}/{1})", _selection.SelectedCount, _selection.MaxVisiblePlayers);

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_civSelect != null)
			{
				HandleSelectionKey(args);
				return true;
			}

			if (args.Key == Key.F1 && _canSelectCivilizations)
			{
				OpenCivSelect();
				return true;
			}

			Destroy();
			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			ArgumentNullException.ThrowIfNull(args);

			if (_civSelect != null)
			{
				if (_civSelect.MouseDown(args.X, args.Y))
				{
					_update = true;
				}
				return true;
			}

			Destroy();
			return true;
		}

		private void HandleSelectionKey(KeyboardEventArgs args)
		{
			// F1 closes the dialog again, every other key stays inside it so the graph screen is not
			// destroyed by a keypress that was meant for the selection.
			if (args.Key == Key.F1)
			{
				CloseCivSelect();
				return;
			}

			if (_civSelect!.KeyDown(args))
			{
				_update = true;
			}
		}

		private void OpenCivSelect()
		{
			_civSelect = new GridMenuDelegate(
				[.. _players.Select(player => player.TribeName)],
				GridMenuDelegate.SelectionMode.CheckUncheck,
				isChecked: index => _selection.IsSelected(_playerNumbers[index]),
				fontId: 0,
				enableHotkeys: true);
			_civSelect.ItemChecked += ToggleCivilization;
			_civSelect.Cancelled += CivSelectCancelled;
			_update = true;
		}

		private void CloseCivSelect()
		{
			_civSelect = null;
			_update = true;
		}

		private void CivSelectCancelled(object? sender, EventArgs args) => CloseCivSelect();

		private void ToggleCivilization(int index)
		{
			if (index < 0 || index >= _playerNumbers.Length)
			{
				return;
			}

			// Toggle returns false when the maximum is already reached; the dialog then simply stays as it is.
			_selection.Toggle(_playerNumbers[index]);
			_update = true;
		}

		private void DrawGraph()
		{
			string title = Translate("CIVILIZATION POWERGraph");
			this.Clear(8)
				.DrawText(title, 0, 5, 100, 3)
				.DrawText(title, 0, 15, 100, 2)
				.DrawRectangle(4, 9, 312, 184);

			DrawTimeline();
			DrawLegend();

			if (_canSelectCivilizations)
			{
				this.DrawText(Translate("F1: Select civilizations"), 1, 15, 312, 184, TextAlign.Right);
			}
		}

		private void DrawTimeline()
		{
			var gameCalendarService = GameCalendarServiceFactory.Current;

			for (int i = 0; i < 13; i++)
			{
				int xx = 4 + (i * 25);
				ushort turn = (ushort)(i * 50);
				if (turn > Game.GameTurn) break;
				this.DrawLine(xx, 9, xx, 192);
				if (turn % 100 != 0) continue;

				var year = gameCalendarService.FormatYear(turn).Replace(" ", "", StringComparison.InvariantCulture);
				this.DrawText(year, 1, 15, xx - 4, 194);
			}
		}

		private void DrawLegend()
		{
			IReadOnlyList<int> visible = _selection.GetVisiblePlayers(_playerNumbers, Game.PlayerNumber(Human));

			int row = 0;
			for (int i = 0; i < _players.Length; i++)
			{
				if (!visible.Contains(_playerNumbers[i]))
				{
					continue;
				}

				this.DrawText(_players[i].TribeName, 0, Common.PlayerColourLight(_playerNumbers[i]), 8, FirstRowY + (row * RowHeight));
				row++;
			}
		}

		public PowerGraph() : base(MouseCursor.None)
		{
			Palette = ScreenServiceFactory.CreateQueryService().TopScreen!.Palette.Copy();

			_players = [.. Game.Players.Where(x => x.Civilization is not Barbarian)];
			_playerNumbers = [.. _players.Select(player => (int)Game.PlayerNumber(player))];
			_canSelectCivilizations = _selection.RequiresSelection(_playerNumbers);

			DrawGraph();
		}
	}
}
