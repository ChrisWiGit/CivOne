using System.Collections.Generic;
using CivOne.Services;

namespace CivOne
{
	/// <summary>
	/// Optional plugin capability that provides one or more image pack variants.
	/// </summary>
	public interface IPluginImageProvider
	{
		/// <summary>
		/// Gets lightweight metadata for all image pack variants offered by this plugin.
		/// </summary>
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
		IImagePackFactory CreateImageFactory(string id, ImageCreationContext context);
	}
}
