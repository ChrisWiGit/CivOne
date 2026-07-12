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
using CivOne.Agents;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.IO;
using CivOne.Persistence.Game;
using CivOne.UserInterface;

namespace CivOne.Screens
{
	internal sealed class NewGameAiSelectionResult
	{
		public required int Difficulty { get; init; }

		public required int Competition { get; init; }

		public required NewGamePlayerSelection Human { get; init; }

		public required IReadOnlyList<NewGamePlayerSelection> Opponents { get; init; }
	}

	internal sealed class NewGameAiSelectionResultEventArgs(NewGameAiSelectionResult result) : EventArgs
	{
		public NewGameAiSelectionResult Result { get; } = result;
	}

	internal sealed class NewGamePlayerSelection
	{
		public required bool IsHuman { get; init; }

		public required string Name { get; set; }

		public required ICivilization Civilization { get; set; }

		public required Guid? AiId { get; set; }

		public required string AiName { get; set; }

		public required int DifficultyIndex { get; set; }

		public required int ColorSlot { get; set; }

		public required int TeamSlot { get; set; }
	}

	internal interface INewGameAiCatalogService
	{
		IReadOnlyList<AiCatalogEntry> GetAiEntries();

		IReadOnlyList<string> GetDifficultyLabels();
	}

	internal sealed class AiCatalogEntry(Guid id, string name, string provider)
	{
		public Guid Id { get; } = id;

		public string Name { get; } = name ?? string.Empty;

		public string Provider { get; } = provider ?? string.Empty;
	}

	internal sealed class DefaultNewGameAiCatalogService : INewGameAiCatalogService
	{
		public IReadOnlyList<AiCatalogEntry> GetAiEntries()
		{
			return
			[
				.. AgentLoaderEntry
					.GetAvailableDefinitions()
					.Where(definition => definition.Id != AiDefinitionIds.BarbarianDisabled)
					.Select(definition => new AiCatalogEntry(definition.Id, definition.DisplayName, definition.Provider))
			];
		}

		private static readonly string[] BaseDifficultyLabels =
		[
			"Chieftain",
			"Warlord",
			"Prince",
			"King",
			"Emperor"
		];

		public IReadOnlyList<string> GetDifficultyLabels()
		{
			return Settings.Instance.DeityEnabled
				? [.. BaseDifficultyLabels, "Deity"]
				: BaseDifficultyLabels;
		}
	}

	internal static class NewGameAiCatalogServiceFactory
	{
		public static INewGameAiCatalogService Create()
		{
			return new DefaultNewGameAiCatalogService();
		}
	}

	[Break, ScreenResizeable]
	internal sealed class NewGameAiSelection : BaseScreen
	{
		private enum SelectionMode
		{
			NewGame,
			InGameEdit
		}

		private const int MaxOpponents = 6;
		private const int BarbarianPlayerId = 0;
		private const int MenuFont = 6;
		private const int RowFont = 0;
		private const int FooterFont = 6;
		private const int RowHeight = 10;
		private const int ButtonHeight = 11;

		private readonly INewGameAiCatalogService _catalogService;
		private readonly IReadOnlyList<AiCatalogEntry> _aiEntries;
		private readonly IReadOnlyList<string> _difficultyLabels;
		private readonly SelectionMode _mode;
		private readonly List<Player> _runtimeOpponentPlayers = [];
		private Player? _runtimeHumanPlayer;
		private readonly ICivilization[] _availableCivilizations;
		private readonly List<NewGamePlayerSelection> _opponents = [];
		private readonly List<(int X, int Y, int W, int H, Action Click)> _clickables = [];
		private InputDialogDelegate? _nameInputDialog;
		private const int MaxSlots = 8;

		private bool _update = true;
		private int _difficulty;
		private NewGamePlayerSelection _human;
		private Guid _lastSelectedAiId;
		private int _lastSelectedDifficultyIndex;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private bool IsValidDifficultyIndex(int difficultyIndex)
		{
			return difficultyIndex >= 0 && difficultyIndex < _difficultyLabels.Count;
		}

		private int GetDefaultDifficultyIndex()
		{
			if (_difficultyLabels.Count == 0)
			{
				return -1;
			}

			return Math.Clamp(_difficulty, 0, _difficultyLabels.Count - 1);
		}

		private string GetDifficultyLabel(int difficultyIndex)
		{
			return IsValidDifficultyIndex(difficultyIndex)
				? _difficultyLabels[difficultyIndex]
				: Translate("Chieftain");
		}

		public event EventHandler<NewGameAiSelectionResultEventArgs>? StartRequested;

