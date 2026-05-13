// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne.Services
{
	/// <summary>
	/// Service for managing save game metadata initialization and display names.
	/// Currently no interface because this service will stay alone and is not expected to have multiple implementations, but this could change in the future.
	/// </summary>
	public class SaveMetaDataService(string _gameVersion, ITranslationService _translationService = null)
	{
		private readonly ITranslationService _translation = _translationService ?? new TranslationIdentityServiceImpl();
		private readonly IGameCalendarService _calendar;

		public SaveMetaDataService(string gameVersion, ITranslationService translationService, IGameCalendarService calendar)
			: this(gameVersion, translationService)
		{
			_calendar = calendar ?? new GameCalendarService(translationService);
		}

		private IGameCalendarService Calendar => _calendar ?? new GameCalendarService(_translation);

		public SaveFileMetaData CreateForNewGame(int difficulty, Player humanPlayer)
		{
			var metadata = new SaveFileMetaData();
			metadata.InitializeForNewGame(_gameVersion, DateTimeOffset.UtcNow);
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
			1 => _translation.Translate("Lord"),
			2 => _translation.Translate("Prince"),
			3 => _translation.Translate("King"),
			4 => _translation.Translate("Emperor"),
			5 => _translation.Translate("Deity"),
			_ => _translation.Translate("Chief"),
		};

		public string BuildDisplayName(int difficulty, Player humanPlayer, ushort gameTurn)
			=> $"{TranslateDisplayName(difficulty, humanPlayer, gameTurn)}";
	}
}
