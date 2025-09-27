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
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.Screens;
using CivOne.Screens.Reports;
using CivOne.src;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

using UniversityBuilding = CivOne.Buildings.University;

namespace CivOne
{
	public class City : BaseInstance, ITurn
	{
		// Dependency Injection
		public static Game Game;

		// Dependency Injection
		// TODO: Replace by DI container instantiation
		internal BitFlagExtensions bitFlagExtensions = new();

		internal int NameId { get; set; }
		internal byte X;
		internal byte Y;
		private byte _owner;
		internal byte Owner
		{
			get => _owner;
			set
			{
				_owner = value;
				ResetResourceTiles();
			}
		}
		internal string Name => Game.CityNames[NameId];
		private byte _size;
		internal byte Size
		{
			get => _size;
			set
			{
				if (X == 255 || Y == 255) return;

				_size = value;
				if (_size == 0)
				{
					Map[X, Y].Road = false;
					Map[X, Y].Irrigation = false;
					if (Game.Started) Game.DestroyCity(this);
					return;
				}
				if (Food > FoodRequired) Food = FoodRequired;
				SetResourceTiles();
			}
		}
		internal int Shields { get; set; }
		internal int Food { get; set; }
		internal IProduction CurrentProduction { get; private set; }

		private List<ITile> _resourceTiles = new List<ITile>();
		private List<Citizen> _specialists = new List<Citizen>();

		internal List<ITile> SetupResourceTiles
		{
			get => _resourceTiles;
			set
			{
				_resourceTiles = value;
			}
		}

		internal List<Citizen> SetupSpecialists
		{
			get => _specialists;
			set
			{
				_specialists = value;
			}
		}

		private List<IBuilding> _buildings = new List<IBuilding>();
		private List<IWonder> _wonders = new List<IWonder>();

		public IBuilding[] Buildings => _buildings.OrderBy(b => b.Id).ToArray();
		public IWonder[] Wonders => _wonders.OrderBy(b => b.Id).ToArray();

		public bool HasBuilding(IBuilding building) => _buildings.Any(b => b.Id == building.Id);
		public bool HasBuilding(Type type) => _buildings.Any(b => b.GetType() == type);
		public bool HasBuilding<T>() where T : IBuilding => _buildings.Any(b => b is T);

		public bool HasWonder(IWonder wonder) => _wonders.Any(w => w.Id == wonder.Id);
		public bool HasWonder(Type type) => _wonders.Any(w => w.GetType() == type);
		public bool HasWonder<T>() where T : IWonder => _wonders.Any(w => w is T);

		public int HappyCitizens => Citizens.Count(c => c == Citizen.HappyMale || c == Citizen.HappyFemale);
		public int UnhappyCitizens => Citizens.Count(c => c == Citizen.UnhappyMale || c == Citizen.UnhappyFemale);
        public int ContentCitizens => Citizens.Count(c => c == Citizen.ContentFemale || c == Citizen.ContentMale);

        public int Entertainers => _specialists.Count(c => c == Citizen.Entertainer);
        public int Scientists => _specialists.Count(c => c == Citizen.Scientist);
        public int Taxmen => _specialists.Count(c => c == Citizen.Taxman);

        public bool IsInDisorder => _size > 0 && UnhappyCitizens > HappyCitizens;
		public bool WasInDisorder {get; set;}

        public bool IsBuildingWonder => CurrentProduction is IWonder;

		/// Number of shields required to maintain units
		internal int ShieldCosts
		{
			get
			{
				IGovernment government = Game.GetPlayer(_owner).Government;
				if (government is Anarchy || government is Despotism)
				{
					int costs = 0;
					for (int i = 0; i < Units.Count(u => (!(u is Diplomat) && !(u is Caravan))); i++)
					{
						if (i < _size) continue;
						costs++;
					}
					return costs;
				}
				return Units.Count(u => (!(u is Diplomat) && !(u is Caravan)));
			}
		}

		internal int ShieldIncome => ShieldTotal - ShieldCosts;

        /// How much food required for citizens and settlers		
		internal int FoodCosts
		{
			get
			{
				int costs = (_size * 2);
				IGovernment government = Game.GetPlayer(_owner).Government;
				if (government is Anarchy || government is Despotism)
				{
					costs += Units.Count(u => (u is Settlers));
				}
				else
				{
					costs += (Units.Count(u => (u is Settlers)) * 2);
				}
				return costs;
			}
		}

		internal int FoodIncome => ResourceTiles.Sum(t => FoodValue(t)) - FoodCosts;
		internal int FoodRequired => (int)(Size + 1) * 10;
		internal int FoodTotal => ResourceTiles.Sum(t => FoodValue(t));

		/// <summary>
		/// Food produced by a tile, taking government and improvements into account
		/// </summary>
		/// <param name="tile"></param>
		/// <returns>count of food units</returns>
		internal int FoodValue(ITile tile)
		{
			int output = tile.Food;
			switch (tile.Type)
			{
				case Terrain.Desert:
				case Terrain.Forest:
				case Terrain.Grassland1:
				case Terrain.Grassland2:
				case Terrain.River:
					if (!Player.AnarchyDespotism && tile.Irrigation) output += 1;
					break;
				case Terrain.Ocean:
				case Terrain.Tundra:
					if (!Player.AnarchyDespotism && tile.Special) output += 1;
					break;
			}
			if (tile.RailRoad) output = (int)Math.Floor((double)output * 1.5);
			if (tile.Pollution) output = (int)Math.Ceiling((double)output * 0.5);
			return output;
		}

		/// <summary>
		/// Shields produced by a tile, taking government and improvements into account
		/// </summary>
		/// <param name="tile"></param>
		/// <returns>count of shields</returns>
		internal int ShieldValue(ITile tile)
		{
			int output = tile.Shield;
			switch (tile.Type)
			{
				case Terrain.Hills:
					if (!Player.AnarchyDespotism && tile.Mine) output += 1;
					break;
			}
			if (tile.RailRoad) output = (int)Math.Floor((double)output * 1.5);
			if (tile.Pollution) output = (int)Math.Ceiling((double)output * 0.5);
			return output;
		}

		/// <summary>
		/// Total number of shields produced by city, taking buildings into account
		/// </summary>
		internal int ShieldTotal
		{
			get
			{
				int shields = ResourceTiles.Sum(t => ShieldValue(t));
				if (_buildings.Any(b => (b is Factory))) shields += (short)Math.Floor((double)shields * (_buildings.Any(b => (b is NuclearPlant)) ? 1.0 : 0.5));
				if (_buildings.Any(b => (b is MfgPlant))) shields += (short)Math.Floor((double)shields * 1.0);
				return shields;
			}
		}