		public NewGameAiSelection(
			int initialDifficulty,
			INewGameAiCatalogService? catalogService = null,
			bool inGameEditMode = false) : base(MouseCursor.Pointer)
		{
			OnResize += Resize;
			_mode = inGameEditMode ? SelectionMode.InGameEdit : SelectionMode.NewGame;
			_catalogService = catalogService ?? NewGameAiCatalogServiceFactory.Create();
			_aiEntries = _catalogService.GetAiEntries();
			_difficultyLabels = [.. _catalogService.GetDifficultyLabels().Select(Translate)];
			_availableCivilizations = [.. Common.Civilizations.Where(c => c.PreferredPlayerNumber > 0)];

			using Palette defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;
			_difficulty = Math.Clamp(initialDifficulty, 0, Math.Max(0, _difficultyLabels.Count - 1));

			ICivilization? defaultHumanCiv = _availableCivilizations.FirstOrDefault(c => c.PreferredPlayerNumber == 1)
				?? _availableCivilizations.FirstOrDefault();

			if (defaultHumanCiv is null)
			{
				throw new InvalidOperationException("No available civilizations found for human player.");
			}

			if (_mode == SelectionMode.InGameEdit)
			{
				_human = CreateHumanRow(defaultHumanCiv);
				InitializeRememberedDefaults();
				InitializeFromCurrentGame();
			}
			else
			{
				_human = CreateHumanRow(defaultHumanCiv);
				InitializeRememberedDefaults();
				AddBarbarianOpponentForNewGame();
				AddOpponent();
			}
		}

		public static NewGameAiSelection CreateInGameEditor(INewGameAiCatalogService? catalogService = null)
		{
			int initialDifficulty = Game.Started ? Game.Instance.Difficulty : 0;
			return new NewGameAiSelection(initialDifficulty, catalogService, inGameEditMode: true);
		}

		private void InitializeFromCurrentGame()
		{
			if (!Game.Started)
			{
				throw new InvalidOperationException("In-game AI selection editor requires an active game.");
			}

			Game game = Game.Instance;
			_runtimeHumanPlayer = game.HumanPlayer;

			_human = new NewGamePlayerSelection
			{
				IsHuman = true,
				Name = game.HumanPlayer.TribeName,
				Civilization = game.HumanPlayer.Civilization,
				AiId = null,
				AiName = string.Empty,
				DifficultyIndex = -1,
				ColorSlot = NormalizeRingValue(game.HumanPlayer.Civilization.PreferredPlayerNumber, MaxSlots),
				TeamSlot = 1
			};

			_opponents.Clear();
			_runtimeOpponentPlayers.Clear();

			Player? barbarianPlayer = game.GetPlayer(BarbarianPlayerId);
			if (barbarianPlayer is not null && !barbarianPlayer.IsHuman)
			{
				AddRuntimeOpponentRow(barbarianPlayer, runtimeIndex: 0);
			}

			Player[] opponents =
			[
				.. game.Players.Where(player => !player.IsHuman && game.PlayerNumber(player) != BarbarianPlayerId && player.Civilization.Id != 0)
			];
			for (int i = 0; i < opponents.Length; i++)
			{
				AddRuntimeOpponentRow(opponents[i], _runtimeOpponentPlayers.Count);
			}

			if (_opponents.Count > 0)
			{
				_lastSelectedAiId = _opponents[0].AiId ?? AiDefinitionIds.Legacy;
				_lastSelectedDifficultyIndex = _opponents[0].DifficultyIndex;
			}
		}

		private void AddRuntimeOpponentRow(Player player, int runtimeIndex)
		{
			(Guid aiId, string aiName) = ResolveAiForPlayer(player);
			int difficultyIndex = GetDifficultyIndexForPlayer(player);

			_runtimeOpponentPlayers.Add(player);
			_opponents.Add(new NewGamePlayerSelection
			{
				IsHuman = false,
				Name = player.TribeName,
				Civilization = player.Civilization,
				AiId = aiId,
				AiName = aiName,
				DifficultyIndex = difficultyIndex,
				ColorSlot = NormalizeRingValue(player.Civilization.PreferredPlayerNumber, MaxSlots),
				TeamSlot = NormalizeRingValue(runtimeIndex + 2, MaxSlots)
			});
		}

		private (Guid Id, string Name) ResolveAiForPlayer(Player player)
		{
			Guid configuredId = player.AiId.GetValueOrDefault();
			if (configuredId == Guid.Empty)
			{
				configuredId = Game.Instance.PlayerNumber(player) == BarbarianPlayerId
					? AiDefinitionIds.BarbarianBridge
					: AiDefinitionIds.Legacy;
			}

			if (configuredId == AiDefinitionIds.BarbarianDisabled)
			{
				return (AiDefinitionIds.BarbarianDisabled, Translate("Disabled"));
			}

			AiCatalogEntry? configuredAi = _aiEntries.FirstOrDefault(entry => entry.Id == configuredId);
			if (configuredAi is not null)
			{
				return (configuredAi.Id, configuredAi.Name);
			}

			(Guid rememberedAiId, string rememberedAiName) = GetRememberedAiOrDefault();
			return (rememberedAiId, rememberedAiName);
		}

		private int GetDifficultyIndexForPlayer(Player player)
		{
			if (_difficultyLabels.Count == 0)
			{
				return -1;
			}

			return Math.Clamp(player.Handicap, 0, _difficultyLabels.Count - 1);
		}

		private void InitializeRememberedDefaults()
		{
			AiCatalogEntry defaultAi = GetRegularDefaultAiOrFallback();
			int defaultDifficultyIndex = GetDefaultDifficultyIndex();

			_lastSelectedAiId = defaultAi.Id;
			_lastSelectedDifficultyIndex = defaultDifficultyIndex;
		}

