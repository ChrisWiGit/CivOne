using System;

namespace CivOne.Sound.Playback;

internal static class SoundPlaybackStrategyFactory
{
	/// <summary>
	/// Creates the strategy that serves a sound pack setting.
	/// </summary>
	/// <param name="soundPack">
	/// Id of the pack to play from, <c>"none"</c>, <c>"wave"</c>, or empty for automatic choice.
	/// </param>
	/// <param name="soundPackPlaybackService">
	/// The service that plays converted packs. It carries the sound that is still being rendered,
	/// so all strategies have to share one instance.
	/// </param>
	/// <returns>The strategy to use.</returns>
	public static ISoundPlaybackStrategy Create(string soundPack, SoundPackPlaybackService soundPackPlaybackService)
	{
		var waveStrategy = new WaveSoundPlaybackStrategy();

		if (string.Equals(soundPack, SoundPlaybackStrategyConstants.NoSoundPack, StringComparison.OrdinalIgnoreCase)) return new NoSoundPlaybackStrategy();
		if (string.Equals(soundPack, SoundPlaybackStrategyConstants.WaveSoundPack, StringComparison.OrdinalIgnoreCase)) return waveStrategy;
		if (string.IsNullOrEmpty(soundPack)) return new AutoSoundPlaybackStrategy(soundPackPlaybackService, waveStrategy);

		return new SoundPackPlaybackStrategy(soundPack, soundPackPlaybackService, waveStrategy);
	}
}
