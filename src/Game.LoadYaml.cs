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
using System.Linq;
using CivOne.Enums;
using CivOne.Persistence;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;
using CivOne.Services.GlobalWarming;
using CivOne.Units;

namespace CivOne
{
	public partial class Game
	{
		private sealed class RuntimeLogger : ILogger
		{
			public void Log(string text, params object[] parameters)
			{
				RuntimeHandler.Runtime.Log(text, parameters);
			}
		}

		private Game(GameState state)
		{
			// Must be first: Player methods access static Player.Game during hydration.
			Player.Game = this;

			ArgumentNullException.ThrowIfNull(state);
			ArgumentNullException.ThrowIfNull(state.Players);

			_instance = this;

			_difficulty = state.Difficulty;
			_competition = Math.Max(0, state.Players.Length - 1);

			_players = [.. state.Players.Select(p => p as Player ?? throw new InvalidOperationException("YAML load requires Player instances."))];
			_cities = state.Cities ?? [];
			_units = state.Units ?? [];

			for (var i = 1; i < _players.Length; i++)
			{
				_players[i].Destroyed += PlayerDestroyed;
			}

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

			GameTurn = (ushort)Math.Clamp((int)state.GameTurn, 0, ushort.MaxValue);

			HumanPlayer = state.HumanPlayer as Player
				?? throw new InvalidOperationException("YAML load requires HumanPlayer to be a Player instance.");

			_currentPlayer = Array.IndexOf(_players, state.CurrentPlayer as Player);
			if (_currentPlayer < 0)
			{
				_currentPlayer = Array.IndexOf(_players, HumanPlayer);
			}
			if (_currentPlayer < 0)
			{
				_currentPlayer = 0;
			}

			_anthologyTurn = state.AnthologyTurn;

			if (state.AdvanceOrigin != null)
				foreach (var kvp in state.AdvanceOrigin)
					_advanceOrigin[kvp.Key] = kvp.Value;

			CityNames = state.CityNames?.Length > 0 ? state.CityNames : Common.AllCityNames.ToArray();

			Common.SetRandomSeed((ushort)Math.Clamp(state.RandomSeed, ushort.MinValue, ushort.MaxValue));

			// Game Settings (same precedence as binary load)
			InstantAdvice = (Settings.InstantAdvice == GameOption.On);
			AutoSave = (Settings.AutoSave != GameOption.Off);
			EndOfTurn = (Settings.EndOfTurn == GameOption.On);
			Animations = (Settings.Animations != GameOption.Off);
			Sound = (Settings.Sound != GameOption.Off);
			EnemyMoves = (Settings.EnemyMoves != GameOption.Off);
			CivilopediaText = (Settings.CivilopediaText != GameOption.Off);
			Palace = (Settings.Palace != GameOption.Off);

			var options = state.GameOptions ?? [];
			if (Settings.InstantAdvice == GameOption.Default) InstantAdvice = options.Contains(GameOptionEnum.InstantAdvice);
			if (Settings.AutoSave == GameOption.Default) AutoSave = options.Contains(GameOptionEnum.AutoSave);
			if (Settings.EndOfTurn == GameOption.Default) EndOfTurn = options.Contains(GameOptionEnum.EndOfTurn);
			if (Settings.Animations == GameOption.Default) Animations = options.Contains(GameOptionEnum.Animations);
			if (Settings.Sound == GameOption.Default) Sound = options.Contains(GameOptionEnum.Sound);
			if (Settings.EnemyMoves == GameOption.Default) EnemyMoves = options.Contains(GameOptionEnum.EnemyMoves);
			if (Settings.CivilopediaText == GameOption.Default) CivilopediaText = options.Contains(GameOptionEnum.CivilopediaText);
			if (Settings.Palace == GameOption.Default) Palace = options.Contains(GameOptionEnum.Palace);

			globalWarmingService = GlobalWarmingServiceFactory.CreateGlobalWarmingService(_cities.AsReadOnly(), Map.AllTiles());
			globalWarmingScourgeService = GlobalWarmingServiceFactory.CreateGlobalWarmingScourgeService(
				globalWarmingService,
				Map.Tiles,
				(tile, newTerrainType) => Map.ChangeTileType(tile.X, tile.Y, newTerrainType),
				DisbandUnit,
				Map.WIDTH,
				Map.HEIGHT
			);

			var humanPlayerId = HumanPlayerId;
			for (var i = 0; i < _units.Count; i++)
			{
				if (_units[i].Owner != humanPlayerId || _units[i].Busy)
				{
					continue;
				}

				_activeUnit = i;
				if (_units[i].MovesLeft > 0)
				{
					break;
				}
			}

			_players.ToList().ForEach(p => p.HandleExtinction(false));
		}

		public static bool LoadYamlGame(string cosFile)
		{
			if (string.IsNullOrWhiteSpace(cosFile))
			{
				BaseInstance.Log("LoadYamlGame called with empty file path.");
				return false;
			}

			try
			{
				Player.Game = null;

				var sanitizer = new YamlReadValueSanitizer(new RuntimeLogger());
				var deps = YamlLoadMapperDependenciesFactory.Create(sanitizer);
				var mapper = new GameStateDtoMapper(deps.PlayerMapper, deps.UnitMapper, deps.MapMapper, deps.Sanitizer);

				var dto = YamlReader
					.OfFile(cosFile)
					.WithStandard()
					.WithTypeConverter(new MapDtoTileDtoYamlConverter())
					.As<GameStateDto>();

				var state = mapper.FromDto(dto);
				_instance = new Game(state);
				Map.Instance.FinalizeYamlLoad();

				BaseInstance.Log("Game instance loaded from YAML (difficulty: {0}, competition: {1})", _instance._difficulty, _instance._competition);
				return true;
			}
			catch (Exception ex)
			{
				BaseInstance.Log("LoadYamlGame failed: {0}", ex);
				return false;
			}
		}
	}
}
