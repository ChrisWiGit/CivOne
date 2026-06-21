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
using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.Persistence.Game;
using CivOne.Screens.Services;
using CivOne.Services.SpaceShip;
using CivOne.src;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	public interface ICity : ITurn, ICityOnContinent, ICityMapper
    { }
	public class City : BaseInstance, ICity
	{
		// Dependency Injection
		// TODO: Replace by DI container instantiation
		internal BitFlagExtensions bitFlagExtensions = new();
		
		Guid _id = Guid.NewGuid();
		
		/*
		New. Each City gets a unique Guid.
		For new saving format.
		*/
		public Guid Id { get => _id; set => _id = value; }
		

		internal int NameId { get; set; }
		internal byte X;
		internal byte Y;

		public Point Location => new(X, Y);

		private byte _owner;
		public byte CityOwnerPlayerIndex
		{
			get => _owner;
			set
			{
				_owner = value;
				InvalidateCityBreakdownCache();
				ResetResourceTiles();
			}
		}
		public string Name => Game.CityNames[NameId];
		private byte _size;
		public byte Size
		{
			get => _size;
			set
			{
				if (X == 255 || Y == 255) return;

				_size = value;
				InvalidateCityBreakdownCache();
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
		public int Shields { get; set; }
		public int Food { get; set; }
		public IProduction CurrentProduction { get; private set; }

		private List<ITile> _resourceTiles = [];
		private List<Citizen> _specialists = [];
		private ICityEconomyService? _economyService;

		internal List<ITile> SetupResourceTiles
		{
			get => _resourceTiles;
			set
			{
				_resourceTiles = value;
				InvalidateCityBreakdownCache();
			}
		}

		/// <summary>
		/// List of tiles that are worked by the city, always including the city tile itself.
		/// </summary>
		public ITile[] ResourceTiles => [.. CityTiles.Where(t => (t.X == X && t.Y == Y) || _resourceTiles.Contains(t))];
		public Citizen[] Specialists => _specialists.ToArray();

		internal List<Citizen> SetupSpecialists
		{
			get => _specialists;
			set
			{
				_specialists = value;
			}
		}

		private List<IBuilding> _buildings = [];
		private List<IWonder> _wonders = [];

		public IBuilding[] Buildings => _buildings.OrderBy(b => b.Id).ToArray();
		public IWonder[] Wonders => _wonders.OrderBy(b => b.Id).ToArray();

		public bool HasBuilding(IBuilding building) => _buildings.Any(b => b.Id == building.Id);
		public bool HasBuilding(Type type) => _buildings.Any(b => b.GetType() == type);
		public bool HasBuilding<T>() where T : IBuilding => _buildings.Any(b => b is T);

		public bool HasWonder(IWonder wonder) => _wonders.Any(w => w.Id == wonder.Id);
		public bool HasWonder(Type type) => _wonders.Any(w => w.GetType() == type);
		public bool HasWonder<T>() where T : IWonder => _wonders.Any(w => w is T);


        public int Entertainers => _specialists.Count(c => c == Citizen.Entertainer);
        public int Scientists => _specialists.Count(c => c == Citizen.Scientist);
        public int Taxmen => _specialists.Count(c => c == Citizen.Taxman);

		public bool WasInDisorder {get; set;}

        public bool IsBuildingWonder => CurrentProduction is IWonder;

		/// Number of shields required to maintain units
		internal int ShieldCosts
		{
			get
			{
				IGovernment government = Game.GetPlayer(_owner)!.Government;
				// store unitCount once to avoid multiple enumeration.
				int unitCount = Units.Count(u => u is not Diplomat && u is not Caravan);
				if (government is Anarchy || government is Despotism)
				{
					int costs = 0;
					for (int i = 0; i < unitCount; i++)
					{
						if (i < _size) continue;
						costs++;
					}
					return costs;
				}
				return unitCount;
			}
		}

		internal int ShieldIncome => ShieldTotal - ShieldCosts;

        /// How much food required for citizens and settlers		
		internal int FoodCosts
		{
			get
			{
				int costs = (_size * 2);
				IGovernment government = Game.GetPlayer(_owner)!.Government;
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

		internal int FoodIncome => FoodRaw - FoodCosts;
		internal int FoodRequired => (int)(Size + 1) * 10;
		internal int FoodTotal => FoodRaw;

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
					if (!CityOwnerPlayer.AnarchyDespotism && tile.Irrigation) output += 1;
					break;
				case Terrain.Ocean:
				case Terrain.Tundra:
					if (!CityOwnerPlayer.AnarchyDespotism && tile.Special) output += 1;
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
					if (!CityOwnerPlayer.AnarchyDespotism && tile.Mine) output += 1;
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
				int shields = ShieldRaw;
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
					if (CityOwnerPlayer.RepublicDemocratic) output += 1;
					break;
				case Terrain.Ocean:
				case Terrain.River:
					if (CityOwnerPlayer.RepublicDemocratic) output += 1;
					break;
				case Terrain.Jungle:
					if (!tile.Special) break;
					if (CityOwnerPlayer.MonarchyCommunist) output += 1;
					if (CityOwnerPlayer.RepublicDemocratic) output += 2;
					break;
				case Terrain.Mountains:
					if (!tile.Special) break;
					if (CityOwnerPlayer.MonarchyCommunist) output += 1;
					if (CityOwnerPlayer.RepublicDemocratic) output += 2;
					break;
			}
			if (output > 0 && HasWonder<Colossus>() && !Game.WonderObsolete<Colossus>()) output += 1;
			if (tile.Pollution) output = (int)Math.Ceiling((double)output * 0.5);
			return output;
		}

		private CityEconomyBreakdown? _cachedCityBreakdown;
		private int? _cachedFoodRaw;
		private ulong _cachedFoodRawStateHash = ulong.MaxValue;
		private int? _cachedShieldRaw;
		private ulong _cachedShieldRawStateHash = ulong.MaxValue;
		private const int TileFoodHashOffset = 128;
		private const int TileShieldHashOffset = 128;

		private CityEconomyBreakdown GetCachedCityBreakdown()
		{
			if (!_cachedCityBreakdown.HasValue)
			{
				_cachedCityBreakdown = GetCityBreakdown();
			}
			return _cachedCityBreakdown.Value;
		}

		internal void InvalidateCityBreakdownCache()
		{
			_cachedCityBreakdown = null;
			_cachedFoodRaw = null;
			_cachedFoodRawStateHash = ulong.MaxValue;
			_cachedShieldRaw = null;
			_cachedShieldRawStateHash = ulong.MaxValue;
		}

		private int FoodRaw
		{
			get
			{
				ulong currentFoodStateHash = GetFoodRawStateHash();
				if (!_cachedFoodRaw.HasValue || _cachedFoodRawStateHash != currentFoodStateHash)
				{
					_cachedFoodRaw = ResourceTiles.Sum(t => FoodValue(t));
					_cachedFoodRawStateHash = currentFoodStateHash;
				}

				return _cachedFoodRaw.Value;
			}
		}

		private int ShieldRaw
		{
			get
			{
				ulong currentShieldStateHash = GetShieldRawStateHash();
				if (!_cachedShieldRaw.HasValue || _cachedShieldRawStateHash != currentShieldStateHash)
				{
					_cachedShieldRaw = ResourceTiles.Sum(t => ShieldValue(t));
					_cachedShieldRawStateHash = currentShieldStateHash;
				}

				return _cachedShieldRaw.Value;
			}
		}

		private ulong GetFoodRawStateHash()
		{
			unchecked
			{
				// FNV-1a 64-bit hash over food-affecting city/tile state.
				ulong hash = 1469598103934665603UL;

				hash = (hash ^ (uint)CityOwnerPlayerIndex) * 1099511628211UL;
				hash = (hash ^ (CityOwnerPlayer.AnarchyDespotism ? 1UL : 0UL)) * 1099511628211UL;
				foreach (ITile tile in ResourceTiles)
				{
					hash = (hash ^ (uint)tile.X) * 1099511628211UL;
					hash = (hash ^ (uint)tile.Y) * 1099511628211UL;
					hash = (hash ^ (uint)tile.Type) * 1099511628211UL;
					// Food is sbyte (-128..127); shift into 0..255 for stable hashing.
					hash = (hash ^ (uint)(tile.Food + TileFoodHashOffset)) * 1099511628211UL;
					hash = (hash ^ (tile.Special ? 1UL : 0UL)) * 1099511628211UL;
					hash = (hash ^ (tile.Irrigation ? 1UL : 0UL)) * 1099511628211UL;
					hash = (hash ^ (tile.RailRoad ? 1UL : 0UL)) * 1099511628211UL;
					hash = (hash ^ (tile.Pollution ? 1UL : 0UL)) * 1099511628211UL;
				}

				return hash;
			}
		}

		private ulong GetShieldRawStateHash()
		{
			unchecked
			{
				// FNV-1a 64-bit hash over shield-affecting city/tile state.
				ulong hash = 1469598103934665603UL;

				hash = (hash ^ (uint)CityOwnerPlayerIndex) * 1099511628211UL;
				hash = (hash ^ (CityOwnerPlayer.AnarchyDespotism ? 1UL : 0UL)) * 1099511628211UL;
				foreach (ITile tile in ResourceTiles)
				{
					hash = (hash ^ (uint)tile.X) * 1099511628211UL;
					hash = (hash ^ (uint)tile.Y) * 1099511628211UL;
					hash = (hash ^ (uint)tile.Type) * 1099511628211UL;
					// Shield is sbyte (-128..127); shift into 0..255 for stable hashing.
					hash = (hash ^ (uint)(tile.Shield + TileShieldHashOffset)) * 1099511628211UL;
					hash = (hash ^ (tile.Mine ? 1UL : 0UL)) * 1099511628211UL;
					hash = (hash ^ (tile.RailRoad ? 1UL : 0UL)) * 1099511628211UL;
					hash = (hash ^ (tile.Pollution ? 1UL : 0UL)) * 1099511628211UL;
				}

				return hash;
			}
		}

		// CW: Prevent negative trade values.
		// Negative trade can occur when corruption exceeds the total trade generated by resource tiles,
		// often in cities located on distant continents with high corruption.
		// This negative value is not displayed in the city screen, but appears in the trade report and confuses players.
		internal int RawTradeTotal => Math.Max(0, ResourceTiles.Sum(TradeValue) - Corruption);
		internal int TradeTotal => GetCachedCityBreakdown().TradeTotal;
		internal int TotalTrade => GetCachedCityBreakdown().TotalTrade;
		internal short TradeScience => GetCachedCityBreakdown().TradeScience;
		internal short TradeLuxuries => GetCachedCityBreakdown().TradeLuxuries;
		internal short TradeTaxes => GetCachedCityBreakdown().TradeTaxes;


		public bool CityOfSameCiv(City city)
		{
			if (city == null) return false;
			return city.CityOwnerPlayer == CityOwnerPlayer;
		}

		private int CalculateTradeValue(City city)
		{
			// CW: Source Civilization Or Rome on 640k A Day by Johnny L. Wilson et al. page 230
			int sameCivPenalty = CityOfSameCiv(city) ? 2 : 1;
			int trading = (int)Math.Round((city.RawTradeTotal + RawTradeTotal + 4) / 8.0 / sameCivPenalty);
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
				foreach (City city in TradingCitiesAsCity)
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
		public int TradingCitiesSumValue => TradingCitiesAsCity.Sum(CalculateTradeValue);

		public int TotalIncome => Taxes;

		public int GetRealTotalIncome()
		{
			CitizenTypes citizenTypes = GetCitizenTypes();
			if (citizenTypes.InDisorder)
			{
				return 0;
			}
			return TotalIncome;
		}
		/// <summary>
		/// Amount of corruption, taking government, buildings, and distance
		/// to capital into account.
		/// </summary>
		internal int Corruption
		{
			get
			{
				IGovernment government = CityOwnerPlayer.Government;
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
						City? capital = CityOwnerPlayer.GetCapital();
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
		public short Luxuries
		{
			get => GetCityBreakdown().Luxuries;
		}

		public int EntertainerLuxuries {
			get => Entertainers * 3;	
		}

		/// <summary>
		/// Amount of taxes collected, taking trade, buildings, and taxmen
		/// into account.
		/// </summary>
		internal short Taxes
		{
			get => GetCityBreakdown().Taxes;
		}

		/// <summary>
		/// Amount of science generated, taking trade, buildings, and scientists
		/// into account.
		/// </summary>
		internal short Science
		{
			get => GetCityBreakdown().Science;
		}

		private CityEconomyBreakdown GetCityBreakdown()
		{
			return GetEconomyService().CalculateBreakdown();
		}

		private ICityEconomyService GetEconomyService()
		{
			return _economyService ??= ICityEconomyService.Create(this, Game);
		}
		internal int GetRealTotalScience()
		{
			CitizenTypes citizenTypes = GetCitizenTypes();
			if (citizenTypes.InDisorder)
			{
				return 0;
			}
			return Science;
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
			else if (CityOwnerPlayer.HasAdvance<Plastics>()) pollutionMultiplier = 100;
			else if (CityOwnerPlayer.HasAdvance<MassProduction>()) pollutionMultiplier = 75;
			else if (CityOwnerPlayer.HasAdvance<Automobile>()) pollutionMultiplier = 50;
			else if (CityOwnerPlayer.HasAdvance<Industrialization>()) pollutionMultiplier = 25;

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

			int maxRandom = 256 - (CityOwnerPlayer.Advances.Length * (1 + Game.Difficulty) / 2);
			if (maxRandom < 1) maxRandom = 2; // Prevents bug -> still 50% chance of pollution with 256 advances

			int rnd = RandomService.NextInt(maxRandom);

			return (2 * SmokeStacks) > rnd;
		}

		internal byte _status;

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
			get => bitFlagExtensions.HasFlag(_status, CityStatus.Riot);
			set => SetStatusFlag(CityStatus.Riot, value);
		}

		public bool IsCoastal 
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.Coastal);
			set => SetStatusFlag(CityStatus.Coastal, value);
		}

		public bool CelebrationCancelled
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.CelebrationCancelled);
			set => SetStatusFlag(CityStatus.CelebrationCancelled, value);
		}

		public bool HydroAvailable
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.HydroAvailable);
			set => SetStatusFlag(CityStatus.HydroAvailable, value);
		}

		public bool AutoBuild
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.AutoBuild);
			set => SetStatusFlag(CityStatus.AutoBuild, value);
		}

		public bool TechStolen
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.TechnologyStolen);
			set => SetStatusFlag(CityStatus.TechnologyStolen, value);
		}

		public bool CelebrationOrRapture
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.CelebrationOrRapture);
			set => SetStatusFlag(CityStatus.CelebrationOrRapture, value);
		}
		
		/// <summary>
		/// Was a building sold in this turn?
		/// </summary>
		public bool BuildingSold
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.ImprovementSold);
			set => SetStatusFlag(CityStatus.ImprovementSold, value);
		}

		internal void SetStatusFlag(CityStatus status, bool value)
		{
			_status = value ? bitFlagExtensions.SetFlag(_status, status) : bitFlagExtensions.ClearFlag(_status, status);
		}

		internal bool OccupiedTile(ITile tile)
		{
			if (ResourceTiles.Any(t => t.X == tile.X && t.Y == tile.Y))
				return false;
			return InvalidTile(tile);
		}

		internal bool InvalidTile(ITile tile)
		{
			return (Game.GetCities().Where(c => c != this).Any(c => c.ResourceTiles.Any(t => t.X == tile.X && t.Y == tile.Y)) || tile.Units.Any(u => u.Owner != CityOwnerPlayerIndex));
		}

		internal void UpdateSpecialists()
		{
			int target = Size - (ResourceTiles.Length - 1);
			
			// This can happen if city size shrinks while ResourceTiles still contains more tiles than the new size,
			// or if workable tiles are limited by invalid terrain.
			// Debug.Assert(target >= 0, "City.UpdateSpecialists: target < 0");
			if (target < 0) target = 0;

			_specialists =
			[
				.. _specialists.Take(target),
				.. Enumerable.Repeat(Citizen.Entertainer, Math.Max(0, target - _specialists.Count)),
			];
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
						if (y == -1) output[0] |= (byte)(0x01 << 4);
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
			InvalidateCityBreakdownCache();

			if (_resourceTiles.Count > Size)
			{
				_resourceTiles.RemoveRange(Size, _resourceTiles.Count - Size);
			}

			for (int index = _resourceTiles.Count; index < Size; index++)
			{
				// CW: must recalculate due to tile removal
				var resourceTiles = ResourceTiles;
				var bestTile = CityTiles
					.Where(t => !OccupiedTile(t) && !resourceTiles.Contains(t))
					.OrderByDescending(FoodValue)
					.ThenByDescending(ShieldValue)
					.ThenByDescending(TradeValue)
					.FirstOrDefault();

				if (bestTile == null)
				{
					// No free worked tile left; stop here to avoid an infinite loop.
					// A city may stay underfilled in this edge case, but that is safer than hanging.
					break;
				}

				_resourceTiles.Add(bestTile);
			}

			UpdateSpecialists();
			SetupCoastalFlag();
		}

		private void SetupCoastalFlag()
		{
			if (!Game.Started) return;

			bool isCoastal = Map[X, Y].GetBorderTiles().Any(t => t.IsOcean);

			SetStatusFlag(CityStatus.Coastal, isCoastal);
		}

		private void SetupHydroFlag()
		{
			if (!Game.Started) return;

			bool isHydroAvailable = Map[X, Y].GetBorderTiles().Any(t => t is Mountains or River);

			SetStatusFlag(CityStatus.HydroAvailable, isHydroAvailable);
		}

		public void ResetResourceTiles()
		{
			InvalidateCityBreakdownCache();
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
			
			bool contains = _resourceTiles.Contains(tile);
			_resourceTiles.Remove(tile); // remove does not need a Contains check.
			
			if (contains)
			{
				InvalidateCityBreakdownCache();
				UpdateSpecialists();

				return;
			}
			_resourceTiles.Add(tile);
			InvalidateCityBreakdownCache();
			UpdateSpecialists();
		}

		public Player CityOwnerPlayer => Game.Instance.GetPlayer(CityOwnerPlayerIndex)!;
		public IPlayer PlayerIntf => CityOwnerPlayer;

		/// <summary>
		/// Return the list of possible city production [units, buildings,
		/// or wonders] taking location and advancements into account.
		/// </summary>
		public IEnumerable<IProduction> AvailableProduction
		{
			get
			{
				foreach (IUnit unit in Reflect.GetUnits().Where(u => CityOwnerPlayer.ProductionAvailable(u)))
				{
					if (unit.UnitCategory == UnitClass.Water && !Map[X, Y].GetBorderTiles().Any(t => t.IsOcean)) continue;
					if (unit is Nuclear && !Game.WonderBuilt<ManhattanProject>()) continue;
					yield return unit;
				}


				if (!HasBuilding<Palace>())
				{
					// CW: Show always Palace as available production if not yet built. It moves capital city to this city.
					yield return new Palace();
				}
									
				foreach (IBuilding building in Reflect.GetBuildings().Where(b => CityOwnerPlayer.ProductionAvailable(b) && !_buildings.Any(x => x.Id == b.Id)))
					{
						if (HasBuilding<Palace>() && building is Courthouse) continue;
						yield return building;
					}
				foreach (IWonder wonder in Reflect.GetWonders().Where(b => CityOwnerPlayer.ProductionAvailable(b)))
				{
					yield return wonder;
				}
			}
		}

		public void SetProduction(IProduction production) => CurrentProduction = production;

		internal void SetProduction(byte productionId)
		{
			IProduction? production = Reflect.GetProduction().FirstOrDefault(p => p.ProductionId == productionId);
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
			int buyPrice = BuyPrice;
			if (buyPrice <= 0) return false;
			if (IsRiot && CurrentProduction is IBuilding) return false;
			if (CityOwnerPlayer.Gold < buyPrice) return false;

			CityOwnerPlayer.Gold -= (short)buyPrice;
			Shields = CurrentProduction.Price * 10;
			return true;
		}

		public bool UpdateAutoBuild()
		{
			if (!AutoBuild)
			{
				return false;
			}
			AI.Instance(CityOwnerPlayer).CityProduction(this);

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
	

		public int ContinentId => Map[X, Y].ContinentId;

		internal IEnumerable<CitizenTypes> Residents
		{
			get
			{
				var service = ICityCitizenService.Create(this,
					Game.Instance, _specialists, Map.Instance);
				return service.EnumerateCitizens();
			}
		}

		/// <summary>
		/// Get the citizen types for the city.
		/// This was refactored from the property Citizens because calling it creates a bigger workload
		/// than you would expect when accessing as a property.
		/// So in this way you have to explicitly call the method when you need it.
		/// In the same way, the properties IsInDisorder, ContentCitizens, UnhappyCitizens, HappyCitizens
		/// also have been removed. Use the returned structure of GetCitizenTypes() instead.
		/// </summary>
		/// <returns></returns>
		internal CitizenTypes GetCitizenTypes()
		{
			UpdateSpecialists();

			var service = ICityCitizenService.Create(this,
				Game.Instance, _specialists, Map.Instance);
			return service.GetCitizenTypes();
		}
		internal IEnumerable<Citizen> GetCitizens()
		{
			return GetCitizenTypes().Citizens;
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
				ITile?[,] tiles = CityRadius;
				for (int xx = 0; xx < 5; xx++)
				{
					for (int yy = 0; yy < 5; yy++)
					{
						if (tiles[xx, yy] == null) continue;
						yield return tiles[xx, yy]!; //TODO: we should not use ! but CityTiles is currently used in many places. 
					}
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
				ITile?[,] tiles = Map[X - 2, Y - 2, 5, 5];
				for (int xx = 0; xx < 5; xx++)
				for (int yy = 0; yy < 5; yy++)
				{
					ITile? tile = tiles[xx, yy];
					if (tile == null) continue;
					if ((xx == 0 || xx == 4) && (yy == 0 || yy == 4)) tiles[xx, yy] = null;
					if (!CityOwnerPlayer.Visible(tile)) tiles[xx, yy] = null;
				}
				return tiles!; //TODO: we should not use ! but CityRadius is currently used in many places.
			}
		}

		private readonly List<IUnit> _homeUnits = [];
		internal void AddHomeUnit(IUnit unit) { if (!_homeUnits.Contains(unit)) _homeUnits.Add(unit); }
		internal void RemoveHomeUnit(IUnit unit) => _homeUnits.Remove(unit);
		public IUnit[] Units => _homeUnits.ToArray();

		public ITile Tile => Map[X, Y];



		public void AddBuilding(IBuilding building) => _buildings.Add(building);

		/// <summary>
		/// Sell a city building.
		/// </summary>
		/// <param name="building"></param>
		public void SellBuilding(IBuilding building)
		{
			RemoveBuilding(building);
			CityOwnerPlayer.Gold += building.SellPrice;
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
					foreach (IUnit unit in Game.GetUnits().Where(x => x.Owner == CityOwnerPlayerIndex && x.UnitCategory ==  UnitClass.Water && x.MovesLeft == x.Move))
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

		private bool HandleSpaceShipProduction()
		{
			if (CurrentProduction is not SpaceShip)
			{
				return false;
			}

			ISpaceShipService service = SpaceShipServiceFactoryProvider.GetInstance().Create(CityOwnerPlayer);
			SpaceShipComponentType partType = CurrentProduction switch
			{
				SSStructural => SpaceShipComponentType.Structural,
				SSComponent => SpaceShipComponentType.Component,
				SSModule => SpaceShipComponentType.Module,
				_ => SpaceShipComponentType.Empty
			};

			bool canShowInstallScreen = partType switch
			{
				SpaceShipComponentType.Structural => service.CanAddPart(partType),
				SpaceShipComponentType.Component => SpaceShipPartOptions.HasAnyAvailable(service, partType),
				SpaceShipComponentType.Module => SpaceShipPartOptions.HasAnyAvailable(service, partType),
				_ => false
			};

			if (partType != SpaceShipComponentType.Empty && canShowInstallScreen && CurrentProduction is ICivilopedia civilopedia)
			{
				Shields = 0;
				Message message = Message.Newspaper(this, TranslateFormattedArray("{0} builds\n{1}.", Name, civilopedia.TranslatedName));
				message.Done += (_, __) =>
				{
					Show showSpaceShip = Show.SpaceShipWithInstall(partType);
					showSpaceShip.Done += (_, __) => GameTask.Insert(Show.CityManager(this));
					GameTask.Enqueue(showSpaceShip);
				};
				GameTask.Enqueue(message);
			}

			return true;
		}

		private bool HandlePalaceBuilding()
		{
			if (CurrentProduction is not Palace)
			{
				return false;
			}
			if (CurrentProduction is not IBuilding palaceBuilding)
			{
				Log($"Error: Palace production is not an IBuilding in city {Name}");
				return false;
			}

			Shields = 0;
			foreach (City city in Game.Instance.GetCities().Where(c => c.CityOwnerPlayerIndex == CityOwnerPlayerIndex))
			{
				// Remove palace from all cites.
				city.RemoveBuilding<Palace>();
			}
			if (HasBuilding<Courthouse>())
			{
				_buildings.RemoveAll(x => x is Courthouse);
			}
			_buildings.Add(palaceBuilding);

			if (CurrentProduction is ICivilopedia civilopedia)
			{
				Message message = Message.Newspaper(this, TranslateFormattedArray("{0} builds\n{1}.", Name, civilopedia.TranslatedName));
				message.Done += (_, __) =>
				{
					GameTask advisorMessage = Message.Advisor(Advisor.Foreign, true, $"{CityOwnerPlayer.TribeName} capital", $"moved to {Name}.");
				advisorMessage.Done += (_, __) => GameTask.Insert(Show.CityManager(this));
				GameTask.Enqueue(advisorMessage);
				};
				
				GameTask.Enqueue(message);
			}

			return true;
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

			int tileToPollute = RandomService.NextInt(possiblePollutionTiles.Count);

			possiblePollutionTiles[tileToPollute].Pollution = true;

			if (Human != CityOwnerPlayerIndex)
			{
				return;
			}

			GameTask.Enqueue(Message.Newspaper(this, TranslateFormattedArray("Pollution in\n{0}!\nHealth problems feared.", Name)));
		}

		public void NewTurn()
		{
			// City was destroyed (DestroyCity sets X/Y to 255 as tombstone but keeps the object
			// in _cities for index-stability of trading routes). Skip processing it.
			if (X == 255 || Y == 255) return;

			ExecutePollution();

			UpdateResources();

			CitizenTypes citizenTypes = GetCitizenTypes();

			if (citizenTypes.InDisorder)
			{
				if (RandomService.NextInt(20) == 1 && HasBuilding<NuclearPlant>() && !CityOwnerPlayer.HasAdvance<FusionPower>())
				{
					// todo: meltdown
				}
				if (WasInDisorder)
				{
					if (CityOwnerPlayer.IsHuman)
						GameTask.Insert(Message.Advisor(Advisor.Domestic, true, "Civil Disorder in", $"{Name}! Mayor", "flees in panic."));
				}
				else
				{
					// TODO fire-eggs not showing loses side-effects
					if (CityOwnerPlayer.IsHuman) // && !Game.Animations)
					{
						Show disorderCity = Show.DisorderCity(this);
						GameTask.Insert(disorderCity);
					}

					Log($"City {Name} belonging to {CityOwnerPlayer.TribeName} has gone into disorder");
				}
				if (WasInDisorder && CityOwnerPlayer.Government is Governments.Democracy)
				{
					// todo: Force revolution
				}
				WasInDisorder = true;
			}
			else
			{
				if (WasInDisorder)
				{
					if (CityOwnerPlayer.IsHuman)
						GameTask.Insert(Message.Advisor(Advisor.Domestic, true, "Order restored", $" in {Name}."));
					Log($"City {Name} belonging to {CityOwnerPlayer.TribeName} is no longer in disorder");
				}
				WasInDisorder = false;
			}
			if (citizenTypes.unhappy == 0 && citizenTypes.redShirt == 0 && citizenTypes.happy >= citizenTypes.content && Size >= 3)
			{
				// we love the president day
				if (CityOwnerPlayer.Government is Governments.Democracy || CityOwnerPlayer.Government is Republic)
				{
					if (Food > 0)
					{
						Size++;
					}
				}
				else
				{
					// we love the king day
					if (Human == CityOwnerPlayerIndex && Settings.Animations != GameOption.Off)
						GameTask.Insert(Show.WeLovePresidentDayCity(this));
				}
			}
			Food += citizenTypes.InDisorder ? 0 : FoodIncome;

			if (Food < 0)
			{
				Food = 0;
				Size--;
				if (Human == CityOwnerPlayerIndex)
				{
					GameTask.Enqueue(Message.Newspaper(this, TranslateFormattedArray("Food storage exhausted\nin {0}!\nFamine feared.", Name)));
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
						Food = FoodRequired / 2;
					}
				}
			}

			if (ShieldIncome < 0)
			{
				int maxDistance = Units.Max(u => Common.DistanceToTile(X, Y, u.X, u.Y));
				IUnit unit = Units.Last(u => Common.DistanceToTile(X, Y, u.X, u.Y) == maxDistance);
				if (Human == CityOwnerPlayerIndex)
				{
					Message message = Message.DisbandUnit(this, unit);
					message.Done += (_, __) =>
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
				Shields += citizenTypes.InDisorder ? 0 : ShieldIncome;
			}

			if (Shields >= (int)CurrentProduction.Price * 10)
			{
				if (CurrentProduction is Settlers && Size == 1 && Game.Difficulty == 0)
				{
					// On Chieftain level, it's not possible to create a Settlers in a city of size 1
				}
				else if (CurrentProduction is IUnit unitProduction)
				{
					Shields = 0;
					IUnit? unit = Game.Instance.CreateUnit(unitProduction.Type, X, Y, CityOwnerPlayerIndex);

					if (unit == null)
					{
						Log($"Error: Failed to create unit of type {unitProduction.Type} in city {Name}");
						return;
					}

					unit.SetHome();
					unit.Veteran = _buildings.Any(b => b is Barracks);
					if (CurrentProduction is Settlers)
					{
						if (Size == 1 && CityOwnerPlayer.Cities.Length == 1) Size++;
						if (Size == 1)
						{
							unit.SetHome(null);
						}
						Size--;
					}
					if (Human == CityOwnerPlayerIndex && (unit is Settlers || unit is Diplomat || unit is Caravan))
					{
						GameTask advisorMessage = Message.Advisor(Advisor.Defense, true, $"{Name} builds {unit.TranslatedName}.");
						advisorMessage.Done += (_, __) => GameTask.Insert(Show.CityManager(this));
						GameTask.Enqueue(advisorMessage);
					}
				}
				if (CurrentProduction is IBuilding buildingProduction && !_buildings.Any(b => b.Id == buildingProduction.Id) && !HandleSpaceShipProduction() && !HandlePalaceBuilding())
				{
					Shields = 0;
					_buildings.Add(buildingProduction);
					GameTask.Enqueue(new ImprovementBuilt(this, buildingProduction));
				}
				if (CurrentProduction is IWonder wonderProduction && !Game.Instance.BuiltWonders.Any(w => w.Id == wonderProduction.Id))
				{
					Shields = 0;
					AddWonder(wonderProduction);
					GameTask.Enqueue(new ImprovementBuilt(this, wonderProduction));
				}
			}

			// TODO: Handle luxuries
			CityOwnerPlayer.Gold += citizenTypes.InDisorder ? (short)0 : Taxes;
			CityOwnerPlayer.Gold -= TotalMaintenance;
			CityOwnerPlayer.Science += Science;

			BuildingSold = false;
			GameTask.Enqueue(new ProcessScience(CityOwnerPlayer));

			if (CityOwnerPlayer.IsHuman)
			{
				UpdateAutoBuild();
			}
			else
			{
				CityOwnerPlayer.AI?.CityProduction(this);
			}
		}

		public void Disaster()
		{
			List<string> message = [];
			bool humanGetsCity = false;

			if (CityOwnerPlayer.Cities.Length == 1)
				return;

			if (Size < 5)
				return;

			switch (RandomService.NextInt(0, 9))
			{
				case 0:
				{
					// Earthquake
					bool hillsNearby = CityTiles.Any(t => t.Type == Terrain.Hills);
					IList<IBuilding> buildingsOtherThanPalace = [.. Buildings.OfType<IBuilding>().Where(b => b is not Palace)];
					
					if (!hillsNearby || !buildingsOtherThanPalace.Any())
						return;

					IBuilding buildingToDestroy = RandomService.NextElement(buildingsOtherThanPalace);
					RemoveBuilding(buildingToDestroy);

					message.Add(TranslateFormatted("Earthquake in {0}!", Name));
					message.Add(TranslateFormatted("{0} destroyed!", buildingToDestroy.TranslatedName));

					break;
				}
				case 1:
				{
					// Plague
					bool hasMedicine = CityOwnerPlayer.HasAdvance<Medicine>();
					bool hasAqueduct = HasBuilding<Aqueduct>();
					bool hasConstruction = CityOwnerPlayer.Advances.Any(a => a is Construction);

					if (!hasMedicine && !hasAqueduct && hasConstruction)
					{
						Size = (byte)(Size - Size / 4);

						message.Add(TranslateFormatted("Plague in {0}!", Name));
						message.Add(Translate("Citizens killed!"));
						message.Add(Translate("Citizens demand AQUEDUCT."));
					}

					break;
				}
				case 2:
				{
					// Flooding
					bool riverNearby = CityTiles.Any(t => t.Type == Terrain.River);
					bool hasCityWalls = HasBuilding<CityWalls>();
					bool hasMasonry = CityOwnerPlayer.HasAdvance<Masonry>();

					if (riverNearby && !hasCityWalls && hasMasonry)
					{
						Size = (byte)(Size - Size / 4);

						message.Add(TranslateFormatted("Flooding in {0}!", Name));
						message.Add(Translate("Citizens killed!"));
						message.Add(Translate("Citizens demand CITY WALLS."));
					}
					break;
				}
				case 3:
				{
					// Volcano
					bool mountainNearby = CityTiles.Any(t => t.Type == Terrain.Mountains);
					bool hasTemple = HasBuilding<Temple>();
					bool hasCeremonialBurial = CityOwnerPlayer.HasAdvance<CeremonialBurial>();

					if (mountainNearby && !hasTemple && hasCeremonialBurial)
					{
						Size = (byte)(Size - Size / 3);

						message.Add(TranslateFormatted("Volcano erupts near {0}!", Name));
						message.Add(Translate("Citizens killed!"));
						message.Add(Translate("Citizens demand TEMPLE."));
					}

					break;
				}
				case 4:
				{
					// Famine
					bool hasGranary = HasBuilding<Granary>();
					bool hasPottery = CityOwnerPlayer.HasAdvance<Pottery>();

					if (!hasGranary && hasPottery)
					{
						Size = (byte)(Size - Size / 3);

						message.Add(TranslateFormatted("Famine in {0}!", Name));
						message.Add(Translate("Citizens killed!"));
						message.Add(Translate("Citizens demand GRANARY."));
					}

					break;
				}
				case 5:
				{
					// Fire
					IList<IBuilding> buildingsOtherThanPalace = [.. Buildings.OfType<IBuilding>().Where(b => b is not Palace)];
					bool hasAqueduct = HasBuilding<Aqueduct>();
					bool hasConstruction = CityOwnerPlayer.HasAdvance<Construction>();

					if (buildingsOtherThanPalace.Any() && !hasAqueduct && hasConstruction)
					{
						IBuilding buildingToDestroy = RandomService.NextElement(buildingsOtherThanPalace);
						RemoveBuilding(buildingToDestroy);

						message.Add(TranslateFormatted("Fire in {0}!", Name));
						message.Add(TranslateFormatted("{0} destroyed!", buildingToDestroy.TranslatedName));
						message.Add(Translate("Citizens demand AQUEDUCT."));
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

						message.Add(TranslateFormatted("Pirates plunder {0}!", Name));
						message.Add(Translate("Production halted, Food Stolen.!"));
						message.Add(Translate("Citizens demand BARRACKS."));
					}

					break;
				}
				case 7:
				case 8:
				case 9:
					// Riot, scandal, corruption

					string[] disasterTypes = ["Scandal", "Riot", "Corruption"];
					string disasterType = RandomService.NextElement(disasterTypes);
					string buildingDemanded = "";
					CitizenTypes citizenTypes = GetCitizenTypes();

					if (!citizenTypes.InDisorder)
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

					message.Add(TranslateFormatted("{0} in {1}", disasterType, Name));
					message.Add(TranslateFormatted("Citizens demand {0}", buildingDemanded));

					if (HasBuilding<Palace>())
						return;

					if (CityOwnerPlayer.Cities.Length < 4)
						return;

					City? admired = null;
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

						CitizenTypes ct = city.GetCitizenTypes();

						int appeal = (ct.happy - ct.unhappy) * 32 / city.Tile.DistanceTo(this);
						if (appeal > 4 && appeal > mostAppeal)
						{
							admired = city;
							mostAppeal = appeal;
						}
					}

					if (admired != null && admired.CityOwnerPlayerIndex != CityOwnerPlayerIndex)
					{
						message.Clear();
						message.Add(TranslateFormatted("Residents of {0} admire the prosperity of {1}", Name, admired.Name));
						message.Add(TranslateFormatted("{0} capture {1}", admired.Name, Name));

						Player previousOwner = CityOwnerPlayer;

                        // TODO fire-eggs captured gold
                        // TODO fire-eggs captured advance?
                        // TODO fire-eggs all owned units convert?
						Show captureCity = Show.CaptureCity(this);
						captureCity.Done += (_, __) =>
						{
							CityOwnerPlayerIndex = admired.CityOwnerPlayerIndex;
							TechStolen = false;

							previousOwner.HandleExtinction();

							if (Human == admired.CityOwnerPlayerIndex)
							{
								GameTask.Insert(Show.CityManager(this));
							}
						};

						if (Human == admired.CityOwnerPlayerIndex)
						{
							humanGetsCity = true;
							GameTask.Insert(captureCity);
						}

					}

					break;
			}

			if (message.Count > 0 && (CityOwnerPlayer.IsHuman || humanGetsCity))
			{
				GameTask.Enqueue(Message.Advisor(Advisor.Domestic, false, [.. message]));
			}
		}

		private Guid[] _tradingCityIds;
		internal void SetTradingCityIds(Guid[] ids)
		{
			// only keep the last 3 trading cities
			_tradingCityIds = [.. ids.Skip(Math.Max(0, ids.Length - 3))];
		}

		public City[] TradingCitiesAsCity {
			get
			{
				if (_tradingCityIds == null)
				{
					return [];
				}
				return [.. _tradingCityIds
					.Select(id => Game.Instance.GetCities().FirstOrDefault(c => c.Id == id))
					.OfType<City>()];
			}
		}

		public ICity[] TradingCities {
			get
			{
				return [.. TradingCitiesAsCity];
			}
		}

		private uint[] _visibleSizes = new uint[16];
		public uint[] VisibleSizes {
			get { 
				// Owner always sees his city size;
				_visibleSizes[CityOwnerPlayerIndex] = Size;
				return _visibleSizes;
			}
			set => _visibleSizes = value is { Length: >= 16 } ? value : new uint[16];
		}

		/// <summary>
		/// Legacy bridge property for the original SVE save format, which stores only a single visible city size per city —
		/// in contrast to the newer per-player <see cref="VisibleSizes"/> array.
		/// <para>
		/// In Civ1, only the human player's perception of a city's size was persisted (fog of war).
		/// This property maps to <see cref="VisibleSizes"/> at the human player's index.
		/// </para>
		/// </summary>
		public uint VisibleSizeToHumanPlayer
		{
			get
			{
				Game game = Game.Instance;
				Debug.Assert(game?.HumanPlayer != null, "City.VisibleSizeToHumanPlayer accessed before HumanPlayer was initialized.");
				if (game?.HumanPlayer == null)
				{
					return 0;
				}

				return _visibleSizes[game.HumanPlayerId];
			}
			set
			{
				Game game = Game.Instance;
				Debug.Assert(game?.HumanPlayer != null, "City.VisibleSizeToHumanPlayer assigned before HumanPlayer was initialized.");
				if (game?.HumanPlayer == null)
				{
					return;
				}

				_visibleSizes[game.HumanPlayerId] = value;
			}
		}

		public void AddTradingCity(City city)
		{
			if (city == null || city == this || TradingCitiesAsCity.Contains(city))
			{
				return;
			}

			SetTradingCityIds([.. (_tradingCityIds ?? []), city.Id]);
		}

		internal void RemoveTradingCity(City city)
		{
			if (city == null || _tradingCityIds == null || _tradingCityIds.Length == 0)
			{
				return;
			}

			SetTradingCityIds([.. _tradingCityIds.Where(id => id != city.Id)]);
		}

		internal void RemoveTradingCitiesOwnedBy(byte owner)
		{
			if (_tradingCityIds == null || _tradingCityIds.Length == 0)
			{
				return;
			}

			SetTradingCityIds([.. TradingCitiesAsCity
				.Where(tradingCity => tradingCity.CityOwnerPlayerIndex != owner)
				.Select(tradingCity => tradingCity.Id)]);
		}


		internal City(byte owner)
		{
			_visibleSizes = new uint[16];
			CityOwnerPlayerIndex = owner;
			_tradingCityIds = [];
			_resourceTiles = [];
			_buildings = [];
			_wonders = [];
			_specialists = [];
			CurrentProduction = new Settlers(); // Default production, should be overridden by caller immediately after city creation.
			
			if (!Game.Started) return;
			if (Player.Game == null) return;
			if (CityOwnerPlayer == null) return;
			
			CurrentProduction = Reflect.GetUnits()
				.Where(CityOwnerPlayer.ProductionAvailable)
				.OrderBy(u => Common.HasAttribute<DefaultUnitProductionAttribute>(u) ? -1 : (int)u.Type)
				.FirstOrDefault() ?? new Settlers(); // Default to Settlers, should never happen that no production is available at city founding, but just in case.
			SetResourceTiles();
		}


		public enum CityStatus
		{
			Riot = 0,
			Coastal = 1,
			CelebrationCancelled = 2,
			HydroAvailable = 3,
			AutoBuild = 4,
			TechnologyStolen = 5,
			CelebrationOrRapture = 6,
			ImprovementSold = 7
		}
	}
}