		/// <summary>
		/// Trade units produced by a tile, taking government, improvements
		/// and wonders into account.
		/// </summary>
		/// <param name="tile"></param>
		/// <returns></returns>
		internal int TradeValue(ITile tile)
		{
			int output = tile.Trade;

			if (tile.RailRoad) output = (int)Math.Floor((double)output * 1.5);
			switch (tile.Type)
			{
				case Terrain.Desert:
				case Terrain.Grassland1:
				case Terrain.Grassland2:
				case Terrain.Plains:
					if (!tile.Road) break;
					if (Player.RepublicDemocratic) output += 1;
					break;
				case Terrain.Ocean:
				case Terrain.River:
					if (Player.RepublicDemocratic) output += 1;
					break;
				case Terrain.Jungle:
					if (!tile.Special) break;
					if (Player.MonarchyCommunist) output += 1;
					if (Player.RepublicDemocratic) output += 2;
					break;
				case Terrain.Mountains:
					if (!tile.Special) break;
					if (Player.MonarchyCommunist) output += 1;
					if (Player.RepublicDemocratic) output += 2;
					break;
			}
			if (output > 0 && HasWonder<Colossus>() && !Game.WonderObsolete<Colossus>()) output += 1;
			if (tile.Pollution) output = (int)Math.Ceiling((double)output * 0.5);
			return output;
		}

		// CW: Prevent negative trade values.
		// Negative trade can occur when corruption exceeds the total trade generated by resource tiles,
		// often in cities located on distant continents with high corruption.
		// This negative value is not displayed in the city screen, but appears in the trade report and confuses players.
		internal int TradeTotal => Math.Max(0, ResourceTiles.Sum(TradeValue) - Corruption);
		internal short TradeScience => (short)Math.Max(0, TradeTotal - TradeLuxuries - TradeTaxes);
		internal short TradeLuxuries => (short)Math.Round((double)(TradeTotal - TradeTaxes) / (10 - Player.TaxesRate) * Player.LuxuriesRate, MidpointRounding.AwayFromZero);
		internal short TradeTaxes => (short)Math.Round((double)TradeTotal / 10 * Player.TaxesRate, MidpointRounding.AwayFromZero);


		public bool CityOfSameCiv(City city)
		{
			if (city == null) return false;
			return city.Player == this.Player;
		}

		private int CalculateTradeValue(City city)
		{
			// CW: Source Civilization Or Rome on 640k A Day by Johnny L. Wilson et al. page 230
			int sameCivPenalty = CityOfSameCiv(city) ? 2 : 1;
			int trading = (int)Math.Round((city.TradeTotal + this.TradeTotal + 4) / 8.0 / sameCivPenalty);
			return trading;
		}

		/// <summary>
		/// List of cities this city is trading with and the trade value from each.
		/// </summary>
		public Dictionary<City, int> TradingCitiesValue
		{
			get
			{
				Dictionary<City, int> tradingCitiesValue = [];
				foreach (City city in TradingCities)
				{
					int trading = CalculateTradeValue(city);
					tradingCitiesValue[city] = trading;
				}
				return tradingCitiesValue;
			}
		}

		/// <summary>
		/// Sum of trade values from all (up to 3) trading cities.
		/// </summary>
		public int TradingCitiesSumValue => TradingCities.Sum(CalculateTradeValue);

		public int TotalIncome => Taxes + TradingCitiesSumValue;
		/// <summary>
		/// Amount of corruption, taking government, buildings, and distance
		/// to capital into account.
		/// </summary>
		internal int Corruption
		{
			get
			{
				IGovernment government = Game.GetPlayer(_owner).Government;
				if (government.CorruptionMultiplier == 0) return 0;

				int distance;
				switch (government)
				{
					case Governments.Communism _:
						distance = 10;
						break;
					default:
						if (HasBuilding<Palace>())
							return 0;
						City capital = Game.GetPlayer(Owner).GetCapital();
						distance = capital == null ? 32 : Common.DistanceToTile(X, Y, capital.X, capital.Y);
						break;
				}

				int totalTrade = ResourceTiles.Sum(t => TradeValue(t));
				int corruption = (int)Math.Round((float)(totalTrade * distance * 3) / (10 * government.CorruptionMultiplier));

				if (HasBuilding<Courthouse>() || (HasBuilding<Palace>() && government is Governments.Communism)) corruption /= 2;

				return corruption;
			}
		}

		/// <summary>
		/// Luxury count for the city, taking trade, buildings and entertainers
		/// into account.
		/// </summary>
		internal short Luxuries
		{
			get
			{
				short luxuries = TradeLuxuries;
				if (HasBuilding<MarketPlace>()) luxuries += (short)Math.Floor((double)luxuries * 0.5);
				if (HasBuilding<Bank>()) luxuries += (short)Math.Floor((double)luxuries * 0.5);
				luxuries += (short)(_specialists.Count(c => c == Citizen.Entertainer) * 2);
				return luxuries;
			}
		}

		/// <summary>
		/// Amount of taxes collected, taking trade, buildings, and taxmen
		/// into account.
		/// </summary>
		internal short Taxes
		{
			get
			{
				// CW: For future changes, we use int max out at short.MaxValue
				int taxes = TradeTaxes;
				if (HasBuilding<MarketPlace>()) taxes += (int)Math.Floor((double)taxes * 0.5);
				if (HasBuilding<Bank>()) taxes += (int)Math.Floor((double)taxes * 0.5);
				taxes += _specialists.Count(c => c == Citizen.Taxman) * 2;

				return (short)Math.Min(short.MaxValue, taxes);
			}
		}

		/// <summary>
		/// Amount of science generated, taking trade, buildings, and scientists
		/// into account.
		/// </summary>
		internal short Science
		{
			get
			{
				short science = TradeScience;
				if (HasBuilding<Library>()) science += (short)Math.Floor((double)science * 0.5);
				if (HasBuilding<UniversityBuilding>()) science += (short)Math.Floor((double)science * 0.5);
				if (!Game.WonderObsolete<CopernicusObservatory>() && HasWonder<CopernicusObservatory>()) science += (short)Math.Floor((double)science * 1.0);
				if (Player.HasWonder<SETIProgram>()) science += (short)Math.Floor((double)science * 0.5);
				science += (short)(_specialists.Count(c => c == Citizen.Scientist) * 2);
				return science;
			}
		}

