// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Text;

namespace CivOne.Screens.GamePlayPanels
{
	/// <summary>
	/// Stores the resolved menu-bar label data used for drawing and hotkey handling.
	/// </summary>
	/// <param name="TranslationKey">
	/// Original English translation key.
	/// </param>
	/// <param name="VisibleText">
	/// Menu text shown on screen without the hotkey marker.
	/// </param>
	/// <param name="HighlightedCharacterIndex">
	/// Zero-based index of the character that should use the hotkey highlight colour.
	/// </param>
	/// <param name="Hotkey">
	/// Uppercase hotkey character resolved from the highlighted position.
	/// </param>
	internal readonly record struct MenuBarTitle(string TranslationKey, string VisibleText, int HighlightedCharacterIndex, char Hotkey);

	/// <summary>
	/// Parses translated top menu labels that may contain a <c>~</c> hotkey marker.
	/// </summary>
	/// <remarks>
	/// The marker is removed from the visible text.
	/// The first valid marker defines both the highlighted character and the Alt hotkey.
	/// If no valid marker exists, the first visible character is used as fallback.
	/// </remarks>
	internal sealed class MenuBarHotkeyDelegate
	{
		private const char Marker = '~';
		private readonly char _marker = Marker;

		/// <summary>
		/// Creates a resolved menu title from a translation key and its translated text.
		/// </summary>
		/// <param name="translationKey">
		/// Original English translation key.
		/// </param>
		/// <param name="translatedText">
		/// Translated text that may contain a <c>~</c> marker before the desired hotkey character.
		/// </param>
		/// <returns>
		/// A parsed <see cref="MenuBarTitle"/> value for rendering and hotkey lookup.
		/// </returns>
		public MenuBarTitle Create(string translationKey, string translatedText) => Parse(translationKey, translatedText);

		/// <summary>
		/// Parses translated menu text and resolves visible text, highlight position, and hotkey.
		/// </summary>
		/// <param name="translationKey">
		/// Original English translation key used as fallback.
		/// </param>
		/// <param name="translatedText">
		/// Translated menu label that may contain a <c>~</c> marker.
		/// </param>
		/// <returns>
		/// A parsed <see cref="MenuBarTitle"/> value.
		/// </returns>
		internal MenuBarTitle Parse(string translationKey, string translatedText)
		{
			string sourceText = string.IsNullOrEmpty(translatedText) ? translationKey : translatedText;
			StringBuilder visibleTextBuilder = new();
			int highlightedCharacterIndex = -1;

			for (int i = 0; i < sourceText.Length; i++)
			{
				char current = sourceText[i];
				if (current == _marker)
				{
					if (highlightedCharacterIndex < 0 && i + 1 < sourceText.Length)
					{
						highlightedCharacterIndex = visibleTextBuilder.Length;
					}
					continue;
				}

				visibleTextBuilder.Append(current);
			}

			string visibleText = visibleTextBuilder.ToString();
			if (visibleText.Length == 0)
			{
				visibleText = translationKey;
			}

			if (highlightedCharacterIndex < 0 || highlightedCharacterIndex >= visibleText.Length)
			{
				highlightedCharacterIndex = 0;
			}

			char hotkey = char.ToUpperInvariant(visibleText[highlightedCharacterIndex]);
			return new MenuBarTitle(translationKey, visibleText, highlightedCharacterIndex, hotkey);
		}
	}
}