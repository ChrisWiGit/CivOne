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
	bool PlaySound(string soundName);
	void Abort();

	/// <summary>
	/// Tries to get how long one pass of a sound takes, without playing it.
	/// </summary>
	/// <param name="soundName">Name of the sound to look up.</param>
	/// <param name="duration">The length of one pass, when this source has timing data for it.</param>
	/// <returns><c>true</c> when the duration is known.</returns>
	bool TryGetDuration(string soundName, out TimeSpan duration);
}