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
using System.Linq;
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Model;
using CivOne.Services;
using CivOne.Services.GlobalWarming;
using CivOne.Services.Random;
using CivOne.Services.StartPositions;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne
{
	public partial class Game
	{
		private bool HasUnitAt(int x, int y)
		{
			return _units.Any(u => u.X == x && u.Y == y);
		}

		/// <summary>
		/// Make sure the starting settlers can be placed at the given location, 
		/// and if so, place them and set the player's starting location. 
		/// </summary>
		/// <param name="player">The player index.</param>
		/// <param name="x">The x-coordinate of the starting location.</param>
		/// <param name="y">The y-coordinate of the starting location.</param>
		/// <param name="allowHut">Whether to allow starting on a hut.</param>
		/// <returns>True if the settlers were successfully placed; otherwise, false.</returns>
		private bool TryPlaceStartingSettlers(byte player, int x, int y, bool allowHut)
		{
			ITile tile = Map[x, y];
			if (tile == null || tile.IsOcean)
			{
				return false;
			}

			if (HasUnitAt(x, y))
			{
				return false;
			}

			if (!allowHut && tile.Hut)
			{
				return false;
			}

			if (tile.Hut)
			{
				tile.Hut = false;
			}

			IUnit? unit = CreateUnit(UnitType.Settlers, x, y);
			
			unit!.Owner = player;
			_units.Add(unit);

			_players[player].StartX = (short)x;
			return true;
		}

		private readonly IMapEditor? _mapEditor;
		private IMapEditor MapEditor => _mapEditor ?? Map.Instance;

		private readonly IStartPositionService? _startPositionService;
		private IStartPositionService StartPositionService => _startPositionService ?? StartPositionServiceFactory.Create();

		/// <summary>
		/// Finds and places starting Settlers for a batch of players in a single call, so algorithms that need to
		/// know the total number of players up front (e.g. dividing the map into equally sized areas) can do so.
		/// </summary>
		/// <param name="players">The player indexes that need a starting position.</param>
		private void PlaceStartingUnits(IReadOnlyList<byte> players)
		{
			StartPositionCandidate[] candidates = [.. players.Select(p => new StartPositionCandidate
			{
				Civilization = _players[p].Civilization,
				MapStartPosition = _players[p].MapStartPosition,
			})];

			StartPositionContext context = new()
			{
				Map = MapEditor,
				RandomService = _randomService,
				IsFirstGameTurn = GameTurn == 0,
				// If the map has any player with a MapStartPosition,
				// then use that mode for every player, instead of the fixed positions of the civilization class.
				AnyFixedMapStartPosition = _players.Any(p => p.MapStartPosition != null),
				GameTurn = GameTurn,
				Difficulty = Difficulty,
				OccupiedTiles = [.. _units.Select(u => new MapLocation((uint)u.X, (uint)u.Y))],
				CityLocations = [.. _cities.Select(c => new MapLocation((uint)c.X, (uint)c.Y))],
				SettlerLocations = [.. _units.Where(u => u is Settlers).Select(u => new MapLocation((uint)u.X, (uint)u.Y))],
				Logger = this,
			};

			IReadOnlyList<StartPositionResult> results = StartPositionService.FindStartPositions(candidates, context);

			for (int i = 0; i < players.Count; i++)
			{
				byte player = players[i];
				StartPositionResult result = results[i];
				if (!result.Success)
				{
					Log("AddStartingUnits: no valid starting position found for player {0}.", player);
					throw new InvalidOperationException($"Unable to place starting settlers for player {player}.");
				}

				int x = (int)result.Position.X, y = (int)result.Position.Y;
				if (!TryPlaceStartingSettlers(player, x, y, true))
				{
					Log("AddStartingUnits: computed starting position for player {0} was unexpectedly invalid at {1},{2}.", player, x, y);
					throw new InvalidOperationException($"Unable to place starting settlers for player {player}.");
				}

				if (result.PlaceSecondSettlerAtSamePosition)
				{
					PlaceSecondSettler(player, x, y);
				}
			}
		}

		/// <summary>
		/// Places a second Settlers unit on the same tile as the player's first one (Chieftain difficulty bonus).
		/// Bypasses the <see cref="HasUnitAt"/> guard used by <see cref="TryPlaceStartingSettlers"/>, which exists
		/// to stop different civilizations from sharing a starting tile, not to stop a player's own units from stacking.
		/// </summary>
		private void PlaceSecondSettler(byte player, int x, int y)
		{
			IUnit? unit = CreateUnit(UnitType.Settlers, x, y);
			unit!.Owner = player;
			_units.Add(unit);
		}

		/// <summary>
		/// Converts a small positive integer to a Roman numeral, used to disambiguate players that share a
		/// reused civilization (e.g. "Caesar II") when there are more non-barbarian players than civilizations.
		/// </summary>
		private static string ToRomanNumeral(int number)
		{
			(int Value, string Numeral)[] table =
			[
				(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
				(100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
				(10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
			];

			System.Text.StringBuilder numeral = new();
			foreach ((int value, string symbol) in table)
			{
				while (number >= value)
				{
					numeral.Append(symbol);
					number -= value;
				}
			}
			return numeral.ToString();
		}

		private static void ApplyMapStartPosition(Player player)
		{
			ArgumentNullException.ThrowIfNull(player);

			if (Map.TryGetStartPosition(player.Civilization, out MapLocation? mapStartPosition))
			{
				player.MapStartPosition = mapStartPosition;
				return;
			}

			player.MapStartPosition = null;
		}

		private void CalculateHandicap(byte player)
		{
			// Translated drom this post by Gowron:
			// http://forums.civfanatics.com/showthread.php?t=494994

			// All Handicap values start from 0.
			byte handicap = 0;
			IUnit? startUnit = _units.FirstOrDefault(u => u.Owner == player);
			if (startUnit == null) return;
			int x = startUnit.X, y = startUnit.Y;

			ITile[] continent = Map.ContinentTiles(Map[x, y].ContinentId).ToArray();
			IUnit[] unitsOnContinent = _units.Where(u => continent.Any(c => c.X == u.X && c.Y == u.Y)).ToArray();

			if (unitsOnContinent.Length == 0)
			{
				// Add +4 if the civ does not share its land mass with any other civs.
				handicap += 4;
			}
			else if (unitsOnContinent.Min(u => Common.DistanceToTile(x, y, u.X, u.Y)) >= 20)
			{
				// If that is not the case, then add +2 if the nearest civ on the same continent is 20 or more squares away.
				handicap += 2;
			}
			else if (unitsOnContinent.Min(u => Common.DistanceToTile(x, y, u.X, u.Y)) >= 10)
			{
				// Add +1 instead if the nearest civ on the same continent is 10-19 squares away.
				handicap += 1;
			}

			// Check the terrain of the starting position and the 8 adjacent map squares.
			if (Map[x, y].GetBorderTiles().Any(t => t is River))
			{
				// Add +2 if there's at least one river square among them.
				handicap += 2;
			}
			else if (Map[x, y].GetBorderTiles().Count(t => t is Grassland) >= 3)
			{
				// If that is not the case, then add +1 if there are 3 or more grassland squares among them.
				handicap += 1;
			}

			if (continent.Length >= 200)
			{
				// Add +2 if the civ starts on a continent that covers at least 200 map squares.
				handicap += 2;
			}
			else if (continent.Length >= 100)
			{
				// If that is not the case, then add +1 if the civ starts on a continent that covers at least 100 map squares.
				handicap += 1;
			}

			_players[player].Handicap = handicap;
		}

		private void ApplyBonus(byte player)
		{
			byte bonus = (byte)(_players.Max(p => p.Handicap) - _players[player].Handicap);
			IUnit? startUnit = _units.FirstOrDefault(u => u.Owner == player);
			if (startUnit == null) return;
			int x = startUnit.X, y = startUnit.Y;

			if (bonus >= 4)
			{
				// If the Bonus value of the civ is 4 or higher, then the civ is granted an extra Settlers unit, for a total of two Settlers units.
				// In this case, the Bonus value is reduced by 3 afterwards.
				IUnit unit = CreateUnit(UnitType.Settlers, x, y)!;
				unit.Owner = player;
				_units.Add(unit);

				bonus -= 3;
			}

			var randomService = RandomServiceFactory.Create();
			// If the Bonus value is (still) greater than zero, then the civ gains a number of technologies equal to the Bonus value.
			while (bonus > 0)
			{
				IAdvance[] available = [.. _players[player].AvailableResearch];
				int advanceId = randomService.NextInt(0, 72);
				for (int i = 0; i < 1000; i++)
				{
					if (!available.Any(a => a.Id == (advanceId + i) % 72)) continue;
					IAdvance advance = available.First(a => a.Id == (advanceId + i) % 72);
					SetAdvanceOrigin(advance, null);
					_players[player].AddAdvance(advance, false);
					break;
				}
				bonus--;
			}
		}

		public static void CreateGame(int difficulty, int competition, ICivilization tribe, string? leaderName = null, string? tribeName = null, string? tribeNamePlural = null, bool replaceExisting = false)
		{
			if (!Map.Ready)
			{
				BaseInstance.Log("ERROR: Game creation requested before map generation finished");
				throw new InvalidOperationException("Game creation requested before map generation finished.");
			}

			if (_instance != null)
			{
				if (!replaceExisting)
				{
					BaseInstance.Log("ERROR: Game instance already exists");
					return;
				}

				BaseInstance.Log("Replacing existing game instance with a new game");
				_instance = null;
			}
			try
			{
				_instance = new Game(difficulty, competition, tribe, leaderName, tribeName, tribeNamePlural);
			}
			catch
			{
				_instance = null;
				throw;
			}

			foreach (IUnit unit in _instance._units)
			{
				unit.Explore();
			}
		}

		private void InitGlobalWarmingServices()
		{
			_globalWarmingService = GlobalWarmingServiceFactory.CreateGlobalWarmingService(Map.AllTiles());
			_globalWarmingScourgeService = GlobalWarmingServiceFactory.CreateGlobalWarmingScourgeService(
				_globalWarmingService,
				Map.Tiles,
				(tile, newTerrainType) => Map.ChangeTileType(tile.X, tile.Y, newTerrainType),
				DisbandUnit,
				Map.WIDTH,
				Map.HEIGHT
			);
		}

		private Game(int difficulty, int competition, ICivilization tribe, string? leaderName, string? playerTribeName, string? playerTribeNamePlural) : this(CreateValueSanitizer())
		{
			if (RuntimeHandler.Runtime.Settings.InitialSeed != 0)
			{
				RandomServiceFactory.Reset(RuntimeHandler.Runtime.Settings.InitialSeed);
			}
			else
			{
				RandomServiceFactory.Reset();
			}

			_loadedFromYamlSaveSource = false;

			_instance = this;
			SaveMetaData.InitializeForNewGame(GameVersion, DateTimeOffset.UtcNow);

			_difficulty = difficulty;
			_competition = competition;
			Log("Game instance created (difficulty: {0}, competition: {1})", _difficulty, _competition);

			InstantAdvice = Settings.InstantAdvice == GameOption.On || (Settings.InstantAdvice == GameOption.Default && difficulty == 0);
			AutoSave = Settings.AutoSave != GameOption.Off;
			EndOfTurn = Settings.EndOfTurn == GameOption.On;
			Animations = Settings.Animations != GameOption.Off;
			Sound = Settings.Sound != GameOption.Off;
			EnemyMoves = Settings.EnemyMoves != GameOption.Off;
			CivilopediaText = Settings.CivilopediaText != GameOption.Off;
			Palace = Settings.Palace != GameOption.Off;

			_cities = [];
			_units = [];

			Player.Game = this;
			_players = new Player[competition + 1];

			CivilizationAssignment assignment = CivilizationAssignment.Create(Common.Random!.InitialSeed, competition, tribe.PreferredPlayerNumber, tribe);
			Dictionary<int, int> civIdOccurrences = [];

			for (int i = 0; i <= competition; i++)
			{
				if (i == tribe.PreferredPlayerNumber)
				{
					_players[i] = new Player(tribe, leaderName, playerTribeName, playerTribeNamePlural);
					ApplyMapStartPosition(_players[i]);
					civIdOccurrences[tribe.Id] = 1;
					_players[i].Destroyed += PlayerDestroyed;
					HumanPlayer = _players[i];
					HumanPlayer.TaxesRate = Settings.TaxRate; // fire-eggs 20190725
					if (difficulty == 0)
					{
						// Chieftain starts with 50 Gold
						HumanPlayer.Gold = 50;
					}
					Log("- Player {0} is {1} of the {2} (human)", i, _players[i].LeaderName, _players[i].TribeNamePlural);
					continue;
				}

				ICivilization civ = assignment[i];
				int occurrence = civIdOccurrences.TryGetValue(civ.Id, out int count) ? count : 0;
				civIdOccurrences[civ.Id] = occurrence + 1;

				// When a civilization is reused (more non-barbarian players than the 14 available civilizations),
				// disambiguate the leader/tribe names of the repeat occurrences instead of showing duplicates.
				string? customLeaderName = null, customTribeName = null, customTribeNamePlural = null;
				if (occurrence > 0)
				{
					string numeral = ToRomanNumeral(occurrence + 1);
					customLeaderName = $"{civ.Leader.Name} {numeral}";
					customTribeName = $"{civ.Name} {numeral}";
					customTribeNamePlural = $"{civ.NamePlural} {numeral}";
				}

				_players[i] = new Player(civ, customLeaderName, customTribeName, customTribeNamePlural);
				if (occurrence == 0)
				{
					// Only the first player assigned a given civilization claims its fixed start position;
					// later reuses fall back to normal scored/random placement in AddStartingUnits to avoid
					// two players colliding on the same starting tile.
					ApplyMapStartPosition(_players[i]);
				}
				if (i != 0)
				{
					// fire-eggs 20190730 never show "barbarian civilization destroyed"
					_players[i].Destroyed += PlayerDestroyed;
				}
				Log("- Player {0} is {1} of the {2}", i, _players[i].LeaderName, _players[i].TribeNamePlural);
			}

			Debug.Assert(HumanPlayer != null, "NewGame invariant violated: HumanPlayer must be initialized during player setup.");
			if (string.IsNullOrWhiteSpace(SaveMetaData.DisplayName))
			{
				SaveMetaData.DisplayName = _saveMetaDataService.BuildDisplayName(difficulty, HumanPlayer, 0);
			}

			Log("Adding starting units...");
			PlaceStartingUnits([.. Enumerable.Range(1, competition).Select(i => (byte)i)]);

			Log("Calculate players handicap...");
			for (byte i = 1; i <= competition; i++)
			{
				CalculateHandicap(i);
			}

			Log("Apply players bonus...");
			for (byte i = 1; i <= competition; i++)
			{
				ApplyBonus(i);
			}

			GameTurn = 0;

			// Number of turns to next anthology needs to be checked
			_anthologyTurn = (ushort)RandomServiceFactory.Create().NextInt(1, 128);

			InitGlobalWarmingServices();
		}
	}
}