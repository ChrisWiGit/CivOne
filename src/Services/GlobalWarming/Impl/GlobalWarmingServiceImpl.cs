using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CivOne.Tiles;

namespace CivOne.Services.GlobalWarming.Impl
{
	public class GlobalWarmingCountService : IGlobalWarmingService
	{
		private IEnumerable<ITile> _tiles = [];
		int _pollutedSquaresCount;
		short _globalWarmingCount;
		WarmingIndicator _warmingIndicator;

		public GlobalWarmingCountService(IGameData? gameData,
				IEnumerable<ITile> tiles)
		{
			if (gameData != null)
			{
				_globalWarmingCount = (short)gameData.GlobalWarmingCount;
				_pollutedSquaresCount = gameData.PollutedSquaresCount;
				_warmingIndicator = (WarmingIndicator)gameData.WarmingIndicator;
			}
			if (_warmingIndicator > WarmingIndicator.White)
				_warmingIndicator = WarmingIndicator.None;

			SetReadonlyTiles(tiles);
		}

		public GlobalWarmingCountService(int globalWarmingCount, int pollutedSquaresCount, WarmingIndicator warmingIndicator,
				IEnumerable<ITile> tiles)
		{
			_globalWarmingCount = (short)Math.Clamp(globalWarmingCount, short.MinValue, short.MaxValue);
			_pollutedSquaresCount = pollutedSquaresCount;
			_warmingIndicator = warmingIndicator > WarmingIndicator.White ? WarmingIndicator.None : warmingIndicator;

			SetReadonlyTiles(tiles);
		}

		public GlobalWarmingCountService(IEnumerable<ITile> tiles) : this(null, tiles)
		{
		}

		public bool IsGlobalWarmingOnNewTurn()
		{
			_pollutedSquaresCount = GetPollutedSquareCount();
			_warmingIndicator = GetWarmingIndicator(_pollutedSquaresCount);

			if (_pollutedSquaresCount >= GetCurrentPollutionLimit())
			{
				if (_globalWarmingCount < short.MaxValue)
				{
					_warmingIndicator = WarmingIndicator.None;
					_globalWarmingCount++;
				}
				return true;
			}

			return false;
		}

		public void RefreshPollutionState()
		{
			_pollutedSquaresCount = GetPollutedSquareCount();
			_warmingIndicator = GetWarmingIndicator(_pollutedSquaresCount);
		}

		protected int GetPollutedSquareCount()
		{
			return _tiles.Count(t => t.Pollution);
		}

		protected int GetCurrentPollutionLimit()
		{
			return 8 + (_globalWarmingCount * 2);
		}

		protected static WarmingIndicator GetWarmingIndicator(int pollutedSquares)
		{
			foreach (var range in IndicatorRanges())
			{
				if (pollutedSquares >= range.MinSquares && pollutedSquares <= range.MaxSquares)
				{
					return range.Indicator;
				}
			}
			return WarmingIndicator.None;
		}

		protected static IndicatorRange[] IndicatorRanges() =>
		[
			new IndicatorRange(WarmingIndicator.None, 0, 0),
			new IndicatorRange(WarmingIndicator.DarkRed, 1, 1),
			new IndicatorRange(WarmingIndicator.LightRed, 2, 3),
			new IndicatorRange(WarmingIndicator.Yellow, 4, 5),
			new IndicatorRange(WarmingIndicator.White, 6, int.MaxValue)
		];

		public int PollutedSquaresCount => _pollutedSquaresCount;

		public WarmingIndicator WarmingIndicator => _warmingIndicator;

		public int GlobalWarmingCount => _globalWarmingCount;

		public void SetReadonlyTiles(IEnumerable<ITile> tiles)
		{
			ArgumentNullException.ThrowIfNull(tiles);
			_tiles = tiles;
		}
	}
}