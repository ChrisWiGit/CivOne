using CivOne.Services;

namespace CivOne.IO.Text
{
	/// <summary>
	/// Mutable game text component with reload and language observer behavior.
	/// Used by the factory-managed singleton instance.
	/// </summary>
	internal interface IGameTextsCommand : IGameTexts, ITranslationLanguageObserver
	{
		/// <summary>
		/// Reloads all configured text files and rebuilds key mappings.
		/// Call this after changing language or file contents.
		/// </summary>
		void Reset();
	}
}