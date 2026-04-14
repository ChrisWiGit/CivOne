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

	public interface ITranslationService
	{
		/// <summary>
		/// Translates a key to a localized string.
		/// The <paramref name="key"/> should be a human-readable English text that also serves as
		/// a unique identifier (e.g. <c>"4000 BC"</c>, <c>"Load game from new format…"</c>).
		/// If no translation is available the key itself should be returned unchanged.
		/// </summary>
		string Translate(string key);

		/// <summary>
		/// Translates a key and formats the result using <paramref name="args"/>.
		/// See <see cref="Translate(string)"/> for key conventions.
		/// </summary>
		string TranslateFormatted(string key, params object[] args);
	}
}