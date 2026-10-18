using System;

namespace CivOne.Sound.Engine;

/// <summary>
/// One running sound.
/// </summary>
/// <remarks>
/// Every member is safe to call from the game thread.
/// A handle only ever asks the mixer to do something; the change takes effect on the mixer's next
/// pass, which is why nothing here returns a result.
/// The handle stays usable after the sound has ended, so a caller never has to guard against a
/// sound that finished while it was not looking.
/// </remarks>
internal interface ISoundHandle
{
	/// <summary>
	/// Gets whether the sound is still running.
	/// A paused sound is still running.
	/// </summary>
	bool IsPlaying { get; }

	/// <summary>
	/// Gets whether the sound is paused.
	/// </summary>
	bool IsPaused { get; }

	/// <summary>
	/// Gets or sets the volume of this sound, from <c>0</c> to <c>1</c>.
	/// </summary>
	/// <remarks>
	/// Setting this jumps to the new volume.
	/// Use <see cref="Pause"/> and <see cref="Resume"/> when the change should be gradual.
	/// </remarks>
	float Volume { get; set; }

	/// <summary>
	/// Stops advancing the sound and keeps its position.
	/// </summary>
	/// <param name="fade">
	/// How long to fade to silence before the sound freezes, so the stop is not audible as a click.
	/// </param>
	/// <remarks>
	/// This is deliberately not the same as setting <see cref="Volume"/> to zero: a paused sound
	/// does not advance, so it continues from the very sample it stopped on.
	/// The original driver ducked a theme this way, and a theme that kept running silently would
	/// come back shifted by the whole length of whatever played over it.
	/// </remarks>
	void Pause(TimeSpan fade = default);

	/// <summary>
	/// Continues from the position the sound was paused at.
	/// </summary>
	/// <param name="fade">How long to fade back up to <see cref="Volume"/>.</param>
	void Resume(TimeSpan fade = default);

	/// <summary>
	/// Ends the sound.
	/// </summary>
	/// <param name="fade">How long to fade to silence before the sound ends.</param>
	void Stop(TimeSpan fade = default);

	/// <summary>
	/// Raised once when the sound has ended, whether it ran out or was stopped.
	/// </summary>
	/// <remarks>
	/// Raised on the game thread from <see cref="ISoundSystem.Process"/>, never from the audio
	/// callback, so a handler may do whatever game code normally does.
	/// <para>
	/// A handler added after the sound has already ended is called at once, so a caller that starts
	/// a sound and subscribes afterwards can never miss it.
	/// </para>
	/// </remarks>
	event Action? Ended;
}
