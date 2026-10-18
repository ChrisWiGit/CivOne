using System;

namespace CivOne.Services
{
	/// <summary>
	/// Service for managing save game metadata initialization and display names.
	/// Currently no interface because this service will stay alone and is not expected to have multiple implementations, but this could change in the future.
	/// </summary>
	public class SaveMetaDataService(string gameVersion, ITranslationService? translationService = null)
	{
		private readonly ITranslationService _translation = translationService ?? TranslationServiceFactory.GetCurrent();
		private readonly IGameCalendarService? _calendar;

		public SaveMetaDataService(string gameVersion, ITranslationService translationService, IGameCalendarService calendar)
			: this(gameVersion, translationService)
		{
			_calendar = calendar ?? new GameCalendarService(translationService);
		}

		private IGameCalendarService Calendar => _calendar ?? new GameCalendarService(_translation);

		public SaveFileMetaData CreateForNewGame(int difficulty, Player humanPlayer)
		{
			var metadata = new SaveFileMetaData();
			metadata.InitializeForNewGame(gameVersion, DateTimeOffset.UtcNow);
			metadata.DisplayName = BuildDisplayName(difficulty, humanPlayer, 0);
			return metadata;
		}

		private string TranslateDisplayName(int difficulty, Player humanPlayer, ushort gameTurn)
		{
			return _translation.TranslateFormatted(
				"In {3} leader {0} {1} of the {2}",
				_translation.Translate(DifficultyName(difficulty)),
				_translation.Translate(humanPlayer.LeaderName),
				_translation.Translate(humanPlayer.TribeNamePlural),
				Calendar.FormatYear(gameTurn)
			);
		}

		/// <summary>
		/// Returns the localized difficulty name for the given level.
		/// Kept here (not in Common) so it can be translated without a Common dependency.
		/// </summary>
		public string DifficultyName(int difficulty) => difficulty switch
		{
			1 => _translation.Translate("Warlord"),
			2 => _translation.Translate("Prince"),
			3 => _translation.Translate("King"),
			4 => _translation.Translate("Emperor"),
			5 => _translation.Translate("Deity"),
			_ => _translation.Translate("Chieftain"),
		};

		public string BuildDisplayName(int difficulty, Player humanPlayer, ushort gameTurn)
			=> $"{TranslateDisplayName(difficulty, humanPlayer, gameTurn)}";
	}
}