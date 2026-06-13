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
using System.Diagnostics;
using System.IO;
using System.Linq;
using CivOne.Enums;
using CivOne.Persistence;
using CivOne.Persistence.Mapper;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;
using CivOne.Services.GlobalWarming;
using CivOne.Services.Palace;
using CivOne.Services.Random;
using CivOne.Services;
using CivOne.Units;
using CivOne.Tasks;

namespace CivOne
{
	// This partial file contains the YAML load pipeline for Game.
	// _valueSanitizer is declared here because it is only needed during
	// YAML hydration (Game(GameState) constructor and its helpers).
	public partial class Game
	{
		private readonly IValueSanitizer _valueSanitizer;

		// CW: TODO: should be made a service for future.
		private sealed class RuntimeLogger : ILogger
		{
			public void Log(string text, params object[] parameters)
			{
				RuntimeHandler.Runtime.Log(text, parameters);
			}
		}

		private static ValueSanitizer CreateValueSanitizer()
		{
			return new ValueSanitizer(new RuntimeLogger());
		}

		#pragma warning disable CS8618 //ignore uninitialized non-nullable fields
		private Game(
			IValueSanitizer valueSanitizer,
			IPalaceUpgradeService? palaceUpgradeService = null,
			ICivilizationRankingTriggerService? civilizationRankingTriggerService = null)
		{
			_valueSanitizer = valueSanitizer ?? throw new ArgumentNullException(nameof(valueSanitizer));
			_palaceUpgradeService = palaceUpgradeService ?? PalaceUpgradeServiceFactory.GetInstance();
			_civilizationRankingTriggerService = civilizationRankingTriggerService ?? CivilizationRankingTriggerServiceFactory.GetInstance();
			
			// Initialize collections and other fields to default values. 
			// This only remedies the warning about uninitialized non-nullable fields.
			// This constructor is not expected to be used directly; 
			// but only as base for the Game(GameState) constructor, which will properly initialize these fields.
			_players = [];
			_cities = [];
			_units = [];
			CityNames = [.. Common.AllCityNames];
			_advanceOrigin = [];
			_replayData = [];
		}
		#pragma warning restore CS8618

		private Game(GameState state) : this(CreateValueSanitizer())
		{
			_loadedFromYamlSaveSource = true;

			// Must be first: Player methods access static Player.Game during hydration.
			Player.Game = this;

			ArgumentNullException.ThrowIfNull(state);
			ArgumentNullException.ThrowIfNull(state.Players);

			SaveMetaData.InitializeForLoadedGame(GameVersion);

			_instance = this;

			_difficulty = state.Difficulty;
			_competition = Math.Max(0, state.Players.Length - 1);

			_players = [.. state.Players.Select(p => p as Player ?? throw new InvalidOperationException("YAML load requires Player instances."))];
			_cities = state.Cities ?? [];
			_units = state.Units ?? [];
			CityNames = state.CityNames?.Length > 0 ? state.CityNames : Common.AllCityNames.ToArray();

			RegisterPlayerDestroyedHandlers();
			ResolveUnitHomeCities();
			InitializePlayerState(state);
			InitializeGameState(state);
			ApplyGameOptions(state.GameOptions ?? []);
			InitializeGlobalWarmingServices(state);
			InitializeActiveUnit();

			_players.ToList().ForEach(p => p.HandleExtinction(false));
		}

		private void RegisterPlayerDestroyedHandlers()
		{
			for (var i = 1; i < _players.Length; i++)
			{
				_players[i].Destroyed += PlayerDestroyed;
			}
		}

		private void ResolveUnitHomeCities()
		{
			var cityById = _cities
				.GroupBy(c => c.Id)
				.ToDictionary(group => group.Key, group => group.First());

			foreach (var unit in _units.OfType<IUnitRestorable>())
			{
				if (unit.PendingHomeCityGuid.HasValue && cityById.TryGetValue(unit.PendingHomeCityGuid.Value, out var city))
				{
					unit.SetHome(city);
				}

				unit.PendingHomeCityGuid = null;
			}
		}

