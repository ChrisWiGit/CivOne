using System.Collections.Generic;
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Governments;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    using PlayerId = System.UInt16;
    using AdvanceId = System.UInt32;
    using GovernmentId = System.Byte;
	using CityId = System.UInt16;
    public class PlayerDto
    {
		[Doc("The civilization of the player.")]
        public CivilizationDto Civilization { get; set; }

		[Doc("The unique id of the player. This may be the index of a player list, but it is not guaranteed to be stable across game sessions.")]
        public PlayerId Id { get; set; }
        
		[Doc("A list of explored advances", null, nameof(AllAdvancesInfo))]
		public List<AdvanceId> Advances { get; set; }
        
		[Doc("A list of player ids with which this player has an embassy.")]
		public List<PlayerId> Embassies { get; set; }

		public static Dictionary<AdvanceId, string> AllAdvancesInfo = [];

        [Doc("The number of turns the player is in anarchy.")]
        public short Anarchy { get; set; }

		[Doc("The amount of gold the player has. Must be non-negative and not exceed 30000. If the value is out of range, it will be set to the closest valid value.", 0, 30000)]
        public long Gold { get; set; }

		[Doc("The id of the advance currently being researched, or null if no research is in progress.", nameof(AllAdvances))]
        public AdvanceId CurrentResearch { get; set; }

		// Must be initialized outside of this class, so it is not coupled to the actual advances in the game. 
		public static string[] AllAdvances = [];

        [Doc("The current index of the city name list for this player, and to be shown next time a city name is needed for a new city.")]
        public int CityNamesSkipped { get; set; }

		[Doc("A list of the player's cities")]
        public List<CityDto> Cities { get; set; }

		[Doc("A list of the player's units")]
        public List<UnitDto> Units { get; set; }

		[Doc("A 2D array indicating which tiles have been explored by the player (1) or not (0). This only accounts for the city tiles to be shown (4x4). Center tile is always 1.", 0,4)]
        public Bool2dMap Explored { get; set; }

        [Doc("A 2D array indicating which tiles are visible to the player (1) or not (0). This only accounts for the city tiles to be shown (4x4). Center tile is always 1.", 0,4)]
        public Bool2dMap Visible { get; set; }

		[Doc("The name of the player's tribe, e.g. 'Romans'.")]
        public string TribeName { get; set; }
        
		[Doc("The plural form of the player's tribe name, e.g. 'Romans'.")]
        public string TribeNamePlural { get; set; }

		[Doc("The id of the government type of the player.", nameof(AllGovernments))]
        public GovernmentId Government { get; set; }

		public static string[] AllGovernments = [];

		[Doc("The percentage of luxuries rate, from 0 to 10, where 0 means 0% and 10 means 100%." +
			"Must be in increments of 10%. If the value is out of range, it will be set to the closest valid value."+
			"The sum of all rates (luxuries, taxes, science) must be 10. If setting this property causes the sum to exceed 10, the result is undefined"
			, 0,10)]
        public int LuxuriesRate { get; set; }
        [Doc("The percentage of taxes rate, from 0 to 10, where 0 means 0% and 10 means 100%." +
			"Must be in increments of 10%. If the value is out of range, it will be set to the closest valid value." +
			"The sum of all rates (luxuries, taxes, science) must be 10. If setting this property causes the sum to exceed 10, the result is undefined"
			, 0,10)]
        public int TaxesRate { get; set; }
        [Doc("The percentage of science rate, from 0 to 10, where 0 means 0% and 10 means 100%." +
			"Must be in increments of 10%. If the value is out of range, it will be set to the closest valid value." +
			"The sum of all rates (luxuries, taxes, science) must be 10. If setting this property causes the sum to exceed 10, the result is undefined"
			, 0,10)]
        public int ScienceRate { get; set; }

		[Doc("The total amount of science points the player has.", 0,30000)]
        public int Science { get; set; }

		[Doc("The player's palace. This may be null if the player has no palace.")]
        public PalaceDto Palace { get; set; }
    }
}

/*
public partial class Player : BaseInstance, ITurn
	{
		// Dependency injection, but static for all the static members.
		public static Game Game = null;
		private readonly ICivilization _civilization;
		private readonly string _tribeName, _tribeNamePlural;

		private readonly bool[,] _explored = new bool[Map.WIDTH, Map.HEIGHT];
		private readonly bool[,] _visible = new bool[Map.WIDTH, Map.HEIGHT];
		private readonly List<byte> _advances = new List<byte>();
		private readonly List<byte> _embassies = new List<byte>();
		
		private short _anarchy = 0;
		private short _gold;
		private IAdvance _currentResearch = null;

		public event EventHandler Destroyed;

		internal int CityNamesSkipped = 0;

		internal short StartX { get; set; }
		
		internal bool AnarchyDespotism => Game.Started && (Government is Anarchy || Government is Despotism);

		internal bool MonarchyCommunist => Game.Started && (Government is Gov.Monarchy || Government is Gov.Communism);

		internal bool RepublicDemocratic => Game.Started && (Government is Republic || Government is Gov.Democracy);

		public ICivilization Civilization => _civilization;
		
		public string LeaderName => _civilization.Leader.Name;
		public string TribeName => _tribeName;
		public string TribeNamePlural => _tribeNamePlural;

		public byte Handicap { get; internal set; }

		public readonly PalaceData Palace = new PalaceData();

		internal AI AI => !IsHuman ? AI.Instance(this) : null;
		
		private IGovernment _government;
		public IGovernment Government
		{
			get => _government;
			internal set
			{
				if (value == null) return;
				_government = value;
			}
		}

		private int _luxuriesRate = 0, _taxesRate = 5, _scienceRate = 5;
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
			GameTask.Enqueue(Message.Newspaper(null, $"The {Game.Instance.HumanPlayer.TribeNamePlural} are", "revolting! Citizens", "demand new govt."));
		}

		public bool IsHuman => (Game.HumanPlayer == this);

		public virtual City[] Cities => Game.GetCities().Where(c => this == c.Owner && c.Size > 0).ToArray();
		
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
				short cost = (short)((Game.Instance.Difficulty + 3) * 2 * (_advances.Count() + 1) * (Common.TurnToYear(Game.Instance.GameTurn) > 0 ? 2 : 1));
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
			Game.Instance.SetAdvanceOrigin(advance, this);
		}

		public void DeleteAdvance(IAdvance advance) => _advances.RemoveAll(x => x == advance.Id);
		
		public string LatestAdvance
		{
			get
			{
				if (_advances.Count == 0)
					return "Irrigation";
				return Reflect.GetAdvances().First(a => a.Id == _advances.Last()).Name;
			}
		}

*/