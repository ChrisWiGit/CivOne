using System;

namespace CivOne.Services.Translation
{
	/// <summary>
	/// Describes a discovered language file.
	/// <para>
	/// Example: <c>civ_german.txt</c> maps to postfix <c>german</c>.
	/// </para>
	/// </summary>
	/// <param name="Postfix">Language postfix extracted from file name pattern <c>civ_&lt;postfix&gt;.txt</c>.</param>
	/// <param name="FilePath">Absolute path to the language file.</param>
	public readonly record struct TranslationLanguageInfo(string Postfix, string FilePath)
	{
		/// <summary>
		/// Returns <see langword="true"/> when <paramref name="postfix"/> matches this language postfix exactly.
		/// </summary>
		public bool MatchesPostfix(string postfix) => string.Equals(Postfix, postfix, StringComparison.OrdinalIgnoreCase);
	}
}
