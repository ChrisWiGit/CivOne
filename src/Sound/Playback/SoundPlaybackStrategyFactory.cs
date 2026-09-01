using System;

namespace CivOne.Sound.Playback;

internal static class SoundPlaybackStrategyFactory
{
	/// <summary>
	/// Creates the strategy that serves a sound pack setting.
	/// </summary>
	/// <param name="soundPack">
	/// Id of the pack to play from, <c>"wave"</c> for the profile's wave files, or <c>"none"</c>.
	/// See <see cref="SoundPackSelectionDelegate"/>, which is what turns an unset setting into one
	/// of these.
	/// </param>
	/// <param name="soundPackPlaybackService">
	/// The service that plays converted packs. It carries the sound that is still being rendered,
	/// so all strategies have to share one instance.
	/// </param>
	/// <param name="aliases">
	/// The sound name redirects to apply before playing, or <c>null</c> for none.
	/// </param>
	/// <returns>The strategy to use.</returns>
	/// <remarks>
	/// <para>
	/// Exactly one source plays. A sound the chosen source does not carry is silent; nothing falls
	/// back to another source.
	/// </para>
	/// <para>
	/// The redirects are applied in front of the chosen strategy, not inside it, so a redirect can
	/// still send a sound to a different source than the one it would normally come from.
	/// </para>
	/// </remarks>
	public static ISoundPlaybackStrategy Create(string soundPack, SoundPackPlaybackService soundPackPlaybackService,
		ISoundAliasRegistry? aliases = null)
	{
		ISoundPlaybackStrategy strategy = CreateSource(soundPack, soundPackPlaybackService);

		return aliases == null ? strategy : new AliasSoundPlaybackStrategy(strategy, aliases);
	}

	private static ISoundPlaybackStrategy CreateSource(string soundPack, SoundPackPlaybackService soundPackPlaybackService)
	{
		// An empty id means nothing has been chosen. Staying silent is the honest answer; the
		// setting is meant to be resolved before it gets here.
		if (string.IsNullOrEmpty(soundPack)) return new NoSoundPlaybackStrategy();

		if (string.Equals(soundPack, SoundPlaybackStrategyConstants.NoSoundPack, StringComparison.OrdinalIgnoreCase)) return new NoSoundPlaybackStrategy();
		if (string.Equals(soundPack, SoundPlaybackStrategyConstants.WaveSoundPack, StringComparison.OrdinalIgnoreCase)) return new WaveSoundPlaybackStrategy();

		return new SoundPackPlaybackStrategy(soundPack, soundPackPlaybackService);
	}
}
