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
		private readonly ITranslationService translationService = _translationService ?? new TranslationIdentityServiceImpl();
		
		public SaveFileMetaData CreateForNewGame(int difficulty, Player humanPlayer)
		{
			var metadata = new SaveFileMetaData();
			metadata.InitializeForNewGame(_gameVersion, DateTimeOffset.UtcNow);
			metadata.DisplayName = BuildDisplayName(difficulty, humanPlayer);
			return metadata;
		}

		string TranslateDisplayName(int difficulty, Player humanPlayer)
		{
			return translationService.TranslateFormatted(
				"{0} {1} of {2}",
				translationService.Translate(Common.DifficultyName(difficulty)),
				translationService.Translate(humanPlayer.LeaderName),
				translationService.Translate(humanPlayer.TribeNamePlural)
			);
		}

		public string BuildDisplayName(int difficulty, Player humanPlayer)
			=> TranslateDisplayName(difficulty, humanPlayer);
	}
}
