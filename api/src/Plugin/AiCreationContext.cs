using CivOne.Services;

namespace CivOne
{
	/// <summary>
	/// Carries runtime options used when creating one AI registration.
	/// </summary>
	/// <param name="TranslationService">
	/// Gets the translation service the plugin can use for localized UI text.
	/// </param>
	/// <param name="Difficulty">
	/// Gets the selected game difficulty index (0..5).
	/// </param>
	public sealed record AiCreationContext(ITranslationService TranslationService, int Difficulty);
}