		private (Guid Id, string Name) GetRememberedAiOrDefault()
		{
			AiCatalogEntry? remembered = _aiEntries.FirstOrDefault(entry => entry.Id == _lastSelectedAiId);
			if (remembered is not null)
			{
				return (remembered.Id, remembered.Name);
			}

			AiCatalogEntry fallback = _aiEntries.Count > 0
				? _aiEntries[0]
				: new AiCatalogEntry(AiDefinitionIds.Legacy, "Legacy AI", "CivOne");

			return (fallback.Id, fallback.Name);
		}

		private static bool IsBarbarianCivilization(ICivilization civilization)
		{
			ArgumentNullException.ThrowIfNull(civilization);
			return civilization.PreferredPlayerNumber == BarbarianPlayerId;
		}

		private static bool IsBarbarianRow(NewGamePlayerSelection row)
		{
			ArgumentNullException.ThrowIfNull(row);
			return IsBarbarianCivilization(row.Civilization);
		}

		private bool HasBarbarianRow()
		{
			return _opponents.Any(IsBarbarianRow);
		}

		private int GetRegularOpponentCount()
		{
			return _opponents.Count(opponent => !IsBarbarianRow(opponent));
		}

		private void AddBarbarianOpponentForNewGame()
		{
			if (_mode != SelectionMode.NewGame || HasBarbarianRow())
			{
				return;
			}

			ICivilization? barbarianCivilization = Common.Civilizations
				.FirstOrDefault(civilization => IsBarbarianCivilization(civilization));
			if (barbarianCivilization is null)
			{
				return;
			}

			AiCatalogEntry defaultBarbarianAi = _aiEntries.FirstOrDefault(entry => entry.Id == AiDefinitionIds.BarbarianBridge)
				?? new AiCatalogEntry(AiDefinitionIds.BarbarianBridge, "Barbarian Bridge", "CivOne");
			int defaultDifficultyIndex = GetRememberedDifficultyOrDefault();

			_opponents.Add(new NewGamePlayerSelection
			{
				IsHuman = false,
				Name = barbarianCivilization.Name,
				Civilization = barbarianCivilization,
				AiId = defaultBarbarianAi.Id,
				AiName = defaultBarbarianAi.Name,
				DifficultyIndex = defaultDifficultyIndex,
				ColorSlot = NormalizeRingValue(barbarianCivilization.PreferredPlayerNumber, MaxSlots),
				TeamSlot = GetNextFreeTeamSlot()
			});
		}

		private AiCatalogEntry GetRegularDefaultAiOrFallback()
		{
			AiCatalogEntry? legacy = _aiEntries.FirstOrDefault(entry => entry.Id == AiDefinitionIds.Legacy);
			if (legacy is not null)
			{
				return legacy;
			}

			return _aiEntries.Count > 0
				? _aiEntries[0]
				: new AiCatalogEntry(AiDefinitionIds.Legacy, "Legacy AI", "CivOne");
		}

		private int GetRememberedDifficultyOrDefault()
		{
			if (IsValidDifficultyIndex(_lastSelectedDifficultyIndex))
			{
				return _lastSelectedDifficultyIndex;
			}

			return GetDefaultDifficultyIndex();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update && _nameInputDialog?.Active != true)
			{
				return false;
			}

			_update = false;
			DrawScreen(gameTick);
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			ArgumentNullException.ThrowIfNull(args);
			if (_nameInputDialog?.Active == true)
			{
				bool handled = _nameInputDialog.KeyDown(args);
				if (handled)
				{
					RequestUpdate();
				}
				return handled;
			}
			if (HasMenu)
			{
				return false;
			}
			if (args.Key == Key.Escape)
			{
				Destroy();
				return true;
			}
			if (args.Key == Key.Character)
			{
				char c = char.ToLowerInvariant(args.KeyChar);
				if (c >= '1' && c <= '8')
				{
					int index = c - '1';
					if (index < _opponents.Count)
					{
						OpenOpponentMenu(index);
						return true;
					}
				}
				if (c == 'h')
				{
					EditHuman();
					return true;
				}
				if (c == 's' && _opponents.Count > 0)
				{
					if (_mode == SelectionMode.InGameEdit)
					{
						ApplyInGameChangesAndClose();
					}
					else
					{
						StartGame();
					}
					return true;
				}
				if (_mode == SelectionMode.NewGame && c == 'a' && GetRegularOpponentCount() < MaxOpponents)
				{
					AddOpponent();
					RequestUpdate();
					return true;
				}
			}
			return false;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			ArgumentNullException.ThrowIfNull(args);
			if (_nameInputDialog?.Active == true)
			{
				bool handled = _nameInputDialog.MouseDown(args);
				if (handled)
				{
					RequestUpdate();
				}
				return handled;
			}
			if (HasMenu)
			{
				return false;
			}

			foreach ((int x, int y, int w, int h, Action click) in _clickables)
			{
				if (args.X < x || args.Y < y || args.X >= x + w || args.Y >= y + h)
				{
					continue;
				}
				click();
				return true;
			}
			return false;
		}

