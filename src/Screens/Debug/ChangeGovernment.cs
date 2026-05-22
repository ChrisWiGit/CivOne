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
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Governments;
using CivOne.Screens.Dialogs;
using CivOne.Tasks;
using CivOne.UserInterface;

namespace CivOne.Screens.Debug
{
	internal class DebugChangeGovernment : BaseDialog
	{
		private enum SelectionStep
		{
			SelectPlayer,
			SelectGovernment
		}

		private readonly Player[] _livingPlayers;
		private readonly IGovernment[] _governments;
		private SelectionStep _step = SelectionStep.SelectPlayer;
		private Player _selectedPlayer;

		private Menu<Player> _playerMenu;
		private Menu<IGovernment> _governmentMenu;

		private void PlayerChoice(object sender, MenuItemEventArgs<Player> args)
		{
			_selectedPlayer = args.Value;
			_step = SelectionStep.SelectGovernment;
			CloseMenus();
			_playerMenu = null;
			Refresh();
		}

		private void GovernmentChoice(object sender, MenuItemEventArgs<IGovernment> args)
		{
			_selectedPlayer.Government = args.Value;
			GameTask.Enqueue(Message.NewGoverment(null,
				$"{_selectedPlayer.TribeName} government",
				$"changed to {args.Value.Name}!"));
			Cancel();
		}

		private void CreatePlayerMenu()
		{
			if (_playerMenu != null)
			{
				return;
			}
			int menuPositionY = 2 * Resources.GetFontHeight(0) + 4;


			int menuHeight = (_livingPlayers.Length * Resources.GetFontHeight(0)) + 4;
			_playerMenu = new Menu<Player>("DebugChangeGovernmentPlayer", Palette, Selection(3, 20, 178, menuHeight))
			{
				X = 70,
				Y = 84 + menuPositionY,
				CenterTo320Coordinates = true,
				MenuWidth = 176,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = 0,
				Indent = 2
			};

			foreach (Player player in _livingPlayers)
			{
				_playerMenu.Items.Add(player.TribeNamePlural, player).OnSelect(PlayerChoice);
			}

			_playerMenu.ActiveItem = Array.FindIndex(_livingPlayers, player => player == Human);
			if (_playerMenu.ActiveItem < 0)
			{
				_playerMenu.ActiveItem = 0;
			}

			_playerMenu.MissClick += Cancel;
			_playerMenu.Cancel += Cancel;
			AddMenu(_playerMenu);
		}

		private void CreateGovernmentMenu()
		{
			if (_governmentMenu != null)
			{
				return;
			}

			int menuPositionY = 2 * Resources.GetFontHeight(0) + 4;

			int menuHeight = (_governments.Length * Resources.GetFontHeight(0)) + 4;
			_governmentMenu = new Menu<IGovernment>("DebugChangeGovernmentGovernment", Palette, Selection(3, 20, 178, menuHeight))
			{
				X = 70,
				Y = 84 + menuPositionY,
				CenterTo320Coordinates = true,
				MenuWidth = 176,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = 0,
				Indent = 2
			};

			foreach (IGovernment government in _governments)
			{
				bool isCurrentGovernment = government.Id == _selectedPlayer.Government.Id;
				_governmentMenu.Items
					.Add(government.Name, government)
					.SetEnabled(!isCurrentGovernment)
					.OnSelect(GovernmentChoice);
			}

			int firstSelectable = Array.FindIndex(_governments, government => government.Id != _selectedPlayer.Government.Id);
			_governmentMenu.ActiveItem = (firstSelectable >= 0) ? firstSelectable : 0;

			_governmentMenu.MissClick += Cancel;
			_governmentMenu.Cancel += Cancel;
			AddMenu(_governmentMenu);
		}

		protected override void FirstUpdate()
		{
			if (_step == SelectionStep.SelectPlayer)
			{
				CreatePlayerMenu();
				return;
			}

			CreateGovernmentMenu();
		}

		private static bool IsLivingCivilization(Player player)
		{
			if (player == null || player.Civilization is Barbarian)
			{
				return false;
			}

			return !player.IsDestroyed;
		}

		public DebugChangeGovernment() : base(68, 80, 182, 84)
		{
			_livingPlayers = [.. Game.Players.Where(IsLivingCivilization)];
			_governments = [.. Reflect.GetGovernments()];

			if (_livingPlayers.Length == 0)
			{
				GameTask.Enqueue(Message.General("No living civilization available."));
				Destroy();
				return;
			}

			DialogBox.DrawText("Debug government change", 0, 15, 5, 5);
			DialogBox.DrawText("Select entry below...", 0, 15, 5, 13);
		}
	}
}