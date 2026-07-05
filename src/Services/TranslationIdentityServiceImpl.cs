// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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