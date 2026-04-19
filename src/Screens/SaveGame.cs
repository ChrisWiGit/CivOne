// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Persistence;
using CivOne.Persistence.Factories;
using CivOne.Persistence.Model;
using CivOne.Services;
using CivOne.UserInterface;
using System;
using System.IO;
using System.Linq;

namespace CivOne.Screens
{
	[Modal]
	internal class SaveGame : BaseScreen
	{
		internal static int SelectedGame = -1;

		private static ISaveGamePathProvider PathProvider =>
			new SaveGamePathProvider(RuntimeHandler.Runtime, Settings.Instance);

		private static IAtomicFileReplacementService AtomicFileReplacementService =>
			new AtomicFileReplacementService();

		private char _driveLetter = 'C';
		private readonly int _border = Common.Random.Next(2);
		private int _gameId;

		internal static string SaveFileName = "";
		private bool _update = true;
		private bool _saving = false;
		private Menu _menu;

		private static string BuildDialogInitialFileName(string fileName)
		{
			var provider = PathProvider;
			string initial = provider.GetInitialSaveFilePath();
			if (!string.IsNullOrWhiteSpace(fileName))
				return Path.Combine(Path.GetDirectoryName(initial) ?? string.Empty, Path.GetFileName(fileName));
			return initial;
		}

		private static void SetLastUsedSaveGameDialogPath(string fileName)
		{
			PathProvider.SetLastUsedSaveGamePath(fileName);
		}

		public override MouseCursor Cursor => (_menu == null ? MouseCursor.Pointer : MouseCursor.None);

		private void SaveFile(object sender, EventArgs args)
		{
			int item = (sender as MenuItem<int>).Value;
			_gameId = item;
			SelectedGame = item;
			_saving = true;
			_update = true;

			SaveGameFile file = SaveGameFile.GetSaveGames(_driveLetter).ToArray()[item];
			Game.Save(file.SveFile, file.MapFile);
		}

		private void SaveFileDialog(object sender, EventArgs args)
		{
			_menu.Close();
			_menu = null;
			SelectedGame = -1;
			_gameId = -1;

			string sveFile = RuntimeHandler.Runtime.FileChooser(
				true,
				"Save Game As...",
				BuildDialogInitialFileName(SaveFileName),
				"CivOne Save Game (*.cos)|*.cos"
			);
			if (string.IsNullOrEmpty(sveFile))
			{
				_update = true;
				return;
			}

			SaveFileName = Path.ChangeExtension(sveFile, ".cos");
			SetLastUsedSaveGameDialogPath(SaveFileName);

			Game.SaveMetaData.DisplayName = Game.SaveMetaDataService.BuildDisplayName(Game.Difficulty, Game.HumanPlayer, Game.GameTurn);

			GameStateHandler gameState = new();
			var mapperDependencies = YamlMapperDependenciesFactory
				.CreateDefault()
				.Create(Game);
			YamlSaveGameStateWriter writer = new(
				mapperDependencies.PlayerMapper,
				mapperDependencies.UnitMapper,
				mapperDependencies.MapMapper,
				mapperDependencies.GlobalWarmingMapper,
				mapperDependencies.Sanitizer);
			AtomicFileReplacementService.ReplaceFile(
				SaveFileName,
				stream => writer.Write(stream, gameState.Create(Game), Game.SaveMetaData));

			_saving = true;
			_update = true;
		}

		private void DrawDriveQuestion()
		{
			Bitmap.Clear();
			this.Clear(15);
			DrawBorder(_border);

			this.DrawText("Which drive contains your", 0, 5, 92, 72, TextAlign.Left)
				.DrawText("Save Game disk?", 0, 5, 104, 80, TextAlign.Left)
				.DrawText(string.Format("{0}:", _driveLetter), 0, 5, 146, 96, TextAlign.Left)
				.DrawText("Press drive letter and", 0, 5, 104, 112, TextAlign.Left)
				.DrawText("Return when disk is inserted", 0, 5, 80, 120, TextAlign.Left)
				.DrawText("Press Escape to cancel", 0, 5, 104, 128, TextAlign.Left);
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (_saving)
			{
				if (!_update) return false;
				_update = false;
				Bitmap.Clear();
				this.Clear(15);
				DrawBorder(_border);

				if (_menu != null)
				{
					this.AddLayer(_menu);
					_menu.Close();
					_menu = null;
				}

				DrawPanel(64, 86, 124, 41);

				if (_gameId >= 0)
				{
					this.DrawText($"{char.ToLower(_driveLetter)}:CIVIL{_gameId}.SVE", 0, 5, 75, 91);
				}
				else
				{
					this.DrawText(Path.GetFileName(SaveFileName), 0, 5, 75, 91);
				}

				this.DrawText($"{Common.DifficultyName(Game.Difficulty)} {Game.HumanPlayer.LeaderName}", 0, 5, 75, 99)
					.DrawText($"{Game.HumanPlayer.TribeNamePlural}/{Game.GameYear}", 0, 5, 75, 107)
					.DrawText("... save in progress.", 0, 5, 75, 115);

				this.DrawText("Game has been saved.", 0, 5, 75, 132)
					.DrawText("Press key to continue.", 0, 5, 75, 140);
				return true;
			}
			else if (_menu != null)
			{
				if (_menu.Update(gameTick))
				{
					this.Clear(15);
					DrawBorder(_border);
					this.AddLayer(_menu);
					return true;
				}
				return false;
			}
			else if (_update)
			{
				DrawDriveQuestion();
				_update = false;
				return true;
			}
			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_saving)
			{
				Destroy();
				return true;
			}

			char c = Char.ToUpper(args.KeyChar);
			if (args.Key == Key.Escape)
			{
				Log("Cancel");
				Destroy();
				return true;
			}
			else if (_menu != null)
			{
				return _menu.KeyDown(args);
			}
			else if (args.Key == Key.Enter)
			{
				if (_gameId >= 0)
				{
					SaveGameFile file = SaveGameFile.GetSaveGames(_driveLetter).ToArray()[_gameId];
					Game.Save(file.SveFile, file.MapFile);
					_saving = true;
					_update = true;
					return true;
				}

				_menu = new Menu(Palette)
				{
					Title = "Select Save File...",
					X = 51,
					Y = 38,
					MenuWidth = 217,
					TitleColour = 12,
					ActiveColour = 11,
					TextColour = 5,
					FontId = 0,
					IndentTitle = 2,
					RowHeight = 8
				};

				_menu.Items.Add("Save with file dialog...", -1).OnSelect(SaveFileDialog);
				int i = 0;
				foreach (SaveGameFile file in SaveGameFile.GetSaveGames(_driveLetter).Take(4))
				{
					_menu.Items.Add(file.Name, i++).OnSelect(SaveFile);
				}

				_menu.ActiveItem = SelectedGame;
			}
			else if (c >= 'A' && c <= 'Z')
			{
				_driveLetter = c;
				_update = true;
				return true;
			}
			return false;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_menu != null)
				return _menu.MouseDown(args);
			if (_saving)
				Destroy();
			return false;
		}

		public override bool MouseUp(ScreenEventArgs args)
		{
			if (_menu != null)
				return _menu.MouseUp(args);
			return false;
		}

		public override bool MouseDrag(ScreenEventArgs args)
		{
			if (_menu != null)
				return _menu.MouseDrag(args);
			return false;
		}

		public SaveGame() : this(-1)
		{
		}

		public SaveGame(int gameId)
		{
			Palette = Resources["SP257"].Palette;
			_gameId = gameId;
		}
	}
}