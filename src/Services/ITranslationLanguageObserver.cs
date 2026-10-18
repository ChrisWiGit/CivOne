namespace CivOne.Services
{
	/// <summary>
	/// Observer for translation language changes.
	/// </summary>
	public interface ITranslationLanguageObserver
	{
		/// <summary>
		/// Called whenever the active language changes.
		/// </summary>
		/// <param name="activeLanguagePostfix">
		/// The active language postfix, or <see langword="null"/> when identity translation is active.
		/// </param>
		void OnLanguageChanged(string? activeLanguagePostfix);
	}
}