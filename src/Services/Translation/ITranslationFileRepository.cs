using System;
using System.Collections.Generic;

namespace CivOne.Services.Translation
{
	/// <summary>
	/// Abstraction for discovering language files and loading translation key-value data from disk.
	/// Used by <see cref="CivOne.Services.TranslationServiceFactory"/>.
	/// </summary>
	public interface ITranslationFileRepository
	{
		/// <summary>
		/// Returns all valid language files found in the runtime storage directory.
		/// </summary>
		IReadOnlyList<TranslationLanguageInfo> GetAvailableLanguages(string storageDirectory, Action<string> log = null);

		/// <summary>
		/// Tries to load translation entries from a language file.
		/// </summary>
		bool TryLoadTranslations(string filePath, out IReadOnlyDictionary<string, string> translations, out string error);
	}
}