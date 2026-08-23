using System;
using System.Collections.Generic;
using CivOne.Agents;

namespace CivOne
{
	/// <summary>
	/// Describes one selectable AI variant exposed by a plugin.
	/// </summary>
	/// <param name="Id">
	/// Gets the stable unique identifier of the AI variant.
	/// This is the single identity of the variant: the host passes it back to
	/// <see cref="IPluginAiProvider.CreateAi(Guid, AiCreationContext)"/> to select it, uses it as the
	/// registry key, and persists it in the save game as the player's chosen AI.
	/// It must therefore stay stable across plugin versions, and it must match the value returned by
	/// the created registration's <c>GetInformation().GetUuid()</c>.
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
	/// <param name="DefaultDifficulty">
	/// Gets the preferred game difficulty for this variant.
	/// Defaults to <see cref="AiDifficulty.Unspecified"/>, which lets the host use the difficulty
	/// the player picked.
	/// </param>
	public sealed record AiDescriptor(
		Guid Id,
		string Name,
		string Author,
		string Description,
		string Version,
		IReadOnlyList<string>? Tags = null,
		AiDifficulty DefaultDifficulty = AiDifficulty.Unspecified);
}
