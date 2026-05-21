using System;
using System.Collections.Generic;

namespace CivOne.Services.Translation
{
	/// <summary>
	/// File-backed translation service.
	/// Performs case-insensitive key lookup from a preloaded dictionary and falls back to
	/// <see cref="TranslationIdentityServiceImpl"/> (or a provided fallback service) for missing keys.
	/// </summary>
	public class FileTranslationServiceImpl(IReadOnlyDictionary<string, string> translations, ITranslationService fallback = null) : ITranslationService
	{
		private readonly IReadOnlyDictionary<string, string> _translations = translations ?? throw new ArgumentNullException(nameof(translations));
		private readonly ITranslationService _fallback = fallback ?? new TranslationIdentityServiceImpl();

		/// <inheritdoc/>
		public string Translate(string key)
		{
			string normalized = NormalizeKey(key);
			if (normalized.Length == 0)
			{
				return _fallback.Translate(key);
			}

			if (_translations.TryGetValue(normalized, out string translated))
			{
				return translated;
			}

			return _fallback.Translate(key);
		}

		/// <inheritdoc/>
		public string TranslateFormatted(string key, params object[] args)
		{
			string formatText = Translate(key);
			return string.Format(formatText, args);
		}

		/// <inheritdoc/>
		public string[] TranslateArray(string key)
			=> Translate(key).Split('\n');

		/// <inheritdoc/>
		public string[] TranslateFormattedArray(string key, params object[] args)
			=> TranslateFormatted(key, args).Split('\n');

		private static string NormalizeKey(string key) => key?.Trim().ToUpperInvariant() ?? string.Empty;
	}
}