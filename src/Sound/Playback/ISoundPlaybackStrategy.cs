using System;

namespace CivOne.Sound.Playback;



internal interface ISoundPlaybackStrategy
{
	/// <summary>
	/// Plays the sound with the given name.
	/// A currently playing sound will be stopped before the new sound is played.
	/// A currently playing sound can be stopped by calling <see cref="Abort"/>.
	/// </summary>
	/// <param name="soundName"></param>
	/// <returns>True if the sound was successfully played, false otherwise.</returns>
	/// <remarks>
	/// Callers use the result to remember that they started the sound and have to stop it again when
	/// they are done, which is why a source that plays nothing at all still reports success: there is
	/// no failure to handle, and <see cref="Abort"/> works either way. It stops whatever is playing
	/// and does nothing when that is nothing, so an owner of silence costs no more than a call.
	/// </remarks>
	bool PlaySound(string soundName);

	/// <summary>
	/// Stops the currently playing sound, if any.
	/// This method is idempotent; calling it multiple times has the same effect as calling it once.
	/// If no sound is currently playing, this method does nothing.
	/// </summary>
	void Abort();

	/// <summary>
	/// Tries to get how long one pass of a sound takes, without playing it.
	/// </summary>
	/// <param name="soundName">Name of the sound to look up.</param>
	/// <param name="duration">The length of one pass, when this source has timing data for it.</param>
	/// <returns><c>true</c> when the duration is known.</returns>
	bool TryGetDuration(string soundName, out TimeSpan duration);
}