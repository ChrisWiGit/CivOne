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
	/// Defines the runtime translation contract used by UI and gameplay services.
	/// Implementations include identity fallback (<see cref="TranslationIdentityService"/>) and file-based lookup
	/// (<see cref="Translation.FileTranslationService"/>).
	///
	/// Static text example:
	/// Before:
	/// <code>
	/// this.DrawText("Population:", 0, 15, x, y);
	/// </code>
	/// After:
	/// <code>
	/// this.DrawText(Translate("Population"), 0, 15, x, y);
	/// </code>
	///
	/// Dynamic text with placeholders example:
	/// Before:
	/// <code>
	/// this.DrawText($"Population: {population} Happy:{happy}% Content:{content}% Unhappy:{unhappy}%", 0, 15, x, y);
	/// </code>
	/// After:
	/// <code>
	/// this.DrawText(TranslateFormatted("Population: {0} Happy:{1}% Content:{2}% Unhappy:{3}%", population, happy, content, unhappy), 0, 15, x, y);
	/// </code>
	/// The Translation key entry looks like this:
	/// <code>
	/// POPULATION: {0} HAPPY:{1}% CONTENT:{2}% UNHAPPY:{3}%: "Population: {0} Happy:{1}% Content:{2}% Unhappy:{3}%"
	/// </code>
	/// </summary>
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

		/// <summary>
		/// Translates a key and splits the result on <c>\n</c>, returning each line as a separate array entry.
		/// Allows a multi-line message to be stored as a single translation key.
		/// </summary>
		string[] TranslateArray(string key);

		/// <summary>
		/// Translates a key, formats the result using <paramref name="args"/>, then splits on <c>\n</c>.
		/// Use "\n" and not "\\n" in the translation value, as the splitting is done after formatting.
		/// </summary>
		string[] TranslateFormattedArray(string key, params object[] args);
	}
}