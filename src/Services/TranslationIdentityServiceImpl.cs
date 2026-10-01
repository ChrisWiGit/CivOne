using System;
using System.Globalization;

namespace CivOne.Services
{

	/// <summary>
	/// Identity translation implementation.
	/// Returns keys unchanged and only applies string formatting for formatted calls.
	/// Used as fallback when no language file is active.
	/// </summary>
	public class TranslationIdentityService : ITranslationService
	{
		/// <inheritdoc/>
		public string Translate(string key)
		{
			return key;
		}

		/// <inheritdoc/>
		public string TranslateFormatted(string key, params object[] args)
		{
			return string.Format(CultureInfo.CurrentCulture, key, args);
		}

		/// <inheritdoc/>
		public string[] TranslateArray(string key)
			=> Translate(key).Split('\n');

		/// <inheritdoc/>
		public string[] TranslateFormattedArray(string key, params object[] args)
			=> TranslateFormatted(key, args).Split('\n');
	}
}