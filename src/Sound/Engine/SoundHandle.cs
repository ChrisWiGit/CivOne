using System;
using System.Threading;

namespace CivOne.Sound.Engine;

/// <summary>
/// The game thread's view of one running sound.
/// </summary>
/// <remarks>
/// Every method queues a command and returns at once; the mixer carries it out on its next pass.
/// The properties are written by the mixer and read here without a lock, which is safe because each
/// is a single word and none of them is read together with another as a pair.
/// </remarks>
/// <param name="id">Identity of the voice, unique for the life of the mixer.</param>
/// <param name="mixer">Mixer the commands go to.</param>
/// <param name="sampleRate">Rate used to turn a fade length into a number of samples.</param>
internal sealed class SoundHandle(int id, SoundMixer mixer, int sampleRate) : ISoundHandle
{
	private int _playing = 1;
	private int _paused;
	private float _volume = 1f;
	private Action? _ended;
	private bool _endedRaised;

	/// <summary>Gets the identity of the voice this handle stands for.</summary>
	public int Id { get; } = id;

	/// <inheritdoc />
	public bool IsPlaying => Volatile.Read(ref _playing) != 0;

	/// <inheritdoc />
	public bool IsPaused => Volatile.Read(ref _paused) != 0;

	/// <inheritdoc />
	public float Volume
	{
		get => Volatile.Read(ref _volume);
		set
		{
			float clamped = Math.Clamp(value, 0f, 1f);
			Volatile.Write(ref _volume, clamped);
			mixer.Post(MixerCommand.SetVolume(Id, clamped));
		}
	}

	/// <inheritdoc />
	/// <remarks>
	/// A handler added after the sound has already ended is called at once. Without that a caller
	/// that starts a sound and subscribes afterwards could miss the event altogether - and a caller
	/// that waits for it to bring ducked music back would then leave the music frozen for good.
	/// </remarks>
	public event Action? Ended
	{
		add
		{
			if (_endedRaised)
			{
				value?.Invoke();
				return;
			}

			_ended += value;
		}

		remove => _ended -= value;
	}

	/// <inheritdoc />
	public void Pause(TimeSpan fade = default) => mixer.Post(MixerCommand.Pause(Id, ToSamples(fade)));

	/// <inheritdoc />
	public void Resume(TimeSpan fade = default)
	{
		Volatile.Write(ref _paused, 0);
		mixer.Post(MixerCommand.Resume(Id, ToSamples(fade)));
	}

	/// <inheritdoc />
	public void Stop(TimeSpan fade = default) => mixer.Post(MixerCommand.Stop(Id, ToSamples(fade)));

	/// <summary>
	/// Records that the mixer has paused or unpaused the voice.
	/// </summary>
	/// <param name="paused">Whether the voice is now frozen.</param>
	public void SetPaused(bool paused) => Volatile.Write(ref _paused, paused ? 1 : 0);

	/// <summary>
	/// Records that the voice has ended.
	/// </summary>
	public void SetEnded() => Volatile.Write(ref _playing, 0);

	/// <summary>
	/// Raises <see cref="Ended"/>.
	/// </summary>
	/// <remarks>
	/// Called from the game thread by <see cref="SoundSystem.Process"/>, never from the mixer.
	/// </remarks>
	public void RaiseEnded()
	{
		if (_endedRaised) return;

		Action? ended = _ended;
		_ended = null;
		_endedRaised = true;
		ended?.Invoke();
	}

	private int ToSamples(TimeSpan fade)
		=> fade <= TimeSpan.Zero ? 0 : (int)Math.Round(fade.TotalSeconds * sampleRate);
}
