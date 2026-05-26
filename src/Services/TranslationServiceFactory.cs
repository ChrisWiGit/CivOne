// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CivOne.Services.Translation;

namespace CivOne.Services
{
	/// <summary>
	/// Central factory and state holder for the currently active translation service.
	/// Coordinates language discovery and loading through <see cref="ITranslationFileRepository"/>.
	/// </summary>
	public static class TranslationServiceFactory
	{
		private static readonly Lock _sync = new();
		private static readonly List<ITranslationLanguageObserver> _languageObservers = [];
		private static ITranslationFileRepository _translationFileRepository = new TranslationFileRepositoryImpl();
		private static ITranslationService _instance;
		private static string _activeLanguagePostfix;

		// This is only a list of translation keys for the language names
		// It is used to display the available languages in the settings menu, 
		// and also to translate the language names in the settings menu.
		//LanguageNameKeys = [
		// 	Translate("English"),
		// 	Translate("German"),
		// 	Translate("French"),
		// 	Translate("Italian"),
		// 	Translate("Spanish"),
		// 	Translate("Portuguese"),
		// 	Translate("Russian"),
		// 	Translate("Chinese"),
		// 	Translate("Japanese"),
		// 	Translate("Korean")

		/// <summary>
		/// Returns the active translation service.
		/// If none is configured yet, initializes and returns <see cref="TranslationIdentityServiceImpl"/>.
		/// 
		/// You should instead call <see cref="GetCurrent"/> to get the active service, 
		/// and only call this method if you explicitly want to initialize a default translation service.
		/// Currently this is the same as <see cref="GetCurrent"/>, but this method is intended to be used for explicit initialization, while <see cref="GetCurrent"/> is intended for general retrieval of the active service.
		/// This separation allows for future changes where the default initialization logic might differ from the retrieval logic, without affecting the call sites that just want to get the current service.
		/// 
		/// Use it in Tests for example, to explicitly initialize identity translation before running tests that depend on it, without relying on the fact that <see cref="GetCurrent"/> also initializes identity translation when no service is active.
		/// </summary>
		public static ITranslationService CreateDefault()
		{
			lock (_sync)
			{
				return _instance ??= new TranslationIdentityServiceImpl();
			}
		}

		/// <summary>
		/// Returns the currently active translation service instance.
		/// If no service is active yet, initializes identity translation.
		/// </summary>
		public static ITranslationService GetCurrent()
		{
			lock (_sync)
			{
				return _instance ??= new TranslationIdentityServiceImpl();
			}
		}

		/// <summary>
		/// Gets the active language postfix, or <see langword="null"/> when identity translation is active.
		/// </summary>
		public static string ActiveLanguagePostfix
		{
			get
			{
				lock (_sync)
				{
					return _activeLanguagePostfix;
				}
			}
		}

		/// <summary>
		/// Replaces the repository used to discover and load translation files.
		/// Useful for tests and custom repository implementations.
		/// </summary>
		public static void ConfigureRepository(ITranslationFileRepository repository)
		{
			if (repository == null)
			{
				throw new ArgumentNullException(nameof(repository));
			}

			lock (_sync)
			{
				_translationFileRepository = repository;
			}
		}

		/// <summary>
		/// Registers an observer that is notified whenever the active language changes.
		/// </summary>
		public static void RegisterLanguageObserver(ITranslationLanguageObserver observer)
		{
			if (observer == null)
			{
				// dont't care
				return;
			}

			lock (_sync)
			{
				if (!_languageObservers.Contains(observer))
				{
					_languageObservers.Add(observer);
				}
			}
		}

		/// <summary>
		/// Removes a previously registered language observer.
		/// </summary>
		public static void UnregisterLanguageObserver(ITranslationLanguageObserver observer)
		{
			if (observer == null)
			{
				return;
			}

			lock (_sync)
			{
				_languageObservers.Remove(observer);
			}
		}

		/// <summary>
		/// Returns all valid language files for the given storage directory.
		/// </summary>
		public static IReadOnlyList<TranslationLanguageInfo> GetAvailableLanguages(string storageDirectory, Action<string> log = null)
		{
			lock (_sync)
			{
				return _translationFileRepository.GetAvailableLanguages(storageDirectory, log);
			}
		}

		/// <summary>
		/// Copies translation files from <paramref name="sourceDirectory"/> into the translations
		/// sub-folder of <paramref name="storageDirectory"/>.
		/// Only files with lowercase filenames on disk are copied to ensure platform consistency.
		/// Case-conflicting files and files with uppercase letters are skipped and reported via <paramref name="log"/>.
		/// </summary>
		/// <returns>Number of files successfully copied.</returns>
		public static int SyncTranslationFiles(string sourceDirectory, string storageDirectory, Action<string> log = null)
		{
			string targetDirectory = Path.Combine(storageDirectory, "translations");
			lock (_sync)
			{
				return _translationFileRepository.SyncFiles(sourceDirectory, targetDirectory, log);
			}
		}

		/// <summary>
		/// Activates identity translation and clears active language metadata.
		/// </summary>
		public static void UseIdentity()
		{
			ITranslationLanguageObserver[] observers;
			lock (_sync)
			{
				_instance = new TranslationIdentityServiceImpl();
				_activeLanguagePostfix = null;
				observers = [.. _languageObservers];
			}

			NotifyLanguageChanged(observers, _activeLanguagePostfix);
		}

		/// <summary>
		/// Tries to activate a language by postfix.
		/// Loads translations from file and swaps the active implementation to
		/// <see cref="FileTranslationServiceImpl"/> when successful.
		/// </summary>
		/// <param name="storageDirectory">Runtime storage root containing the translation folder.</param>
		/// <param name="postfix">Language postfix from file name pattern <c>civ_&lt;postfix&gt;.txt</c>.</param>
		/// <param name="error">Error text on failure; otherwise <see langword="null"/>.</param>
		/// <param name="log">Optional logger used during language discovery/loading.</param>
		public static bool TryUseLanguage(string storageDirectory, string postfix, out string error, Action<string> log = null)
		{
			error = null;
			ITranslationLanguageObserver[] observers;
			string activeLanguagePostfix;

			if (string.IsNullOrEmpty(postfix))
			{
				UseIdentity();
				return true;
			}

			lock (_sync)
			{
				IReadOnlyList<TranslationLanguageInfo> languages = _translationFileRepository.GetAvailableLanguages(storageDirectory, log);
				if (!languages.Any(language => language.MatchesPostfix(postfix)))
				{
					error = $"Language postfix '{postfix}' not found.";
					return false;
				}

				TranslationLanguageInfo languageInfo = languages.First(language => language.MatchesPostfix(postfix));

				if (!_translationFileRepository.TryLoadTranslations(languageInfo.FilePath, out IReadOnlyDictionary<string, string> translations, out string fileError))
				{
					error = fileError;
					return false;
				}

				_instance = new FileTranslationServiceImpl(translations);
				_activeLanguagePostfix = languageInfo.Postfix;
				observers = [.. _languageObservers];
				activeLanguagePostfix = _activeLanguagePostfix;
			}

			NotifyLanguageChanged(observers, activeLanguagePostfix);
			return true;
		}

		private static void NotifyLanguageChanged(IReadOnlyList<ITranslationLanguageObserver> observers, string activeLanguagePostfix)
		{
			foreach (ITranslationLanguageObserver observer in observers)
			{
				observer.OnLanguageChanged(activeLanguagePostfix);
			}
		}
	}
}
