using System;

namespace CivOne.Sound.Playback;

/// <summary>
/// Plays sounds from one converted sound pack.
/// </summary>
/// <remarks>
/// The pack is the only source. A sound it does not carry is silent rather than being taken from
/// the profile's wave files, so what the player picked is what the player hears.
/// </remarks>
/// <param name="packId">Id of the pack to play from.</param>
/// <param name="soundPackPlaybackService">The service that renders and plays a pack's tunes.</param>
internal sealed class SoundPackPlaybackStrategy(
	string packId,
	SoundPackPlaybackService soundPackPlaybackService) : ISoundPlaybackStrategy
{
	/// <inheritdoc/>
	public bool PlaySound(string soundName) => soundPackPlaybackService.TryPlay(soundName, packId);

	/// <inheritdoc/>
	public void Abort()
	{
		// A tune that is still being rendered must not start after the game has silenced everything.
		soundPackPlaybackService.CancelPending();
		RuntimeHandler.Runtime.StopSound();
	}

	/// <inheritdoc/>
	public bool TryGetDuration(string soundName, out TimeSpan duration)
		=> soundPackPlaybackService.TryGetDuration(soundName, packId, out duration);
}
