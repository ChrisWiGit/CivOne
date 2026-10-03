using System;

namespace CivOne.Sound.Engine;

/// <summary>
/// Plays sounds, several at a time, with looping, pausing, volume and fades.
/// </summary>
/// <remarks>
/// This is what <c>IRuntime.PlaySound</c> cannot do: that path hands a whole file to the audio
/// backend and loses hold of it, so a sound can only be started or cut off. Here the samples are
/// produced as they are needed, which is what makes every capability above possible.
/// <para>
/// The two run side by side while callers are moved over. They do not share a device and do not
/// stop each other, so anything on <see cref="SoundBus.Music"/> belongs to this system alone once
/// it is in use - otherwise music would play twice.
/// </para>
/// </remarks>
internal interface ISoundSystem : IDisposable
{
	/// <summary>
	/// Starts a sound.
	/// </summary>
	/// <param name="request">What to play and how.</param>
	/// <returns>
	/// A handle for pausing, fading or stopping it, or <c>null</c> when the file could not be read.
	/// </returns>
	ISoundHandle? Play(SoundRequest request);

	/// <summary>
	/// Stops everything on a bus.
	/// </summary>
	/// <param name="bus">The bus to clear.</param>
	/// <param name="fade">How long to fade out; <see cref="TimeSpan.Zero"/> stops at once.</param>
	void StopAll(SoundBus bus, TimeSpan fade = default);

	/// <summary>
	/// Sets the master volume of a bus.
	/// </summary>
	/// <param name="bus">The bus to change.</param>
	/// <param name="volume">The new volume, from <c>0</c> to <c>1</c>.</param>
	void SetBusVolume(SoundBus bus, float volume);

	/// <summary>
	/// Raises the events of sounds that have ended.
	/// </summary>
	/// <remarks>
	/// Call once per frame from the game thread. The mixer cannot raise them itself: it runs on the
	/// audio thread, where game code must not run.
	/// </remarks>
	void Process();
}
