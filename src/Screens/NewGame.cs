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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using CivOne.Agents;
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Persistence.Game;
using CivOne.Screens.NewGamePanels;
using CivOne.Tasks;
using CivOne.Units;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	[ScreenResizeable]
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Screen members stay instance members so they can use the screen state and be overridden by derived screens.")]
	internal class NewGame : BaseScreen, INewGameMenuHost
	{
		private readonly NewGameRulesDelegate _rules;
		private readonly NewGameCompetitionMenuDelegate _competitionMenu;
		private readonly NewGameBarbarianMenuDelegate _barbarianMenu;
		private readonly NewGameTribeMenuDelegate _tribeMenu;

		private ICivilization[] _tribesAvailable = [];
		private readonly string[] _menuItemsDifficulty;
		private readonly Picture _background;

		private int OffsetX => (Width - 320) / 2;
		private int OffsetY => (Height - 200) / 2;

		private int _difficulty = -1, _competition = -1, _tribe = -1;
		private string? _leaderName;
		private string? _tribeName;
		private string? _tribeNamePlural;
		private NewGameAiSelectionResult? _aiSelectionResult;

		private bool _done, _showIntroText, _gameCreated, _introDirty;
		private int _introBorderStyle = -1;

		/// <summary>
		/// Menu value of the extra difficulty menu entry that opens the AI selection screen.
		/// Kept outside the range of the difficulty indices so it cannot collide with one.
		/// </summary>
		private const int AiSelectionMenuValue = -999;

		/// <summary>
		/// Creates an empty menu with the look and position used by this screen.
		/// </summary>
		/// <param name="title">Menu title.</param>
		/// <param name="yOffset">Vertical shift relative to the default menu position.</param>
		/// <returns>The created menu.</returns>
		public Menu CreateNewGameMenu(string title, int yOffset = 0)
		{
			return new Menu("NewGameMenu", Palette)
			{
				Title = title,
				X = OffsetX + 163,
				Y = OffsetY + 39 + yOffset,
				MenuWidth = 114,
				TitleColour = 3,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 8,
				FontId = 6,
				IndentTitle = 2,
				RowHeight = 8
			};
		}

		/// <summary>
		/// Shows a menu created by <see cref="CreateNewGameMenu"/>.
		/// </summary>
		/// <param name="menu">The menu to show.</param>
		public void ShowMenu(Menu menu)
		{
			AddMenu(menu);
		}

		/// <summary>
		/// Closes every menu currently shown by this screen.
		/// </summary>
		public void CloseOpenMenus()
		{
			CloseMenus();
		}

		/// <summary>
		/// Reopens the competition menu.
		/// The submenus reached from it return here when they are left.
		/// </summary>
		private void ShowCompetitionMenu()
		{
			_competitionMenu.ShowMenu();
		}

		private void MenuDifficulty()
		{
			Menu menu = CreateNewGameMenu(Translate("Difficulty Level..."));
			menu.Hints = [Translate("Esc: Back")];
			for (int i = 0; i < _menuItemsDifficulty.Length; i++)
			{
				menu.Items.Add(_menuItemsDifficulty[i], i).OnSelect(SetDifficulty);
			}
			menu.Items.Add(Translate("Use AI selections..."), AiSelectionMenuValue).TextColor(3).OnSelect(SetDifficulty);
			menu.Cancel += DifficultyMenu_Cancel;
			AddMenu(menu);
		}

		private void InputLeaderName()
		{
			if (Common.HasScreenType<Input>()) return;

			ICivilization civ = _tribesAvailable[_tribe];
			Input input = new(Palette, civ.Leader.Name, 6, 5, 11, OffsetX + 168, OffsetY + 105, 109, 10, 13);
			input.Accept += LeaderName_Accept;
			input.Cancel += LeaderName_Accept;
			Common.AddScreen(input);
		}

		private void SetDifficulty(object sender, MenuItemEventArgs<int> args)
		{
			if (args.Value == AiSelectionMenuValue)
			{
				CloseMenus();
				OpenAiSelectionScreen();
				return;
			}

			_difficulty = args.Value;
			CloseMenus();
			Log("Difficulty: {0}", _menuItemsDifficulty[_difficulty]);
		}

		private void OpenAiSelectionScreen()
		{
			int initialDifficulty = _difficulty >= 0 ? _difficulty : 0;
			NewGameAiSelection aiSelection = new(initialDifficulty);
			aiSelection.StartRequested += AiSelection_StartRequested;
			aiSelection.Closed += AiSelection_Closed;
			Common.AddScreen(aiSelection);
		}

		/// <summary>
		/// Takes over the setup made on the AI selection screen, so the remaining steps of this screen
		/// (competition menu, tribe menu, name input) are already answered and get skipped.
		/// </summary>
		private void AiSelection_StartRequested(object? sender, NewGameAiSelectionResultEventArgs args)
		{
			ArgumentNullException.ThrowIfNull(args);
			_aiSelectionResult = args.Result;

			_difficulty = _aiSelectionResult.Difficulty;

			// The selection screen reports the number of non-barbarian players, this screen counts the
			// opponents of the human player.
			_competition = _aiSelectionResult.Competition - 1;
			_tribeMenu.Reset();

			ICivilization humanCivilization = _aiSelectionResult.Human.Civilization;
			_tribesAvailable = _rules.GetSelectableCivilizations(_competition);
			_tribe = Array.FindIndex(_tribesAvailable, civ => civ.Id == humanCivilization.Id);
			if (_tribe < 0)
			{
				// The selection screen offers every civilization, the tribe menu of this screen only the
				// ones allowed for the chosen game size. Keep the player's choice instead of replacing it.
				_tribesAvailable = [.. _tribesAvailable, humanCivilization];
				_tribe = _tribesAvailable.Length - 1;
			}

			_leaderName = _aiSelectionResult.Human.Name;
			_tribeName = humanCivilization.Name;
			_tribeNamePlural = humanCivilization.NamePlural;
		}

		private void AiSelection_Closed(object? sender, EventArgs args)
		{
			if (_difficulty < 0)
			{
				CloseMenus();
			}
		}

		/// <summary>
		/// Copies the AI, name and difficulty picked per opponent onto the players of the created game.
		/// The player slots are assigned during game creation, so the selections are matched by
		/// civilization rather than by a slot number known beforehand.
		/// Each created player is claimed at most once, so a civilization reused by a large game still
		/// gets one selection each.
		/// </summary>
		private void ApplyAiSelectionsToCreatedGame()
		{
			if (_aiSelectionResult is null)
			{
				return;
			}

			List<Player> unclaimedPlayers = [.. Game.Players.Where(player => !player.IsHuman)];

			foreach (NewGamePlayerSelection selection in _aiSelectionResult.Opponents)
			{
				Player? player = unclaimedPlayers.Find(candidate => candidate.Civilization.Id == selection.Civilization.Id);
				if (player is null)
				{
					Log("NewGame: No player slot for the selected {0}, skipping its AI setup.", selection.Civilization.NamePlural);
					continue;
				}

				unclaimedPlayers.Remove(player);

				IPlayerRestorable restorable = player;
				restorable.AiId = selection.AiId;
				if (!string.IsNullOrWhiteSpace(selection.Name))
				{
					string trimmedName = selection.Name.Trim();
					restorable.TribeName = trimmedName;
					restorable.TribeNamePlural = trimmedName;
				}

				if (selection.DifficultyIndex >= 0 && selection.DifficultyIndex <= Game.Instance.MaxDifficulty)
				{
					player.AiDifficulty = (AiDifficulty)selection.DifficultyIndex;
				}
			}
		}

		/// <summary>
		/// Stores the opponent count picked in the competition menu and rebuilds the list of
		/// civilizations that may be picked for a game of that size.
		/// </summary>
		/// <param name="opponents">The number of AI opponents.</param>
		private void SetCompetition(int opponents)
		{
			_competition = opponents;
			_tribeMenu.Reset();
			Log("Competition: {0} Civilizations", _rules.OpponentsToCompetition(_competition));

			_tribesAvailable = _rules.GetSelectableCivilizations(_competition);
		}

		private void DifficultyMenu_Cancel(object? sender, EventArgs args)
		{
			CloseMenus();
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

		/// <summary>
		/// Opens the input box for a custom name of the civilization the player wants to rename.
		/// </summary>
		/// <param name="tribeIndex">Index of the civilization in the list of available tribes.</param>
		private void StartCustomTribeNameInput(int tribeIndex)
		{
			_tribe = tribeIndex;

			ICivilization civ = _tribesAvailable[_tribe];
			Input input = new(Palette, civ.NamePlural, 6, 5, 11, OffsetX + 168, OffsetY + 105, 109, 10, 11);
			input.Accept += TribeName_Accept;
			input.Cancel += TribeName_Accept;
			Common.AddScreen(input);
		}

		/// <summary>
		/// Stores the civilization picked in the tribe menu.
		/// </summary>
		/// <param name="tribeIndex">Index of the civilization in the list of available tribes.</param>
		private void SetTribe(int tribeIndex)
		{
			_tribe = tribeIndex;

			ICivilization civ = _tribesAvailable[_tribe];
			_tribeName = civ.Name;
			_tribeNamePlural = civ.NamePlural;
			Log("Tribe: {0}", civ.Name);
		}

		private void LeaderName_Accept(object? sender, EventArgs args)
		{
			if (sender is not Input input) return;

			_leaderName = input.Text;
			input.Close();
		}

		private void TribeName_Accept(object? sender, EventArgs args)
		{
			if (sender is not Input input) return;

			_tribeNamePlural = input.Text;
			_tribeName = input.Text;
			input.Close();
		}

		private Picture DifficultyPicture
		{
			get
			{
				int pictureId = _difficulty;
				if (pictureId > 4) pictureId = 4;

				int x = (pictureId % 2) == 0 ? 21 : 80;
				int y = 6 + (35 * pictureId);
				return _background[x, y, 53, 47];
			}
		}

		private void DrawInputBox(string text)
		{
			this.FillRectangle(OffsetX + 158, OffsetY + 88, 161, 33, 11)
				.FillRectangle(OffsetX + 159, OffsetY + 89, 159, 31, 15)
				.DrawText(text, 6, 5, OffsetX + 166, OffsetY + 90)
				.FillRectangle(OffsetX + 166, OffsetY + 103, 113, 14, 5)
				.FillRectangle(OffsetX + 167, OffsetY + 104, 111, 12, 15);
		}

		private bool IsMapGenerationComplete()
		{
			if (!Map.Ready)
			{
				Log("Map not ready yet. Waiting for generation to finish.");
				return false;
			}

			return true;
		}

		[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching all exceptions is necessary to ensure that failure to create a game does not crash the application, and that any exceptions are logged appropriately.")]
		protected override bool HasUpdate(uint gameTick)
		{
			if (HasMenu) return false;

			if (_difficulty == -1) MenuDifficulty();
			else if (_competition == -1) _competitionMenu.ShowMenu();
			else if (_tribe == -1) _tribeMenu.ShowMenu();
			else if (_leaderName == null) InputLeaderName();
			else if (!_done)
			{
				if (!_gameCreated)
				{
					if (!IsMapGenerationComplete())
					{
						return false;
					}

					ICivilization civ = _tribesAvailable[_tribe];
					try
					{
						Game.CreateGame(_difficulty, _rules.OpponentsToCompetition(_competition), civ, _leaderName, _tribeName, _tribeNamePlural, replaceExisting: true);
						ApplyAiSelectionsToCreatedGame();
					}
					catch (Exception ex)
					{
						Log("NewGame: game creation failed - {0}", ex.Message);
						return false;
					}

					_gameCreated = true;
					_introBorderStyle = RandomService.NextInt(2);
					_introDirty = true;
				}

				if (_showIntroText && !_introDirty) return false;

				this.Clear(15);
				DrawBorder(_introBorderStyle);

				this.AddLayer(DifficultyPicture, OffsetX + 134, OffsetY + 20);

				int yy = OffsetY + 81;
				foreach (string textLine in GetGameText("KING/INIT"))
				{
					string line = textLine
						.Replace("$RPLC1", Human.LeaderName, StringComparison.InvariantCulture)
						.Replace("$US", Human.TribeNamePlural, StringComparison.InvariantCulture)
						.Replace("^", "", StringComparison.InvariantCulture);
					this.DrawText(line, 0, 5, OffsetX + 88, yy);
					yy += 8;
					Log(line);
				}
				StringBuilder sb = new();
				int i = 0;
				foreach (IAdvance advance in Human.Advances.OrderBy(a => a.Id))
				{
					sb.Append(CultureInfo.CurrentCulture,$"{advance.TranslatedName}, ");
					i++;
					if (i % 2 == 0) sb.Append('|');
				}
				sb.Append(Translate("and Roads."));

				foreach (string line in sb.ToString().Split('|'))
				{
					this.DrawText(line, 0, 5, OffsetX + 88, yy);
					Log(line);
					yy += 8;
				}

				PlaySound(Human.Civilization.Tune);

				_showIntroText = true;
				_introDirty = false;
				return true;
			}
			else if (HandleScreenFadeOut())
			{
				return true;
			}
			else
			{
				if (!_gameCreated)
				{
					_done = false;
					return false;
				}

				Destroy();

				GamePlay gamePlay = new();
				Common.AddScreen(gamePlay);
				IUnit? startUnit = Game.GetUnits().FirstOrDefault(x => Game.Human == x.Owner);
				if (startUnit != null)
				{
					gamePlay.CenterOnPoint(startUnit.X, startUnit.Y);
				}
				else
				{
					// Without a start unit there is nothing to activate: the map would come up with no
					// blinking unit and swallow every input, so say so instead of locking up silently.
					Log("NewGame: No human start unit found. Falling back to map center.");
					gamePlay.CenterOnPoint(Map.WIDTH / 2, Map.HEIGHT / 2);
					GameTask.Enqueue(Message.Error(
						Translate("--- Map Problem ---"),
						TranslateArray("Your civilization has no\nstarting position on this map.")));
				}

				if (Game.UnplacedCivilizations.Count > 0)
				{
					string names = string.Join(", ", Game.UnplacedCivilizations.Select(c => c.NamePlural));
					GameTask.Enqueue(Message.Error(
						Translate("--- Map Problem ---"),
						TranslateFormattedArray("This map has no free land for:\n{0}\nRemoved from the game.", names)));
				}

				if (Game.InstantAdvice)
				{
					GameTask.Enqueue(Show.InterfaceHelp);
					GameTask.Enqueue(Message.Help(Translate("--- Civilization Note ---"), GetGameText("HELP/FIRSTMOVE")));
				}
				return true;
			}

			if (_tribe != -1 && _tribeName == null)
			{
				DrawInputBox(Translate("Name of your Tribe..."));
				return true;
			}

			// Draw background
			Bitmap = new Bytemap(Width, Height);
			if (_difficulty == -1)
			{
				this.AddLayer(_background, OffsetX, OffsetY);
			}
			else
			{
				if (_tribe == -1)
					this.AddLayer(_background[140, 0, 180, 200], OffsetX + 140, OffsetY);
				// One stacked portrait per civilization in the game (human player included), capped at the
				// seven the original game draws.
				int pictureStack = (_competition <= 0) ? 1 : Math.Min(_rules.OpponentsToCompetition(_competition), 7);
				for (int i = pictureStack; i > 0; i--)
				{
					this.AddLayer(DifficultyPicture, OffsetX + 22 + (i * 2), OffsetY + 100 + (i * 3));
				}

				if (_tribe != -1 && _leaderName == null)
				{
					this.DrawText(_tribeNamePlural!, 6, 15, OffsetX + 47, OffsetY + 92, TextAlign.Center);
					DrawInputBox(Translate("Your Name..."));
				}
			}

			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_tribe != -1 && _leaderName == null)
			{
				if (args.Key == Key.Enter)
				{
					ICivilization civ = _tribesAvailable[_tribe];
					_leaderName = civ.Leader.Name;
					return true;
				}
				return false;
			}
			if (_difficulty > -1 && _competition > -1 && _tribe > -1 && _gameCreated && !_done)
				_done = true;
			return _done;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_difficulty > -1 && _competition > -1 && _tribe > -1 && _gameCreated && !_done)
				_done = true;
			return _done;
		}

		private void Resize(object? _, ResizeEventArgs args)
		{
			this.FillRectangle(0, 0, args.Width, args.Height, 5);
			if (_leaderName == null)
			{
				CloseMenus();
			}
			foreach (Input input in Common.Screens.Where(x => x is Input))
			{
				input.X = OffsetX + 168;
				input.Y = OffsetY + 105;
			}
			if (_showIntroText)
			{
				_introDirty = true;
			}
		}

		public NewGame() : base(MouseCursor.Pointer)
		{
			OnResize += Resize;

			if (Runtime.Settings.Free || !Resources.Exists("DIFFS"))
			{
				_background = new Picture(Free.Difficulties, Common.GetPalette256);
			}
			else
			{
				_background = Resources["DIFFS"];
			}

			Palette = _background.Palette.Copy();
			this.AddLayer(_background);

			_rules = new NewGameRulesDelegate();
			_barbarianMenu = new NewGameBarbarianMenuDelegate(this, ShowCompetitionMenu);
			_competitionMenu = new NewGameCompetitionMenuDelegate(this, _rules, SetCompetition, MenuDifficulty, barbarianMenu: _barbarianMenu);
			_tribeMenu = new NewGameTribeMenuDelegate(this, _rules, () => _tribesAvailable, SetTribe, ShowCompetitionMenu, StartCustomTribeNameInput);

			_menuItemsDifficulty = _rules.BuildDifficultyMenuItems();
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_background?.Dispose();
			base.Dispose(disposing);
		}
	}
}
