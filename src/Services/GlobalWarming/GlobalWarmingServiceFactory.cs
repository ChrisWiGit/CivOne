using System.Collections.Generic;
using CivOne.Persistence.Model;
using CivOne.Services.GlobalWarming.Impl;
using CivOne.Services.Random;
using CivOne.Tiles;

namespace CivOne.Services.GlobalWarming
{
	public static class GlobalWarmingServiceFactory
	{

		public static IGlobalWarmingService CreateGlobalWarmingService(IGameData gameData,
				IEnumerable<ITile> tiles)
		{
			return new GlobalWarmingCountService(gameData, tiles);
		}
		public static IGlobalWarmingService CreateGlobalWarmingService(IEnumerable<ITile> tiles)
		{
			return new GlobalWarmingCountService(tiles);
		}

		public static IGlobalWarmingService CreateGlobalWarmingService(
				int globalWarmingCount, int pollutedSquaresCount, WarmingIndicator warmingIndicator,
				IEnumerable<ITile> tiles)
		{
			return new GlobalWarmingCountService(globalWarmingCount, pollutedSquaresCount, warmingIndicator, tiles);
		}

		public static IGlobalWarmingStoreService CreateGlobalWarmingStoreService(IGlobalWarmingService globalWarmingService, IValueSanitizer valueSanitizer)
		{
			return new GlobalWarmingStoreService(globalWarmingService, valueSanitizer);
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
				return new GlobalWarmingScourgeWithFloodService(
						globalWarmingService,
						tiles,
						changeTileType,
						removeUnit,
						mapWidth,
						mapHeight,
						Settings.Instance.GlobalWarmingFeatureFlags,
						RandomServiceFactory.Create()
					);
			}

			return new GlobalWarmingScourgeService(
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