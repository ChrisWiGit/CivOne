using System;
using System.Collections.Generic;
using CivOne.Agents;
using CivOne.Services;

namespace CivOne
{
	/// <summary>
	/// Optional plugin capability that provides one or more AI variants.
	/// The host discovers this capability on any public type of an enabled plugin assembly and
	/// registers every descriptor in the AI selection menu of a new game.
	/// </summary>
	public interface IPluginAiProvider
	{
		/// <summary>
		/// Gets lightweight metadata for all AI variants offered by this plugin.
		/// The host calls this whenever plugins change and builds the selection menu from the result
		/// alone, so this method must stay cheap and must not create any AI instances.
		/// </summary>
		/// <param name="translationService">
		/// The translation service used to localize the descriptor text.
		/// </param>
		/// <returns>
		/// The available AI variant descriptors.
		/// </returns>
		IReadOnlyList<AiDescriptor> GetAiDescriptors(ITranslationService translationService);

		/// <summary>
		/// Creates an AI registration for the selected variant and runtime options.
		/// The host calls this lazily, only once a player is actually resolved to this variant,
		/// never merely to populate the selection menu.
		/// </summary>
		/// <param name="id">
		/// The selected AI variant identifier from <see cref="AiDescriptor.Id"/>.
		/// </param>
		/// <param name="context">
		/// The runtime creation options including selected difficulty.
		/// </param>
		/// <returns>
		/// The created AI registration.
		/// Its <c>GetInformation().GetUuid()</c> is expected to equal <paramref name="id"/>.
		/// </returns>
		IAgentRegistration CreateAi(Guid id, AiCreationContext context);
	}
}
