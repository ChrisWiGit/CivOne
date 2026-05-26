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

		/// <summary>
		/// Copies translation files from <paramref name="sourceDirectory"/> into <paramref name="targetDirectory"/>.
		/// <para>
		/// Only files with lowercase filenames on disk are copied to ensure consistency across platforms.
		/// Files with any uppercase letters are skipped with a log message.
		/// Files with identical names are treated as a conflict: all conflicting files are skipped and a warning is written via <paramref name="log"/>.
		/// </para>
		/// </summary>
		/// <param name="sourceDirectory">Directory containing source translation files.</param>
		/// <param name="targetDirectory">Destination directory. Will be created if it does not exist.</param>
		/// <param name="log">Optional logger for progress and conflict warnings.</param>
		/// <returns>Number of files successfully copied.</returns>
		int SyncFiles(string sourceDirectory, string targetDirectory, Action<string> log = null);
	}
}