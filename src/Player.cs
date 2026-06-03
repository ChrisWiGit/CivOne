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
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.Graphics.Sprites;
using CivOne.Persistence.Game;
using CivOne.Persistence.Model;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;
using CivOne.Services.SpaceShip;

using Gov = CivOne.Governments;

namespace CivOne
{
	public partial class Player : BaseInstance, ITurn, IPlayer, IPlayerSpaceRace
    {
		// Dependency injection via IPlayerGame; set by Game on load/new game.
		internal static new IPlayerGame Game;
		private readonly ICivilization _civilization;
		private Guid _playerGuid = Guid.NewGuid();
		private string _tribeName, _tribeNamePlural;

		private readonly bool[,] _explored = new bool[Map.WIDTH, Map.HEIGHT];
		private readonly bool[,] _visible = new bool[Map.WIDTH, Map.HEIGHT];
		private readonly List<byte> _advances = new List<byte>();
		private readonly List<byte> _embassies = new List<byte>();
		/// <summary>
		/// Runtime-only bilateral war state used by the new diplomacy API.
		/// This state is currently not serialized to, loaded from, or reconstructed from
		/// legacy SVE diplomacy flags.
		/// </summary>
		private readonly HashSet<byte> _warWith = new HashSet<byte>();
		/// <summary>
		/// Raw legacy diplomacy bitmask storage (8 targets).
		/// The bit semantics are not fully documented; gameplay war logic does not currently
		/// read or write these flags directly.
		/// </summary>
		private readonly ushort[] _diplomacy = new ushort[8];
		private readonly ushort[] _unitsLost = new ushort[28];
		private readonly ushort[] _unitsDestroyedBy = new ushort[8];
		internal readonly (short X, short Y)[] MapPositions = new (short X, short Y)[9];
		internal readonly string[] MapPositionNames = new string[9];
		internal (short X, short Y) LastMapPosition = (-1, -1);
		private int _mapZoomBasisPoints = MapZoomSettings.DefaultBasisPoints;
		
		private short _anarchy;
		private ushort _epicRanking;
		private ushort _militaryPower;
		private ushort _civilizationScore;
		private short _gold;
		private IAdvance? _currentResearch;

		public event EventHandler? Destroyed;

		internal int CityNamesSkipped;
		internal ushort FutureTechCount { get; set; }
		internal ushort HumanContactTurn { get; set; }

		internal short StartX { get; set; }

		internal SpaceShipComponentType[,] SpaceShipGrid { get; set; } = new SpaceShipComponentType[SpaceShipSlotBlueprintFactoryProvider.CanonicalGridWidth, SpaceShipSlotBlueprintFactoryProvider.CanonicalGridHeight];
		internal ushort SpaceShipPopulation { get; set; }
		internal short SpaceShipLaunchYear { get; set; }
		
		public bool AnarchyDespotism => Game.Started && (Government is Anarchy || Government is Despotism);

		public bool MonarchyCommunist => Game.Started && (Government is Gov.Monarchy || Government is Gov.Communism);

		public bool RepublicDemocratic => Game.Started && (Government is Republic || Government is Gov.Democracy);

		public ICivilization Civilization => _civilization;
		public Guid PlayerGuid => _playerGuid;
		
		public string LeaderName => _civilization.Leader.Name;
		public string TribeName => _tribeName ?? _civilization?.Name ?? "Unknown";
		public string TribeNamePlural => _tribeNamePlural ?? _civilization?.NamePlural ?? "Unknown";

		public byte Handicap { get; internal set; }

		private PalaceData _palace = new();
		public PalaceData Palace => _palace;

		internal AI? AI => IsHuman ? null : AI.Instance(this);
		
		private IGovernment _government = new Despotism();
		public IGovernment Government
		{
			get => _government;
			internal set
			{
				if (value == null) return;
				_government = value;
			}
		}

		private int _luxuriesRate, _taxesRate = 5, _scienceRate = 5;
		internal int MapZoomBasisPoints
		{
			get => _mapZoomBasisPoints;
			set => _mapZoomBasisPoints = MapZoomSettings.NormalizeBasisPoints(value);
		}

