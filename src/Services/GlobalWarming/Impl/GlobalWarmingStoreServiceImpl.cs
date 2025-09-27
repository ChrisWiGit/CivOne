using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CivOne.Tiles;

namespace CivOne.Services.GlobalWarming.Impl
{	
	public class GlobalWarmingStoreServiceImpl(IGlobalWarmingService globalWarmingService) : IGlobalWarmingStoreService
	{
		public void Store(IGameData gameData)
		{
			// gameData.GlobalWarmingCount = globalWarmingService.GlobalWarmingCount;
			// gameData.PollutedSquaresCount = globalWarmingService.PollutedSquaresCount;
			// gameData.WarmingIndicator = (short)globalWarmingService.WarmingIndicator;
		}
	}
}