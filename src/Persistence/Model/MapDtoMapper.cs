using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Model.Attributes;
using CivOne.Tiles;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Model
{
    public interface TileFactory
    {
        ITile CreateTile(int x, int y, Terrain terrain);
    }
    public interface MapFactory
    {
        IMapTiles CreateMap(int width, int height); 
    }

    public class MapDtoMapper(
        TileFactory _tileFactory
    ) : DtoMapper<MapDto, IMapTiles>
    {


        public IMapTiles FromDto(MapDto dto)
        {
            var tiles = dto.Tiles;
            int width = tiles.Width();
            int height = tiles.Height();
            var mapTiles = new Map2d<ITile>(width, height);
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
                    tileDtos[x, y] = ToDto(tile);
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

        ITile FromDto(TileDto dto, int x, int y)
        {
            return _tileFactory.CreateTile(x, y, dto.Terrain);
        }
        TileDto ToDto(ITile tile)
        {
            return new TileDto
            {
                Terrain = tile.Type,
                Road = tile.Road,
                RailRoad = tile.RailRoad,
                Irrigation = tile.Irrigation,
                Pollution = tile.Pollution,
                Fortress = tile.Fortress,
                Mine = tile.Mine,
                Hut = tile.Hut,
                LandValue = tile.LandValue,
                LandScore = tile.LandScore
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