		private void InitializePlayerState(GameState state)
		{
			GameTurn = _valueSanitizer.ClampToUInt16(state.GameTurn, nameof(Game), nameof(GameTurn));

			HumanPlayer = state.HumanPlayer as Player
				?? throw new InvalidOperationException("YAML load requires HumanPlayer to be a Player instance.");

			if (HumanPlayer.LastMapPosition.X >= 0 && HumanPlayer.LastMapPosition.Y >= 0)
			{
				_pendingMapPositionRestore = HumanPlayer.LastMapPosition;
			}

			_currentPlayer = Array.IndexOf(_players, state.CurrentPlayer as Player);
			if (_currentPlayer < 0)
				_currentPlayer = Array.IndexOf(_players, HumanPlayer);
			if (_currentPlayer < 0)
				_currentPlayer = 0;
		}

		private void InitializeGameState(GameState state)
		{
			_anthologyTurn = state.AnthologyTurn;
			_peaceTurns = state.PeaceTurns;
			_playerFutureTech = HumanPlayer?.FutureTechCount ?? state.PlayerFutureTech;

			if (state.AdvanceOrigin != null)
			{
				foreach (var kvp in state.AdvanceOrigin)
				{
					_advanceOrigin[kvp.Key] = kvp.Value;
				}
			}

			if (state.ReplayData?.Count > 0)
			{
				_replayData.AddRange(state.ReplayData);
			}

			RandomServiceFactory.Reset(_valueSanitizer.ClampToUInt16(state.RandomSeed, nameof(Game), nameof(state.RandomSeed)));
		}

		private void ApplyGameOptions(IEnumerable<GameOptionEnum> options)
		{
			InstantAdvice = Settings.InstantAdvice == GameOption.On;
			AutoSave = Settings.AutoSave != GameOption.Off;
			EndOfTurn = Settings.EndOfTurn == GameOption.On;
			Animations = Settings.Animations != GameOption.Off;
			Sound = Settings.Sound != GameOption.Off;
			EnemyMoves = Settings.EnemyMoves != GameOption.Off;
			CivilopediaText = Settings.CivilopediaText != GameOption.Off;
			Palace = Settings.Palace != GameOption.Off;

			var optionList = options as IList<GameOptionEnum> ?? [.. options];
			if (Settings.InstantAdvice == GameOption.Default) InstantAdvice = optionList.Contains(GameOptionEnum.InstantAdvice);
			if (Settings.AutoSave == GameOption.Default) AutoSave = optionList.Contains(GameOptionEnum.AutoSave);
			if (Settings.EndOfTurn == GameOption.Default) EndOfTurn = optionList.Contains(GameOptionEnum.EndOfTurn);
			if (Settings.Animations == GameOption.Default) Animations = optionList.Contains(GameOptionEnum.Animations);
			if (Settings.Sound == GameOption.Default) Sound = optionList.Contains(GameOptionEnum.Sound);
			if (Settings.EnemyMoves == GameOption.Default) EnemyMoves = optionList.Contains(GameOptionEnum.EnemyMoves);
			if (Settings.CivilopediaText == GameOption.Default) CivilopediaText = optionList.Contains(GameOptionEnum.CivilopediaText);
			if (Settings.Palace == GameOption.Default) Palace = optionList.Contains(GameOptionEnum.Palace);
		}

		private void InitializeGlobalWarmingServices(GameState state)
		{
			_globalWarmingService = GlobalWarmingServiceFactory.CreateGlobalWarmingService(
				state.GlobalWarmingCount, state.PollutedSquaresCount, state.WarmingIndicator, Map.AllTiles());
			_globalWarmingScourgeService = GlobalWarmingServiceFactory.CreateGlobalWarmingScourgeService(
				_globalWarmingService,
				Map.Tiles,
				(tile, newTerrainType) => Map.ChangeTileType(tile.X, tile.Y, newTerrainType),
				DisbandUnit,
				Map.WIDTH,
				Map.HEIGHT
			);
		}

		private void InitializeActiveUnit()
		{
			var humanPlayerId = HumanPlayerId;
			for (var i = 0; i < _units.Count; i++)
			{
				if (_units[i].Owner != humanPlayerId || _units[i].Busy)
					continue;

				_activeUnit = i;
				if (_units[i].MovesLeft > 0)
					break;
			}
		}