		internal short TotalMaintenance => (short)_buildings.Sum(b => b.Maintenance);

		internal int CalculateSmokeStacks()
		{
			// CW: Source Civilization Or Rome on 640k A Day by Johnny L. Wilson et al. page 231
			int industrialPollution = ShieldTotal;
			if (HasBuilding<RecyclingCenter>()) industrialPollution /= 3;
			else if (HasBuilding<HydroPlant>()) industrialPollution /= 2;
			else if (HasBuilding<NuclearPlant>()) industrialPollution /= 2;

			int pollutionMultiplier = 100;
			if (HasBuilding<MassTransit>()) pollutionMultiplier = 0;
			else if (Player.HasAdvance<Plastics>()) pollutionMultiplier = 100;
			else if (Player.HasAdvance<MassProduction>()) pollutionMultiplier = 75;
			else if (Player.HasAdvance<Automobile>()) pollutionMultiplier = 50;
			else if (Player.HasAdvance<Industrialization>()) pollutionMultiplier = 25;

			int populationPollution = (int)Math.Round((double)(Size * pollutionMultiplier) / 100);
			int smokeStacks = industrialPollution + populationPollution;
			int toleranceSmokeStacks = smokeStacks - 20;

			return Math.Max(0, toleranceSmokeStacks);
		}

		public int SmokeStacks => CalculateSmokeStacks();

		internal bool GeneratePollution()
		{
			// CW: Source: https://forums.civfanatics.com/threads/pollution-bug-nailed.535608/
			// IF ( 2 * CityPollution > Random(256 - CityOwnerTechCount * difficultyLevel / 2) ) THEN AddPollution
			if (SmokeStacks == 0) 
			{
				// CW: Bugfix: Prevent the formula to be used on cities that do not generate pollution.
				return false;
			}

			int maxRandom = 256 - (Player.Advances.Length * (1 + Game.Difficulty) / 2);
			if (maxRandom < 1) maxRandom = 2; // Prevents bug -> still 50% chance of pollution with 256 advances

			int rnd = Common.Random.Next(maxRandom);

			return (2 * SmokeStacks) > rnd;
		}

		internal byte _status = 0;

		/// <summary>
		/// Only used for saving/loading, not for gameplay
		/// Use IsRiot, IsCoastal, HydroAvailable, AutoBuild, TechStolen, CelebrationOrRapture, BuildingSold instead.
		/// </summary>
		public byte Status
		{
			get => _status;
		}

		public void SetupStatus(byte status)
		{
			_status = status;

			// recalculate these specific flags, because older versions may not have set them
			SetupCoastalFlag();
			SetupHydroFlag();
		}

