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
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Model;
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

		private readonly List<ICivilization> _unplacedCivilizations = [];

		/// <summary>
		/// Civilizations that could not be placed on the map and were destroyed during game creation.
		/// Read once by the new-game flow to inform the player. Empty for any normal map.
		/// </summary>
		public IReadOnlyList<ICivilization> UnplacedCivilizations => _unplacedCivilizations;

		// Resolved lazily and then cached: the factory reads Settings, so it must not run at construction time,
		// but it also shouldn't create a new service on every access.
		private IStartPositionService? _startPositionService;
		private IStartPositionService StartPositionService => _startPositionService ??= StartPositionServiceFactory.Create();

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
				if (result.Success && TryPlaceStartingSettlers(player, (int)result.Position.X, (int)result.Position.Y, true))
				{
					continue;
				}

				Log("PlaceStartingUnits: no usable starting position for player {0}; trying last-resort placement.", player);
				if (TryLastResortPlacement(player, context))
				{
					continue;
				}

				// The map has no free land tile at all (e.g. a degenerate custom map): don't abort game
				// creation over one civilization. Mark it destroyed outright instead of leaving a
				// "phantom" player with zero units and zero cities floating around.
				//
				// The Destroyed event is deliberately suppressed. We are still inside the Game
				// constructor here, and PlayerDestroyed would:
				//   - build an AdvisorMessage screen right away (portrait, palette, fonts), pulling the
				//     graphics subsystem into game construction, and queue it as a GameTask that the
				//     runtime pops on the next tick - while the new-game intro screen is still up and
				//     GamePlay does not exist yet, so the popup would appear over the intro;
				//   - respawn the civilization and call PlaceStartingUnits again from inside this very
				//     loop, which fails the same way on a landless map.
				// The replay entry is therefore written directly below.
				Log("PlaceStartingUnits: no free land tile left for player {0}; player will be destroyed.", player);
				_unplacedCivilizations.Add(_players[player].Civilization);
				_players[player].HandleExtinction(invokeDestroyedEvent: false);

				// The player slot index, not Civilization.PreferredPlayerNumber: with civilization reuse beyond
				// player 7 the latter no longer identifies the slot, and every reader of DestroyedId
				// (Player.AllowedToRespawn, Conquest) expects the slot index.
				// Attributed to the Barbarians (player 0): nobody actually defeated this civilization,
				// the map simply had no room for it.
				_replayData.Add(new ReplayData.CivilizationDestroyed(_gameTurn, player, 0));
			}
		}

		/// <summary>
		/// Places the starting Settlers on any free land tile, ignoring the regular placement rules.
		/// Used when the starting-position service could not satisfy them, so a player is never left
		/// without units while the map still has usable land.
		/// </summary>
		/// <param name="player">The player index to place Settlers for.</param>
		/// <param name="context">The context of the current placement batch, reused for its map reference.</param>
		/// <returns>True if Settlers were placed; otherwise, false.</returns>
		private bool TryLastResortPlacement(byte player, StartPositionContext context)
		{
			// Occupied tiles are re-read here instead of taken from the context, because units placed
			// earlier in this same batch are already on the map.
			MapLocation[] occupiedTiles = [.. _units.Select(u => new MapLocation((uint)u.X, (uint)u.Y))];
			MapLocation? tile = new FallbackTileScanDelegate(context).FindAnyUsableTile(occupiedTiles);
			if (tile == null)
			{
				return false;
			}

			if (!TryPlaceStartingSettlers(player, (int)tile.X, (int)tile.Y, true))
			{
				return false;
			}

			Log("PlaceStartingUnits: last-resort placement for player {0} at {1},{2}.", player, tile.X, tile.Y);
			return true;
		}

		/// <summary>
		/// Terrain editor map start positions.
		/// Only looks up and stores a custom starting position for the player, if the map has one for their civilization
		/// (e.g. painted in the terrain editor). Does not place any units. <see cref="PlaceStartingUnits"/> is what
		/// actually creates the Settlers, using <see cref="Player.MapStartPosition"/> set here as one of its inputs.
		/// </summary>
		private static void ApplyMapStartPositionFromMapFile(Player player)
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

		/// <summary>
		/// The smallest number of non-barbarian player slots a new game can be created with:
		/// the human player plus at least one opponent.
		/// A game without an opponent would satisfy the "conquered the entire planet" condition on the very
		/// first end of turn, which queues the conquest screen over the map before the player can move.
		/// </summary>
		internal const int MinCompetition = 2;

		/// <summary>
		/// The largest number of non-barbarian player slots a new game can be created with.
		/// Slot 0 belongs to the barbarians, so this is one less than the number of player slots.
		/// </summary>
		internal const int MaxCompetition = MaxPlayers - 1;

		/// <summary>
		/// Validates the number of non-barbarian player slots (human player plus AI opponents) a new game
		/// is created with.
		/// The New Game menu asks for the number of opponents and adds the human player itself, so a value
		/// arriving here is always "opponents + 1".
		/// </summary>
		/// <param name="competition">The number of non-barbarian player slots.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The value is below <see cref="MinCompetition"/> or above <see cref="MaxCompetition"/>.
		/// </exception>
		private static void ValidateCompetition(int competition)
		{
			if (competition < MinCompetition || competition > MaxCompetition)
			{
				BaseInstance.Log("ERROR: Invalid competition {0}. Expected {1}-{2} non-barbarian players (human player plus {3}-{4} opponents).",
					competition, MinCompetition, MaxCompetition, MinCompetition - 1, MaxCompetition - 1);
				throw new ArgumentOutOfRangeException(nameof(competition),
					$"Competition must be between {MinCompetition} and {MaxCompetition} (human player plus opponents).");
			}
		}

		public static void CreateGame(int difficulty, int competition, ICivilization tribe, string? leaderName = null, string? tribeName = null, string? tribeNamePlural = null, bool replaceExisting = false)
		{
			ValidateCompetition(competition);

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

		private static byte ResolveHumanPlayerIndex(int competition, ICivilization tribe)
		{
			ArgumentNullException.ThrowIfNull(tribe);

			ValidateCompetition(competition);

			if (tribe.PreferredPlayerNumber == 0)
			{
				// Slot 0 is the barbarian slot: it gets no starting units and never ends its turn like a
				// regular player, so a human player placed there could never move.
				throw new ArgumentException("The chosen civilization has no player slot of its own (it is the barbarian civilization).", nameof(tribe));
			}

			if (tribe.PreferredPlayerNumber <= competition)
			{
				return tribe.PreferredPlayerNumber;
			}

			// For low player counts where the chosen civilization's preferred slot is not present,
			// place the human in the highest available non-barbarian slot.
			return (byte)competition;
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
			byte humanPlayerIndex = ResolveHumanPlayerIndex(competition, tribe);
			// competition counts the non-barbarian players, so the human player is one of them and the
			// remaining slots are the AI opponents. Slot 0 is the barbarian player and is not counted.
			Log("Player setup: {0} civilizations (1 human player + {1} opponents), plus barbarians. Human player slot: {2}",
				competition, competition - 1, humanPlayerIndex);

			CivilizationAssignment assignment = CivilizationAssignment.Create(Common.Random!.InitialSeed, competition, humanPlayerIndex, tribe);
			CivilizationNameDelegate civilizationNames = new();
			Dictionary<int, int> civIdOccurrences = [];

			for (int i = 0; i <= competition; i++)
			{
				if (i == humanPlayerIndex)
				{
					_players[i] = new Player(tribe, leaderName, playerTribeName, playerTribeNamePlural);
					ApplyMapStartPositionFromMapFile(_players[i]);
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
				CivilizationNames names = civilizationNames.Build(civ, occurrence);

				_players[i] = new Player(civ, names.LeaderName, names.TribeName, names.TribeNamePlural);
				if (occurrence == 0)
				{
					// Only the first player assigned a given civilization claims its fixed start position;
					// later reuses fall back to normal scored/random placement in AddStartingUnits to avoid
					// two players colliding on the same starting tile.
					ApplyMapStartPositionFromMapFile(_players[i]);
				}
				if (i != 0)
				{
					// fire-eggs 20190730 never show "barbarian civilization destroyed"
					_players[i].Destroyed += PlayerDestroyed;
				}
				Log("- Player {0} is {1} of the {2}", i, _players[i].LeaderName, _players[i].TribeNamePlural);
			}

			// Checked rather than asserted: a Debug.Assert is removed in release builds, and a game without a
			// human player looks like a running game while accepting no input at all.
			if (HumanPlayer == null || PlayerNumber(HumanPlayer) != humanPlayerIndex || humanPlayerIndex == 0)
			{
				throw new InvalidOperationException($"New game invariant violated: the human player must hold a non-barbarian slot (expected slot {humanPlayerIndex}).");
			}

			if (string.IsNullOrWhiteSpace(SaveMetaData.DisplayName))
			{
				SaveMetaData.DisplayName = _saveMetaDataService.BuildDisplayName(difficulty, HumanPlayer, 0);
			}

			Log("Adding starting units...");
			PlaceStartingUnits([.. Enumerable.Range(1, competition).Select(i => (byte)i)]);

			// Without a unit the human player has nothing to activate, so the map would come up with no
			// blinking unit and no way to end the turn. PlaceStartingUnits already logs why a slot stayed
			// empty; this makes the consequence for the human player explicit.
			if (!_units.Any(unit => unit.Owner == humanPlayerIndex))
			{
				Log("ERROR: The human player (slot {0}) has no starting unit. The map has no usable start position left.", humanPlayerIndex);
			}

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