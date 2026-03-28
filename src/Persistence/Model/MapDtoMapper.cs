using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Model.Attributes;
using CivOne.Tiles;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Model
{
    public interface ITileFactory
    {
        ITile CreateTile(int x, int y, Terrain terrain);
    }
    public interface IMapFactory
    {
        /// <summary>
        /// Creates a new map with the specified width, height, and terrain seed. The terrain seed is used to generate the same map layout consistently. The method returns an IMapTiles instance, which provides access to the individual tiles of the map. The implementation of this method should initialize the map's tile array and populate it based on the terrain seed, ensuring that the resulting map matches the expected layout for that seed.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="terrainSeed"></param>
        /// <returns></returns>
        IMapTiles CreateMap(int width, int height, uint terrainSeed);
    }

    public interface ITileDtoMapper : DtoMapper<TileDto, ITile>
    {
        /// <summary>
        /// Sets the properties of an existing tile based on the provided TileDto. This method modifies the tile in place rather than returning a new tile instance. The x and y parameters indicate the position of the tile being set, which may be useful for certain mapping logic that depends on tile coordinates.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        void SetTileFromDto(TileDto dto, int x, int y);
    }


    public class MapDtoMapper(
        IMapFactory _mapFactory,
        ITileDtoMapper _tileDtoMapper,
        uint _terrainSeed = 0
    ) : DtoMapper<MapDto, IMapTiles>
    {
        public IMapTiles FromDto(MapDto dto)
        {
            var tiles = dto.Tiles;
            int width = tiles.Width();
            int height = tiles.Height();
            var map = _mapFactory.CreateMap(width, height, dto.TerrainSeed);
            ConvertTileDtos(tiles);

            return map;
        }

        void ConvertTileDtos(Map2d<TileDto> tileDtos)
        {
            int width = tileDtos.Width();
            int height = tileDtos.Height();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tileDto = tileDtos[x, y];
                    _tileDtoMapper.SetTileFromDto(tileDto, x, y);
                }
            }
        }
        public MapDto ToDto(IMapTiles map)
        {
            return new MapDto
            {
                TerrainSeed = _terrainSeed,
                Tiles = ConvertMapTilesToTileDtos(map)
            };
        }


        Map2d<TileDto> ConvertMapTilesToTileDtos(IMapTiles map)
        {
            int width = map.Width;
            int height = map.Height;
            var tileDtos = new TileDto[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = map[x, y];
                    Debug.Assert(tile != null, $"Tile at position ({x}, {y}) is null. This should not happen in a valid map.");
                    tileDtos[x, y] = _tileDtoMapper.ToDto(tile);
                }
            }
            return new Map2d<TileDto>(tileDtos);
        }
    }

    public class DefaultTileDtoMapper(
        ITileFactory _tileFactory
    ) : ITileDtoMapper
    {
        public void SetTileFromDto(TileDto dto, int x, int y)
        {
            // may raise exception if dto.Terrain is invalid
            var tile = _tileFactory.CreateTile(x, y, dto.Terrain);
            tile.Road = dto.Road;
            tile.RailRoad = dto.RailRoad;
            tile.Irrigation = dto.Irrigation;
            tile.Pollution = dto.Pollution;
            tile.Fortress = dto.Fortress;
            tile.Mine = dto.Mine;
            tile.Hut = dto.Hut;
            tile.LandValue = dto.LandValue;
        }


        public TileDto ToDto(ITile domain)
        {
            return new TileDto
            {
                Terrain = domain.Type,
                Road = domain.Road,
                RailRoad = domain.RailRoad,
                Irrigation = domain.Irrigation,
                Pollution = domain.Pollution,
                Fortress = domain.Fortress,
                Mine = domain.Mine,
                Hut = domain.Hut,
                LandValue = domain.LandValue,
            };
        }
        public ITile FromDto(TileDto dto)
        {
            throw new NotImplementedException("Use FromDto(TileDto dto, int x, int y) instead");
        }
    }
}