		public int LuxuriesRate
		{
			get => _luxuriesRate;
			set
			{
				int diff = _luxuriesRate - value;
				_luxuriesRate = value;
				_scienceRate += diff;
			}
		}
		public int TaxesRate
		{
			get => _taxesRate;
			set
			{
				int diff = _taxesRate - value;
				_taxesRate = value;
				_scienceRate += diff;
			}
		}
		public int ScienceRate => _scienceRate;

		public void Revolt()
		{
			_anarchy = (short)((HasWonder<Pyramids>() && !Game.WonderObsolete<Pyramids>()) ? 0 : 4 - (Game.GameTurn % 4) - 1);
			Government = new Anarchy();
			if (!IsHuman) return;
			GameTask.Enqueue(Message.Newspaper(null, TranslateFormattedArray("The {0} are\nrevolting! Citizens\ndemand new govt.", Game.HumanPlayer.TribeNamePlural)));
		}

		public bool IsHuman => (Game.HumanPlayer == this);

		public virtual City[] Cities => Game.GetCities().Where(c => this == c.Owner && c.Size > 0).ToArray();
		
		/** <summary>
		 * Interface for City collection.
		 * This new property is to avoid exposing the City class directly,
		 * and will be used to refactor code to use ICity instead of City.
		 * </summary> */
		public virtual ICity[] CitiesInterface => Game.GetCities().Where(c => this == c.Owner && c.Size > 0).ToArray();

		public int Population => Cities.Sum(c => c.Population);
		
		public short Gold
		{
			get
			{
				return _gold;
			}
			internal set
			{
				if (value < 0)
				{
					//TODO: Implement sold improvements task
					value = 0;
				}
				if (value > 30000)
					value = 30000;
				_gold = value;
			}
		}

		internal short ScienceCost
		{
			get
			{
				short cost = (short)((Game.Difficulty + 3) * 2 * (_advances.Count + 1) * (Common.TurnToYear(Game.GameTurn) > 0 ? 2 : 1));
				if (cost < 12)
					return 12;
				return cost;
			}
		}
		
		public short Science { get; internal set; }

		public void AddAdvance(IAdvance advance, bool setOrigin = true)
		{
			if (Game.Started && Game.CurrentPlayer.CurrentResearch?.Id == advance.Id)
				GameTask.Enqueue(new TechSelect(Game.CurrentPlayer));
			_advances.Add(advance.Id);
			if (!setOrigin) return;
			Game.SetAdvanceOrigin(advance, this);
		}

		public void DeleteAdvance(IAdvance advance) => _advances.RemoveAll(x => x == advance.Id);
		
		public string LatestAdvance
		{
			get
			{
				if (_advances.Count == 0)
					return "Irrigation";
				return Reflect.GetAdvances().First(a => a.Id == _advances.Last()).TranslatedName;
			}
		}

		public IAdvance[] Advances => _advances.Select(a => Common.Advances.First(x => x.Id == a)).ToArray();
		
		public virtual bool HasAdvance<T>() where T : IAdvance => Advances.Any(a => a is T);

		public virtual bool HasAdvance(IAdvance advance) => (advance == null || Advances.Any(a => a.Id == advance.Id));

		SpaceShipComponentType[,] IPlayerSpaceRace.SpaceShipGrid
		{
			get => SpaceShipGrid;
			set => SpaceShipGrid = value;
		}

		ushort IPlayerSpaceRace.SpaceShipPopulation
		{
			get => SpaceShipPopulation;
			set => SpaceShipPopulation = value;
		}

		short IPlayerSpaceRace.SpaceShipLaunchYear
		{
			get => SpaceShipLaunchYear;
			set => SpaceShipLaunchYear = value;
		}

		bool IPlayerSpaceRace.HasSpaceFlightAdvance() => HasAdvance<SpaceFlight>();

		bool IPlayerSpaceRace.HasPlasticsAdvance() => HasAdvance<Plastics>();

		bool IPlayerSpaceRace.HasRoboticsAdvance() => HasAdvance<Robotics>();

		bool IPlayerSpaceRace.HasApolloProgram() => HasWonder<ApolloProgram>();

		public Player[] Embassies => [.._embassies.Select(e => Game.Players.FirstOrDefault(p => e == Game.PlayerNumber(p))).Where(p => p != null)];

		public bool HasEmbassy(Player player) => _embassies.Any(e => e == Game.PlayerNumber(player));

		public void EstablishEmbassy(Player player)
		{
			byte playerNumber = Game.PlayerNumber(player);
			if (_embassies.Contains(playerNumber)) return;
			_embassies.Add(playerNumber);
		}