		public static bool LoadYamlGame(string cosFile)
		{
			if (string.IsNullOrWhiteSpace(cosFile))
			{
				BaseInstance.Log("LoadYamlGame called with empty file path.");
				return false;
			}

			var previousPlayerGame = Player.Game;

			try
			{
				var sanitizer = CreateValueSanitizer();
				var deps = YamlLoadMapperDependenciesFactory.Create(sanitizer);
				var mapper = new GameStateDtoMapper(deps.PlayerMapper, deps.UnitMapper, deps.MapMapper, deps.GlobalWarmingMapper, deps.Sanitizer);
				var yaml = File.ReadAllText(cosFile);

				VersionedSaveFile parsedSaveFile = ParseVersionedSaveFile(yaml);
				var dto = parsedSaveFile.GameState;
				var saveGuid = parsedSaveFile.SaveGuid;

				// Reset static player context only for the hydration window,
				// then Game(state) will set Player.Game to the new game instance.
				Player.Game = null!;
				var state = mapper.FromDto(dto);
				_instance = new Game(state);

				if (parsedSaveFile.Meta != null)
				{
					var playDuration = TimeSpan.FromMinutes(Math.Max(0L, parsedSaveFile.Meta.PlayDurationMinutes));
					_instance.SaveMetaData.RestoreFromSave(
						parsedSaveFile.Meta.GetCreatedAtOr(DateTimeOffset.UtcNow),
						parsedSaveFile.Meta.GameVersion,
						playDuration,
						parsedSaveFile.Meta.DisplayName,
						saveGuid);
				}
				else
				{
					_instance.SaveMetaData.RestoreSaveGuid(saveGuid);
				}

				Map.Instance.FinalizeYamlLoad();

				BaseInstance.Log("Game instance loaded from YAML (difficulty: {0}, competition: {1})", _instance._difficulty, _instance._competition);
				return true;
			}
			catch (Exception ex)
			{
				Player.Game = previousPlayerGame;
				BaseInstance.Log("LoadYamlGame failed: {0}", ex);
				return false;
			}
		}

		private readonly record struct VersionedSaveFile(GameStateDto GameState, SaveGameMetaDataDto Meta, Guid SaveGuid);

		/// <summary>
		/// This is the main entry point for loading a YAML save file. It reads the file, determines the format version, and dispatches to the appropriate parser. It returns a VersionedSaveFile containing the GameStateDto, metadata, and save GUID. 
		/// The GameStateDto is then mapped to the runtime GameState and used to construct the Game instance.
		/// Currently, only format version 1 is supported, which corresponds to SaveGame1FileRootDto. Future versions can be added with new DTOs and parsing logic as needed.
		/// </summary>
		/// <param name="yaml"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		private static VersionedSaveFile ParseVersionedSaveFile(string yaml)
		{
			uint formatVersion = ReadFormatVersion(yaml);

			return formatVersion switch
			{
				SaveGame1FileRootDto.CurrentFormatVersion => ParseSaveV1(yaml),
				_ => throw new NotSupportedException($"Save file format version {formatVersion} is not supported.")
			};
		}

		private static VersionedSaveFile ParseSaveV1(string yaml)
		{
			SaveGame1FileRootDto saveFile = YamlReader
				.OfString(yaml)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.As<SaveGame1FileRootDto>();

			if (saveFile?.GameState == null)
				throw new InvalidOperationException("GameState is required in save file.");

			Guid saveGuid = saveFile.Meta?.SaveGuid ?? Guid.NewGuid();
			return new VersionedSaveFile(saveFile.GameState, saveFile.Meta!, saveGuid);
		}

		private static uint ReadFormatVersion(string yaml)
		{
			// Quick regex to extract FormatVersion from YAML without full parse
			var match = System.Text.RegularExpressions.Regex.Match(yaml, @"FormatVersion:\s*(\d+)");
			return match.Success && uint.TryParse(match.Groups[1].Value, out var version) ? version : 0;
		}
	}
}
