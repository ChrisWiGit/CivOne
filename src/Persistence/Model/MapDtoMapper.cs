using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence.Model.Attributes;
using CivOne.Tiles;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Model
{
    public class MapDtoMapper(
    ) : DtoMapper<MapDto, IMapTiles>
    {
        public IMapTiles FromDto(MapDto dto)
        {
            throw new NotImplementedException();
        }

        private Map2d<TileDto> ConvertTileDtos(IMapTiles map)
        {
            var result = new Map2d<TileDto>(map.Width, map.Height);
            int width = map.Width;
            int height = map.Height;
            var tileDtos = new TileDto[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = map[x, y];
                    // tileDtos[x, y] = new TileDto
                    // {
                    //     TerrainType = tile.TerrainType,
                    //     Road = tile.Road,
                    //     River = tile.River,
                    //     Fortress = tile.Fortress,
                    //     Mine = tile.Mine,
                    //     Hut = tile.Hut,
                    //     LandValue = tile.LandValue,
                    //     LandScore = tile.LandScore
                    // };
                }
            }
            return result;
        }

        public MapDto ToDto(IMapTiles map)
        {
            return new MapDto
            {
                Tiles = ConvertTileDtos(map)
            };
        }
    }
}

// 	public interface IMapTiles
// {
// 	ITile this[int x, int y] { get; }

// 	int Width { get; }
// 	int Height { get; }
// }