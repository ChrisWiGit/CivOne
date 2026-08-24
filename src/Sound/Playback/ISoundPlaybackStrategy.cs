namespace CivOne.Sound.Playback;

#nullable enable

internal interface ISoundPlaybackStrategy
{
	/// <summary>
	/// Plays the sound with the given name.
	/// A currently playing sound will be stopped before the new sound is played.
	/// A currently playing sound can be stopped by calling <see cref="Abort"/>.
	/// </summary>
	/// <param name="soundName"></param>
	/// <returns>True if the sound was successfully played, false otherwise.</returns>
	bool PlaySound(string soundName);
	void Abort();
}