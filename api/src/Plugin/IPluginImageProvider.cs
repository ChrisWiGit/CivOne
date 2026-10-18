using System;
using System.Collections.Generic;
using CivOne.Services;

namespace CivOne
{
	/// <summary>
	/// Optional plugin capability that provides one or more image pack variants.
	/// </summary>
	/// <remarks>
	/// NOT YET CONSUMED BY THE HOST.
	/// The host discovers and instantiates implementations of this interface, but it never calls
	/// <see cref="GetImageDescriptors"/> or <see cref="CreateImageFactory"/>: sprite lookup in
	/// <c>Resources</c> reads the game data files directly and has no override step for an
	/// <see cref="ImageStore"/>. Implementing this interface therefore has no observable effect yet.
	/// See the "Plugin capability providers" section in <c>TODO.md</c> for what is missing.
	/// </remarks>
	public interface IPluginImageProvider
	{
		/// <summary>
		/// Gets lightweight metadata for all image pack variants offered by this plugin.
		/// </summary>
		/// <param name="translationService">
		/// The translation service used to localize the descriptor text.
		/// </param>
		/// <returns>
		/// The available image pack descriptors.
		/// </returns>
		IReadOnlyList<ImagePackDescriptor> GetImageDescriptors(ITranslationService translationService);

		/// <summary>
		/// Creates an image pack factory for the selected variant identifier.
		/// </summary>
		/// <param name="id">
		/// The selected image pack identifier from <see cref="ImagePackDescriptor.Id"/>.
		/// </param>
		/// <param name="context">
		/// The runtime creation options.
		/// </param>
		/// <returns>
		/// The created image pack factory instance.
		/// </returns>
		IImagePackFactory CreateImageFactory(Guid id, ImageCreationContext context);
	}
}
