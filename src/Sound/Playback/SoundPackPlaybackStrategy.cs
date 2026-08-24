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
		RuntimeHandler.Runtime.StopSound();
	}
}