		private void DrawScreen(uint gameTick)
		{
			_clickables.Clear();
			Bitmap = new Bytemap(Width, Height);
			this.Tile(Pattern.PanelGrey);
			DrawBorder(0);

			int innerLeft = BorderTileSize + 1;
			int innerRight = Width - BorderTileSize - 1;
			int innerTop = BorderTileSize + 1;
			int innerBottom = Height - BorderTileSize - 1;

			ComputeColumnLayout(innerLeft + 4, innerRight - 4);

			int titleCenterX = (innerLeft + innerRight) / 2;
			this.DrawText(Translate("Civilization Selection"), 3, 5, titleCenterX, innerTop + 2, TextAlign.Center);

			int headerY = innerTop + 20;
			DrawColumnHeaders(headerY);

			int y = headerY + RowHeight;
			DrawPlayerRow(_human, y, humanRow: true, opponentIndex: -1);
			y += RowHeight;

			for (int i = 0; i < _opponents.Count; i++)
			{
				DrawPlayerRow(_opponents[i], y, humanRow: false, opponentIndex: i);
				y += RowHeight;
			}

			if (_mode == SelectionMode.NewGame && GetRegularOpponentCount() < MaxOpponents)
			{
				DrawClickableButton(Translate("+ Add opponent (A)"), innerLeft + 4, y + 2, () =>
				{
					AddOpponent();
					RequestUpdate();
				}, enabled: HasFreeCivilization());
			}

			int buttonY = innerBottom - ButtonHeight;
			int startRight = innerRight;
			int backRight;
			if (_mode == SelectionMode.InGameEdit)
			{
				backRight = DrawClickableButtonRightAligned(
					Translate("Apply (S)"), startRight, buttonY, () => ApplyInGameChangesAndClose(),
					enabled: _opponents.Count > 0);
			}
			else
			{
				backRight = DrawClickableButtonRightAligned(
					Translate("Start Game (S)"), startRight, buttonY, () => StartGame(),
					enabled: _opponents.Count > 0);
			}
			backRight -= 4;
			DrawClickableButtonRightAligned(Translate("Back"), backRight, buttonY, Destroy);

			int footerFontHeight = Resources.GetFontHeight(FooterFont);
			int footerY = innerBottom - footerFontHeight;
			
			this.DrawText(Translate("Colors and Teams are currently not implemented."), FooterFont, 8, innerLeft + 4, footerY - footerFontHeight * 2);
			
			int displayOpponentLimit = (_mode == SelectionMode.InGameEdit || HasBarbarianRow()) ? MaxOpponents + 1 : MaxOpponents;
			string[] helpText = _mode == SelectionMode.InGameEdit
				? TranslateFormattedArray("Opponents: {0}/{1} - Keys 1..8 edit\nH = human, S = apply, ESC = back.", _opponents.Count, displayOpponentLimit)
				: TranslateFormattedArray("Opponents: {0}/{1} - Keys 1..7 edit\nH = human, ESC = back.", _opponents.Count, displayOpponentLimit);
			this.DrawText(helpText[0], FooterFont, 5, innerLeft + 4, footerY - footerFontHeight);
			this.DrawText(helpText[1], FooterFont, 5, innerLeft + 4, footerY);

			_nameInputDialog?.Draw(this, gameTick, Width, Height);
		}

		private int _colNo, _colType, _colName, _colCiv, _colAi, _colDiff, _colColor, _colTeam, _colRightEdge;

		private static readonly int[] ColumnPercentages = [6, 10, 18, 24, 16, 14, 6, 6];

		private void ComputeColumnLayout(int left, int right)
		{
			int usable = Math.Max(0, right - left);
			int[] widths = new int[ColumnPercentages.Length];
			int assigned = 0;

			for (int i = 0; i < ColumnPercentages.Length; i++)
			{
				widths[i] = usable * ColumnPercentages[i] / 100;
				assigned += widths[i];
			}

			int remainder = usable - assigned;
			for (int i = 0; i < remainder; i++)
			{
				widths[i % widths.Length]++;
			}

			_colNo = left;
			_colType = _colNo + widths[0];
			_colName = _colType + widths[1];
			_colCiv = _colName + widths[2];
			_colAi = _colCiv + widths[3];
			_colDiff = _colAi + widths[4];
			_colColor = _colDiff + widths[5];
			_colTeam = _colColor + widths[6];
			_colRightEdge = _colTeam + widths[7];
		}

		private void DrawColumnHeaders(int y)
		{
			const byte c = 15;
			this.DrawText(Translate("Num."), RowFont, c, _colNo, y);
			this.DrawText(Translate("Type"), RowFont, c, _colType, y);
			this.DrawText(Translate("Name"), RowFont, c, _colName, y);
			this.DrawText(Translate("Civilization"), RowFont, c, _colCiv, y);
			this.DrawText(Translate("AI"), RowFont, c, _colAi, y);
			this.DrawText(Translate("Difficulty"), RowFont, c, _colDiff, y);
			this.DrawText(Translate("Col."), RowFont, c, _colColor, y);
			this.DrawText(Translate("Team"), RowFont, c, _colTeam, y);
		}

