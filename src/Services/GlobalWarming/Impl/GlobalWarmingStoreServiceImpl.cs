using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CivOne.Persistence.Model;
using CivOne.Tiles;

namespace CivOne.Services.GlobalWarming.Impl
{	
	public class GlobalWarmingStoreServiceImpl(IGlobalWarmingService globalWarmingService, IValueSanitizer valueSanitizer) : IGlobalWarmingStoreService
	{
		public void Store(IGameData gameData)
		{
			gameData.GlobalWarmingCount = valueSanitizer.ClampToUInt16(
				globalWarmingService.GlobalWarmingCount,
				nameof(GlobalWarmingStoreServiceImpl),
				nameof(IGameData.GlobalWarmingCount));
			gameData.PollutedSquaresCount = valueSanitizer.ClampToUInt16(
				globalWarmingService.PollutedSquaresCount,
				nameof(GlobalWarmingStoreServiceImpl),
				nameof(IGameData.PollutedSquaresCount));
			gameData.WarmingIndicator = valueSanitizer.ClampToUInt16(
				(int)globalWarmingService.WarmingIndicator,
				nameof(GlobalWarmingStoreServiceImpl),
				nameof(IGameData.WarmingIndicator));
		}
	}
}