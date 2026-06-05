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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.IO;
using CivOne.IO.Text;
using CivOne.Screens;
using CivOne.Screens.Reports;
using CivOne.Screens.Services;
using CivOne.Services;
using CivOne.Services.EndGame;
using CivOne.Services.GlobalWarming;
using CivOne.Services.Palace;
using CivOne.Services.Random;
using CivOne.Services.SpaceShip;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	public partial class Game : BaseInstance, IGame, ILogger, IGameCitizenDependency, ISveSaveCompatibilityProvider
	{
		private static readonly string GameVersion = GetGameVersion();
		private readonly IRandomService _randomService = RandomServiceFactory.Create();

		private readonly int _difficulty, _competition;
		private readonly Player[] _players;
		private readonly List<City> _cities;
		private readonly List<IUnit> _units;
		private readonly Dictionary<byte, byte> _advanceOrigin = [];
		private readonly List<ReplayData> _replayData = [];

		internal readonly string[] CityNames = [.. Common.AllCityNames];

		private int _currentPlayer;

		public void SetCurrentPlayerForTesting(int currentPlayer) => _currentPlayer = currentPlayer;
		
		private int _activeUnit;

		private ushort _anthologyTurn;
		private ushort _peaceTurns;
		private ushort _playerFutureTech;
		private bool _hostileActionOccurred;
		private bool _loadedFromYamlSaveSource;
		private (short X, short Y) _pendingMapPositionRestore = (-1, -1);

		/// <summary>
		/// The metadata for the current save file, which is initialized when starting a new game or loading an existing game, and updated when saving a game.
		/// This is the real structure used by the game. SaveFileMetaDataDto is only used for serialization and should not be used in the game logic.
		/// </summary>
		public SaveFileMetaData SaveMetaData { get; } = new();

		private readonly SaveMetaDataService _saveMetaDataService = new(GameVersion);

		/// <summary>
		/// This service provides methods for creating and managing save game metadata, which is used for display in the load game dialog and for informational purposes in the game.
		/// Use this service only for SaveMetaData.
		/// </summary>
		public SaveMetaDataService SaveMetaDataService => _saveMetaDataService;

		public int Competition => _competition;

		private static string GetGameVersion()
			=> Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

		public bool Animations { get; set; }
		public bool Sound { get; set; }
		public bool CivilopediaText { get; set; }
		public bool EndOfTurn { get; set; }
		public bool InstantAdvice { get; set; }
		public bool AutoSave { get; set; }
		public bool EnemyMoves { get; set; }
		public bool Palace { get; set; }

		public void SetAdvanceOrigin(IAdvance advance, Player player)
		{
			if (_advanceOrigin.ContainsKey(advance.Id))
				return;
			byte playerNumber = 0;
			if (player != null)
				playerNumber = PlayerNumber(player);
			_advanceOrigin.Add(advance.Id, playerNumber);
		}
		public bool GetAdvanceOrigin(IAdvance advance, Player player)
		{
			if (_advanceOrigin.TryGetValue(advance.Id, out byte value))
				return value == PlayerNumber(player);
			return false;
		}

		public int Difficulty => Math.Clamp(_difficulty, 0, MaxDifficulty);
		
		/*
		 * Chieftain = 0 (easiest)
		 * Warlord = 1
		 * Prince = 2
		 * King = 3
		 * Emperor = 4 (hardest in original Civ1)
		 * Deity = 5 (hardest)
		 */
		public int MaxDifficulty {
			get { return Settings.Instance.DeityEnabled ? 5 : 4; }
		}

		public static bool HasUpdate => false;

		private ushort _gameTurn;
		public ushort GameTurn
		{
			get
			{
				return _gameTurn;
			}
			set
			{
				_gameTurn = value;
				Log($"Turn {_gameTurn}: {GameYear}");
				if (_anthologyTurn >= _gameTurn)
				{
					//TODO: Show anthology
					_anthologyTurn = (ushort)(_gameTurn + 20 + _randomService.NextInt(40));
				}
			}
		}

		internal string GameYear => Common.YearString(GameTurn);

		internal bool IsYamlSaveSource => _loadedFromYamlSaveSource;

		internal bool TryConsumePendingMapPositionRestore(out int x, out int y)
		{
			x = -1;
			y = -1;

			if (_pendingMapPositionRestore.X < 0 || _pendingMapPositionRestore.Y < 0)
			{
				return false;
			}

			x = _pendingMapPositionRestore.X;
			y = _pendingMapPositionRestore.Y;
			_pendingMapPositionRestore = (-1, -1);
			return true;
		}

		internal void MarkAsYamlSaveSource()
		{
			_loadedFromYamlSaveSource = true;
		}

		internal SveSaveCompatibilityResult GetSveSaveCompatibility()
		{
			var cityLookup = _cities.ToHashSet();
			var sveUnitOwners = _players
				.SelectMany((player, index) => Enumerable.Repeat((byte)index, _units.Where(unit => player == unit.Owner).GetUnitData().Count()))
				.ToArray();
			var fortifiedUnitCountsPerCity = _cities
				.Select(city => city.Tile?.Units.Count(unit => unit.Home == city && unit.Fortify) ?? 0)
				.ToArray();
			// CW: TODO simply inject service associated with Game constructor in future if necessary.
			var service = new SveSaveCompatibilityService();
			var snapshot = SveSaveCompatibilitySnapshot.Builder()
				.FromYamlSource(_loadedFromYamlSaveSource)
				.WithPlayerCount(_players.Length)
				.WithMapSize(Map.WIDTH, Map.HEIGHT)
				.WithCityCount(_cities.Count)
				.WithReplayDataLengthBytes(GetSveReplayDataLengthBytes())
				.WithInvalidTradeCityReferences(_cities.Any(city => city.TradingCitiesAsCity.Any(tradingCity => !cityLookup.Contains(tradingCity))))
				.WithInvalidUnitHomeCityReferences(_units.Any(unit => unit.Home != null && !cityLookup.Contains(unit.Home)))
				.WithOutOfBoundsCityCoordinates(_cities.Any(city => city.X >= Map.WIDTH || city.Y >= Map.HEIGHT))
				.WithOutOfBoundsUnitCoordinates(_units.Any(unit => unit.X < 0 || unit.Y < 0 || unit.X >= Map.WIDTH || unit.Y >= Map.HEIGHT))
				.WithOutOfBoundsUnitGotoCoordinates(_units.Any(unit => !unit.Goto.IsEmpty && (unit.Goto.X < 0 || unit.Goto.Y < 0 || unit.Goto.X >= Map.WIDTH || unit.Goto.Y >= Map.HEIGHT)))
				.WithTradeCityCountsPerCity([.. _cities.Select(city => city.TradingCities?.Length ?? 0)])
				.WithCityOwners([.. _cities.Select(city => city.Owner)])
				.WithUnitOwners(sveUnitOwners)
				.WithUnitsCount(sveUnitOwners.Length)
				.WithFortifiedUnitCountsPerCity(fortifiedUnitCountsPerCity)
				.WithFortifiedUnitsCount(fortifiedUnitCountsPerCity.Sum())
				.Build();

			return service.Evaluate(snapshot);
		}

		private int GetSveReplayDataLengthBytes()
		{
			var length = 0;
			foreach (var replayEntry in _replayData)
			{
				switch (replayEntry)
				{
					case ReplayData.CivilizationDestroyed _:
						length += 4;
						break;
				}
			}

			return length;
		}

		SveSaveCompatibilityResult ISveSaveCompatibilityProvider.GetSveSaveCompatibility() => GetSveSaveCompatibility();

		internal Player HumanPlayer { get; set; }

		internal byte HumanPlayerId => PlayerNumber(HumanPlayer);

		internal Player CurrentPlayer => _players[_currentPlayer];

		internal T[] GetReplayData<T>() where T : ReplayData => [.. _replayData.OfType<T>()];

		internal void RegisterFutureTech(Player player)
		{
			ArgumentNullException.ThrowIfNull(player);

			player.FutureTechCount++;
			if (player == HumanPlayer)
			{
				_playerFutureTech = player.FutureTechCount;
			}
		}

		internal void RegisterHostileAction()
		{
			_hostileActionOccurred = true;
		}

		private void PlayerDestroyed(object? sender, EventArgs args)
		{
			if (sender is not Player player) throw new ArgumentException("Sender is not a Player.", nameof(sender));

			ICivilization destroyed = player.Civilization;
			ICivilization destroyedBy = Game.CurrentPlayer.Civilization;
			if (destroyedBy == destroyed) destroyedBy = Game.GetPlayer(0)!.Civilization;

			_replayData.Add(new ReplayData.CivilizationDestroyed(_gameTurn, destroyed.PreferredPlayerNumber, destroyedBy.PreferredPlayerNumber));

			if (player.IsHuman)
			{
				// TODO: Move Game Over code here
				return;
			}

			if (Common.TurnToYear(Game.GameTurn) < 0 &&
				player.AllowedToRespawn(GetReplayData<ReplayData.CivilizationDestroyed>()))
			{
				Player newPlayer = player.Respawn();
				var index = newPlayer.Civilization.PreferredPlayerNumber;

				_players[index] = newPlayer;
				_players[index].Destroyed += PlayerDestroyed;

				AddStartingUnits(index);
				// CW: Not sure, but are these new civs given technology or better units?
				// Could be a feature to advance such a civilization.
				// In higher dificulties, does the civ get more units and techs?
			}

			GameTask.Insert(Message.Advisor(Advisor.Defense, false,
				TranslateFormattedArray("{0}\ncivilization\ndestroyed\nby {1}!", destroyed.Name, destroyedBy.NamePlural)));
		}

		/// <summary>
		/// Returns the player number for the given player, or 0 if the player is not found. 
		/// This method is used for serialization and should not be used in game logic, as it may return 0 for a valid player if the player is not found, which can lead to bugs.
		/// If null is passed as player, 0 is returned, which is also a valid player number, so this method should be used with caution and only for serialization purposes.
		/// </summary>
		/// <param name="player">The player for which to get the player number.</param>
		/// <returns>The player number, or 0 if the player is not found or if null is passed.</returns>
		internal byte PlayerNumber(Player player)
		{
			byte i = 0;
			foreach (Player p in _players)
			{
				if (p == player)
					return i;
				i++;
			}
			return 0;
		}

		public Player? GetPlayer(byte number)
		{
			if (number >= _players.Length)
			{
				Debug.Assert(false, $"Player number {number} is out of bounds. Returning null.");
				return null;
			}
			return _players[number];
		}

		internal IEnumerable<Player> Players => _players;


		internal IGlobalWarmingService _globalWarmingService;

		public IGlobalWarmingService GlobalWarmingService => _globalWarmingService;
		internal IGlobalWarmingScourgeService _globalWarmingScourgeService;

		internal readonly IPalaceUpgradeService _palaceUpgradeService;
		internal readonly ICivilizationRankingTriggerService _civilizationRankingTriggerService;

		private readonly struct PlayerGameStateAdapter(Player player) : IPlayerGameState
		{
			private readonly Player _player = player;

			public int CivilizationScore => ((Persistence.Game.IPlayer)_player).CivilizationScore;
			public IPalaceData Palace => _player.Palace;
			public bool IsHuman => _player.IsHuman;
		}

		/// <summary>
		/// End the current player's turn and advance to the next player.
		/// There are multiple references to this method in various places.
		/// This method is called several times (2 current round + 1 on next round) 
		/// and thus events may be triggered multiple times,
		/// so an "origin" parameter is added to identify the origin of the call.
		/// </summary> 
		/// <param name="origin">The origin of the turn end request.</param>
		public void EndTurn(int origin)
		{
			foreach (Player player in _players.Where(x => x.Civilization is not Barbarian))
			{
				player.HandleExtinction();
			}

			if (++_currentPlayer >= _players.Length)
			{
				_currentPlayer = 0;
				GameTurn++;
				AdvancePeaceTurns();
				if (AutoSave)
				{
					var sveCompatibility = GetSveSaveCompatibility();
					if (Settings.Instance.PreferSveSaveFormat)
					{
						if (sveCompatibility.CanSaveAsSve)
						{
							if (GameTurn % 50 == 0)
							{
								GameTask.Enqueue(Show.AutoSave);
							}
						}
						else if (GameTurn % 50 == 0)
						{
							Log("SVE autosave unavailable: {0}. Falling back to COS autosave.", sveCompatibility.Reason);
							SaveCosAutoSave();
						}
					}
					else
					{
						SaveCosAutoSave();
					}
				}

				IEnumerable<City> disasterCities = _cities.OrderBy(o => _randomService.NextInt(0, 1000)).Take(2).AsEnumerable();
				foreach (City city in disasterCities)
					city.Disaster();

				if (Barbarian.IsSeaSpawnTurn)
				{
					// KBR 20200927 use cdonges land spawn code
					// https://github.com/cdonges/CivOne/commit/e54fe9377030de625c51b674c0ecf29a335e0556
					// TODO land spawning and sea spawning as separate timing / acts
					if (_randomService.NextInt(100) > 50)
					{
						ITile? tile = Barbarian.LandSpawnPosition;
						if (tile != null)
						{
							foreach (UnitType unitType in Barbarian.LandSpawnUnits)
								CreateUnit(unitType, tile.X, tile.Y, 0, false);
						}
					}
					else
					{
						ITile? tile = Barbarian.SeaSpawnPosition;
						if (tile != null)
						{
							foreach (UnitType unitType in Barbarian.SeaSpawnUnits)
								CreateUnit(unitType, tile.X, tile.Y, 0, false);
						}
					}
				}
			}

			if (!_players.Any(x => Game.PlayerNumber(x) != 0 && x != Human && !x.HandleExtinction()))
			{
				PlaySound("wintune");

				GameTask conquest;
				GameTask.Enqueue(Message.Newspaper(null, TranslateArray("Your civilization\nhas conquered\nthe entire planet!")));
				GameTask.Enqueue(conquest = Show.Screen<Conquest>());
				conquest.Done += (_, __) => _ = EndGameServiceFactory.CreateForHuman().HandleConquestAsync();
			}

			bool gameEnds = !CheckSpaceVitory();
			if (gameEnds)
			{
				return;
			}

			if (origin == 0)
			{
				HandleGlobalWarming();
			}

			// Palace upgrade trigger check for all players (trigger evaluation is player-independent)
			if (_palaceUpgradeService != null)
			{
				foreach (Player player in _players.Where(x => x.Civilization is not Barbarian))
				{
					if (_palaceUpgradeService.ShouldShowPalaceUpgrade(new PlayerGameStateAdapter(player)))
					{
						// Enqueue show-palace action only for human player (AI palace upgrades handled separately in future)
						if (player.IsHuman)
						{
							GameTask.Enqueue(Show.BuildPalace());
						}
					}
				}
			}

			foreach (IUnit unit in _units.Where(u => u.Owner == _currentPlayer))
			{
				GameTask.Enqueue(Turn.New(unit));
			}
			foreach (City city in _cities.Where(c => c.Owner == _currentPlayer).ToArray())
			{
				GameTask.Enqueue(Turn.New(city));
			}
			GameTask.Enqueue(Turn.New(CurrentPlayer));

			if (CurrentPlayer == HumanPlayer && _civilizationRankingTriggerService?.ShouldShowRanking(HumanPlayer, this) == true)
			{
				GameTask.Enqueue(Show.Screen(CivilizationRankingScreenFactory.Create()));
			}

			if (Game.InstantAdvice && CurrentPlayer == HumanPlayer && (Common.TurnToYear(Game.GameTurn) == -3600 || Common.TurnToYear(Game.GameTurn) == -2800))
				GameTask.Enqueue(Message.Help(Translate("--- Civilization Note ---"), TextFileFactory.Get().GetGameText("HELP/HELP1")));
			else if (Game.InstantAdvice && CurrentPlayer == HumanPlayer && (Common.TurnToYear(Game.GameTurn) == -3200 || Common.TurnToYear(Game.GameTurn) == -2400))
				GameTask.Enqueue(Message.Help(Translate("--- Civilization Note ---"), TextFileFactory.Get().GetGameText("HELP/HELP2")));
		}

		private bool CheckSpaceVitory()
		{
			int currentYear = Common.TurnToYear(GameTurn);
			foreach (Player player in _players.Where(x => x.Civilization is not Barbarian && x.SpaceShipLaunchYear != 0))
			{
				ISpaceShipService shipService = SpaceShipServiceFactoryProvider.GetInstance().Create(player);
				SpaceShipScreenData screenData = shipService.GetScreenData();
				int arrivalYear = player.SpaceShipLaunchYear + (int)Math.Ceiling(screenData.FlightTimeYears);
				if (currentYear < arrivalYear)
				{
					continue;
				}

				if (player == HumanPlayer)
				{
					PlaySound("wintune");

					GameTask.Enqueue(Message.Newspaper(null, TranslateArray("Your civilization\nhas reached\nAlpha Centauri!")));
					_ = EndGameServiceFactory.CreateForHuman().HandleAlphaCentauriAsync();
				}
				else
				{
					GameTask.Enqueue(Message.Newspaper(null, TranslateFormattedArray("{0} space ship\nhas reached\nAlpha Centauri!", player.TribeName)));
					_ = EndGameServiceFactory.CreateForHuman().HandleDefeatAsync();
				}

				return false;
			}

			return true;
		}

		protected void HandleGlobalWarming()
		{
			if (!_globalWarmingService.IsGlobalWarmingOnNewTurn())
			{
				return;
			}
			
			_globalWarmingScourgeService.UnleashScourgeOfPollution();
			_globalWarmingService.RefreshPollutionState();

			GameTask.Enqueue(Message.Newspaper(null, TranslateArray("Global temperature\nrises! Icecaps melt.\nSevere Drought.")));
		}

		private void AdvancePeaceTurns()
		{
			if (_hostileActionOccurred)
			{
				_peaceTurns = 0;
				_hostileActionOccurred = false;
				return;
			}

			if (_peaceTurns < ushort.MaxValue)
			{
				_peaceTurns++;
			}
		}

		private void SaveCosAutoSave()
		{
			SaveGamePathProvider pathProvider = new(RuntimeHandler.Runtime, Settings.Instance);
			string saveDirectory = pathProvider.EnsureAutoSaveDirectory();
			string autoSaveFile = Path.Combine(saveDirectory, "autosave.cos");
			new YamlSaveGameService(this).SaveCos(autoSaveFile);
		}

		// store last active player unit to check if a previous player move happened or a game was loaded.
		IUnit LastActivePlayerUnit;

		public void Update()
		{
			IUnit? unit = ActiveUnit;
			if (CurrentPlayer == HumanPlayer)
			{
				LastActivePlayerUnit = unit ?? LastActivePlayerUnit;

				if (unit != null && !unit.Goto.IsEmpty)
				{
					ITile[] tiles = [.. (unit as BaseUnit)!.MoveTargets.OrderBy(x => x.DistanceTo(unit.Goto)).ThenBy(x => x.Movement)];

					if (Settings.Instance.PathFinding)
					{
						/*  Use AStar  */
						AStar.sPosition Destination, Pos;
						Destination.iX = unit.Goto.X;
						Destination.iY = unit.Goto.Y;
						Pos.iX = unit.X;
						Pos.iY = unit.Y;

						if (Destination.iX == Pos.iX && Destination.iY == Pos.iY)
						{
							unit.Goto = Point.Empty;   // eh... never mind
							return;
						}
						AStar AStar = new AStar();
						AStar.sPosition NextPosition = AStar.FindPath(Destination, unit);
						if (NextPosition.iX < 0)
						{         // if no path found
							unit.Goto = Point.Empty;
							return;
						}
						unit.MoveTo(NextPosition.iX - Pos.iX, NextPosition.iY - Pos.iY);
						return;

					}
					else
					{

						int distance = unit.Tile.DistanceTo(unit.Goto);
						if (tiles.Length == 0 || tiles[0].DistanceTo(unit.Goto) > distance)
						{
							// No valid tile to move to, cancel goto
							unit.Goto = Point.Empty;
							return;
						}
						else if (tiles[0].DistanceTo(unit.Goto) == distance)
						{
							// Distance is unchanged, 50% chance to cancel goto
							if (_randomService.NextInt(0, 100) < 50)
							{
								unit.Goto = Point.Empty;
								return;
							}
						}
					}

					unit.MoveTo(tiles[0].X - unit.X, tiles[0].Y - unit.Y);
					return;
				}

				if (!EndOfTurn && ActiveUnit == null && LastActivePlayerUnit != null)
				{
					// checking LastActivePlayerUnit allows loading a game without automatically ending the turn
					// i.e. the year does not change right after loading a game
					if (LetPlayerContinueMovingAfterEndOfTurn())
					{
						return;
					}
				}

				return;
			}
			if (unit != null && (unit.MovesLeft > 0 || unit.PartMoves > 0))
			{
				GameTask.Enqueue(Turn.Move(unit));
				return;
			}



			GameTask.Enqueue(Turn.End());
		}

		/// <summary>
		/// Checks if a player can continue moving after the end of turn.
		/// In Civ1, you can disable "End of Turn" in the options menu, which allows you to continue moving 
		/// without hitting Return all the time.
		/// </summary>
		/// <returns></returns>
		internal bool LetPlayerContinueMovingAfterEndOfTurn()
		{
			// Goto-Units are not allowed to continue moving after the end of turn. Tested with original Civ1.
			bool playerHasUnitsToMove = _units
						.Where(u => u.Owner == _currentPlayer)
						.Where(u => !u.HasAction)
						.Any(u => !u.HasMovesLeft);
			bool notAlreadyEndingTurn = !GameTask.Is<Tasks.Turn>();

			if (playerHasUnitsToMove && notAlreadyEndingTurn)
			{
				GameTask.Enqueue(Turn.End());

				return true;
			}
			return false;
		}

		internal int CityNameId(Player player)
		{
			ICivilization civilization = player.Civilization;
			ICivilization[] civilizations = Common.Civilizations;
			int startIndex = Enumerable.Range(1, civilization.Id - 1).Sum(i => civilizations[i].CityNames.Length);
			int spareIndex = Enumerable.Range(1, Common.Civilizations.Length - 1).Sum(i => civilizations[i].CityNames.Length);
			int[] used = [.. _cities.Select(c => c.NameId)];
			int[] available = [.. Enumerable.Range(0, CityNames.Length)
				.Where(i => !used.Contains(i))
				.OrderBy(i => (i >= startIndex && i < startIndex + civilization.CityNames.Length) ? 0 : 1)
				.ThenBy(i => (i >= spareIndex) ? 0 : 1)
				.ThenBy(i => i)];
			if (player.CityNamesSkipped >= available.Length)
				return 0;

			var nameId = available[player.CityNamesSkipped];
			Log($"AI: {player.LeaderName} of the {player.TribeNamePlural} decides to found {CityNames[nameId]}.");
			return nameId;
		}

		internal City? AddCity(Player player, int nameId, int x, int y)
		{
			bool hasCity = _cities.Any(c => c.X == x && c.Y == y);
			if (hasCity)
			{
				return null;
			}

			City city = new(PlayerNumber(player))
			{
				X = (byte)x,
				Y = (byte)y,
				NameId = nameId
			};
			// Order is important here -
			// first explore the tile to reveal it to the player, 
			// then add the city so that the city tile is properly initialized with the explored tile!
			player.Explore(x, y);
			city.Size = 1;

			if (!_cities.Any(c => c.Size > 0 && c.Owner == city.Owner))
			{
				Palace palace = new();
				palace.SetFree();
				city.AddBuilding(palace);
			}
			if ((Map[x, y] is Desert) || (Map[x, y] is Grassland) || (Map[x, y] is Hills) || (Map[x, y] is Plains) || (Map[x, y] is River))
			{
				Map[x, y].Irrigation = true;
			}
			if (!Map[x, y].RailRoad)
			{
				Map[x, y].Road = true;
			}
			_cities.Add(city);
			Game.UpdateResources(city.Tile);
			return city;
		}

		public void DestroyCity(City city)
		{
			foreach (IUnit unit in _units.Where(u => u.Home == city).ToArray())
			{
				unit.SetHome(null);
				_units.Remove(unit);
			}
			city.X = 255;
			city.Y = 255;
			city.Owner = 0;
		}

		internal City? GetCity(int x, int y)
		{
			while (x < 0) x += Map.WIDTH;
			while (x >= Map.WIDTH) x -= Map.WIDTH;
			if (y < 0) return null;
			if (y >= Map.HEIGHT) return null;
			return _cities.FirstOrDefault(c => c.X == x && c.Y == y && c.Size > 0);
		}

		private static IUnit? CreateUnit(UnitType type, int x, int y)
		{
			IUnit? unit = CreateUnit(type);
			if (unit == null) return null;
			unit.X = x;
			unit.Y = y;
			unit.MovesLeft = unit.Move;
			return unit;
		}

		public static IUnit? CreateUnit(UnitType type)
		{
			IUnit? unit = type switch
			{
				UnitType.Settlers => new Settlers(),
				UnitType.Militia => new Militia(),
				UnitType.Phalanx => new Phalanx(),
				UnitType.Legion => new Legion(),
				UnitType.Musketeers => new Musketeers(),
				UnitType.Riflemen => new Riflemen(),
				UnitType.Cavalry => new Cavalry(),
				UnitType.Knights => new Knights(),
				UnitType.Catapult => new Catapult(),
				UnitType.Cannon => new Cannon(),
				UnitType.Chariot => new Chariot(),
				UnitType.Armor => new Armor(),
				UnitType.MechInf => new MechInf(),
				UnitType.Artillery => new Artillery(),
				UnitType.Fighter => new Fighter(),
				UnitType.Bomber => new Bomber(),
				UnitType.Trireme => new Trireme(),
				UnitType.Sail => new Sail(),
				UnitType.Frigate => new Frigate(),
				UnitType.Ironclad => new Ironclad(),
				UnitType.Cruiser => new Cruiser(),
				UnitType.Battleship => new Battleship(),
				UnitType.Submarine => new Submarine(),
				UnitType.Carrier => new Carrier(),
				UnitType.Transport => new Transport(),
				UnitType.Nuclear => new Nuclear(),
				UnitType.Diplomat => new Diplomat(),
				UnitType.Caravan => new Caravan(),
				_ => null
			};
			if (unit == null)
			{
				Debug.Assert(false, $"Unknown unit type: {type}");
			}
			return unit;
		}

		public IUnit? CreateUnit(UnitType type, int x, int y, byte owner, bool endTurn = false)
		{
			IUnit? unit = CreateUnit(type, x, y);
			if (unit == null) return null;

			unit.Owner = owner;
			if (unit.Class == UnitClass.Water)
			{
				Player? player = GetPlayer(owner);
				if (player != null && ((player.HasWonder<Lighthouse>() && !WonderObsolete<Lighthouse>()) ||
					(player.HasWonder<MagellansExpedition>() && !WonderObsolete<MagellansExpedition>())))
				{
					unit.MovesLeft++;
				}
			}
			if (endTurn)
			{
				unit.SkipTurn();
			}
			Instance._units.Add(unit);
			return unit;
		}

		public IUnit[]? GetUnits(int x, int y)
		{
			while (x < 0) x += Map.WIDTH;
			while (x >= Map.WIDTH) x -= Map.WIDTH;
			if (y < 0) return null;
			if (y >= Map.HEIGHT) return null;
			return [.. _units.Where(u => u.X == x && u.Y == y).OrderBy(u => (u == ActiveUnit) ? 0 : (u.Fortify || u.FortifyActive ? 1 : 2))];
		}

		public IUnit[] GetUnits() => [.. _units];

		internal void UpdateResources(ITile tile, bool ownerCities = true)
		{
			for (int relY = -3; relY <= 3; relY++)
			{
				for (int relX = -3; relX <= 3; relX++)
				{
					if (tile[relX, relY] == null) continue;
					City city = tile[relX, relY].City;
					if (city == null) continue;
					if (!ownerCities && CurrentPlayer == city.Owner) continue;
					city.UpdateResources();
				}
			}
		}

		public City[] GetCities() => [.. _cities];

		public ReadOnlyCollection<City> Cities { get { return _cities.AsReadOnly(); } }
		
		/** <summary>
		 * Interface for City collection.
		 * This new property is to avoid exposing the City class directly,
		 * and will be used to refactor code to use ICity instead of City.
		 * </summary> */
		public ReadOnlyCollection<ICity> CitiesInterface { get { 
			return _cities.Cast<ICity>().ToList().AsReadOnly(); } }

		public IWonder[] BuiltWonders => [.. _cities.SelectMany(c => c.Wonders)];

		public bool WonderBuilt<T>() where T : IWonder => BuiltWonders.Any(w => w is T);

		public bool WonderBuilt(IWonder wonder) => BuiltWonders.Any(w => w.Id == wonder.Id);

		public bool WonderObsolete<T>() where T : IWonder, new() => WonderObsolete(new T());

		public bool WonderObsolete(IWonder wonder) => (wonder.ObsoleteTech != null && _players.Any(x => x.HasAdvance(wonder.ObsoleteTech)));

		public void DisbandUnit(IUnit? unit)
		{
			if (unit == null)
			{
				return;
			}

			IUnit? activeUnit = ActiveUnit;

			if (unit == activeUnit)
			{
				activeUnit = null;
			}

			if (!_units.Contains(unit)) return;
			if (unit.Tile is Ocean && unit is IBoardable boardable)
			{
				int totalCargo = unit.Tile.Units.OfType<IBoardable>().Sum(u => u.Cargo) - boardable.Cargo;
				while (unit.Tile.Units.Count(u => u.Class != UnitClass.Water) > totalCargo)
				{
					IUnit subUnit = unit.Tile.Units.First(u => u.Class != UnitClass.Water);
					subUnit.SetHome(null);
					subUnit.X = 255;
					subUnit.Y = 255;
					_units.Remove(subUnit);
				}
			}
			unit.SetHome(null);
			unit.X = 255;
			unit.Y = 255;
			_units.Remove(unit);

			GetPlayer(unit.Owner)?.HandleExtinction();

			if (activeUnit != null && _units.Contains(activeUnit))
			{
				_activeUnit = _units.IndexOf(activeUnit);
			}
		}

		// The currently active unit has been requested to wait. Move the "candidate unit"
		// index forward.
		public void UnitWait() => _activeUnit++;

		/// <summary>
		/// Determine which unit should be the "active" unit. The "_activeUnit" index may
		/// not currently point to a unit belonging to the current player or to a unit
		/// that is not busy. Desires to advance _activeUnit to the NEXT possible unit.
		/// 
		/// Returns: null if the current players units are all busy
		/// </summary>
		public IUnit? ActiveUnit
		{
			get
			{
				if (!_units.Any(u => u.Owner == _currentPlayer && !u.Busy))
					return null;

				// If the unit counter is too high, return to 0
				if (_activeUnit >= _units.Count)
					_activeUnit = 0;

				// Does the current unit still have moves left?
				if (_units[_activeUnit].Owner == _currentPlayer && !_units[_activeUnit].Busy)
					return _units[_activeUnit];

				// Task busy, don't change the active unit
				if (GameTask.Any())
					return _units[_activeUnit];

				IUnit? anyActive = _units.Find(u => u.Owner == _currentPlayer && !u.Busy);
				// Check if any units are still available for this player; used to start "end of turn" for the human
				if (anyActive == null)
				{
					if (CurrentPlayer == HumanPlayer && !EndOfTurn && !GameTask.Any() && (Common.TopScreen is GamePlay))
					{
						GameTask.Enqueue(Turn.End());
					}
					return null;
				}

				// Loop through units to find the NEXT inactive unit belonging to the current player.
				// Since we've successfully passed the "does this player have ANY active units" test,
				// we should find an inactive unit.
				while (_units[_activeUnit].Owner != _currentPlayer ||
					_units[_activeUnit].Busy)
				{
					_activeUnit++;
					if (_activeUnit >= _units.Count)
						_activeUnit = 0;
				}

				return _units[_activeUnit];
			}
			internal set
			{
				IUnit? toActivateUnit = value;

				if (toActivateUnit == null)
				{
					return;
				}
				// Behavior in Civ1 if a unit is clicked by the player and set as active:
				// - If the unit is doing Sentry or Fortify, it will be cancelled
				// - If the unit is doing Sentry or Fortify and has no moves left, it will not be set as active but it will next round.
				// - If the unit has no moves left, it will not be set as active
				// - If the unit is has an action (e.g. building a road, fortify, sentry, goto), it will not be set as active

				bool isAlreadyMoved = toActivateUnit.MovesLeft == 0 && toActivateUnit.PartMoves == 0;
				bool isSentryOrFortify = toActivateUnit.Sentry || toActivateUnit.Fortify || toActivateUnit.FortifyActive;

				if (toActivateUnit is Settlers settlers)
				{
					// Cancel order if settler is set active
					settlers.ResetOrder();
				}

				if (isSentryOrFortify && isAlreadyMoved)
				{
					toActivateUnit.Sentry = false;
					toActivateUnit.Fortify = false;

					return;
				}
				if (isAlreadyMoved || toActivateUnit.HasAction)
				{
					return;
				}

				toActivateUnit.Fortify = false;
				toActivateUnit.Sentry = false;

				_activeUnit = _units.IndexOf(toActivateUnit);
			}
		}

		public IUnit? MovingUnit => _units.FirstOrDefault(u => u.Moving);

		public static bool Started => _instance != null;


		/**
		 * Logging method.
		 * Use this method with ILogger for Dependency Injection.
		 *
		 * @param text The text to log.
		 * @param parameters Optional parameters to format the text with.
		 */
		public new void Log(string text, params object[] parameters)
		{
			BaseInstance.Log(text, parameters);
		}

		private static Game? _instance;
		public static Game Instance
		{
			get
			{
				if (_instance == null)
				{
					BaseInstance.Log("ERROR: Game instance does not exist");
					Debug.Assert(false, "Game instance does not exist. This is a precondition to execute playing the game!");
				}
				return _instance!; // This is a precondition to execute playing the game!
			}
		}

		/// <summary>
		/// Fire-eggs 20190704: for unit testing, reset
		/// </summary>
		internal static void Wipe()
		{
			_instance = null;
		}
	}
}