		private void DrawPlayerRow(NewGamePlayerSelection row, int y, bool humanRow, int opponentIndex)
		{
			string keyHint = humanRow ? "H" : (opponentIndex + 1).ToString(CultureInfo.InvariantCulture);
			string typeLabel = humanRow ? Translate("Human") : Translate("AI");
			string aiLabel = humanRow ? "-" : row.AiName;
			string difficultyLabel = humanRow ? "-" : GetDifficultyLabel(row.DifficultyIndex);

			const byte rowColour = 1;
			this.DrawText(keyHint, RowFont, 11, _colNo, y);
			this.DrawText(TruncateText(typeLabel, _colName - _colType - 2), RowFont, rowColour, _colType, y);
			this.DrawText(TruncateText(row.Name, _colCiv - _colName - 2), RowFont, rowColour, _colName, y);
			this.DrawText(TruncateText(row.Civilization.Name, _colAi - _colCiv - 2), RowFont, rowColour, _colCiv, y);
			this.DrawText(TruncateText(aiLabel, _colDiff - _colAi - 2), RowFont, rowColour, _colAi, y);
			this.DrawText(TruncateText(difficultyLabel, _colColor - _colDiff - 2), RowFont, rowColour, _colDiff, y);
			this.DrawText(row.ColorSlot.ToString(CultureInfo.InvariantCulture), RowFont, rowColour, _colColor, y);
			this.DrawText(row.TeamSlot.ToString(CultureInfo.InvariantCulture), RowFont, rowColour, _colTeam, y);

			int rowLeft = _colNo - 2;
			int rowWidth = _colRightEdge - rowLeft;
			if (humanRow)
			{
				_clickables.Add((rowLeft, y - 1, rowWidth, 9, () => EditHuman()));
			}
			else
			{
				int index = opponentIndex;
				_clickables.Add((rowLeft, y - 1, rowWidth, 9, () => OpenOpponentMenu(index)));
			}
		}

