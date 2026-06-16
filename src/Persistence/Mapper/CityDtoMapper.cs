namespace CivOne.Persistence.Model
{
    using System;
    using System.Drawing;
	using System.Collections.Generic;
	using System.Linq;
    using CivOne.Buildings;
    using CivOne.Enums;
    using CivOne.Wonders;
    using CivOne.Tiles;
	using CivOne.Persistence.Game;
	using CivOne.Persistence.Resolver;
	using CivOne.Persistence.Mapper;

    #pragma warning disable CA1822 // Mark members as static
	public class CityDtoMapper(
        ProductionDtoMapper productionMapper,
		ICityDefinitionResolver cityDefinitionResolver,
		IValueSanitizer valueSanitizer) : IDtoMapper<CityDto, ICityMapper>
    {
        public ICityMapper FromDto(CityDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var location = dto.Location ?? new MapLocation();
            var locationX = valueSanitizer.ClampToInt32(location.X, nameof(CityDtoMapper), nameof(CityDto.Location));
            var locationY = valueSanitizer.ClampToInt32(location.Y, nameof(CityDtoMapper), nameof(CityDto.Location));
            var centerTile = new Grassland(locationX, locationY);
            var restored = new RestorableCity
            {
                Id = dto.Id,
                CityOwnerPlayerIndex = dto.Owner,
                Name = dto.Name ?? string.Empty,
                Size = valueSanitizer.ClampToByte(dto.Size, nameof(CityDtoMapper), nameof(CityDto.Size)),
                Food = valueSanitizer.ClampToInt32(dto.Food, nameof(CityDtoMapper), nameof(CityDto.Food), min: 0, max: 65535),
                Shields = valueSanitizer.ClampToInt32(dto.Shields, nameof(CityDtoMapper), nameof(CityDto.Shields), min: 0, max: 65535),
                Location = new Point(locationX, locationY),
                Tile = centerTile,
                CurrentProduction = dto.CurrentProduction == null ? null : productionMapper.FromDto(dto.CurrentProduction),
                Specialists = [.. (dto.Specialists ?? []).ToArray()],
                VisibleSizes = dto.VisibleSizes ?? [],
                ContinentId = dto.ContinentId,
                WasInDisorder = dto.WasInDisorder,
                Buildings = cityDefinitionResolver.ResolveBuildings(dto.Buildings),
                Wonders = cityDefinitionResolver.ResolveWonders(dto.Wonders),
                TradingCities = []
            };

            MapStatusFlags(restored, dto.Status ?? []);

            Bool2dMap resourceMap = dto.ResourceTiles ?? new Bool2dMap(5, 5);
            restored.ResourceTiles = [..
                MapMapToTiles(restored, resourceMap)
                .Where(t => !(t.X == restored.Tile.X && t.Y == restored.Tile.Y))
            ];

            return restored;
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

        public Bool2dMap MapResourceTiles(ITile[] resourceTiles, ITile cityCenter)
        {
            Bool2dMap map = new(5, 5);
			if (resourceTiles == null || resourceTiles.Length == 0)
			{
				return map;
			}

            foreach (var tile in resourceTiles)
            {
                // Index [dx+2, dy+2] corresponds to offset (dx, dy) from city position.
                int dx = tile.X - cityCenter.X + 2;
                int dy = tile.Y - cityCenter.Y + 2;

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
                Owner = domain.CityOwnerPlayerIndex,
                Size = domain.Size,
                Food = domain.Food,
                Shields = domain.Shields,

                ResourceTiles = MapResourceTiles(domain.ResourceTiles, domain.Tile),
                Specialists = [.. domain.Specialists],

                Location = new MapLocation(
                    domain.Location
                ),
                Name = domain.Name,
                ContinentId = domain.ContinentId,
                CurrentProduction = domain.CurrentProduction == null ? null : productionMapper.ToDto(domain.CurrentProduction),

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

        public List<CityStatus> MapStatusFlags(ICityStatus status)
        {
            List<CityStatus> flags = [];

            if (status.IsRiot) flags.Add(CityStatus.Riot);
            if (status.IsCoastal) flags.Add(CityStatus.Coastal);
            if (status.CelebrationCancelled) flags.Add(CityStatus.CelebrationCancelled);
            if (status.HydroAvailable) flags.Add(CityStatus.HydroAvailable);
            if (status.AutoBuild) flags.Add(CityStatus.AutoBuild);
            if (status.TechStolen) flags.Add(CityStatus.TechStolen);
            if (status.CelebrationOrRapture) flags.Add(CityStatus.CelebrationRapture);
            if (status.BuildingSold) flags.Add(CityStatus.ImprovementSold);

            return flags;
        }

        public void MapStatusFlags(ICityStatus status, List<CityStatus> flags)
        {
            status.IsRiot = flags.Contains(CityStatus.Riot);
            status.IsCoastal = flags.Contains(CityStatus.Coastal);
            status.CelebrationCancelled = flags.Contains(CityStatus.CelebrationCancelled);
            status.HydroAvailable = flags.Contains(CityStatus.HydroAvailable);
            status.AutoBuild = flags.Contains(CityStatus.AutoBuild);
            status.TechStolen = flags.Contains(CityStatus.TechStolen);
            status.CelebrationOrRapture = flags.Contains(CityStatus.CelebrationRapture);
            status.BuildingSold = flags.Contains(CityStatus.ImprovementSold);
        }

        private class RestorableCity : ICityTradingCitiesWritable
        {
            public Guid Id { get; set; }
            public Point Location { get; set; }
            public byte Size { get; set; }
            public short Luxuries { get; set; }
            public int EntertainerLuxuries { get; set; }
            public byte CityOwnerPlayerIndex { get; set; }
            public string Name { get; set; } = string.Empty;
            public ITile[] ResourceTiles { get; set; } = [];
            public Citizen[] Specialists { get; set; } = [];
            public int Shields { get; set; }
            public int Food { get; set; }
            public int ContinentId { get; set; }
            public IPlayer PlayerIntf { get; set; } = null!;
            public int Entertainers { get; set; }
            public int Scientists { get; set; }
            public int Taxmen { get; set; }
            public IProduction? CurrentProduction { get; set; } 
            public IBuilding[] Buildings { get; set; } = [];
            public IWonder[] Wonders { get; set; } = [];
            public byte Status { get; set; }
            public bool WasInDisorder { get; set; }
            public ICity[] TradingCities { get; set; } = [];
            public uint[] VisibleSizes { get; set; } = [];
            public ITile Tile { get; set; } = null!;
            public bool IsRiot { get; set; }
            public bool IsCoastal { get; set; }
            public bool CelebrationCancelled { get; set; }
            public bool HydroAvailable { get; set; }
            public bool AutoBuild { get; set; }
            public bool TechStolen { get; set; }
            public bool CelebrationOrRapture { get; set; }
            public bool BuildingSold { get; set; }

            public bool HasBuilding<T>() where T : IBuilding => Buildings?.Any(b => b is T) == true;
            public bool HasWonder<T>() where T : IWonder => Wonders?.Any(w => w is T) == true;
            public void NewTurn()
            {
            }
        }
	}
}