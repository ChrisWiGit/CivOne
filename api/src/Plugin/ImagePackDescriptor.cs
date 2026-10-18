using System;
using System.Collections.Generic;

namespace CivOne
{
	/// <summary>
	/// Describes one selectable image pack variant exposed by a plugin.
	/// </summary>
	/// <param name="Id">
	/// Gets the stable unique identifier of the image pack variant.
	/// The host passes it back to
	/// <see cref="IPluginImageProvider.CreateImageFactory(Guid, ImageCreationContext)"/>
	/// to select this variant, so it must stay stable across plugin versions.
	/// </param>
	/// <param name="Name">
	/// Gets the user-facing display name.
	/// </param>
	/// <param name="Author">
	/// Gets the author or provider name.
	/// </param>
	/// <param name="Description">
	/// Gets the short human-readable behavior description.
	/// </param>
	/// <param name="Version">
	/// Gets the semantic version string of the variant.
	/// </param>
	/// <param name="Tags">
	/// Gets optional tags for grouping and filtering in the UI.
	/// </param>
	public sealed record ImagePackDescriptor(
		Guid Id,
		string Name,
		string Author,
		string Description,
		string Version,
		IReadOnlyList<string>? Tags = null);
}