		/// <summary>
		/// WARNING! This state is not persisted to, loaded from, or reconstructed from legacy SVE diplomacy flags or YAML data. 
		/// Sets or clears runtime war state against a player number.
		/// This updates only <see cref="_warWith"/> and does not write legacy diplomacy flags.
		/// </summary>
		/// <param name="playerNumber">Target player number.</param>
		/// <param name="atWar">True to set war, false to clear war.</param>
		internal void SetAtWar(byte playerNumber, bool atWar)
		{
			if (Game == null)
			{
				return;
			}

			byte ownPlayerNumber = Game.PlayerNumber(this);
			if (ownPlayerNumber == 0 || playerNumber == 0 || ownPlayerNumber == playerNumber)
			{
				return;
			}

			if (atWar)
			{
				_warWith.Add(playerNumber);
				return;
			}

			_warWith.Remove(playerNumber);
		}

		/// <summary>
		/// WARNING! This state is not persisted to, loaded from, or reconstructed from legacy SVE diplomacy flags or YAML data. 
		/// Returns whether this player is currently at war with <paramref name="player"/>
		/// according to runtime state in <see cref="_warWith"/>.
		/// This does not consult legacy diplomacy flags.
		/// </summary>
		/// <param name="player">Potential enemy player.</param>
		/// <returns>True if runtime war state is set; otherwise false.</returns>
		public bool IsAtWar(Player player)
		{
			if (player == null || Game == null)
			{
				return false;
			}

			byte ownPlayerNumber = Game.PlayerNumber(this);
			byte enemyPlayerNumber = Game.PlayerNumber(player);
			if (ownPlayerNumber == 0 || enemyPlayerNumber == 0 || ownPlayerNumber == enemyPlayerNumber)
			{
				return false;
			}

			return _warWith.Contains(enemyPlayerNumber);
		}

		/// <summary>
		/// WARNING! This state is not persisted to, loaded from, or reconstructed from legacy SVE diplomacy flags or YAML data. 
		/// Declares war symmetrically for both players in runtime state.
		/// Also removes inter-party trading links and shows advisor messages for human-facing cases.
		/// This method does not update legacy diplomacy bit flags in SVE data.
		/// </summary>
		/// <param name="enemy">The enemy player.</param>
		public void DeclareWar(Player enemy)
		{
			ArgumentNullException.ThrowIfNull(enemy);

			if (Game == null)
			{
				return;
			}

			byte ownPlayerNumber = Game.PlayerNumber(this);
			byte enemyPlayerNumber = Game.PlayerNumber(enemy);
			if (ownPlayerNumber == 0 || enemyPlayerNumber == 0 || ownPlayerNumber == enemyPlayerNumber)
			{
				return;
			}

			if (IsAtWar(enemy))
			{
				return;
			}

			SetAtWar(enemyPlayerNumber, true);
			enemy.SetAtWar(ownPlayerNumber, true);
			PurgeTradingCitiesForWar(enemyPlayerNumber, ownPlayerNumber);

			if (Game.HumanPlayer == this)
			{
				GameTask.Enqueue(Message.Advisor(Advisor.Foreign, false, $"You have declared war on {enemy.TribeName}."));
			}
			else if (Game.HumanPlayer == enemy)
			{
				GameTask.Enqueue(Message.Advisor(Advisor.Foreign, false, $"{TribeName} has declared war on us."));
			}
		}

		/// <summary>
		/// Makes peace symmetrically for both players in runtime state.
		/// This method does not update legacy diplomacy bit flags in SVE data.
		/// </summary>
		/// <param name="enemy">The enemy player.</param>
		public void MakePeace(Player enemy)
		{
			ArgumentNullException.ThrowIfNull(enemy);

			if (Game == null)
			{
				return;
			}

			byte ownPlayerNumber = Game.PlayerNumber(this);
			byte enemyPlayerNumber = Game.PlayerNumber(enemy);
			if (ownPlayerNumber == 0 || enemyPlayerNumber == 0 || ownPlayerNumber == enemyPlayerNumber)
			{
				return;
			}

			if (!IsAtWar(enemy))
			{
				return;
			}

			SetAtWar(enemyPlayerNumber, false);
			enemy.SetAtWar(ownPlayerNumber, false);
		}

