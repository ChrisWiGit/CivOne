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
using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.IO;
using CivOne.Screens;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	public partial class Game : BaseInstance, IGame, ILogger
	{
		private readonly int _difficulty, _competition;
		private readonly Player[] _players;
		private readonly List<City> _cities;
		private readonly List<IUnit> _units;
		private readonly Dictionary<byte, byte> _advanceOrigin = new Dictionary<byte, byte>();
		private readonly List<ReplayData> _replayData = new List<ReplayData>();

		internal readonly string[] CityNames = Common.AllCityNames.ToArray();

		public int _currentPlayer = 0; // public for unit testing
		private int _activeUnit;

		private ushort _anthologyTurn = 0;

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
			if (_advanceOrigin.ContainsKey(advance.Id))
				return (_advanceOrigin[advance.Id] == PlayerNumber(player));
			return false;
		}

		public int Difficulty => _difficulty;

		public bool HasUpdate => false;

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
					_anthologyTurn = (ushort)(_gameTurn + 20 + Common.Random.Next(40));
				}
			}
		}

		internal string GameYear => Common.YearString(GameTurn);

		internal Player HumanPlayer { get; set; }

		internal Player CurrentPlayer => _players[_currentPlayer];

		internal ReplayData[] GetReplayData() => _replayData.ToArray();
		internal T[] GetReplayData<T>() where T : ReplayData => _replayData.Where(x => x is T).Select(x => (x as T)).ToArray();

		private void PlayerDestroyed(object sender, EventArgs args)
		{
			Player player = (sender as Player);

			ICivilization destroyed = player.Civilization;
			ICivilization destroyedBy = Game.CurrentPlayer.Civilization;
			if (destroyedBy == destroyed) destroyedBy = Game.GetPlayer(0).Civilization;

			_replayData.Add(new ReplayData.CivilizationDestroyed(_gameTurn, destroyed.PreferredPlayerNumber, destroyedBy.PreferredPlayerNumber));

			if (player.IsHuman)
			{
				// TODO: Move Game Over code here
				return;
			}

			GameTask.Insert(Message.Advisor(Advisor.Defense, false, destroyed.Name, "civilization", "destroyed", $"by {destroyedBy.NamePlural}!"));
		}

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

		public Player GetPlayer(byte number)
		{
			if (_players.Length < number)
				return null;
			return _players[number];
		}

		internal IEnumerable<Player> Players => _players;

		public void EndTurn()
		{
			foreach (Player player in _players.Where(x => !(x.Civilization is Barbarian)))
			{
				player.IsDestroyed();
			}

			if (++_currentPlayer >= _players.Length)
			{
				_currentPlayer = 0;
				GameTurn++;
				if (GameTurn % 50 == 0 && AutoSave)
				{
					GameTask.Enqueue(Show.AutoSave);
				}

				IEnumerable<City> disasterCities = _cities.OrderBy(o => Common.Random.Next(0, 1000)).Take(2).AsEnumerable();
				foreach (City city in disasterCities)
					city.Disaster();

				if (Barbarian.IsSeaSpawnTurn)
				{
					// KBR 20200927 use cdonges land spawn code
					// https://github.com/cdonges/CivOne/commit/e54fe9377030de625c51b674c0ecf29a335e0556
					// TODO land spawning and sea spawning as separate timing / acts
					if (Common.Random.Next(100) > 50)
					{
						ITile tile = Barbarian.LandSpawnPosition;
						if (tile != null)
						{
							foreach (UnitType unitType in Barbarian.LandSpawnUnits)
								CreateUnit(unitType, tile.X, tile.Y, 0, false);
						}
					}
					else
					{
						ITile tile = Barbarian.SeaSpawnPosition;
						if (tile != null)
						{
							foreach (UnitType unitType in Barbarian.SeaSpawnUnits)
								CreateUnit(unitType, tile.X, tile.Y, 0, false);
						}
					}
				}
			}

			if (!_players.Any(x => Game.PlayerNumber(x) != 0 && x != Human && !x.IsDestroyed()))
			{
				PlaySound("wintune");

				GameTask conquest;
				GameTask.Enqueue(Message.Newspaper(null, "Your civilization", "has conquered", "the entire planet!"));
				GameTask.Enqueue(conquest = Show.Screen<Conquest>());
				conquest.Done += (s, a) => Runtime.Quit();
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
			if (Game.InstantAdvice && (Common.TurnToYear(Game.GameTurn) == -3600 || Common.TurnToYear(Game.GameTurn) == -2800))
				GameTask.Enqueue(Message.Help("--- Civilization Note ---", TextFile.Instance.GetGameText("HELP/HELP1")));
			else if (Game.InstantAdvice && (Common.TurnToYear(Game.GameTurn) == -3200 || Common.TurnToYear(Game.GameTurn) == -2400))
				GameTask.Enqueue(Message.Help("--- Civilization Note ---", TextFile.Instance.GetGameText("HELP/HELP2")));
		}

		// store last active player unit to check if a previous player move happened or a game was loaded.
		IUnit LastActivePlayerUnit = null;

		public void Update()
		{
			IUnit unit = ActiveUnit;
			if (CurrentPlayer == HumanPlayer)
			{
				LastActivePlayerUnit = unit != null ? unit : LastActivePlayerUnit;

				if (unit != null && !unit.Goto.IsEmpty)
				{
					ITile[] tiles = (unit as BaseUnit).MoveTargets.OrderBy(x => x.DistanceTo(unit.Goto)).ThenBy(x => x.Movement).ToArray();

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
							if (Common.Random.Next(0, 100) < 50)
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
			int[] used = _cities.Select(c => c.NameId).ToArray();
			int[] available = Enumerable.Range(0, CityNames.Length)
				.Where(i => !used.Contains(i))
				.OrderBy(i => (i >= startIndex && i < startIndex + civilization.CityNames.Length) ? 0 : 1)
				.ThenBy(i => (i >= spareIndex) ? 0 : 1)
				.ThenBy(i => i)
				.ToArray();
			if (player.CityNamesSkipped >= available.Length)
				return 0;

			var nameId = available[player.CityNamesSkipped];
			Log($"AI: {player.LeaderName} of the {player.TribeNamePlural} decides to found {CityNames[nameId]}.");
			return nameId;
		}

		internal City AddCity(Player player, int nameId, int x, int y)
		{
			bool hasCity = _cities.Any(c => c.X == x && c.Y == y);
			if (hasCity)
			{
				return null;
			}

			City city = new City(PlayerNumber(player))
			{
				X = (byte)x,
				Y = (byte)y,
				NameId = nameId,
				Size = 1
			};
			if (!_cities.Any(c => c.Size > 0 && c.Owner == city.Owner))
			{
				Palace palace = new Palace();
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
				_units.Remove(unit);
			city.X = 255;
			city.Y = 255;
			city.Owner = 0;
		}

		internal City GetCity(int x, int y)
		{
			while (x < 0) x += Map.WIDTH;
			while (x >= Map.WIDTH) x -= Map.WIDTH;
			if (y < 0) return null;
			if (y >= Map.HEIGHT) return null;
			return _cities.Where(c => c.X == x && c.Y == y && c.Size > 0).FirstOrDefault();
		}

		private static IUnit CreateUnit(UnitType type, int x, int y)
		{
			IUnit unit = CreateUnit(type);
			unit.X = x;
			unit.Y = y;
			unit.MovesLeft = unit.Move;
			return unit;
		}

		public static IUnit CreateUnit(UnitType type)
		{
			IUnit unit;
			switch (type)
			{
				case UnitType.Settlers: unit = new Settlers(); break;
				case UnitType.Militia: unit = new Militia(); break;
				case UnitType.Phalanx: unit = new Phalanx(); break;
				case UnitType.Legion: unit = new Legion(); break;
				case UnitType.Musketeers: unit = new Musketeers(); break;
				case UnitType.Riflemen: unit = new Riflemen(); break;
				case UnitType.Cavalry: unit = new Cavalry(); break;
				case UnitType.Knights: unit = new Knights(); break;
				case UnitType.Catapult: unit = new Catapult(); break;
				case UnitType.Cannon: unit = new Cannon(); break;
				case UnitType.Chariot: unit = new Chariot(); break;
				case UnitType.Armor: unit = new Armor(); break;
				case UnitType.MechInf: unit = new MechInf(); break;
				case UnitType.Artillery: unit = new Artillery(); break;
				case UnitType.Fighter: unit = new Fighter(); break;
				case UnitType.Bomber: unit = new Bomber(); break;
				case UnitType.Trireme: unit = new Trireme(); break;
				case UnitType.Sail: unit = new Sail(); break;
				case UnitType.Frigate: unit = new Frigate(); break;
				case UnitType.Ironclad: unit = new Ironclad(); break;
				case UnitType.Cruiser: unit = new Cruiser(); break;
				case UnitType.Battleship: unit = new Battleship(); break;
				case UnitType.Submarine: unit = new Submarine(); break;
				case UnitType.Carrier: unit = new Carrier(); break;
				case UnitType.Transport: unit = new Transport(); break;
				case UnitType.Nuclear: unit = new Nuclear(); break;
				case UnitType.Diplomat: unit = new Diplomat(); break;
				case UnitType.Caravan: unit = new Caravan(); break;
				default: return null;
			}
			return unit;
		}

		public IUnit CreateUnit(UnitType type, int x, int y, byte owner, bool endTurn = false)
		{
			IUnit unit = CreateUnit((UnitType)type, x, y);
			if (unit == null) return null;

			unit.Owner = owner;
			if (unit.Class == UnitClass.Water)
			{
				Player player = GetPlayer(owner);
				if ((player.HasWonder<Lighthouse>() && !WonderObsolete<Lighthouse>()) ||
					(player.HasWonder<MagellansExpedition>() && !WonderObsolete<MagellansExpedition>()))
				{
					unit.MovesLeft++;
				}
			}
			if (endTurn)
				unit.SkipTurn();
			_instance._units.Add(unit);
			return unit;
		}

		internal IUnit[] GetUnits(int x, int y)
		{
			while (x < 0) x += Map.WIDTH;
			while (x >= Map.WIDTH) x -= Map.WIDTH;
			if (y < 0) return null;
			if (y >= Map.HEIGHT) return null;
			return _units.Where(u => u.X == x && u.Y == y).OrderBy(u => (u == ActiveUnit) ? 0 : (u.Fortify || u.FortifyActive ? 1 : 2)).ToArray();
		}

		internal IUnit[] GetUnits() => _units.ToArray();

		internal void UpdateResources(ITile tile, bool ownerCities = true)
		{
			for (int relY = -3; relY <= 3; relY++)
				for (int relX = -3; relX <= 3; relX++)
				{
					if (tile[relX, relY] == null) continue;
					City city = tile[relX, relY].City;
					if (city == null) continue;
					if (!ownerCities && CurrentPlayer == city.Owner) continue;
					city.UpdateResources();
				}
		}

		public City[] GetCities() => _cities.ToArray();

		public ReadOnlyCollection<City> Cities { get { return _cities.AsReadOnly(); } }

		public IWonder[] BuiltWonders => _cities.SelectMany(c => c.Wonders).ToArray();

		public bool WonderBuilt<T>() where T : IWonder => BuiltWonders.Any(w => w is T);

		public bool WonderBuilt(IWonder wonder) => BuiltWonders.Any(w => w.Id == wonder.Id);

		public bool WonderObsolete<T>() where T : IWonder, new() => WonderObsolete(new T());

		public bool WonderObsolete(IWonder wonder) => (wonder.ObsoleteTech != null && _players.Any(x => x.HasAdvance(wonder.ObsoleteTech)));

		public void DisbandUnit(IUnit unit)
		{
			if (unit == null)
			{
				return;
			}

			IUnit activeUnit = ActiveUnit;

			if (!_units.Contains(unit)) return;
			if (unit.Tile is Ocean && unit is IBoardable)
			{
				int totalCargo = unit.Tile.Units.Where(u => u is IBoardable).Sum(u => (u as IBoardable).Cargo) - (unit as IBoardable).Cargo;
				while (unit.Tile.Units.Count(u => u.Class != UnitClass.Water) > totalCargo)
				{
					IUnit subUnit = unit.Tile.Units.First(u => u.Class != UnitClass.Water);
					subUnit.X = 255;
					subUnit.Y = 255;
					_units.Remove(subUnit);
				}
			}
			unit.X = 255;
			unit.Y = 255;
			_units.Remove(unit);

			GetPlayer(unit.Owner).IsDestroyed();

			if (_units.Contains(activeUnit))
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
		public IUnit ActiveUnit
		{
			get
			{
				if (_units.Count(u => u.Owner == _currentPlayer && !u.Busy) == 0)
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

				IUnit anyActive = _units.Find(u => u.Owner == _currentPlayer && !u.Busy);
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
				IUnit toActivateUnit = value;

				if (toActivateUnit == null)
				{
					return;
				}
				// Behavior in Civ1 if a unit is clicked by the player and set as active:
				// - If the unit is doing Sentry or Fortify, it will be cancelled
				// - If the unit is doing Sentry or Fortify and has no moves left, it will not be set as active but it will next round.
				// - If the unit has no moves left, it will not be set as active
				// - If the unit is has an action (e.g. building a road, fortify, sentry, goto), it will not be set as active

				bool isSettlers = toActivateUnit is Settlers;
				bool isAlreadyMoved = toActivateUnit.MovesLeft == 0 && toActivateUnit.PartMoves == 0;
				bool isSentryOrFortify = toActivateUnit.Sentry || toActivateUnit.Fortify || toActivateUnit.FortifyActive;

				if (isSettlers)
				{
					// Cancel order if settler is set active
					var settler = toActivateUnit as Settlers;
					settler.ResetOrder();
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

		public IUnit MovingUnit => _units.FirstOrDefault(u => u.Moving);

		public static bool Started => (_instance != null);


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

		private static Game _instance;
		public static Game Instance
		{
			get
			{
				if (_instance == null)
				{
					BaseInstance.Log("ERROR: Game instance does not exist");
				}
				return _instance;
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