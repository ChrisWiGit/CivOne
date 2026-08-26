namespace CivOne.Sound.Playback;

internal sealed class SoundPackPlaybackStrategy(
	string packId,
	SoundPackPlaybackService soundPackPlaybackService,
	ISoundPlaybackStrategy fallback) : ISoundPlaybackStrategy
{
	public bool PlaySound(string soundName)
	{
		if (soundPackPlaybackService.TryPlay(soundName, packId)) return true;

		return fallback.PlaySound(soundName);
	}

	public void Abort()
	{
		// A tune that is still being rendered must not start after the game has silenced everything.
		soundPackPlaybackService.CancelPending();
		RuntimeHandler.Runtime.StopSound();
	}
}
