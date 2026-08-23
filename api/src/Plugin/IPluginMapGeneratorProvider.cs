using System;
using System.Collections.Generic;
using CivOne.Services;

namespace CivOne
{
	/// <summary>
	/// Optional plugin capability that provides one or more map generator variants.
	/// </summary>
	/// <remarks>
	/// NOT YET CONSUMED BY THE HOST.
	/// The host discovers and instantiates implementations of this interface, but it never calls
	/// <see cref="GetMapGeneratorDescriptors"/> or <see cref="CreateMapGenerator"/>: world generation
	/// is still hard-wired in <c>Map.Generate</c>, and the world setup screen offers no generator
	/// choice. Implementing this interface therefore has no observable effect yet.
	/// See the "Plugin capability providers" section in <c>TODO.md</c> for what is missing.
	/// </remarks>
	public interface IPluginMapGeneratorProvider
	{
		/// <summary>
		/// Gets lightweight metadata for all map generator variants offered by this plugin.
		/// </summary>
		/// <param name="translationService">
		/// The translation service used to localize the descriptor text.
		/// </param>
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
		IMapGenerator CreateMapGenerator(Guid id, MapGeneratorCreationContext context);
	}
}
