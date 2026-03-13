using System.Collections.Generic;
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Governments;
using CivOne.Persistence.Model.Attributes;
using CivOne.Wonders;

namespace CivOne.Persistence.Model
{
    using PlayerId = System.UInt16;
    using AdvanceId = System.UInt32;
    using GovernmentId = System.Byte;
	using CityId = System.UInt16;

	public interface IPlayerEffects
	{
		bool HasWonderEffect<T>() where T : IWonder, new();
		bool HasAdvance<T>() where T : IAdvance;
	}

	public interface IPlayer : IPlayerEffects
	{
		// einfach nur alle properties von Player
		// auch die privaten direkt raus
		ICivilization Civilization { get; }
		string TribeName { get; }
		string TribeNamePlural { get; }
		bool[,] Explored { get; }
		bool[,] Visible { get; }
		List<byte> Advances { get; }
		List<byte> Embassies { get; }
		short Anarchy { get; }
		short Gold { get; }
		IAdvance CurrentResearch { get; }
		int CityNamesSkipped { get; }
		short StartX { get; }
		IGovernment Government { get; }
		bool RepublicDemocratic { get; }
		bool AnarchyDespotism { get; }
		bool MonarchyCommunist { get; }

		int LuxuriesRate { get; }
		int TaxesRate { get; }
		int ScienceRate { get; }
		short Science { get; }
		PalaceData Palace { get; }
		List<ICity> Cities { get; }
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