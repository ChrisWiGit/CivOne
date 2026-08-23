using CivOne.Agents;
using CivOne.Services;

namespace CivOne
{
	/// <summary>
	/// Carries runtime options used when creating one AI registration.
	/// </summary>
	/// <remarks>
	/// This deliberately does not carry a difficulty. One registration is created per AI variant and
	/// shared by every player using it, and at creation time there is no acting player yet.
	/// The difficulty is per player, so an AI reads it from
	/// <see cref="ITurnContext.Difficulty"/> when its turn runs.
	/// </remarks>
	/// <param name="TranslationService">
	/// Gets the translation service the plugin can use for localized UI text.
	/// </param>
	public sealed record AiCreationContext(ITranslationService TranslationService);
}
