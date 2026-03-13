namespace CivOne.Persistence.Model
{
	using System.Collections.Generic;
	using System.Linq;
    using CivOne.Tiles;
    
    public class CityDtoMapper(
        ProductionDtoMapper productionMapper) : DtoMapper<CityDto, ICityMapper>
    {
        public ICityMapper FromDto(CityDto dto)
        {
            throw new System.NotImplementedException();
        }

        public List<ITile> MapMapToTiles(ICityTile city, Bool2dMap activatedResourceTileMap)
		{
            List<ITile> tiles = [];

            for (int x = 0; x < activatedResourceTileMap.Width(); x++)
            for (int y = 0; y < activatedResourceTileMap.Height(); y++)
            {
                if (activatedResourceTileMap[x, y])
                {
                    int dx = x - 2;
                    int dy = y - 2;

                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    var tile = city.Tile[dx, dy];

                    tiles.Add(tile);
                }
            } 

            return tiles;
        }

        public Bool2dMap MapResourceTiles(ITile[] resourceTiles)
        {
            Bool2dMap map = new(5, 5);

            int minX = resourceTiles.Min(t => t.X);
            int minY = resourceTiles.Min(t => t.Y);

            foreach (var tile in resourceTiles)
            {
                int dx = tile.X - minX;
                int dy = tile.Y - minY;

                if (dx < 0 || dx >= 5 || dy < 0 || dy >= 5)
                {
                    throw new System.ArgumentException($"Tile at ({tile.X}, {tile.Y}) is out of bounds for city resource tiles");
                }

                map[dx, dy] = true;
            }

            return map;
        }

        public CityDto ToDto(ICityMapper domain)
        {
            return new CityDto
            {
                Id = domain.Id,
                Owner = domain.Owner,
                Size = domain.Size,

                ResourceTiles = MapResourceTiles(domain.ResourceTiles),
                Specialists = [.. domain.Specialists],

                Location = new MapLocation(
                    domain.Location
                ),
                Name = domain.Name,
                ContinentId = domain.ContinentId,
                CurrentProduction = domain.CurrentProduction == null ? null : 
                    productionMapper.ToDto(domain.CurrentProduction),

                Buildings = [.. domain.Buildings
                    .Select(b => b.Type)],
                Wonders = [.. domain.Wonders
                    .Select(w => w.Type)],

                Status = MapStatusFlags(domain),
                WasInDisorder = domain.WasInDisorder,
                VisibleSizes = [.. domain.VisibleSizes],

                TradingCities = [.. domain.TradingCities
                    .Select(c => c.Id)],
            };
        }

        public List<CityStatusEnum> MapStatusFlags(ICityStatus status)
        {
            List<CityStatusEnum> flags = [];

            if (status.IsRiot) flags.Add(CityStatusEnum.Riot);
            if (status.IsCoastal) flags.Add(CityStatusEnum.Coastal);
            if (status.CelebrationCancelled) flags.Add(CityStatusEnum.CelebrationCancelled);
            if (status.HydroAvailable) flags.Add(CityStatusEnum.HydroAvailable);
            if (status.AutoBuild) flags.Add(CityStatusEnum.AutoBuild);
            if (status.TechStolen) flags.Add(CityStatusEnum.TechStolen);
            if (status.CelebrationOrRapture) flags.Add(CityStatusEnum.CelebrationRapture);
            if (status.BuildingSold) flags.Add(CityStatusEnum.ImprovementSold);

            return flags;
        }

        public void MapStatusFlags(ICityStatus status, List<CityStatusEnum> flags)
        {
            status.IsRiot = flags.Contains(CityStatusEnum.Riot);
            status.IsCoastal = flags.Contains(CityStatusEnum.Coastal);
            status.CelebrationCancelled = flags.Contains(CityStatusEnum.CelebrationCancelled);
            status.HydroAvailable = flags.Contains(CityStatusEnum.HydroAvailable);
            status.AutoBuild = flags.Contains(CityStatusEnum.AutoBuild);
            status.TechStolen = flags.Contains(CityStatusEnum.TechStolen);
            status.CelebrationOrRapture = flags.Contains(CityStatusEnum.CelebrationRapture);
            status.BuildingSold = flags.Contains(CityStatusEnum.ImprovementSold);
        }
	}
}


/*
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
*/
/*

public interface ICity : ITurn, ICityBasic, ICityBuildings, ICityOnContinent
	{


    public interface ICityBasic
	{
		Point Location { get; }
		byte Size { get; }
		short Luxuries { get; }
		public int EntertainerLuxuries { get; }
		byte Owner { get; set; }
		ITile Tile { get; }

		int ContinentId { get; }

		Player Player { get; }

		int Entertainers { get; }
		int Scientists { get; }
		int Taxmen { get; }
	}

namespace CivOne.Persistence.Model
{
	using System.Collections.Generic;
	using CivOne.Enums;
	using CityId = System.UInt32;
    using PlayerId = System.Byte;

    public class CityDto
	{
		public CityId Id { get; set; }

        public MapLocation Location { get; set; }

        public PlayerId Owner { get; set; }

        public string Name { get; set; }

        public uint Size { get; set; }

        public int Shields { get; set; }

        public int Food { get; set; }

        public ProductionDto CurrentProduction { get; set; }

        /// <summary>
        /// 5x5 bitmask of active resource tiles relative to the city center.
        /// Index [dx+2, dy+2] corresponds to offset (dx, dy) from city position.
        /// The center tile [2,2] is always implicitly used and not stored here.
        /// </summary>
        public Bool2dMap ResourceTiles { get; set; }

        public List<Citizen> Specialists { get; set; }

        public List<Building> Buildings { get; set; }

        public List<Wonder> Wonders { get; set; }

        public byte Status { get; set; }

        public bool WasInDisorder { get; set; }

        public CityId[] TradingCities { get; set; }
	}
}
*/