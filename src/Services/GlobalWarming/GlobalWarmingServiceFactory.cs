using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CivOne.Enums;
using CivOne.Services.GlobalWarming.Impl;
using CivOne.Tiles;

namespace CivOne.Services.GlobalWarming
{
	public static class GlobalWarmingServiceFactory
	{

		public static IGlobalWarmingService CreateGlobalWarmingService(IGameData gameData,
				ReadOnlyCollection<City> cities,
				IEnumerable<ITile> tiles)
		{
			return new GlobalWarmingCountServiceImpl(gameData, cities, tiles);
		}
		public static IGlobalWarmingService CreateGlobalWarmingService(ReadOnlyCollection<City> cities,
				IEnumerable<ITile> tiles)
		{
			return new GlobalWarmingCountServiceImpl(cities, tiles);
		}

		public static IGlobalWarmingStoreService CreateGlobalWarmingStoreService(IGlobalWarmingService globalWarmingService)
		{
			return new GlobalWarmingStoreServiceImpl(globalWarmingService);
		}

		public static IGlobalWarmingScourgeService CreateGlobalWarmingScourgeService(IGlobalWarmingService globalWarmingService,
				ITile[,] tiles,
				TileChangeRequestCallback changeTileType,
				RemoveUnit removeUnit,
				int mapWidth,
				int mapHeight)
		{
			if (Settings.Instance.GlobalWarmingFeatureFlags != 0)
			{
				return new GlobalWarmingScourgeWithFloodServiceImpl(
						globalWarmingService,
						tiles,
						changeTileType,
						removeUnit,
						mapWidth,
						mapHeight,
						Settings.Instance.GlobalWarmingFeatureFlags
					);
			}

			return new GlobalWarmingScourgeServiceImpl(
					globalWarmingService,
					tiles,
					changeTileType,
					mapWidth,
					mapHeight
				);
		}
		
	}
}

// 			globalWarmingService = new GlobalWarmingCountServiceImpl(gameData, _cities.AsReadOnly(), Map.AllTiles());
			// globalWarmingScourgeService = new GlobalWarmingScourgeWithFloodServiceImpl(
			// 		globalWarmingService,
			// 		Map.Tiles,
			// 		(tile, newTerrainType) => Map.ChangeTileType(tile.X, tile.Y, newTerrainType),
			// 		Map.WIDTH,
			// 		Map.HEIGHT
			// 	);