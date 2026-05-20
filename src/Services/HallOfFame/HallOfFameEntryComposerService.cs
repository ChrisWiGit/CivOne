using System;
using CivOne.Screens.Reports;

namespace CivOne.Services.HallOfFame
{
	internal sealed class HallOfFameEntryComposerService(
		ICivilizationScoreService civilizationScoreService,
		GameCalendarService gameCalendarService,
		LeaderOrderDelegate leaderOrderDelegate,
		ITranslationService translationService) : IHallOfFameEntryComposerService
	{
		private readonly ICivilizationScoreService _civilizationScoreService = civilizationScoreService;
		private readonly GameCalendarService _gameCalendarService = gameCalendarService;
		private readonly LeaderOrderDelegate _leaderOrderDelegate = leaderOrderDelegate;
		private readonly ITranslationService _translationService = translationService;

		public HallOfFameEntry ComposeForHuman()
		{
			int totalScore = _civilizationScoreService.TotalScore(Game.Instance.HumanPlayer);
			int topLeaderThreshold = _leaderOrderDelegate.GetLeaderOrder()[0].RatingThreshold;
			int ratingPercent = _civilizationScoreService.RatingPercent(totalScore, topLeaderThreshold);
			LeaderOrderResult leaderOrderResult = _leaderOrderDelegate.Calculate(ratingPercent);

			return new HallOfFameEntry(
				LeaderName: Game.Instance.HumanPlayer.LeaderName,
				LeaderTitle: GetDifficultyTitle(Game.Instance.Difficulty),
				CivilizationNamePlural: Game.Instance.HumanPlayer.TribeNamePlural,
				YearLabel: _gameCalendarService.FormatYear(Game.Instance.GameTurn),
				Population: Game.Instance.HumanPlayer.Population,
				Score: totalScore,
				RatingRankLabel: leaderOrderResult.SelectedLeaderName,
				RatingPercent: ratingPercent,
				CreatedAtUtc: DateTimeOffset.UtcNow);
		}

		private string GetDifficultyTitle(int difficulty)
		{
			return difficulty switch
			{
				1 => _translationService.Translate("Lord"),
				2 => _translationService.Translate("Prince"),
				3 => _translationService.Translate("King"),
				4 => _translationService.Translate("Emperor"),
				5 => _translationService.Translate("Deity"),
				_ => _translationService.Translate("Chief")
			};
		}
	}
}
