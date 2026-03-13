namespace CivOne.Persistence.Model
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using CivOne;
    using CivOne.Buildings;
    using CivOne.Enums;
    using CivOne.Persistence.Model.Attributes;
    using CivOne.Persistence.YamlConverter;
    using CivOne.Tiles;
    using CivOne.Units;
    using CivOne.UnitTests;
    using CivOne.Wonders;
    using Xunit;
    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.EventEmitters;
    using YamlDotNet.Serialization.NamingConventions;
    using CityId = System.UInt32;

    public class CityDtoToDtoMapperTest : TestsBase2
    {
        private readonly CityDtoMapper _testee;
        public List<ICity> _cities = [];

        public CityDtoToDtoMapperTest()
        {
            _testee = new CityDtoMapper(
                new ProductionDtoMapper(new MockedReflect()));

            var city2 = new MockedICity(2);
            _cities.Add(city2);
            var city3 = new MockedICity(3);
            _cities.Add(city3);

            var city = new MockedICity(1)
            {
                TradingCities = [city2, city3],
                Player = new MockedIPlayer()
                {
                    Cities = [city2, city3]
                }
            };
            _cities.Add(city);


        }

        [Fact]
        public void TestToDto()
        {
            var dto = _testee.ToDto(_cities.Last());
            // testee.ToDto	
            Assert.NotNull(dto);

            WriteToFile("citydto.yaml", dto);
        }

        private void WriteToFile(string filename, CityDto dto)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder()
                // .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .WithTypeConverter(new Bool2dMapYamlTypeConverter())
                .WithEventEmitter(next => new DocCommentEventEmitter(next))
                .Build();

            string yaml = serializer.Serialize(dto);
            System.IO.File.WriteAllText(filename, yaml);
        }
    }
}

/*
public sealed class DocAttribute : Attribute
    {
        public string Description { get; }
        public string[] AllowedValues { get; }

    // TextStream
            using var stream = new MemoryStream();
            actualWriter.Write(stream, mockGameState);

            // stream to text
            stream.Seek(0, SeekOrigin.Begin);
            // to file
            using var reader = new StreamReader(stream);
            string yamlOutput = reader.ReadToEnd();
            File.WriteAllText("test_output.yaml", yamlOutput);
        }
*/


/*

    public class CityDtoMapper(
        ProductionDtoMapper productionMapper) : DtoMapper<CityDto, ICity>
    {
        public ICity FromDto(CityDto dto)
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

        public CityDto ToDto(ICity domain)
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
                Shields = domain.Shields,
                Food = domain.Food,
                ContinentId = domain.ContinentId,
                CurrentProduction = productionMapper.
                    ToDto(domain.CurrentProduction),

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
*/