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
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	#pragma warning disable CA1822 // Mark members as static
	[ScreenResizeable]
	internal class NewGame : BaseScreen
	{
		private const int ClassicCivilizationMaxId = 14;
		private const int CompetitionOpenExtendedValue = -900;
		private const int CompetitionScrollLessValue = -901;
		private const int CompetitionScrollMoreValue = -902;
		// Every value in this screen is an opponent count (the number of AI players), while Game.CreateGame
		// expects the number of non-barbarian players (the human player plus the opponents).
		// OpponentsToCompetition converts between the two; the bounds are derived from the game limits so
		// the menu can never offer a player count the game would reject.
		private const int CompetitionMin = Game.MinCompetition - 1;
		private const int CompetitionMainMin = 2;
		private const int CompetitionMainMax = 6;
		private const int ClassicMaxOpponents = 6;
		private const int CompetitionMax = Game.MaxCompetition - 1;
		private const int ExtendedCompetitionMenuPageSize = 11;
		private const int TribeOpenExtendedMenuValue = -1000;
		private const int TribeScrollPreviousValue = -1001;
		private const int TribeScrollNextValue = -1002;
		private const int TribeBackToClassicValue = -1003;
		private const int ExtendedTribeMenuPageSize = 13;
		private const int TribeMenuYOffset = -8;

		private ICivilization[] _tribesAvailable = [];
		private readonly string[] _menuItemsDifficulty;
		private string[] _menuItemsTribes = [];
		private readonly Picture _background;

		private int OffsetX => (Width - 320) / 2;
		private int OffsetY => (Height - 200) / 2;

		private int _difficulty = -1, _competition = -1, _tribe = -1;
		private string? _leaderName;
		private string? _tribeName;
		private string? _tribeNamePlural;

		private bool _done, _showIntroText, _gameCreated, _introDirty;
		private int _introBorderStyle = -1;
		private int _extendedCompetitionMenuOffset;
		private int _extendedTribeMenuOffset;

		/// <summary>
		/// Converts the number of opponents selected in this screen into the number of non-barbarian
		/// players <see cref="Game.CreateGame"/> expects: the human player plus the opponents.
		/// The barbarians always get their own slot on top and are not part of this count.
		/// </summary>
		/// <param name="opponents">The number of AI opponents.</param>
		/// <returns>The number of non-barbarian players, clamped to the range a game can be created with.</returns>
		private static int OpponentsToCompetition(int opponents)
		{
			return Math.Clamp(opponents + 1, Game.MinCompetition, Game.MaxCompetition);
		}
		
		
		private Menu CreateMenu(string title, MenuItemEventAction<int> setChoice, int yOffset = 0, params string[] menuTexts)
		{
			Menu menu = new("NewGameMenu", Palette)
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
			
			for (int i = 0; i < menuTexts.Length; i++)
			{
				menu.Items.Add(menuTexts[i], i).OnSelect(setChoice);
			}
			return menu;
		}
		
		private void MenuDifficulty()
		{
			Menu menu = CreateMenu(Translate("Difficulty Level..."), SetDifficulty, 0, _menuItemsDifficulty);
			menu.Hints = [Translate("Esc: Back")];
			menu.Cancel += DifficultyMenu_Cancel;
			AddMenu(menu);
		}
		
		private void MenuCompetition()
		{
			Menu menu = CreateMenu(Translate("Level of Competition..."), SetCompetition);
			menu.Hints = [Translate("Esc: Back")];
			menu.OnCustomKeyDown = CompetitionMenu_KeyDown;
			for (int i = CompetitionMainMax; i >= CompetitionMainMin; i--)
			{
				menu.Items.Add(TranslateFormatted("{0} Civilizations", OpponentsToCompetition(i)), i).OnSelect(SetCompetition);
			}

			menu.Items.Add(Translate("More Civilizations..."), CompetitionOpenExtendedValue).OnSelect(SetCompetition);
			menu.Cancel += SetCompetition_Cancel;
			AddMenu(menu);
		}

		private bool CompetitionMenu_KeyDown(KeyboardEventArgs args)
		{
			int totalEntries = CompetitionMax - CompetitionMin + 1;
			int maxOffset = Math.Max(0, totalEntries - ExtendedCompetitionMenuPageSize);

			switch (args.Key)
			{
				case Key.PageDown:
					_extendedCompetitionMenuOffset = 0;
					CloseMenus();
					MenuExtendedCompetition(selectFirstNumberItem: true);
					return true;
				case Key.PageUp:
					_extendedCompetitionMenuOffset = maxOffset;
					CloseMenus();
					MenuExtendedCompetition(selectFirstNumberItem: true);
					return true;
				default:
					return false;
			}
		}

		private void MenuExtendedCompetition(bool selectFirstNumberItem = false)
		{
			Menu menu = CreateMenu(Translate("More Civilizations..."), SetCompetition);
			menu.Hints = [Translate("Esc: Back")];
			menu.OnCustomKeyDown = ExtendedCompetitionMenu_KeyDown;
			int totalEntries = CompetitionMax - CompetitionMin + 1;
			int maxOffset = Math.Max(0, totalEntries - ExtendedCompetitionMenuPageSize);
			if (_extendedCompetitionMenuOffset > maxOffset)
			{
				_extendedCompetitionMenuOffset = maxOffset;
			}

			bool needsPaging = totalEntries > ExtendedCompetitionMenuPageSize;
			if (needsPaging)
			{
				menu.Items.Add(Translate("Back..."), CompetitionScrollLessValue).OnSelect(SetCompetition);
			}

			int startValue = CompetitionMin + _extendedCompetitionMenuOffset;
			int endValue = Math.Min(CompetitionMax, startValue + ExtendedCompetitionMenuPageSize - 1);
			for (int value = startValue; value <= endValue; value++)
			{
				menu.Items.Add(TranslateFormatted("{0} Civilizations", OpponentsToCompetition(value)), value).OnSelect(SetCompetition);
			}

			if (needsPaging && endValue < CompetitionMax)
			{
				menu.Items.Add(Translate("More..."), CompetitionScrollMoreValue).OnSelect(SetCompetition);
			}

			menu.Cancel += ExtendedCompetitionMenu_Cancel;
			if (selectFirstNumberItem)
			{
				menu.ActiveItem = needsPaging ? 1 : 0;
			}
			AddMenu(menu);
		}

		private bool ExtendedCompetitionMenu_KeyDown(KeyboardEventArgs args)
		{
			int totalEntries = CompetitionMax - CompetitionMin + 1;
			int maxOffset = Math.Max(0, totalEntries - ExtendedCompetitionMenuPageSize);

			switch (args.Key)
			{
				case Key.PageUp:
					if (_extendedCompetitionMenuOffset == 0)
					{
						CloseMenus();
						MenuCompetition();
						return true;
					}

					_extendedCompetitionMenuOffset = Math.Max(0, _extendedCompetitionMenuOffset - ExtendedCompetitionMenuPageSize);
					CloseMenus();
					MenuExtendedCompetition(selectFirstNumberItem: true);
					return true;
				case Key.PageDown:
					if (_extendedCompetitionMenuOffset >= maxOffset)
					{
						CloseMenus();
						MenuCompetition();
						return true;
					}

					_extendedCompetitionMenuOffset = Math.Min(maxOffset, _extendedCompetitionMenuOffset + ExtendedCompetitionMenuPageSize);
					CloseMenus();
					MenuExtendedCompetition(selectFirstNumberItem: true);
					return true;
				default:
					return false;
			}
		}
		
		private void MenuTribe()
		{
			Menu menu = CreateMenu(Translate("Pick your tribe..."), SetTribe, TribeMenuYOffset);
			menu.Hints = [
				Translate("Esc: Back"),
				Translate("R: Rename")
			];
			menu.OnCustomKeyDown = args => TribeMenu_KeyDown(menu, args);

			for (int i = 0; i < _tribesAvailable.Length; i++)
			{
				ICivilization civilization = _tribesAvailable[i];
				if (IsExtendedCivilization(civilization))
				{
					continue;
				}

				menu.Items.Add(civilization.Name, i).OnSelect(SetTribe);
			}

			if (_tribesAvailable.Any(IsExtendedCivilization))
			{
				menu.Items.Add(Translate("New Civilizations..."), TribeOpenExtendedMenuValue).OnSelect(SetTribe);
			}

			menu.Cancel += SetTribe_Cancel;
			AddMenu(menu);
		}

		private void MenuExtendedTribe()
		{
			Menu menu = CreateMenu(Translate("Pick new civilization..."), SetTribe, TribeMenuYOffset);
			menu.Hints = [
				Translate("Esc: Back"),
				Translate("R: Rename")
			];
			menu.OnCustomKeyDown = args => TribeMenu_KeyDown(menu, args);

			(int Index, ICivilization Civilization)[] extendedCivilizations =
				[.. _tribesAvailable
					.Select((civ, index) => (Index: index, Civilization: civ))
					.Where(x => IsExtendedCivilization(x.Civilization))];

			if (extendedCivilizations.Length == 0)
			{
				MenuTribe();
				return;
			}

			menu.Items.Add(Translate("Original Civilizations..."), TribeBackToClassicValue).OnSelect(SetTribe);

			int maxOffset = Math.Max(0, extendedCivilizations.Length - ExtendedTribeMenuPageSize);
			if (_extendedTribeMenuOffset > maxOffset)
			{
				_extendedTribeMenuOffset = maxOffset;
			}

			int startIndex = _extendedTribeMenuOffset;
			int endExclusive = Math.Min(extendedCivilizations.Length, startIndex + ExtendedTribeMenuPageSize);

			if (startIndex > 0)
			{
				menu.Items.Add(Translate("Previous civilizations..."), TribeScrollPreviousValue).OnSelect(SetTribe);
			}

			for (int i = startIndex; i < endExclusive; i++)
			{
				var entry = extendedCivilizations[i];
				menu.Items.Add(entry.Civilization.Name, entry.Index).OnSelect(SetTribe);
			}

			if (endExclusive < extendedCivilizations.Length)
			{
				menu.Items.Add(Translate("Next civilizations..."), TribeScrollNextValue).OnSelect(SetTribe);
			}

			menu.Cancel += ExtendedTribeMenu_Cancel;
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
			_difficulty = args.Value;
			CloseMenus();
			Log("Difficulty: {0}", _menuItemsDifficulty[_difficulty]);
		}
		
		private void SetCompetition(object sender, MenuItemEventArgs<int> args)
		{
			if (args.Value == CompetitionOpenExtendedValue)
			{
				_extendedCompetitionMenuOffset = 0;
				CloseMenus();
				MenuExtendedCompetition();
				return;
			}

			if (args.Value == CompetitionScrollLessValue)
			{
				CloseMenus();
				MenuCompetition();
				return;
			}

			if (args.Value == CompetitionScrollMoreValue)
			{
				int totalEntries = CompetitionMax - CompetitionMin + 1;
				int maxOffset = Math.Max(0, totalEntries - ExtendedCompetitionMenuPageSize);
				_extendedCompetitionMenuOffset = Math.Min(maxOffset, _extendedCompetitionMenuOffset + ExtendedCompetitionMenuPageSize);
				CloseMenus();
				MenuExtendedCompetition(selectFirstNumberItem: true);
				return;
			}

			_competition = args.Value;
			_extendedTribeMenuOffset = 0;
			CloseMenus();
			Log("Competition: {0} Civilizations", OpponentsToCompetition(_competition));

			// For classic-sized games (up to 6 opponents), offer classic civilizations only.
			// For larger games, offer the full civilization list.
			IEnumerable<ICivilization> selectable = Common.Civilizations
				.Where(c => c.PreferredPlayerNumber > 0);

			if (_competition <= ClassicMaxOpponents)
			{
				selectable = selectable.Where(c => c.Id <= ClassicCivilizationMaxId);
			}

			_tribesAvailable = [.. selectable
				.OrderBy(c => IsExtendedCivilization(c))
				.ThenBy(c => c.Id)];
			_menuItemsTribes = [.. _tribesAvailable.Select(c => c.Name)];
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

		private void SetCompetition_Cancel(object? sender, EventArgs args)
		{
			CloseMenus();
			MenuDifficulty();
		}

		private void ExtendedCompetitionMenu_Cancel(object? sender, EventArgs args)
		{
			CloseMenus();
			MenuCompetition();
		}

		private static bool IsExtendedCivilization(ICivilization civilization)
		{
			return civilization.Id > ClassicCivilizationMaxId;
		}

		private bool TribeMenu_KeyDown(Menu menu, KeyboardEventArgs args)
		{
			if (args.KeyChar != 'r' && args.KeyChar != 'R')
			{
				return false;
			}

			StartCustomTribeNameInput(menu);
			return true;
		}

		private void StartCustomTribeNameInput(Menu menu)
		{
			int selectedTribeIndex = menu.SelectedItem.Value;
			if (selectedTribeIndex < 0 || selectedTribeIndex >= _tribesAvailable.Length)
			{
				selectedTribeIndex = RandomService.NextInt(_tribesAvailable.Length);
			}

			_tribe = selectedTribeIndex;
			CloseMenus();

			ICivilization civ = _tribesAvailable[_tribe];
			Input input = new(Palette, civ.NamePlural, 6, 5, 11, OffsetX + 168, OffsetY + 105, 109, 10, 11);
			input.Accept += TribeName_Accept;
			input.Cancel += TribeName_Accept;
			Common.AddScreen(input);
		}
		
		private void SetTribe(object sender, MenuItemEventArgs<int> args)
		{
			if (args.Value == TribeOpenExtendedMenuValue)
			{
				CloseMenus();
				MenuExtendedTribe();
				return;
			}

			if (args.Value == TribeBackToClassicValue)
			{
				CloseMenus();
				MenuTribe();
				return;
			}

			if (args.Value == TribeScrollPreviousValue)
			{
				_extendedTribeMenuOffset = Math.Max(0, _extendedTribeMenuOffset - ExtendedTribeMenuPageSize);
				CloseMenus();
				MenuExtendedTribe();
				return;
			}

			if (args.Value == TribeScrollNextValue)
			{
				int totalExtended = _tribesAvailable.Count(IsExtendedCivilization);
				int maxOffset = Math.Max(0, totalExtended - ExtendedTribeMenuPageSize);
				_extendedTribeMenuOffset = Math.Min(maxOffset, _extendedTribeMenuOffset + ExtendedTribeMenuPageSize);
				CloseMenus();
				MenuExtendedTribe();
				return;
			}

			_tribe = args.Value;

			ICivilization civ = _tribesAvailable[_tribe];
			_tribeName = civ.Name;
			_tribeNamePlural = civ.NamePlural;
			CloseMenus();
			Log("Tribe: {0}", _menuItemsTribes[_tribe]);
		}
		
		private void SetTribe_Cancel(object? sender, EventArgs args)
		{
			CloseMenus();
			MenuCompetition();
		}

		private void ExtendedTribeMenu_Cancel(object? sender, EventArgs args)
		{
			CloseMenus();
			MenuTribe();
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
			else if (_competition == -1) MenuCompetition();
			else if (_tribe == -1) MenuTribe();
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
						Game.CreateGame(_difficulty, OpponentsToCompetition(_competition), civ, _leaderName, _tribeName, _tribeNamePlural, replaceExisting: true);
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
				int pictureStack = (_competition <= 0) ? 1 : Math.Min(OpponentsToCompetition(_competition), 7);
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

		private string[] BuildDifficultyMenuItems()
		{
			string easiest = TranslateFormatted("{0} (easiest)", Common.DifficultyName(0));
			string toughestEnabled = TranslateFormatted("{0} (toughest)", Common.DifficultyName(5));
			string toughestDefault = TranslateFormatted("{0} (toughest)", Common.DifficultyName(4));

			if (Settings.Instance.DeityEnabled)
			{
				return [
					easiest,
					Common.DifficultyName(1),
					Common.DifficultyName(2),
					Common.DifficultyName(3),
					Common.DifficultyName(4),
					toughestEnabled
				];
			}

			return [
				easiest,
				Common.DifficultyName(1),
				Common.DifficultyName(2),
				Common.DifficultyName(3),
				toughestDefault
			];
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

			_menuItemsDifficulty = BuildDifficultyMenuItems();
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