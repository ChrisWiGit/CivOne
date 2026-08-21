using System.Collections.Generic;
using CivOne.Services;

namespace CivOne
{
	/// <summary>
	/// Optional plugin capability that provides one or more map generator variants.
	/// </summary>
	public interface IPluginMapGeneratorProvider
	{
		/// <summary>
		/// Gets lightweight metadata for all map generator variants offered by this plugin.
		/// </summary>
		/// <returns>
		/// The available map generator descriptors.
		/// </returns>
		IReadOnlyList<MapGeneratorDescriptor> GetMapGeneratorDescriptors(ITranslationService translationService);

		/// <summary>
		/// Creates a map generator for the selected variant identifier.
		/// </summary>
		/// <param name="id">
		/// The selected map generator identifier from <see cref="MapGeneratorDescriptor.Id"/>.
		/// </param>
		/// <param name="context">
		/// The runtime creation options.
		/// </param>
		/// <returns>
		/// The created map generator instance.
		/// </returns>
		IMapGenerator CreateMapGenerator(string id, MapGeneratorCreationContext context);
	}
}
