namespace CivOne.Sound.Playback;

#nullable enable

internal sealed class NoSoundPlaybackStrategy : ISoundPlaybackStrategy
{
	public bool PlaySound(string soundName)
	{
		// return true to avoid error handling in the calling code.
		return true;
	}

	public void Abort()
	{
		RuntimeHandler.Runtime.StopSound();
	}
}