		public bool IsRiot
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.RIOT);
			set => SetStatusFlag(CityStatus.RIOT, value);
		}

		public bool IsCoastal => bitFlagExtensions.HasFlag(_status, CityStatus.COASTAL);

		public bool CelebrationCancelled
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.CELEBRATION_CANCELLED);
			set => SetStatusFlag(CityStatus.CELEBRATION_CANCELLED, value);
		}

		public bool HydroAvailable	{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.HYDRO_AVAILABLE);
		}

		public bool AutoBuild
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.AUTO_BUILD);
			set => SetStatusFlag(CityStatus.AUTO_BUILD, value);
		}

		public bool TechStolen
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.TECH_STOLEN);
			set => SetStatusFlag(CityStatus.TECH_STOLEN, value);
		}

		public bool CelebrationOrRapture
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.CELEBRATION_RAPTURE);
			set => SetStatusFlag(CityStatus.CELEBRATION_RAPTURE, value);
		}
		
		/// <summary>
		/// Was a building sold in this turn?
		/// </summary>
		public bool BuildingSold
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.IMPROVEMENT_SOLD);
			set => SetStatusFlag(CityStatus.IMPROVEMENT_SOLD, value);
		}

		internal void SetStatusFlag(CityStatus status, bool value)
		{
			_status = value ? bitFlagExtensions.SetFlag(_status, status) : bitFlagExtensions.ClearFlag(_status, status);
		}

		internal IEnumerable<ITile> ResourceTiles => CityTiles.Where(t => (t.X == X && t.Y == Y) || _resourceTiles.Contains(t));

		internal bool OccupiedTile(ITile tile)
		{
			if (ResourceTiles.Any(t => t.X == tile.X && t.Y == tile.Y))
				return false;
			return InvalidTile(tile);
		}

		internal bool InvalidTile(ITile tile)
		{
			return (Game.GetCities().Where(c => c != this).Any(c => c.ResourceTiles.Any(t => t.X == tile.X && t.Y == tile.Y)) || tile.Units.Any(u => u.Owner != Owner));
		}

		private void UpdateSpecialists()
		{
			while (_specialists.Count < (_size - ResourceTiles.Count())) _specialists.Add(Citizen.Entertainer);
			while (_specialists.Count > 0 && _specialists.Count > (_size - ResourceTiles.Count() - 1)) _specialists.RemoveAt(_specialists.Count - 1);
		}

		/// <summary>
		/// Export internal info about a city's resource tiles to the format
		/// required by the savefile.
		/// TODO move to savefile module
		/// </summary>
		/// <returns></returns>
		internal byte[] GetResourceTiles()
		{
			byte[] output = new byte[6];
			foreach (ITile tile in _resourceTiles)
			{
				int x = tile.X - X;
				int y = tile.Y - Y;
				switch(x)
				{
					case -2:
						if (y == -1) output[2] |= (byte)(0x01 << 3);
						if (y == 0) output[1] |= (byte)(0x01 << 3);
						if (y == 1) output[2] |= (byte)(0x01 << 2);
						continue;
					case -1:
						if (y == -2) output[1] |= (byte)(0x01 << 4);
						if (y == -1) output[0] |= (byte)(0x01 << 7);
						if (y == 0) output[0] |= (byte)(0x01 << 3);
						if (y == 1) output[0] |= (byte)(0x01 << 6);
						if (y == 2) output[2] |= (byte)(0x01 << 1);
						continue;
					case 0:
						if (y == -2) output[1] |= (byte)(0x01 << 0);
						if (y == -1) output[0] |= (byte)(0x01 << 0);
						if (y == 1) output[0] |= (byte)(0x01 << 2);
						if (y == 2) output[1] |= (byte)(0x01 << 2);
						continue;
					case 1:
						if (y == -2) output[1] |= (byte)(0x01 << 5);
						if (y == -1) output[1] |= (byte)(0x01 << 6);
						if (y == 0) output[0] |= (byte)(0x01 << 1);
						if (y == 1) output[0] |= (byte)(0x01 << 5);
						if (y == 2) output[2] |= (byte)(0x01 << 0);
						continue;
					case 2:
						if (y == -1) output[1] |= (byte)(0x01 << 6);
						if (y == 0) output[1] |= (byte)(0x01 << 1);
						if (y == 1) output[1] |= (byte)(0x01 << 7);
						continue;
				}
			}

			SetSpecialists(output);
			return output;
		}

		internal void SetSpecialists(byte[] output)
		{
			if (_specialists.Count == 0) return;

			int specialistBits = 0;
			for (int i = 0; i < _specialists.Count && i < 8; i++)
			{
				switch (_specialists[i])
				{
					case Citizen.Taxman:
						specialistBits |= 1 << (2 * i);
						break;
					case Citizen.Scientist:
						specialistBits |= 2 << (2 * i);
						break;
					case Citizen.Entertainer:
						specialistBits |= 3 << (2 * i);
						break;
				}
			}

			output[4] = (byte)(specialistBits & 0xFF);
			output[5] = (byte)((specialistBits >> 8) & 0xFF);
		}

		private void SetResourceTiles()
		{
			if (!Game.Started) return;

			while (_resourceTiles.Count > Size)
				_resourceTiles.RemoveAt(_resourceTiles.Count - 1);
			if (_resourceTiles.Count == Size) return;
			if (_resourceTiles.Count < Size)
			{
				IEnumerable<ITile> tiles = CityTiles.Where(t => !OccupiedTile(t) && !ResourceTiles.Contains(t)).OrderByDescending(t => FoodValue(t)).ThenByDescending(t => ShieldValue(t)).ThenByDescending(t => TradeValue(t));
				if (tiles.Count() > 0)
					_resourceTiles.Add(tiles.First());
			}

			UpdateSpecialists();

			SetupCoastalFlag();
		}

		private void SetupCoastalFlag()
		{
			if (!Game.Started) return;

			bool isCoastal = Map[X, Y].GetBorderTiles().Any(t => t.IsOcean);

			SetStatusFlag(CityStatus.COASTAL, isCoastal);
		}

		private void SetupHydroFlag()
		{
			if (!Game.Started) return;

			bool isHydroAvailable = Map[X, Y].GetBorderTiles().Any(t => t is Mountains or River);

			SetStatusFlag(CityStatus.HYDRO_AVAILABLE, isHydroAvailable);
		}

		public void ResetResourceTiles()
		{
			_resourceTiles.Clear();
			for (int i = 0; i < Size; i++)
				SetResourceTiles();
		}

		public void RelocateResourceTile(ITile tile)
		{
			if (tile.X == X && tile.Y == Y) return;
			SetResourceTile(tile);
			SetResourceTiles();
		}

		public void SetResourceTile(ITile tile)
		{
			if (tile == null || OccupiedTile(tile) || !CityTiles.Contains(tile) || (tile.X == X && tile.Y == Y) || (_resourceTiles.Count >= Size && !_resourceTiles.Contains(tile)))
			{
				ResetResourceTiles();
				return;
			}
			if (_resourceTiles.Contains(tile))
			{
				_resourceTiles.Remove(tile);
				return;
			}
			_resourceTiles.Add(tile);
			UpdateSpecialists();
		}

		public Player Player => Game.Instance.GetPlayer(Owner);

		/// <summary>
		/// Return the list of possible city production [units, buildings,
		/// or wonders] taking location and advancements into account.
		/// </summary>
		public IEnumerable<IProduction> AvailableProduction
		{
			get
			{
				foreach (IUnit unit in Reflect.GetUnits().Where(u => Player.ProductionAvailable(u)))
				{
					if (unit.Class == UnitClass.Water && !Map[X, Y].GetBorderTiles().Any(t => t.IsOcean)) continue;
					if (unit is Nuclear && !Game.WonderBuilt<ManhattanProject>()) continue;
					yield return unit;
				}
				foreach (IBuilding building in Reflect.GetBuildings().Where(b => Player.ProductionAvailable(b) && !_buildings.Any(x => x.Id == b.Id)))
				{
					if (HasBuilding<Palace>() && building is Courthouse) continue;
					yield return building;
				}
				foreach (IWonder wonder in Reflect.GetWonders().Where(b => Player.ProductionAvailable(b)))
				{
					yield return wonder;
				}
			}
		}

		public void SetProduction(IProduction production) => CurrentProduction = production;

		internal void SetProduction(byte productionId)
		{
			IProduction production = Reflect.GetProduction().FirstOrDefault(p => p.ProductionId == productionId);
			if (production == null)
			{
				Log($"Invalid production ID for {Name}: {productionId}");
				return;
			}
			CurrentProduction = production;
		}

		/// <summary>
		/// How much it will cost to complete the current production.
		/// </summary>
		internal short BuyPrice
		{
			get
			{
				if (Shields > 0)
				{
					// Thanks to Tristan_C (http://forums.civfanatics.com/threads/buy-unit-building-wonder-price.576026/#post-14490920)
					if (CurrentProduction is IUnit)
					{
						double x = (double)((CurrentProduction.Price * 10) - Shields) / 10;
						double price = 5 * (x * x) + (20 * x);
						return (short)(Math.Floor(price));
					}
					return (short)(((CurrentProduction.Price * 10) - Shields) * (CurrentProduction is IWonder ? 4 : 2));
				}
				return CurrentProduction.BuyPrice;
			}
		}

		/// <summary>
		/// Complete the current production by purchasing it.
		/// </summary>
		/// <returns>false if purchase not possible</returns>
		public bool Buy()
		{
			if (Game.CurrentPlayer.Gold < BuyPrice) return false;

			Game.CurrentPlayer.Gold -= BuyPrice;
			Shields = (int)CurrentProduction.Price * 10;
			return true;
		}

		public int Population
		{
			get
			{
				int output = 0;
				for (int i = 1; i <= Size; i++)
				{
					output += 10000 * i;
				}
				return output;
			}
		}

		public struct CitizenTypes
        {
            public int happy;
            public int content;
            public int unhappy;
            public int redshirt;
            public int elvis;
            public int einstein;
            public int taxman;

            public int Sum()
            {
                return happy + content + unhappy + redshirt + elvis + einstein + taxman;
            }

            public bool Valid()
            {
                return happy >= 0 && content >= 0 && unhappy >= 0;
            }
        }

        // Microprose: initial state -> add entertainers -> 'after luxury' state
        // City size 4, King:
        //   3c1u -> 2c1u1ent -> 1h1c1u1ent
        //   3c1u -> 1c1u2ent -> 1h1c2ent
        //   3c1u -> 1u3ent   -> 1h3ent
        // City size 5, King:
        //   3c2u -> 2c2u1ent -> 1h1c2u1ent
        //   3c2u -> 1c2u2ent -> 1h1c1u2ent
        //   3c2u -> 2u3ent   -> 1h1c3ent
        //   3c2u -> 1u4ent   -> 1h4ent
        // City size 6, King:
        //   3c3u -> 2c3u1ent -> 1h1c3u1ent
        //   3c3u -> 1c3u2ent -> 1h1c2u2ent
        //   3c3u -> 3u3ent   -> 1h1c1u3ent
        //   3c3u -> 2u4ent   -> 2h4ent

        internal IEnumerable<CitizenTypes> Residents
        {
            get
            {
                // TODO fire-eggs: add side-effect of recalc specialties a la Citizens
                CitizenTypes start = new CitizenTypes();
                start.elvis = Entertainers;
                start.einstein = Scientists;
                start.taxman = Taxmen;

                int specialists = start.elvis + start.einstein + start.taxman;
                int available = Size - specialists;
                int initialContent = 6 - Game.Difficulty;

                // Stage 1: basic content/unhappy
                start.content = Math.Max(0, Math.Min(available, initialContent - specialists));
                start.unhappy = available - start.content;

                Debug.Assert(start.Sum() == Size);
                Debug.Assert(start.Valid());
                yield return start;

                if (available < 1)
                    yield return start;
                else
                {
                    // Stage 2: impact of luxuries: content->happy; unhappy->content and then content->happy
                    int happyUpgrades = (int)Math.Floor((double)Luxuries / 2);
                    int cont = start.content;
                    int unha = start.unhappy;
                    int happ = start.happy;
                    for (int h = 0; h < happyUpgrades; h++)
                    {
                        if (cont > 0)
                        {
                            happ++;
                            cont--;
                            continue;
                        }
                        if (unha > 0)
                        {
                            cont++;
                            unha--;
                        }
                    }

                    start.happy = happ;
                    start.content = cont;
                    start.unhappy = unha;

                    Debug.Assert(start.Sum() == Size);
                    Debug.Assert(start.Valid());

                    // TODO fire-eggs impact of luxury setting?
                    yield return start;
                }

                // Stage 3: impact of buildings
                if (!(HasBuilding<Temple>() || HasBuilding<Colosseum>() || HasBuilding<Cathedral>()))
                    yield return start;

                int unhappyDelta = 0;
                if (HasBuilding<Temple>())
                {
                    int templeEffect = 1;
                    if (Player.HasAdvance<Mysticism>()) templeEffect <<= 1;
                    if (Player.HasWonder<Oracle>() && !Game.WonderObsolete<Oracle>()) templeEffect <<= 1;
                    unhappyDelta += templeEffect;
                }
                if (HasBuilding<Colosseum>()) unhappyDelta += 3;
                if (HasBuilding<Cathedral>()) unhappyDelta += 4;

                unhappyDelta = Math.Min(start.unhappy, unhappyDelta);
                start.content += unhappyDelta;
                start.unhappy -= unhappyDelta;

                Debug.Assert(start.Sum() == Size);
                Debug.Assert(start.Valid());

                yield return start;
            }
        }


		internal IEnumerable<Citizen> Citizens
		{
			get
			{
				// Update specialist count
				while (_specialists.Count < Size - (ResourceTiles.Count() - 1)) _specialists.Add(Citizen.Entertainer);
				while (_specialists.Count > Size - (ResourceTiles.Count() - 1)) _specialists.Remove(_specialists.Last());

                // TODO fire-eggs verify luxury makes happy first, then clears unhappy
				int happyCount = (int)Math.Floor((double)Luxuries / 2);
				if (Player.HasWonder<HangingGardens>() && !Game.WonderObsolete<HangingGardens>()) happyCount++;
				if (Player.HasWonder<CureForCancer>()) happyCount++;

				int unhappyCount = Size - (6 - Game.Difficulty) - happyCount;
				if (HasWonder<ShakespearesTheatre>() && !Game.WonderObsolete<ShakespearesTheatre>())
				{
					unhappyCount = 0;
				}
				else
				{
					if (HasBuilding<Temple>())
					{
						int templeEffect = 1;
						if (Player.HasAdvance<Mysticism>()) templeEffect <<= 1;
						if (Player.HasWonder<Oracle>() && !Game.WonderObsolete<Oracle>()) templeEffect <<= 1;
						unhappyCount -= templeEffect;
					}
					if (Tile != null && Map.ContentCities(Tile.ContinentId).Any(x => x.Size > 0 && x.Owner == Owner && x.HasWonder<JSBachsCathedral>()))
					{
						unhappyCount -= 2;
					}
					if (HasBuilding<Colosseum>()) unhappyCount -= 3;
					if (HasBuilding<Cathedral>()) unhappyCount -= 4;
				}

                // 20190612 fire-eggs Martial law : reduce unhappy count for every attack-capable unit in city [max 3]
                if (Player.AnarchyDespotism || Player.MonarchyCommunist)
                {
                    var attackUnitsInCity = Game.Instance.GetUnits()
                        .Where(u => u.X == this.X && u.Y == this.Y && u.Attack > 0)
                        .Count();
                    attackUnitsInCity = Math.Min(attackUnitsInCity, 3);
                    unhappyCount -= attackUnitsInCity;

                    // TODO fire-eggs: absent units make people unhappy (republic, democracy)
                }

                int content = 0;
				int unhappy = 0;
				int working = (ResourceTiles.Count() - 1);
				int specialist = 0;

				for (int i = 0; i < Size; i++)
				{
					if (i < working)
					{
						if (happyCount-- > 0)
						{
							yield return (i % 2 == 0) ? Citizen.HappyMale : Citizen.HappyFemale;
							continue;
						}
						if ((unhappyCount - (working - i)) >= 0)
						{
							unhappyCount--;
							yield return ((unhappy++) % 2 == 0) ? Citizen.UnhappyMale : Citizen.UnhappyFemale;
							continue;
						}
						yield return ((content++) % 2 == 0) ? Citizen.ContentMale : Citizen.ContentFemale;
						continue;
					}
					yield return _specialists[specialist++];
				}
			}
		}
		internal void ChangeSpecialist(int index)
		{
			if (index >= _specialists.Count) return;

			while (_specialists.Count < (index + 1)) _specialists.Add(Citizen.Entertainer);
			_specialists[index] = (Citizen)((((int)_specialists[index] - 5) % 3) + 6);
		}


		/// <summary>
		/// The explored city area tiles.
		/// </summary>
		public IEnumerable<ITile> CityTiles
		{
			get
			{
				ITile[,] tiles = CityRadius;
				for (int xx = 0; xx < 5; xx++)
				for (int yy = 0; yy < 5; yy++)
				{
					if (tiles[xx, yy] == null) continue;
					yield return tiles[xx, yy];
				}
			}
		}

		/// <summary>
		/// The tiles which make up the city "area", with unexplored
		/// tiles removed. 
		/// </summary>
		public ITile[,] CityRadius
		{
			get
			{
				Player player = Game.Instance.GetPlayer(Owner);
				ITile[,] tiles = Map[X - 2, Y - 2, 5, 5];
				for (int xx = 0; xx < 5; xx++)
				for (int yy = 0; yy < 5; yy++)
				{
					ITile tile = tiles[xx, yy];
					if (tile == null) continue;
					if ((xx == 0 || xx == 4) && (yy == 0 || yy == 4)) tiles[xx, yy] = null;
					if (!player.Visible(tile)) tiles[xx, yy] = null;
				}
				return tiles;
			}
		}

		public IUnit[] Units => Game.Instance.GetUnits().Where(u => u.Home == this).ToArray();

		public ITile Tile => Map[X, Y];



		public void AddBuilding(IBuilding building) => _buildings.Add(building);

		/// <summary>
		/// Sell a city building.
		/// </summary>
		/// <param name="building"></param>
		public void SellBuilding(IBuilding building)
		{
			RemoveBuilding(building);
			Game.CurrentPlayer.Gold += building.SellPrice;
			BuildingSold = true;
		}

		public void RemoveBuilding(IBuilding building) => _buildings.RemoveAll(b => b.Id == building.Id);
		public void RemoveBuilding<T>() where T : IBuilding => _buildings.RemoveAll(b => b is T);

		public void AddWonder(IWonder wonder)
		{
			_wonders.Add(wonder);
			if (Game.Started)
			{
				if (wonder is Colossus && !Game.WonderObsolete<Colossus>())
				{
					ResetResourceTiles();
				}
				if ((wonder is Lighthouse && !Game.WonderObsolete<Lighthouse>()) ||
					(wonder is MagellansExpedition && !Game.WonderObsolete<MagellansExpedition>()))
				{
					// Apply Lighthouse/Magellan's Expedition wonder effects in the first turn
					foreach (IUnit unit in Game.GetUnits().Where(x => x.Owner == Owner && x.Class ==  UnitClass.Water && x.MovesLeft == x.Move))
					{
						unit.MovesLeft++;
					}
				}
			}
		}

		public void UpdateResources()
		{
			foreach (ITile tile in ResourceTiles.Where(t => InvalidTile(t)))
			{
				RelocateResourceTile(tile);
			}
		}
		
		internal void ExecutePollution()
		{
			if (!GeneratePollution())
			{
				return;
			}

			List<ITile> possiblePollutionTiles = [.. CityTiles.Where(t => !t.Pollution && !t.HasCity && t is not Ocean)];
			if (possiblePollutionTiles.Count == 0)
			{
				return;
			}

			int tileToPollute = Common.Random.Next(possiblePollutionTiles.Count);

			possiblePollutionTiles[tileToPollute].Pollution = true;

			if (Human != Owner)
			{
				return;
			}

			GameTask.Enqueue(Message.Newspaper(this, "Pollution in", $"{this.Name}!", "Health problems feared."));
		}

		public void NewTurn()
		{
			ExecutePollution();

			UpdateResources();

			if (IsInDisorder)
			{
				if (Common.Random.Next(20) == 1 && HasBuilding<Buildings.NuclearPlant>() && !Player.HasAdvance<Advances.FusionPower>())
				{
					// todo: meltdown
				}
				if (WasInDisorder)
				{
					if (Player.IsHuman)
						GameTask.Insert(Message.Advisor(Advisor.Domestic, true, "Civil Disorder in", $"{Name}! Mayor", "flees in panic."));
				}
				else
				{
					// TODO fire-eggs not showing loses side-effects
					if (Player.IsHuman) // && !Game.Animations)
					{
						Show disorderCity = Show.DisorderCity(this);
						GameTask.Insert(disorderCity);
					}

					Log($"City {Name} belonging to {Player.TribeName} has gone into disorder");
				}
				if (WasInDisorder && Player.Government is Governments.Democracy)
				{
					// todo: Force revolution
				}
				WasInDisorder = true;
			}
			else
			{
				if (WasInDisorder)
				{
					if (Player.IsHuman)
						GameTask.Insert(Message.Advisor(Advisor.Domestic, true, "Order restored", $" in {Name}."));
					Log($"City {Name} belonging to {Player.TribeName} is no longer in disorder");
				}
				WasInDisorder = false;
			}
			if (UnhappyCitizens == 0 && HappyCitizens >= ContentCitizens && Size >= 3)
			{
				// we love the president day
				if (Player.Government is Governments.Democracy || Player.Government is Republic)
				{
					if (Food > 0)
					{
						Size++;
					}
				}
				else
				{
					// we love the king day
					if (Human == Owner && Settings.Animations != GameOption.Off)
						GameTask.Insert(Show.WeLovePresidentDayCity(this));
				}
			}
			Food += IsInDisorder ? 0 : FoodIncome;

			if (Food < 0)
			{
				Food = 0;
				Size--;
				if (Human == Owner)
				{
					GameTask.Enqueue(Message.Newspaper(this, "Food storage exhausted", $"in {Name}!", "Famine feared."));
				}
				if (Size == 0) return;
			}
			else if (Food > FoodRequired)
			{
				Food -= FoodRequired;

				if (Size == 10 && _buildings.All(b => b.Id != (int)Building.Aqueduct))
				{
					GameTask.Enqueue(Message.Advisor(Advisor.Domestic, false, $"{Name} requires an AQUEDUCT", "for further growth."));
				}
				else
				{
					Size++;
				}

				if (_buildings.Any(b => (b is Granary)))
				{
					if (Food < (FoodRequired / 2))
					{
						Food = (FoodRequired / 2);
					}
				}
			}

			if (ShieldIncome < 0)
			{
				int maxDistance = Units.Max(u => Common.DistanceToTile(X, Y, u.X, u.Y));
				IUnit unit = Units.Last(u => Common.DistanceToTile(X, Y, u.X, u.Y) == maxDistance);
				if (Human == Owner)
				{
					Message message = Message.DisbandUnit(this, unit);
					message.Done += (s, a) =>
					{
						Game.DisbandUnit(unit);
					};
					GameTask.Enqueue(message);
				}
				else
				{
					Game.DisbandUnit(unit);
				}
			}
			else if (ShieldIncome > 0)
			{
				Shields += IsInDisorder ? 0 : ShieldIncome;
			}

			if (Shields >= (int)CurrentProduction.Price * 10)
			{
				if (CurrentProduction is Settlers && Size == 1 && Game.Difficulty == 0)
				{
					// On Chieftain level, it's not possible to create a Settlers in a city of size 1
				}
				else if (CurrentProduction is IUnit)
				{
					Shields = 0;
					IUnit unit = Game.Instance.CreateUnit((CurrentProduction as IUnit).Type, X, Y, Owner);
					unit.SetHome();
					unit.Veteran = (_buildings.Any(b => (b is Barracks)));
					if (CurrentProduction is Settlers)
					{
						if (Size == 1 && Player.Cities.Length == 1) Size++;
						if (Size == 1)
						{
							unit.SetHome(null);
						}
						Size--;
					}
					if (Human == Owner && (unit is Settlers || unit is Diplomat || unit is Caravan))
					{
						GameTask advisorMessage = Message.Advisor(Advisor.Defense, true, $"{this.Name} builds {unit.Name}.");
						advisorMessage.Done += (s, a) => GameTask.Insert(Show.CityManager(this));
						GameTask.Enqueue(advisorMessage);
					}
				}
				if (CurrentProduction is IBuilding && !_buildings.Any(b => b.Id == (CurrentProduction as IBuilding).Id))
				{
					Shields = 0;
					if (CurrentProduction is ISpaceShip)
					{
						Message message = Message.Newspaper(this, $"{this.Name} builds", $"{(CurrentProduction as ICivilopedia).Name}.");
						message.Done += (s, a) =>
						{
							// TODO: Add space ship component
							GameTask.Insert(Show.CityManager(this));
						};
						GameTask.Enqueue(message);
					}
					else if (CurrentProduction is Palace)
					{
						foreach (City city in Game.Instance.GetCities().Where(c => c.Owner == Owner))
						{
							// Remove palace from all cites.
							city.RemoveBuilding<Palace>();
						}
						if (HasBuilding<Courthouse>())
						{
							_buildings.RemoveAll(x => x is Courthouse);
						}
						_buildings.Add(CurrentProduction as IBuilding);

						Message message = Message.Newspaper(this, $"{this.Name} builds", $"{(CurrentProduction as ICivilopedia).Name}.");
						message.Done += (s, a) =>
						{
							GameTask advisorMessage = Message.Advisor(Advisor.Foreign, true, $"{Player.TribeName} capital", $"moved to {Name}.");
							advisorMessage.Done += (s1, a1) => GameTask.Insert(Show.CityManager(this));
							GameTask.Enqueue(advisorMessage);
						};
						GameTask.Enqueue(message);
					}
					else
					{
						_buildings.Add(CurrentProduction as IBuilding);
						GameTask.Enqueue(new ImprovementBuilt(this, (CurrentProduction as IBuilding)));
					}
				}
				if (CurrentProduction is IWonder && !Game.Instance.BuiltWonders.Any(w => w.Id == (CurrentProduction as IWonder).Id))
				{
					Shields = 0;
					AddWonder(CurrentProduction as IWonder);
					GameTask.Enqueue(new ImprovementBuilt(this, (CurrentProduction as IWonder)));
				}
			}

			// TODO: Handle luxuries
			Player.Gold += IsInDisorder ? (short)0 : Taxes;
			Player.Gold += IsInDisorder ? (short)0 : (short)TradingCitiesSumValue;
			Player.Gold -= TotalMaintenance;
			Player.Science += Science;

			BuildingSold = false;
			GameTask.Enqueue(new ProcessScience(Player));

			if (Player.IsHuman) return;

			Player.AI.CityProduction(this);
		}

		public void Disaster()
		{
			List<string> message = new List<string>();
			bool humanGetsCity = false;

			if (Player.Cities.Length == 1)
				return;

			if (Size < 5)
				return;

			switch (Common.Random.Next(0, 9))
			{
				case 0:
				{
					// Earthquake
					bool hillsNearby = CityTiles.Any(t => t.Type == Terrain.Hills);
					IList<IBuilding> buildingsOtherThanPalace = Buildings.Where(b => !(b is Palace)).ToList();
					if (!hillsNearby || !buildingsOtherThanPalace.Any())
						return;

					IBuilding buildingToDestroy = buildingsOtherThanPalace[Common.Random.Next(0, buildingsOtherThanPalace.Count - 1)];
					RemoveBuilding(buildingToDestroy);

					message.Add($"Earthquake in {Name}!");
					message.Add($"{buildingToDestroy.Name} destroyed!");

					break;
				}
				case 1:
				{
					// Plague
					bool hasMedicine = Player.HasAdvance<Medicine>();
					bool hasAqueduct = HasBuilding<Aqueduct>();
					bool hasConstruction = Player.Advances.Any(a => a is Construction);

					if (!hasMedicine && !hasAqueduct && hasConstruction)
					{
						Size = (byte)(Size - Size / 4);

						message.Add($"Plague in {Name}!");
						message.Add($"Citizens killed!");
						message.Add($"Citizens demand AQUEDUCT.");
					}

					break;
				}
				case 2:
				{
					// Flooding
					bool riverNearby = CityTiles.Any(t => t.Type == Terrain.River);
					bool hasCityWalls = HasBuilding<CityWalls>();
					bool hasMasonry = Player.HasAdvance<Masonry>();

					if (riverNearby && !hasCityWalls && hasMasonry)
					{
						Size = (byte)(Size - Size / 4);

						message.Add($"Flooding in {Name}!");
						message.Add($"Citizens killed!");
						message.Add($"Citizens demand CITY WALLS.");
					}
					break;
				}
				case 3:
				{
					// Volcano
					bool mountainNearby = CityTiles.Any(t => t.Type == Terrain.Mountains);
					bool hasTemple = HasBuilding<Temple>();
					bool hasCeremonialBurial = Player.HasAdvance<CeremonialBurial>();

					if (mountainNearby && !hasTemple && hasCeremonialBurial)
					{
						Size = (byte)(Size - Size / 3);

						message.Add($"Volcano erupts near {Name}!");
						message.Add($"Citizens killed!");
						message.Add($"Citizens demand TEMPLE.");
					}

					break;
				}
				case 4:
				{
					// Famine
					bool hasGranary = HasBuilding<Granary>();
					bool hasPottery = Player.HasAdvance<Pottery>();

					if (!hasGranary && hasPottery)
					{
						Size = (byte)(Size - Size / 3);

						message.Add($"Famine in {Name}!");
						message.Add($"Citizens killed!");
						message.Add($"Citizens demand GRANARY.");
					}

					break;
				}
				case 5:
				{
					// Fire
					IList<IBuilding> buildingsOtherThanPalace = Buildings.Where(b => !(b is Palace)).ToList();
					bool hasAqueduct = HasBuilding<Aqueduct>();
					bool hasConstruction = Player.HasAdvance<Construction>();

					if (buildingsOtherThanPalace.Any() && !hasAqueduct && hasConstruction)
					{
						IBuilding buildingToDestroy = buildingsOtherThanPalace[Common.Random.Next(0, buildingsOtherThanPalace.Count - 1)];
						RemoveBuilding(buildingToDestroy);

						message.Add($"Fire in {Name}!");
						message.Add($"{buildingToDestroy.Name} destroyed!");
						message.Add($"Citizens demand AQUEDUCT.");
					}

					break;
				}
				case 6:
				{
					// Pirates
					bool oceanNearby = CityTiles.Any(t => t.Type == Terrain.Ocean);
					bool hasBarracks = HasBuilding<Barracks>();
					if (oceanNearby && !hasBarracks)
					{
						Food = 0;
						Shields = 0;

						message.Add($"Pirates plunder {Name}!");
						message.Add($"Production halted, Food Stolen.!");
						message.Add($"Citizens demand BARRACKS.");
					}

					break;
				}
				case 7:
				case 8:
				case 9:
					// Riot, scandal, corruption

					string[] disasterTypes = { "Scandal", "Riot", "Corruption" };
					string disasterType = disasterTypes[Common.Random.Next(0, disasterTypes.Length - 1)];
					string buildingDemanded = "";

					if (HappyCitizens >= UnhappyCitizens)
						return;

					if (!HasBuilding<Temple>())
						buildingDemanded = nameof(Temple);
					else if (!HasBuilding<Courthouse>())
						buildingDemanded = nameof(Courthouse);
					else if (!HasBuilding<MarketPlace>())
						buildingDemanded = nameof(MarketPlace);
					else if (!HasBuilding<Cathedral>())
						buildingDemanded = nameof(Cathedral);
					else
						buildingDemanded = "lower taxes";

					Food = 0;
					Shields = 0;

					message.Add($"{disasterType} in {Name}");
					message.Add($"Citizens demand {buildingDemanded}");

					if (HasBuilding<Palace>())
						return;

					if (Player.Cities.Length < 4)
						return;

					City admired = null;
					int mostAppeal = 0;

					foreach (City city in Game.GetCities())
					{
						if (city == this)
							break;

                        // TODO fire-eggs got a null pointer error once. Need to investigate.
                        if (city.Tile == null)
                        {
                            Log($"Appeal check: City tile not set! {city.Name}");
                            continue;
                        }

						int appeal = ((city.HappyCitizens - city.UnhappyCitizens) * 32) / city.Tile.DistanceTo(this);
						if (appeal > 4 && appeal > mostAppeal)
						{
							admired = city;
							mostAppeal = appeal;
						}
					}

					if (admired != null && admired.Owner != this.Owner)
					{
						message.Clear();
						message.Add($"Residents of {Name} admire the prosperity of {admired.Name}");
						message.Add($"{admired.Name} capture {Name}");

						Player previousOwner = Game.GetPlayer(this.Owner);

                        // TODO fire-eggs captured gold
                        // TODO fire-eggs captured advance?
                        // TODO fire-eggs all owned units convert?
						Show captureCity = Show.CaptureCity(this, null);
						captureCity.Done += (s1, a1) =>
						{
							this.Owner = admired.Owner;

							previousOwner.HandleExtinction();

							if (Human == admired.Owner)
							{
								GameTask.Insert(Tasks.Show.CityManager(this));
							}
						};

						if (Human == admired.Owner)
						{
							humanGetsCity = true;
							GameTask.Insert(captureCity);
						}

					}

					break;
			}

			if (message.Count > 0 && (Player.IsHuman || humanGetsCity))
			{
				GameTask.Enqueue(Message.Advisor(Advisor.Domestic, false, message.ToArray()));
			}
		}

		private int[] tradingCities;
		internal void SetTradingCitiesIndexes(int[] tradingCities)
		{
			// only keep the last 3 trading cities
			this.tradingCities = [.. tradingCities.Skip(Math.Max(0, tradingCities.Length - 3))];
		}

		public City[] TradingCities {
			get
			{
				if (tradingCities == null)
				{
					return Array.Empty<City>();
				}
				return [.. tradingCities.Select(index => Game.Instance.Cities[index])];
			}
		}

		int IndexOfCity(City city)
		{
			for (int i = 0; i < Game.Instance.Cities.Count; i++)
			{
				if (Game.Instance.Cities[i] == city) return i;
			}
			return -1;
		}

		public void AddTradingCity(City city)
		{
			if (city == null || city == this || TradingCities.Contains(city))
			{
				return;
			}

			List<City> cities = [.. TradingCities];
			cities.Add(city);
			SetTradingCitiesIndexes([.. cities.Select(c => IndexOfCity(c)).Where(i => i >= 0)]);
		}


		internal City(byte owner)
		{
			Owner = owner;
			if (!Game.Started) return;
			CurrentProduction = Reflect.GetUnits().Where(u => Player.ProductionAvailable(u)).OrderBy(u => Common.HasAttribute<Default>(u) ? -1 : (int)u.Type).First();
			SetResourceTiles();
		}


		public enum CityStatus
		{
			RIOT = 0,
			COASTAL = 1,
			CELEBRATION_CANCELLED = 2,
			HYDRO_AVAILABLE = 3,
			AUTO_BUILD = 4,
			TECH_STOLEN = 5,
			CELEBRATION_RAPTURE = 6,
			IMPROVEMENT_SOLD = 7
		}
	}
}
