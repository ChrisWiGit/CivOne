using System.Collections.Generic;
using CivOne.Agents;
using CivOne.Services;

namespace CivOne
{
	/// <summary>
	/// Optional plugin capability that provides one or more AI variants.
	/// </summary>
	public interface IPluginAiProvider
	{
		/// <summary>
		/// Gets lightweight metadata for all AI variants offered by this plugin.
		/// </summary>
		/// <returns>
		/// The available AI variant descriptors.
		/// </returns>
		IReadOnlyList<AiDescriptor> GetAiDescriptors(ITranslationService translationService);

		/// <summary>
		/// Creates an AI registration for the selected variant and runtime options.
		/// </summary>
		/// <param name="id">
		/// The selected AI variant identifier from <see cref="AiDescriptor.Id"/>.
		/// </param>
		/// <param name="context">
		/// The runtime creation options including selected difficulty.
		/// </param>
		/// <returns>
		/// The created AI registration.
		/// </returns>
		IAgentRegistration CreateAi(string id, AiCreationContext context);
	}
}
