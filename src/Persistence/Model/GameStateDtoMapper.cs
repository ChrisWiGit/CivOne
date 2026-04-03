using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Persistence.Model
{
    public class GameStateDtoMapper(
        PlayerDtoMapper playerMapper,
        UnitDtoMapper unitMapper,
        DtoMapper<MapDto, IMapTiles> mapMapper
    ) : DtoMapper<GameStateDto, GameState>
    {
        public GameState FromDto(GameStateDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var players = MapPlayers(dto);
            ValidateHumanPlayerIndex(dto, players);
            ResolveTradingCities(dto, players);

            var units = MapUnits(dto);
            var cities = MapCities(players);
            var map = MapMap(dto.Map);

            return BuildGameState(dto, players, units, cities, map);
        }

        private IPlayer[] MapPlayers(GameStateDto dto)
            => [.. dto.Players.Select(playerMapper.FromDto)];

        private void ValidateHumanPlayerIndex(GameStateDto dto, IPlayer[] players)
        {
            if (dto.HumanPlayer >= players.Length)
            {
                throw new InvalidOperationException($"Human player index {dto.HumanPlayer} is out of range");
            }
        }

        private List<IUnit> MapUnits(GameStateDto dto)
            => [.. dto.Players
                .SelectMany(p => p.Units ?? [])
                .Select(unitMapper.FromDto)];

        private List<City> MapCities(IPlayer[] players)
            => [.. players
                .SelectMany(p => p.Cities ?? [])
                .OfType<City>()];

        private (int width, int height, int terrainSeed, ITile[,] mapTiles) MapMap(MapDto dtoMap)
        {
            Debug.Assert(dtoMap != null, "MapDto should not be null");
            Debug.Assert(dtoMap.Tiles != null, "MapDto.Tiles should not be null");

            var terrainSeed = (int)Math.Clamp(dtoMap.TerrainSeed, 0, int.MaxValue);

            var mappedMap = mapMapper.FromDto(dtoMap);
            var mapWidth = mappedMap.Width;
            var mapHeight = mappedMap.Height;
            var mapTiles = ToTileArray(mappedMap);

            return (mapWidth, mapHeight, terrainSeed, mapTiles);
        }

        private static GameState BuildGameState(
            GameStateDto dto,
            IPlayer[] players,
            List<IUnit> units,
            List<City> cities,
            (int width, int height, int terrainSeed, ITile[,] mapTiles) map)
        {
            return new GameState
            {
                GameTurn = dto.GameTurn,
                HumanPlayer = players[dto.HumanPlayer],
                RandomSeed = (int)Math.Clamp(dto.RandomSeed, 0, int.MaxValue),
                Difficulty = (int)Math.Clamp((int)dto.Difficulty, 0, int.MaxValue),
                Players = players,
                Cities = cities,
                Units = units,
                GameOptions = dto.GameOptions ?? [],
                AnthologyTurn = (ushort)Math.Clamp(dto.AnthologyTurn, 0, ushort.MaxValue),
                TerrainSeed = map.terrainSeed,
                MapWidth = map.width,
                MapHeight = map.height,
                MapTiles = map.mapTiles
            };
        }

        private static void ResolveTradingCities(GameStateDto dto, IPlayer[] players)
        {
            var allMappedCities = players
                .SelectMany(player => player.Cities ?? [])
                .ToArray();

            var cityById = allMappedCities
                .GroupBy(city => city.Id)
                .ToDictionary(group => group.Key, group => group.First());

            for (var playerIndex = 0; playerIndex < players.Length; playerIndex++)
            {
                var mappedCities = players[playerIndex].Cities ?? [];
                var sourceCities = dto.Players[playerIndex].Cities ?? [];
                var cityCount = Math.Min(mappedCities.Count, sourceCities.Count);

                for (var cityIndex = 0; cityIndex < cityCount; cityIndex++)
                {
                    if (mappedCities[cityIndex] is not ICityTradingCitiesWritable writableCity)
                    {
                        continue;
                    }

                    var tradingCityIds = sourceCities[cityIndex].TradingCities ?? [];
                    writableCity.TradingCities = [..
                        tradingCityIds
                            .Where(cityById.ContainsKey)
                            .Select(id => cityById[id])
                    ];
                }
            }
        }

        ushort FindHumanPlayerIndex(IPlayer[] players, IPlayer humanPlayer)
        {
            for (ushort i = 0; i < players.Length; i++)
            {
                if (players[i] == humanPlayer)
                {
                    return i;
                }
            }
            throw new Exception("Human player not found in players array");
        }

        public GameStateDto ToDto(GameState gameState)
        {
            var mapDto = CreateMapDto(gameState);

            var gameStateDto = new GameStateDto
            {
                Difficulty = (DifficultyLevel)gameState.Difficulty,
                GameTurn = gameState.GameTurn,
                Players = [.. gameState.Players.Select(playerMapper.ToDto)],
                HumanPlayer = FindHumanPlayerIndex(gameState.Players, gameState.HumanPlayer),

                RandomSeed = (uint)gameState.RandomSeed,
                AnthologyTurn = gameState.AnthologyTurn,
                Map = mapDto,
                GameOptions = gameState.GameOptions ?? []
            };

            foreach (var player in gameStateDto.Players)
            {
                player.Id = (ushort)gameStateDto.Players.IndexOf(player);
            }
            return gameStateDto;
        }

        private static ITile[,] ToTileArray(IMapTiles map)
        {
            var result = new ITile[map.Width, map.Height];
            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Height; y++)
                {
                    result[x, y] = map[x, y];
                }
            }

            return result;
        }

        private MapDto CreateMapDto(GameState gameState)
        {
            var width = Math.Max(0, gameState.MapWidth);
            var height = Math.Max(0, gameState.MapHeight);

            if (gameState.MapTiles != null)
            {
                width = gameState.MapTiles.GetLength(0);
                height = gameState.MapTiles.GetLength(1);
            }

            if (gameState.MapTiles != null && mapMapper != null)
            {
                var mappedDto = mapMapper.ToDto(new MapTilesArrayAdapter(gameState.MapTiles));
                mappedDto.TerrainSeed = (uint)Math.Clamp(gameState.TerrainSeed, 0, int.MaxValue);
                return mappedDto;
            }

            return new MapDto
            {
                TerrainSeed = (uint)Math.Clamp(gameState.TerrainSeed, 0, int.MaxValue),
                Tiles = new Map2d<TileDto>(width, height)
            };
        }

        private class MapTilesArrayAdapter(ITile[,] tiles) : IMapTiles
        {
            public ITile this[int x, int y] => tiles[x, y];
            public int Width => tiles.GetLength(0);
            public int Height => tiles.GetLength(1);
        }

        // public class GameStateDto
        // {
        //     [Doc("The difficulty level of the game.",
        //         nameof(DifficultyAll))]
        //     public DifficultyLevel Difficulty { get; set; }

        //     public uint GameTurn { get; set; }
        //     public ushort HumanPlayer { get; set; }

        //     public List<PlayerDto> Players { get; set; }

        //     public uint RandomSeed { get; set; }

        //     public uint AnthologyTurn { get; set; }

        //     public uint TerrainSeed { get; set; }

        //     public MapDto Map { get; set; }

        //     [Doc("The game options that are enabled in the game.",
        //         nameof(GameOptionsAll))]
        //     [YamlDotNet.Serialization.YamlMember(typeof(List<string>))]
        //     public List<GameOptionEnum> GameOptions { get; set; }

        //     private static string DifficultyAll { get => string.Join(", ", Enum.GetNames<DifficultyLevel>()); }
        //     private static string GameOptionsAll { get => string.Join(", ", Enum.GetNames<GameOptionEnum>()); }
        // }
    }
}