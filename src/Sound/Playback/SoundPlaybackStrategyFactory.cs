using System;

namespace CivOne.Sound.Playback;



internal static class SoundPlaybackStrategyFactory
{
	public static ISoundPlaybackStrategy Create(string soundPack)
	{
		var waveStrategy = new WaveSoundPlaybackStrategy();
		var soundPackPlaybackService = new SoundPackPlaybackService(new SoundPackWaveRenderService());

		if (string.Equals(soundPack, SoundPlaybackStrategyConstants.NoSoundPack, StringComparison.OrdinalIgnoreCase)) return new NoSoundPlaybackStrategy();
		if (string.Equals(soundPack, SoundPlaybackStrategyConstants.WaveSoundPack, StringComparison.OrdinalIgnoreCase)) return waveStrategy;
		if (string.IsNullOrEmpty(soundPack)) return new AutoSoundPlaybackStrategy(soundPackPlaybackService, waveStrategy);

		return new SoundPackPlaybackStrategy(soundPack, soundPackPlaybackService, waveStrategy);
	}
}