		/// <summary>
		/// Removes bilateral trading-city links between both war parties.
		/// Uses current TradingCities model and does not touch legacy _tradeRoutes structures.
		/// </summary>
		/// <param name="enemyPlayerNumber">Enemy player number.</param>
		/// <param name="ownPlayerNumber">Own player number.</param>
		private void PurgeTradingCitiesForWar(byte enemyPlayerNumber, byte ownPlayerNumber)
		{
			foreach (City city in Cities)
			{
				city.RemoveTradingCitiesOwnedBy(enemyPlayerNumber);
			}

			foreach (City city in Game.GetCities().Where(city => city.Owner == enemyPlayerNumber && city.Size > 0))
			{
				city.RemoveTradingCitiesOwnedBy(ownPlayerNumber);
			}
		}

		public IAdvance? CurrentResearch
		{
			get => _currentResearch;
			set => _currentResearch = value;
		}

		public IEnumerable<IAdvance> AvailableResearch
		{
			get
			{
				foreach (IAdvance advance in Common.Advances.Where(a => !_advances.Contains(a.Id)))
				{
					if (advance.RequiredTechs.Length > 0 && !advance.RequiredTechs.All(a => _advances.Contains(a.Id))) continue;
					yield return advance;
				}
			}
		}

		public IEnumerable<IGovernment> AvailableGovernments
		{
			get
			{
				bool allGovernments = !Game.WonderObsolete<Pyramids>() && HasWonder<Pyramids>();
				foreach (IGovernment government in Reflect.GetGovernments().Where(g => g.Id > 0))
				{
					if (!allGovernments && !HasAdvance(government.RequiredTech)) continue;
					yield return government; 
				}
			}
		}

		private bool UnitAvailable(IUnit unit)
		{
			// Determine if the unit is obsolete
			if (_advances.Any(a => unit.ObsoleteTech != null && unit.ObsoleteTech.Id == a))
				return false;
			
			// Require Manhattan Project to be built for Nuclear unit
			if ((unit is Nuclear) && !Game.WonderBuilt<ManhattanProject>())
				return false;
			
			// Determine if the unit requires a tech
			if (unit.RequiredTech == null)
				return true;
			
			// Determine if the Player has the required tech
			if (_advances.Any(a => unit.RequiredTech.Id == a))
				return true;
			
			return false;
		}

		private bool BuildingAvailable(IBuilding building)
		{
			// Only allow spaceship to be built if Apollo Program exists
			if ((building is ISpaceShip) && !Game.WonderBuilt<ApolloProgram>())
				return false;

			// Determine if the building requires a tech
			if (building.RequiredTech == null)
				return true;
			
			// Determine if the Player has the required tech
			if (_advances.Any(a => building.RequiredTech.Id == a))
				return true;
			
			return false;
		}

		private bool WonderAvailable(IWonder wonder)
		{
			// Determine if the wonder has already been built
			if (Game.BuiltWonders.Any(w => w.Id == wonder.Id))
				return false;

			// Determine if the building requires a tech
			if (wonder.RequiredTech == null)
				return true;
			
			// Determine if the Player has the required tech
			if (_advances.Any(a => wonder.RequiredTech.Id == a))
				return true;
			
			return false;							
		}

		public virtual bool HasWonderEffect<T>() where T : IWonder, new() => HasWonder<T>() && !Game.WonderObsolete<T>();

		public bool HasWonder<T>() where T : IWonder => Cities.Any(c => c.HasWonder<T>());

		public bool ProductionAvailable(IProduction production)
		{
			if (production is IUnit unit)
				return UnitAvailable(unit);

			if (production is IBuilding building)
				return BuildingAvailable(building);

			if (production is IWonder wonder)
				return WonderAvailable(wonder);

			return true;
		}

		public int Pollution
		{
			get
			{
				int pollution = 0;
				foreach (City city in Cities)
				{
					pollution += city.SmokeStacks;
				}
				return pollution;
			}
		}

		private bool _destroyed; // fire-eggs: hack fix for Issue #68: need to be able set destroyed state on game load


