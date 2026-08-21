using CivOne.Services;

namespace CivOne
{
	/// <summary>
	/// Carries runtime options used when creating one image pack factory instance.
	/// </summary>
	/// <param name="TranslationService">
	/// Gets the translation service the plugin can use for localized UI text.
	/// </param>
	public sealed record ImageCreationContext(ITranslationService TranslationService);
}
