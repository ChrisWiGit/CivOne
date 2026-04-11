using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CivOne.Persistence.Stubs;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Persistence.Model
{
    public class GameStateDtoMapper(
        PlayerDtoMapper playerMapper,
        UnitDtoMapper unitMapper,
        DtoMapper<MapDto, IMapTiles> mapMapper,
        DtoMapper<GlobalWarmingDto, GameState> globalWarmingMapper,
        IValueSanitizer yamlReadValueSanitizer,
        ICityNameCatalog cityNameCatalog = null
    ) : DtoMapper<GameStateDto, GameState>
    {
        private readonly ICityNameCatalog _cityNameCatalog = cityNameCatalog ?? new CommonCityNameCatalog();
        public GameState FromDto(GameStateDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            // Map MUST be initialized before players, because CityDtoMapper.MapMapToTiles
            // accesses Map.Instance[x,y] when resolving city resource tiles.
            var map = MapMap(dto.Map);

            var players = MapPlayers(dto);
            ApplyLegacyFutureTech(dto, players);
            ValidateHumanPlayerIndex(dto, players);
            ValidateCurrentPlayerIndex(dto, players);
            ResolveTradingCities(dto, players);

            var units = MapUnits(dto);
            
            // During city materialization, City.Size setter triggers SetResourceTiles() which
            // may call methods that check Player.Game (e.g., AnarchyDespotism).
            // Create a temporary stub context to allow these checks to work during mapping.
            var savedGame = Player.Game;
            try
            {
                Player.Game = new PlayerGameStub();
                var (cities, cityNames) = MapCities(players);
                
                var randomSeed = yamlReadValueSanitizer.ClampToInt32(dto.GameRandomSeed, nameof(GameStateDtoMapper), nameof(GameStateDto.GameRandomSeed));
                var difficulty = yamlReadValueSanitizer.ClampToInt32((int)dto.Difficulty, nameof(GameStateDtoMapper), nameof(GameStateDto.Difficulty));
                var anthologyTurn = (ushort)yamlReadValueSanitizer.ClampToInt32(dto.AnthologyTurn, nameof(GameStateDtoMapper), nameof(GameStateDto.AnthologyTurn), min: 0, max: ushort.MaxValue);
                var globalWarmingState = globalWarmingMapper.FromDto(dto.GlobalWarming);

                return BuildGameState(dto, players, units, cities, cityNames, map, randomSeed, difficulty, anthologyTurn, globalWarmingState);
            }
            finally
            {
                Player.Game = savedGame;
            }
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

        private void ValidateCurrentPlayerIndex(GameStateDto dto, IPlayer[] players)
        {
            if (dto.CurrentPlayer >= players.Length)
            {
                throw new InvalidOperationException($"Current player index {dto.CurrentPlayer} is out of range");
            }
        }

        private static void ApplyLegacyFutureTech(GameStateDto dto, IPlayer[] players)
        {
            if (dto.HumanPlayer >= players.Length)
            {
                return;
            }

            if (dto.Players?.Any(player => player.FutureTechCount > 0) == true)
            {
                return;
            }

            if (dto.PlayerFutureTech == 0)
            {
                return;
            }

            if (players[dto.HumanPlayer] is IPlayerRestorable humanPlayer)
            {
                humanPlayer.FutureTechCount = dto.PlayerFutureTech;
            }
        }

        private List<IUnit> MapUnits(GameStateDto dto)
            => [.. dto.Players
                .SelectMany(p => p.Units ?? [])
                .Select(unitMapper.FromDto)];

        private (List<City> cities, string[] cityNames) MapCities(IPlayer[] players)
        {
            List<ICity> sourceCities = [.. players.SelectMany(p => p.Cities ?? [])];
            var (cityNames, cityNameIndexByName) = BuildCityNameCatalog(sourceCities);
            var (mappedCities, _) = MaterializeCities(sourceCities, cityNames, cityNameIndexByName);
            ApplyTradingLinks(sourceCities, mappedCities);
            return (mappedCities, [.. cityNames]);
        }

        private (List<string>, Dictionary<string, int>) BuildCityNameCatalog(List<ICity> sourceCities)
        {
            // Seed from the full global catalog so that NameId values align with
            // Common.AllCityNames offsets that Game.CityNameId() depends on.
            // The catalog is injected to keep this class testable without a live graphics runtime.
            var cityNames = new List<string>(_cityNameCatalog.GetAllCityNames());
            var cityNameIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < cityNames.Count; i++)
                cityNameIndexByName[cityNames[i]] = i;

            // Custom / renamed city names that are not in the global catalog
            // are appended at the end so they remain accessible via NameId.
            foreach (var sourceCity in sourceCities.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
            {
                if (!cityNameIndexByName.ContainsKey(sourceCity.Name))
                    AddCityName(cityNames, cityNameIndexByName, sourceCity.Name);
            }

            return (cityNames, cityNameIndexByName);
        }

        private (List<City>, Dictionary<Guid, int>) MaterializeCities(List<ICity> sourceCities, List<string> cityNames, Dictionary<string, int> cityNameIndexByName)
        {
            var mappedCities = new List<City>(sourceCities.Count);
            var cityIndexById = new Dictionary<Guid, int>();

            for (var i = 0; i < sourceCities.Count; i++)
            {
                var sourceCity = sourceCities[i];
                var city = CreateCity(sourceCity, cityNames, cityNameIndexByName);
                ApplyStatusFlags(city, sourceCity);
                ApplyProductionAndCollections(city, sourceCity);
                ApplySize(city, sourceCity);
                
                mappedCities.Add(city);
                if (!cityIndexById.ContainsKey(city.Id))
                {
                    cityIndexById[city.Id] = i;
                }
            }

            return (mappedCities, cityIndexById);
        }

        private City CreateCity(ICity sourceCity, List<string> cityNames, Dictionary<string, int> cityNameIndexByName)
        {
            var locationX = yamlReadValueSanitizer.ClampToInt32(sourceCity.Location.X, nameof(GameStateDtoMapper), "City.Location.X", min: 0, max: byte.MaxValue);
            var locationY = yamlReadValueSanitizer.ClampToInt32(sourceCity.Location.Y, nameof(GameStateDtoMapper), "City.Location.Y", min: 0, max: byte.MaxValue);
            var cityName = sourceCity.Name ?? string.Empty;
            var nameId = cityNameIndexByName.TryGetValue(cityName, out var mappedNameId)
                ? mappedNameId
                : AddCityName(cityNames, cityNameIndexByName, cityName);

            return new City(sourceCity.Owner)
            {
                Id = sourceCity.Id,
                X = (byte)locationX,
                Y = (byte)locationY,
                NameId = nameId,
                Food = sourceCity.Food,
                Shields = sourceCity.Shields,
                WasInDisorder = sourceCity.WasInDisorder,
                VisibleSizes = sourceCity.VisibleSizes ?? [],
            };
        }

        private void ApplySize(City city, ICity sourceCity)
        {
            city.Size = sourceCity.Size;
        }

        private void ApplyStatusFlags(City city, ICity sourceCity)
        {
            city.SetupStatus(sourceCity.Status);
            city.IsRiot = sourceCity.IsRiot;
            city.IsCoastal = sourceCity.IsCoastal;
            city.CelebrationCancelled = sourceCity.CelebrationCancelled;
            city.HydroAvailable = sourceCity.HydroAvailable;
            city.AutoBuild = sourceCity.AutoBuild;
            city.TechStolen = sourceCity.TechStolen;
            city.CelebrationOrRapture = sourceCity.CelebrationOrRapture;
            city.BuildingSold = sourceCity.BuildingSold;
        }

        private void ApplyProductionAndCollections(City city, ICity sourceCity)
        {
            if (sourceCity.CurrentProduction != null)
            {
                city.SetProduction(sourceCity.CurrentProduction);
            }

            city.SetupSpecialists = [.. sourceCity.Specialists ?? []];
            city.SetupResourceTiles = [.. (sourceCity.ResourceTiles ?? [])
                .Where(t => t != null && !(t.X == city.X && t.Y == city.Y))];

            foreach (var building in sourceCity.Buildings ?? [])
            {
                city.AddBuilding(building);
            }

            foreach (var wonder in sourceCity.Wonders ?? [])
            {
                city.AddWonder(wonder);
            }
        }

        private void ApplyTradingLinks(List<ICity> sourceCities, List<City> mappedCities)
        {
            for (var i = 0; i < sourceCities.Count; i++)
            {
                Guid selfId = sourceCities[i].Id;
                Guid[] tradingIds = [.. (sourceCities[i].TradingCities ?? [])
                    .Where(c => c != null && c.Id != selfId)
                    .Select(c => c.Id)
                    .Distinct()];

                mappedCities[i].SetTradingCityIds(tradingIds);
            }
        }

        private static int AddCityName(List<string> cityNames, Dictionary<string, int> cityNameIndexByName, string cityName)
        {
            if (cityNameIndexByName.TryGetValue(cityName, out var existingIndex))
            {
                return existingIndex;
            }

            var newIndex = cityNames.Count;
            cityNames.Add(cityName);
            cityNameIndexByName[cityName] = newIndex;
            return newIndex;
        }

        private (int width, int height, int terrainSeed, ITile[,] mapTiles) MapMap(MapDto dtoMap)
        {
            Debug.Assert(dtoMap != null, "MapDto should not be null");
            Debug.Assert(dtoMap.Tiles != null, "MapDto.Tiles should not be null");

            var terrainSeed = yamlReadValueSanitizer.ClampToInt32(dtoMap.MapSeed, nameof(GameStateDtoMapper), nameof(MapDto.MapSeed), min: 0, max: int.MaxValue);

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
            string[] cityNames,
			(int width, int height, int terrainSeed, ITile[,] mapTiles) map,
			int randomSeed,
			int difficulty,
            ushort anthologyTurn,
            GameState globalWarmingState)
        {
            return new GameState
            {
                GameTurn = dto.GameTurn,
                HumanPlayer = players[dto.HumanPlayer],
                CurrentPlayer = players[dto.CurrentPlayer],
                RandomSeed = randomSeed,
                Difficulty = difficulty,
                Players = players,
                Cities = cities,
                Units = units,
                CityNames = cityNames,
                GameOptions = dto.GameOptions ?? [],
                AnthologyTurn = anthologyTurn,
                TerrainSeed = map.terrainSeed,
                MapWidth = map.width,
                MapHeight = map.height,
                MapTiles = map.mapTiles,
                AdvanceOrigin = dto.AdvanceOrigin,
                ReplayData = new ReplayDataDtoMapper().FromDtoList(dto.ReplayData ?? []),
                PeaceTurns = dto.PeaceTurns,
                PlayerFutureTech = players[dto.HumanPlayer].FutureTechCount,
                GlobalWarmingCount = globalWarmingState.GlobalWarmingCount,
                PollutedSquaresCount = globalWarmingState.PollutedSquaresCount,
                WarmingIndicator = globalWarmingState.WarmingIndicator
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

        private static ushort FindPlayerIndex(IPlayer[] players, IPlayer player, string playerKind)
        {
            for (ushort i = 0; i < players.Length; i++)
            {
                if (players[i] == player)
                {
                    return i;
                }
            }

            throw new Exception($"{playerKind} player not found in players array");
        }

        public GameStateDto ToDto(GameState gameState)
        {
            var mapDto = CreateMapDto(gameState);
            var gameUnits = gameState.Units ?? [];

            var gameStateDto = new GameStateDto
            {
                Difficulty = (DifficultyLevel)gameState.Difficulty,
                GameTurn = gameState.GameTurn,
                Players = [.. gameState.Players.Select(playerMapper.ToDto)],
                HumanPlayer = FindPlayerIndex(gameState.Players, gameState.HumanPlayer, "Human"),
                CurrentPlayer = FindPlayerIndex(gameState.Players, gameState.CurrentPlayer ?? gameState.HumanPlayer, "Current"),

                GameRandomSeed = (uint)gameState.RandomSeed,
                AnthologyTurn = gameState.AnthologyTurn,
                Map = mapDto,
                GameOptions = gameState.GameOptions ?? [],
                AdvanceOrigin = gameState.AdvanceOrigin,
                ReplayData = new ReplayDataDtoMapper().ToDtoList(gameState.ReplayData ?? []),
                PeaceTurns = gameState.PeaceTurns,
				PlayerFutureTech = gameState.HumanPlayer?.FutureTechCount ?? gameState.PlayerFutureTech,
                GlobalWarming = globalWarmingMapper.ToDto(gameState)
            };

            foreach (var player in gameStateDto.Players)
            {
                player.Id = (ushort)gameStateDto.Players.IndexOf(player);
                player.Units = [.. gameUnits
                    .Where(unit => unit.Owner == player.Id)
                    .Select(unitMapper.ToDto)];
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
                mappedDto.MapSeed = (uint)Math.Clamp(gameState.TerrainSeed, 0, int.MaxValue);
                return mappedDto;
            }

            return new MapDto
            {
                MapSeed = (uint)Math.Clamp(gameState.TerrainSeed, 0, int.MaxValue),
                Tiles = new Map2d<TileDto>(width, height)
            };
        }

        private class MapTilesArrayAdapter(ITile[,] tiles) : IMapTiles
        {
            public ITile this[int x, int y] => tiles[x, y];
            public int Width => tiles.GetLength(0);
            public int Height => tiles.GetLength(1);
        }
    }
}