		public bool HandleExtinction(bool invokeDestroyedEvent = true)
		{
			if (this == 0) return false;
			if (_destroyed) return true;

			if (Cities.Length != 0 ||
				Game.GetUnits().Any(unit => this == unit.Owner && unit is Settlers && unit.Home == null))
			{
				return false;
			}


			IUnit? unit;
			do
			{
				unit = Game.GetUnits().FirstOrDefault(x => this == x.Owner);
				Game.DisbandUnit(unit);
			}
			while (unit != null);

			_destroyed = true;

			if (invokeDestroyedEvent)
			{
				Destroyed?.Invoke(this, EventArgs.Empty);
			}

			return true;
		}

		/// <summary>
		/// Returns whether the player is destroyed or not.
		/// It does not invoke the Destroyed event and thus does not show a message.
		/// To do so, use HandleExtinction() instead.
		/// </summary>
		public bool IsDestroyed { get => HandleExtinction(false); }

		public void Explore(int x, int y, int range = 1, bool sea = false)
		{
			ExploreVisibleTiles(x, y, range, sea);
			UpdateHumanContactTurnIfHumanAssetsSeen(x, y, range, sea);
			UpdateVisibleCitySizes();
		}

		private void ExploreVisibleTiles(int x, int y, int range, bool sea)
		{
			_explored[x, y] = true;
			for (int relX = -range; relX <= range; relX++)
			for (int relY = -range; relY <= range; relY++)
			{
				int xx = x + relX;
				int yy = y + relY;
				if (yy < 0 || yy >= Map.HEIGHT) continue;
				while (xx < 0) xx += Map.WIDTH;
				while (xx >= Map.WIDTH) xx -= Map.WIDTH;
				if (sea && !Map[xx, yy].IsOcean && (Math.Abs(relX) > 1 || Math.Abs(relY) > 1))
					continue;
				_visible[xx, yy] = true;
			}
		}

		private void UpdateHumanContactTurnIfHumanAssetsSeen(int x, int y, int range, bool sea)
		{
			if (!ShouldTrackHumanContact(out var humanPlayerId))
			{
				return;
			}

			if (CanSeeHumanAssetsInExploreArea(x, y, range, sea, humanPlayerId))
			{
				HumanContactTurn = Game.GameTurn;
			}
		}

		private bool ShouldTrackHumanContact(out byte humanPlayerId)
		{
			humanPlayerId = 0;
			if (!Game.Started || IsHuman || Game.HumanPlayer == null)
			{
				return false;
			}

			humanPlayerId = Game.PlayerNumber(Game.HumanPlayer);
			return true;
		}