		private static string TruncateText(string text, int maxPixelWidth)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}
			if (Resources.GetTextSize(RowFont, text).Width <= maxPixelWidth)
			{
				return text;
			}
			string result = text;
			while (result.Length > 1 && Resources.GetTextSize(RowFont, result + "...").Width > maxPixelWidth)
			{
				result = result[..^1];
			}
			return result + "...";
		}

		private void DrawClickableButton(string text, int x, int y, Action onClick, int minWidth = 0, bool enabled = true)
		{
			int textWidth = Resources.GetTextSize(RowFont, text).Width;
			int width = Math.Max(minWidth, textWidth + 8);
			byte colour = enabled ? (byte)9 : (byte)5;
			byte colourDark = enabled ? (byte)1 : (byte)8;
			DrawButton(text, RowFont, colour, colourDark, x, y, width, ButtonHeight);
			if (enabled)
			{
				_clickables.Add((x, y, width, ButtonHeight, onClick));
			}
		}

		private int DrawClickableButtonRightAligned(string text, int rightX, int y, Action onClick, bool enabled = true)
		{
			int textWidth = Resources.GetTextSize(RowFont, text).Width;
			int width = textWidth + 8;
			int x = rightX - width;
			byte colour = enabled ? (byte)9 : (byte)5;
			byte colourDark = enabled ? (byte)1 : (byte)8;
			DrawButton(text, RowFont, colour, colourDark, x, y, width, ButtonHeight);
			if (enabled)
			{
				_clickables.Add((x, y, width, ButtonHeight, onClick));
			}
			return x;
		}

		private void EditHuman()
		{
			CloseMenus();
			Menu<int> menu = CreatePopupMenu(Translate("Edit human player"), OffsetX + 80, OffsetY + 34);
			menu.Items.Add(Translate("Change name"), 1).OnSelect((s, a) => OpenNameInput(_human));
			if (_mode == SelectionMode.NewGame)
			{
				menu.Items.Add(Translate("Change civilization"), 2).OnSelect((s, a) => OpenCivilizationMenu(_human));
			}
			else
			{
				menu.Items.Add(Translate("Change civilization"), 2).Disable();
			}
			menu.Items.Add(Translate("Back"), 3).OnSelect((s, a) =>
			{
				CloseMenus();
				RequestUpdate();
			});
			AutoSizeMenuWidth(menu);
			AddMenu(menu);
		}

		private void OpenOpponentMenu(int opponentIndex)
		{
			if (opponentIndex < 0 || opponentIndex >= _opponents.Count)
			{
				return;
			}

			CloseMenus();
			NewGamePlayerSelection row = _opponents[opponentIndex];
			Menu<int> menu = CreatePopupMenu(TranslateFormatted("Edit AI #{0}", opponentIndex + 1), OffsetX + 44, OffsetY + 26);

			menu.Items.Add(Translate("Change name"), 1).OnSelect((s, a) => OpenNameInput(row));
			menu.Items.Add(TranslateFormatted("AI: {0}", row.AiName), 2).OnSelect((s, a) => OpenAiTypeMenu(row));
			menu.Items.Add(TranslateFormatted("Difficulty: {0}", GetDifficultyLabel(row.DifficultyIndex)), 3).OnSelect((s, a) => OpenAiDifficultyMenu(row));
			if (_mode == SelectionMode.NewGame && !IsBarbarianRow(row))
			{
				menu.Items.Add(TranslateFormatted("Civilization: {0}", row.Civilization.Name), 4).OnSelect((s, a) => OpenCivilizationMenu(row));
			}
			else
			{
				menu.Items.Add(TranslateFormatted("Civilization: {0}", row.Civilization.Name), 4).Disable();
			}
			menu.Items.Add(TranslateFormatted("Color: #{0}", row.ColorSlot), 5).OnSelect((s, a) => OpenColorMenu(row)).Disable();
			menu.Items.Add(TranslateFormatted("Team: #{0}", row.TeamSlot), 6).OnSelect((s, a) => OpenTeamMenu(row)).Disable();
			if (_mode == SelectionMode.NewGame && !IsBarbarianRow(row))
			{
				menu.Items.Add(Translate("Remove opponent"), 7).OnSelect((s, a) =>
				{
					_opponents.RemoveAt(opponentIndex);
					CloseMenus();
					RequestUpdate();
				});
			}
			else
			{
				menu.Items.Add(Translate("Remove opponent"), 7).Disable();
			}
			menu.Items.Add(Translate("Back"), 8).OnSelect((s, a) =>
			{
				CloseMenus();
				RequestUpdate();
			});

			AutoSizeMenuWidth(menu);
			AddMenu(menu);
		}

		private void OpenNameInput(NewGamePlayerSelection row)
		{
			CloseMenus();
			_nameInputDialog = new InputDialogDelegate(
				Translate("Enter name"),
				maxLength: 20,
				acceptValidator: value => !string.IsNullOrWhiteSpace(value),
				validationFailedAction: _ => { },
				textColour: 5,
				frameColour: 11,
				dialogInnerColour: 15,
				fieldInnerColour: 15);

			_nameInputDialog.Accepted += value =>
			{
				string trimmed = value.Trim();
				if (!string.IsNullOrWhiteSpace(trimmed))
				{
					row.Name = trimmed;
				}
				_nameInputDialog = null;
				RequestUpdate();
			};

			_nameInputDialog.Cancelled += (_, _) =>
			{
				_nameInputDialog = null;
				RequestUpdate();
			};

			_nameInputDialog.Open(row.Name);
			RequestUpdate();
		}

		private void OpenCivilizationMenu(NewGamePlayerSelection row)
		{
			CloseMenus();
			Menu<int> menu = CreatePopupMenu(Translate("Pick civilization"), OffsetX + 44, OffsetY + 20);
			HashSet<int> usedBuddySlots = GetUsedBuddySlots(exclude: row);
			for (int i = 0; i < _availableCivilizations.Length; i++)
			{
				ICivilization civilization = _availableCivilizations[i];
				bool isAvailable = !usedBuddySlots.Contains(civilization.PreferredPlayerNumber);
				menu.Items.Add(civilization.Name, i)
					.SetEnabled(isAvailable)
					.OnSelect((s, a) =>
				{
					row.Civilization = civilization;
					CloseMenus();
					RequestUpdate();
				});
			}
			menu.Items.Add(Translate("Back"), -1).OnSelect((s, a) =>
			{
				CloseMenus();
				RequestUpdate();
			});
			AutoSizeMenuWidth(menu);
			AddMenu(menu);
		}

		private void OpenColorMenu(NewGamePlayerSelection row)
		{
			CloseMenus();
			Menu<int> menu = CreatePopupMenu(Translate("Pick color"), OffsetX + 84, OffsetY + 36);
			HashSet<int> usedColors = GetUsedColorSlots(exclude: row);
			for (int i = 1; i <= MaxSlots; i++)
			{
				int color = i;
				bool isAvailable = !usedColors.Contains(color);
				menu.Items.Add(TranslateFormatted("Color #{0}", color), color)
					.SetEnabled(isAvailable)
					.OnSelect((s, a) =>
				{
					row.ColorSlot = color;
					CloseMenus();
					RequestUpdate();
				});
			}
			menu.Items.Add(Translate("Back"), -1).OnSelect((s, a) =>
			{
				CloseMenus();
				RequestUpdate();
			});
			AutoSizeMenuWidth(menu);
			AddMenu(menu);
		}

		private void OpenTeamMenu(NewGamePlayerSelection row)
		{
			CloseMenus();
			Menu<int> menu = CreatePopupMenu(Translate("Pick team"), OffsetX + 84, OffsetY + 36);
			for (int i = 1; i <= MaxSlots; i++)
			{
				int team = i;
				menu.Items.Add(TranslateFormatted("Team #{0}", team), team).OnSelect((s, a) =>
				{
					row.TeamSlot = team;
					CloseMenus();
					RequestUpdate();
				});
			}
			menu.Items.Add(Translate("Back"), -1).OnSelect((s, a) =>
			{
				CloseMenus();
				RequestUpdate();
			});
			AutoSizeMenuWidth(menu);
			AddMenu(menu);
		}

		private void OpenAiTypeMenu(NewGamePlayerSelection row)
		{
			CloseMenus();
			Menu<int> menu = CreatePopupMenu(Translate("Pick AI"), OffsetX + 80, OffsetY + 36);
			if (IsBarbarianRow(row))
			{
				menu.Items.Add(Translate("Disabled"), -2).OnSelect((s, a) =>
				{
					row.AiId = AiDefinitionIds.BarbarianDisabled;
					row.AiName = Translate("Disabled");
					_lastSelectedAiId = AiDefinitionIds.BarbarianDisabled;
					CloseMenus();
					RequestUpdate();
				});
			}

			for (int i = 0; i < _aiEntries.Count; i++)
			{
				AiCatalogEntry entry = _aiEntries[i];
				menu.Items.Add(TranslateFormatted("{0} ({1})", entry.Name, entry.Provider), i).OnSelect((s, a) =>
				{
					row.AiId = entry.Id;
					row.AiName = entry.Name;
					_lastSelectedAiId = entry.Id;
					CloseMenus();
					RequestUpdate();
				});
			}
			menu.Items.Add(Translate("Back"), -1).OnSelect((s, a) =>
			{
				CloseMenus();
				RequestUpdate();
			});
			AutoSizeMenuWidth(menu);
			AddMenu(menu);
		}

		private void OpenAiDifficultyMenu(NewGamePlayerSelection row)
		{
			CloseMenus();
			Menu<int> menu = CreatePopupMenu(Translate("Pick AI difficulty"), OffsetX + 84, OffsetY + 36);
			for (int i = 0; i < _difficultyLabels.Count; i++)
			{
				int difficultyIndex = i;
				menu.Items.Add(_difficultyLabels[difficultyIndex], difficultyIndex).OnSelect((s, a) =>
				{
					row.DifficultyIndex = difficultyIndex;
					_lastSelectedDifficultyIndex = difficultyIndex;
					CloseMenus();
					RequestUpdate();
				});
			}
			menu.Items.Add(Translate("Back"), -1).OnSelect((s, a) =>
			{
				CloseMenus();
				RequestUpdate();
			});
			AutoSizeMenuWidth(menu);
			AddMenu(menu);
		}

		private static int NormalizeRingValue(int value, int max)
		{
			if (max <= 0)
			{
				return 1;
			}

			int normalized = value % max;
			if (normalized <= 0)
			{
				normalized += max;
			}

			return normalized;
		}

		private void AddOpponent()
		{
			if (GetRegularOpponentCount() >= MaxOpponents)
			{
				return;
			}

			ICivilization? civilization = GetFirstFreeCivilization();
			if (civilization is null)
			{
				return;
			}

			AiCatalogEntry defaultRegularAi = GetRegularDefaultAiOrFallback();
			(Guid rememberedAiId, string rememberedAiName) = (defaultRegularAi.Id, defaultRegularAi.Name);
			int rememberedDifficultyIndex = GetRememberedDifficultyOrDefault();

			_lastSelectedAiId = rememberedAiId;
			_lastSelectedDifficultyIndex = rememberedDifficultyIndex;

			_opponents.Add(new NewGamePlayerSelection
			{
				IsHuman = false,
				Name = civilization.Name,
				Civilization = civilization,
				AiId = rememberedAiId,
				AiName = rememberedAiName,
				DifficultyIndex = rememberedDifficultyIndex,
				ColorSlot = GetNextFreeColorSlot(),
				TeamSlot = GetNextFreeTeamSlot()
			});
		}

		private bool HasFreeCivilization()
		{
			return GetFirstFreeCivilization() is not null;
		}

		private ICivilization? GetFirstFreeCivilization()
		{
			HashSet<int> usedBuddySlots = GetUsedBuddySlots();
			return _availableCivilizations.FirstOrDefault(c => !usedBuddySlots.Contains(c.PreferredPlayerNumber));
		}

		private HashSet<int> GetUsedBuddySlots(NewGamePlayerSelection? exclude = null)
		{
			HashSet<int> used = [];
			if (!ReferenceEquals(_human, exclude))
			{
				used.Add(_human.Civilization.PreferredPlayerNumber);
			}

			foreach (NewGamePlayerSelection opponent in _opponents)
			{
				if (ReferenceEquals(opponent, exclude))
				{
					continue;
				}
				used.Add(opponent.Civilization.PreferredPlayerNumber);
			}

			return used;
		}

		private int GetNextFreeTeamSlot()
		{
			HashSet<int> usedTeams = [_human.TeamSlot, .. _opponents.Select(o => o.TeamSlot)];
			for (int team = 1; team <= MaxSlots; team++)
			{
				if (!usedTeams.Contains(team))
				{
					return team;
				}
			}

			return NormalizeRingValue(_opponents.Count + 2, MaxSlots);
		}

		private HashSet<int> GetUsedColorSlots(NewGamePlayerSelection? exclude = null)
		{
			HashSet<int> used = [];
			if (!ReferenceEquals(_human, exclude))
			{
				used.Add(_human.ColorSlot);
			}

			foreach (NewGamePlayerSelection opponent in _opponents)
			{
				if (ReferenceEquals(opponent, exclude))
				{
					continue;
				}
				used.Add(opponent.ColorSlot);
			}

			return used;
		}

		private int GetNextFreeColorSlot()
		{
			HashSet<int> usedColors = GetUsedColorSlots();
			for (int color = 1; color <= MaxSlots; color++)
			{
				if (!usedColors.Contains(color))
				{
					return color;
				}
			}

			return NormalizeRingValue(_opponents.Count + 2, MaxSlots);
		}

		private static NewGamePlayerSelection CreateHumanRow(ICivilization civilization)
		{
			return new NewGamePlayerSelection
			{
				IsHuman = true,
				Name = civilization.Leader.Name,
				Civilization = civilization,
				AiId = null,
				AiName = string.Empty,
				DifficultyIndex = -1,
				ColorSlot = NormalizeRingValue(civilization.PreferredPlayerNumber, MaxSlots),
				TeamSlot = 1
			};
		}

		private int GetLowestOpponentDifficulty()
		{
			if (_opponents.Count == 0)
			{
				return _difficulty;
			}

			int fallbackDifficulty = Math.Clamp(_difficulty, 0, Math.Max(0, _difficultyLabels.Count - 1));
			int lowestDifficulty = fallbackDifficulty;
			bool hasMappedDifficulty = false;

			foreach (NewGamePlayerSelection opponent in _opponents)
			{
				if (!IsValidDifficultyIndex(opponent.DifficultyIndex))
				{
					continue;
				}

				if (!hasMappedDifficulty)
				{
					lowestDifficulty = opponent.DifficultyIndex;
					hasMappedDifficulty = true;
					continue;
				}

				if (opponent.DifficultyIndex < lowestDifficulty)
				{
					lowestDifficulty = opponent.DifficultyIndex;
				}
			}

			return hasMappedDifficulty ? lowestDifficulty : fallbackDifficulty;
		}

		private void StartGame()
		{
			if (_mode == SelectionMode.InGameEdit)
			{
				ApplyInGameChangesAndClose();
				return;
			}

			int selectedGameDifficulty = GetLowestOpponentDifficulty();
			int participantCount = Math.Max(2, GetRegularOpponentCount() + 1);
			int highestPreferredSlot = Math.Max(
				_human.Civilization.PreferredPlayerNumber,
				_opponents.Count == 0 ? 0 : _opponents.Max(row => row.Civilization.PreferredPlayerNumber));

			int competition = Math.Clamp(Math.Max(participantCount, highestPreferredSlot), 2, 7);

			NewGameAiSelectionResult result = new()
			{
				Difficulty = selectedGameDifficulty,
				Competition = competition,
				Human = new NewGamePlayerSelection
				{
					IsHuman = true,
					Name = _human.Name,
					Civilization = _human.Civilization,
					AiId = null,
					AiName = string.Empty,
					DifficultyIndex = -1,
					ColorSlot = _human.ColorSlot,
					TeamSlot = _human.TeamSlot
				},
				Opponents = [.. _opponents.Select(row => new NewGamePlayerSelection
				{
					IsHuman = false,
					Name = row.Name,
					Civilization = row.Civilization,
					AiId = row.AiId,
					AiName = row.AiName,
					DifficultyIndex = row.DifficultyIndex,
					ColorSlot = row.ColorSlot,
					TeamSlot = row.TeamSlot
				})]
			};

			StartRequested?.Invoke(this, new NewGameAiSelectionResultEventArgs(result));
			Destroy();
		}

		private void ApplyInGameChangesAndClose()
		{
			if (_mode != SelectionMode.InGameEdit)
			{
				return;
			}

			if (_runtimeHumanPlayer is IPlayerRestorable restorableHuman)
			{
				restorableHuman.TribeName = _human.Name;
				restorableHuman.TribeNamePlural = _human.Name;
			}

			for (int i = 0; i < _opponents.Count && i < _runtimeOpponentPlayers.Count; i++)
			{
				NewGamePlayerSelection row = _opponents[i];
				Player player = _runtimeOpponentPlayers[i];

				if (player is IPlayerRestorable restorablePlayer)
				{
					restorablePlayer.AiId = row.AiId;
					restorablePlayer.TribeName = row.Name;
					restorablePlayer.TribeNamePlural = row.Name;
				}

				if (IsValidDifficultyIndex(row.DifficultyIndex))
				{
					player.Handicap = (byte)row.DifficultyIndex;
				}
			}

			Destroy();
		}

		private Menu<int> CreatePopupMenu(string title, int x, int y)
		{
			Menu<int> menu = new("NewGameAiPopup", Palette)
			{
				Title = title,
				X = x,
				Y = y,
				MenuWidth = 0,
				TitleColour = 5,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 8,
				FontId = MenuFont,
				IndentTitle = 2,
				RowHeight = 8,
				DrawFullBackground = true
			};

			menu.Cancel += (sender, args) =>
			{
				CloseMenus();
				RequestUpdate();
			};

			return menu;
		}

		private static void AutoSizeMenuWidth(Menu<int> menu, int minWidth = 120)
		{
			int maxWidth = 0;
			if (!string.IsNullOrEmpty(menu.Title))
			{
				maxWidth = Resources.GetTextSize(menu.FontId, menu.Title).Width;
			}
			foreach (MenuItem<int> item in menu.Items)
			{
				int w = Resources.GetTextSize(menu.FontId, item.Text ?? string.Empty).Width;
				if (w > maxWidth)
				{
					maxWidth = w;
				}
			}
			menu.MenuWidth = Math.Max(minWidth, maxWidth + 20);
		}

		private void RequestUpdate()
		{
			_update = true;
		}

		private void Resize(object? sender, ResizeEventArgs args)
		{
			CloseMenus();
			_nameInputDialog = null;
			RequestUpdate();
		}
	}
}
