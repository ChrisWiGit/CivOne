using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CivOne.Tiles;

namespace CivOne.Services.GlobalWarming.Impl
{
	public class GlobalWarmingCountServiceImpl : IGlobalWarmingService
	{
		private ReadOnlyCollection<City> _cities = null;
		private IEnumerable<ITile> _tiles = null;
		int _pollutedSquaresCount = 0;
		short _globalWarmingCount = 0;
		WarmingIndicator _warmingIndicator = WarmingIndicator.None;

		public GlobalWarmingCountServiceImpl(IGameData gameData,
				ReadOnlyCollection<City> cities,
				IEnumerable<ITile> tiles)
		{
			// Constructor implementation
			if (gameData != null)
			{
				// _globalWarmingCount = gameData.GlobalWarmingCount;
				// _pollutedSquaresCount = gameData.PollutedSquaresCount;
				// _warmingIndicator = (WarmingIndicator)gameData.WarmingIndicator;
			}
			if (_warmingIndicator > WarmingIndicator.White)
				{
					_warmingIndicator = WarmingIndicator.None;
				}
			// Testwerte
			_globalWarmingCount = 0;
			_pollutedSquaresCount = 0;
			_warmingIndicator = WarmingIndicator.None;

			SetCities(cities);
			SetReadonlyTiles(tiles);
		}

		public GlobalWarmingCountServiceImpl(ReadOnlyCollection<City> cities,
				IEnumerable<ITile> tiles): this(null, cities, tiles)
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

		protected int GetPollutedSquareCount()
		{
			return _tiles.Count(t => t.Pollution);
		}

		protected int GetCurrentPollutionLimit()
		{
			return 8 + (_globalWarmingCount * 2);
		}

		protected WarmingIndicator GetWarmingIndicator(int pollutedSquares)
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

		protected IndicatorRange[] IndicatorRanges() =>
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

		public void SetCities(ReadOnlyCollection<City> cities)
		{
			_cities = cities;
		}
		public void SetReadonlyTiles(IEnumerable<ITile> tiles)
		{
			_tiles = tiles;
		}
	}
}