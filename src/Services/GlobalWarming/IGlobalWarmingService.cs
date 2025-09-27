using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CivOne.Tiles;

namespace CivOne.Services.GlobalWarming
{
	public enum WarmingIndicator
	{
		None = 0,
		DarkRed = 1,
		LightRed = 2,
		Yellow = 3,
		White = 4
	}
	public interface IGlobalWarmingService
	{
		bool IsGlobalWarmingOnNewTurn();
		int PollutedSquaresCount { get; }

		WarmingIndicator WarmingIndicator { get; }

		int GlobalWarmingCount { get; }
	}

	public record struct IndicatorRange(WarmingIndicator Indicator, int MinSquares, int MaxSquares);
}