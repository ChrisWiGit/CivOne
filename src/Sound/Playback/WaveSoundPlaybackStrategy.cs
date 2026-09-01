namespace CivOne.Sound.Playback;

/// <summary>
/// Plays a sound from a wave file in the profile's sounds folder.
/// </summary>
/// <remarks>
/// Which file that is - and that a collection of wave files usually covers only part of the sounds
/// the game knows - is <see cref="WaveSoundFileDelegate"/>'s business.
/// </remarks>
internal sealed class WaveSoundPlaybackStrategy : ISoundPlaybackStrategy
{
	private readonly WaveSoundFileDelegate _files = new();

	/// <inheritdoc/>
	public bool PlaySound(string soundName)
	{
		if (!_files.TryResolve(soundName, out string? soundFile)) return false;

		RuntimeHandler.Runtime.PlaySound(soundFile);
		return true;
	}

	/// <inheritdoc/>
	public void Abort()
	{
		RuntimeHandler.Runtime.StopSound();
	}
}
