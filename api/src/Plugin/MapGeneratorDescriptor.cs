using System.Collections.Generic;

namespace CivOne
{
	/// <summary>
	/// Describes one selectable map generator variant exposed by a plugin.
	/// </summary>
	/// <param name="Id">
	/// Gets the stable unique identifier of the map generator variant.
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
	/// <param name="SupportedSizes">
	/// Gets the predefined map size presets the generator supports.
	/// These are shown in order in the map size selection menu.
	/// An empty list means the generator does not offer any size presets.
	/// </param>
	/// <param name="SupportsCustomSize">
	/// Gets whether the generator accepts arbitrary width and height values
	/// beyond the entries in <see cref="SupportedSizes"/>.
	/// When <see langword="false"/> the UI must restrict selection to <see cref="SupportedSizes"/> only.
	/// </param>
	/// <param name="Tags">
	/// Gets optional tags for grouping and filtering in the UI.
	/// </param>
	public sealed record MapGeneratorDescriptor(
		string Id,
		string Name,
		string Author,
		string Description,
		string Version,
		IReadOnlyList<MapSizePreset> SupportedSizes,
		bool SupportsCustomSize = false,
		IReadOnlyList<string>? Tags = null);
}