		private static bool CanSeeHumanAssetsInExploreArea(int x, int y, int range, bool sea, byte humanPlayerId)
		{
			for (int relX = -range; relX <= range; relX++)
			for (int relY = -range; relY <= range; relY++)
			{
				int xx = x + relX;
				int yy = y + relY;
				if (yy < 0 || yy >= Map.HEIGHT) continue;
				while (xx < 0) { xx += Map.WIDTH; }
				while (xx >= Map.WIDTH) { xx -= Map.WIDTH; }
				if (sea && !Map[xx, yy].IsOcean && (Math.Abs(relX) > 1 || Math.Abs(relY) > 1))
					continue;

				ITile visibleTile = Map[xx, yy];
				if ((visibleTile.City != null && visibleTile.City.Owner == humanPlayerId) ||
					visibleTile.Units.Any(unit => unit.Owner == humanPlayerId))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// For every enemy city now visible to this player, update its <see cref="City.VisibleSizes"/>
		/// entry so the map shows the correct (last-known) city size under fog-of-war.
		/// </summary>
		private void UpdateVisibleCitySizes()
		{
			if (!Game.Started) return;
			byte playerId = Game.PlayerNumber(this);
			foreach (City city in Game.GetCities())
			{
				if (city.Size == 0) continue; // destroyed city
				if (city.Owner == playerId) continue;
				if (_visible[city.X, city.Y])
					city.VisibleSizes[playerId] = city.Size;
			}
		}

		public bool Visible(int x, int y)
		{
			if (y < 0 || y >= Map.HEIGHT) return false;
			while (x < 0) x += Map.WIDTH;
			while (x >= Map.WIDTH) x -= Map.WIDTH;
			return _visible[x, y];
		}

		public bool Visible(ITile tile)
		{
			if (tile == null) return false;
			return Visible(tile.X, tile.Y);
		}

		public bool Visible(ITile tile, Direction direction)
		{
			if (tile == null) return false;
			return Visible(tile.GetBorderTile(direction));
		}

		public void NewTurn()
		{
			if (!Game.GetCities().Any(x => this == x.Owner) && !Game.GetUnits().Any(x => this == x.Owner))
			{
				GameTask.Enqueue(Turn.GameOver(this));
			}

			if (_anarchy == 0 && Government is Anarchy)
			{
				if (Human == Game.CurrentPlayer)
				{
					GameTask.Enqueue(Show.ChooseGovernment);
				}
				else
				{
					Government = new Despotism();
				}
			}
			if (_anarchy > 0) _anarchy--;
		}

		public override bool Equals (object? obj)
		{
			if (obj is byte v)
				return Game.PlayerNumber(this) == v;
			if (obj is Player p)
				return Game.PlayerNumber(this) == Game.PlayerNumber(p);
			return false;
		}

		bool[,] IPlayer.Explored => _explored;

		bool[,] IPlayer.Visible => _visible;

		List<byte> IPlayer.Advances => _advances;

		List<byte> IPlayer.Embassies => _embassies;

		ushort[] IPlayer.Diplomacy => _diplomacy;

		public short Anarchy => _anarchy;

		int IPlayer.CityNamesSkipped => CityNamesSkipped;

		ushort IPlayer.FutureTechCount => FutureTechCount;

		ushort IPlayer.HumanContactTurn => HumanContactTurn;

		short IPlayer.StartX => StartX;

		(short X, short Y)[] IPlayer.MapPositions => MapPositions;

		string[] IPlayer.MapPositionNames => MapPositionNames;

		(short X, short Y) IPlayer.LastMapPosition => LastMapPosition;

		int IPlayer.MapZoomBasisPoints => _mapZoomBasisPoints;

		ushort[] IPlayer.UnitsLost => _unitsLost;

		ushort[] IPlayer.UnitsDestroyedBy => _unitsDestroyedBy;

		ushort IPlayer.EpicRanking => _epicRanking;

		ushort IPlayer.MilitaryPower => _militaryPower;

		ushort IPlayer.CivilizationScore => _civilizationScore;

		PalaceData IPlayer.Palace => _palace;

		List<ICity> IPlayer.Cities => (Game != null && Game.Started)
			? Cities.Cast<ICity>().ToList()
			: (RestoredCities?.ToList() ?? []);

		
		public override int GetHashCode() => Game.PlayerNumber(this);

		
		public static explicit operator Player(byte playerNumber) => Game.GetPlayer(playerNumber);
		public static explicit operator byte(Player player) => Game.PlayerNumber(player);
		
		public static bool operator ==(Player p1, byte p2) => Game.PlayerNumber(p1) == p2;
		public static bool operator !=(Player p1, byte p2) => Game.PlayerNumber(p1) != p2;

		public Player(ICivilization civilization, string? customLeaderName = null, string? customTribeName = null, string? customTribeNamePlural = null)
		{
			_civilization = civilization;
			if (!string.IsNullOrEmpty(customLeaderName)) _civilization.Leader.Name = customLeaderName;
			_tribeName = string.IsNullOrEmpty(customTribeName) ? _civilization.Name : customTribeName;
			_tribeNamePlural = string.IsNullOrEmpty(customTribeNamePlural) ? _civilization.NamePlural : customTribeNamePlural;
			Government = new Despotism();

			for (int xx = 0; xx < Map.WIDTH; xx++)
				for (int yy = 0; yy < Map.HEIGHT; yy++)
				{
					_explored[xx, yy] = false;
					_visible[xx, yy] = false;
				}

			InitializeMapPositions();
		}

		#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Only used for MockPlayer in tests.
		internal Player()
		{
			// for MockPlayer
			InitializeMapPositions();
		}

		internal Player(ICivilization civilization)
		{
			// for MockPlayer. Do not access Map here
			_civilization = civilization;
			InitializeMapPositions();
		}
		#pragma warning restore CS8618

		private void InitializeMapPositions()
		{
			for (var i = 0; i < MapPositions.Length; i++)
			{
				MapPositions[i] = (-1, -1);
				MapPositionNames[i] = string.Empty;
			}

			_mapZoomBasisPoints = MapZoomSettings.DefaultBasisPoints;
		}

		internal static int NormalizeMapZoomBasisPoints(int basisPoints) => MapZoomSettings.NormalizeBasisPoints(basisPoints);
	}
}