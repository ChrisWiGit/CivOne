using System;
using CivOne.Services.Random;

namespace CivOne.Services
{
	internal interface ICivilizationRankingTriggerService
	{
		bool ShouldShowRanking(Player player, Game game);
	}

	internal static class CivilizationRankingTriggerServiceFactory
	{
		private static ICivilizationRankingTriggerService? _instance;

		public static ICivilizationRankingTriggerService GetInstance()
		{
			if (_instance != null)
			{
				return _instance;
			}

			_instance = new CivilizationRankingTriggerService();
			return _instance;
		}

		internal static ICivilizationRankingTriggerService Override(ICivilizationRankingTriggerService service)
		{
			_instance = service;
			return _instance;
		}

		internal static void Reset() => _instance = null;
	}

	internal sealed class CivilizationRankingTriggerService : ICivilizationRankingTriggerService
	{
		private const int MinIntervalYears = 300;
		private const int MaxIntervalYears = 500;

		private readonly IRandomService _randomService;
		private int? _nextRankingYear;

		public CivilizationRankingTriggerService(IRandomService? randomService = null)
		{
			_randomService = randomService ?? RandomServiceFactory.Create();
		}

		public bool ShouldShowRanking(Player player, Game game)
		{
			ArgumentNullException.ThrowIfNull(player);
			ArgumentNullException.ThrowIfNull(game);

			if (!player.IsHuman)
			{
				return false;
			}

			int currentYear = Common.TurnToYear(game.GameTurn);
			if (!_nextRankingYear.HasValue)
			{
				_nextRankingYear = currentYear + GetIntervalYears();
				return false;
			}

			if (currentYear < _nextRankingYear.Value)
			{
				return false;
			}

			_nextRankingYear = currentYear + GetIntervalYears();
			return true;
		}

		private int GetIntervalYears()
		{
			return _randomService.NextInt(MinIntervalYears, MaxIntervalYears + 1);
		}
	}
}