using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CivOne.Sound.Dsp;

namespace CivOne.Sound.Engine;

/// <summary>
/// Sums the running voices into one output buffer.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Render"/> runs on the audio thread. Everything else runs on the game thread and only
/// ever queues work, so the audio thread never waits for a lock.
/// </para>
/// <para>
/// The mixer knows nothing about files, packs or sound names. It is handed sample data and told
/// what to do with it, which is what lets it be tested without a sound card.
/// </para>
/// <para>
/// Deliberately rate-free. A fade reaches it as a number of samples, not as a length of time, so
/// the rate is only needed where a <see cref="TimeSpan"/> is turned into samples - and that is the
/// device's own rate, which is final only once the device has been opened.
/// </para>
/// </remarks>
internal sealed class SoundMixer
{
	private readonly ConcurrentQueue<MixerCommand> _commands = new();
	private readonly ConcurrentQueue<SoundHandle> _ended = new();
	private readonly List<MixerVoice> _voices = [];
	private readonly float[] _busVolume = [.. Enumerable.Repeat(1f, Enum.GetValues<SoundBus>().Length)];
	private readonly SoftLimiterDelegate _limiter = new();

	private int _nextHandleId;

	/// <summary>
	/// Queues a command for the next pass.
	/// </summary>
	/// <param name="command">The command to queue.</param>
	public void Post(MixerCommand command) => _commands.Enqueue(command);

	/// <summary>
	/// Hands out the next identity for a voice.
	/// </summary>
	/// <returns>An identity that is unique for the life of this mixer.</returns>
	public int NextHandleId() => Interlocked.Increment(ref _nextHandleId);

	/// <summary>
	/// Takes the next sound that has ended, so the game thread can raise its event.
	/// </summary>
	/// <param name="handle">The handle of the sound that ended.</param>
	/// <returns><c>true</c> when one was waiting.</returns>
	public bool TryTakeEnded(out SoundHandle? handle) => _ended.TryDequeue(out handle);

	/// <summary>
	/// Fills the buffer with the sum of all voices.
	/// </summary>
	/// <param name="buffer">Buffer to fill; its previous contents are overwritten.</param>
	public void Render(Span<float> buffer)
	{
		ApplyCommands();

		buffer.Clear();

		for (int index = 0; index < _voices.Count; index++)
		{
			MixVoice(_voices[index], buffer);
		}

		RemoveFinished();
		_limiter.Limit(buffer);
	}

	private void ApplyCommands()
	{
		while (_commands.TryDequeue(out MixerCommand command))
		{
			switch (command.Kind)
			{
				case MixerCommandKind.Add when command.Voice != null:
					_voices.Add(command.Voice);
					break;

				case MixerCommandKind.Pause:
					PauseVoice(Find(command.HandleId), command.FadeSamples);
					break;

				case MixerCommandKind.Resume:
					ResumeVoice(Find(command.HandleId), command.FadeSamples);
					break;

				case MixerCommandKind.Stop:
					Find(command.HandleId)?.FadeTo(0f, command.FadeSamples, VoiceFadeAction.Stop);
					break;

				case MixerCommandKind.SetVolume:
					SetVoiceVolume(Find(command.HandleId), command.Value);
					break;

				case MixerCommandKind.StopBus:
					StopBus(command.Bus, command.FadeSamples);
					break;

				case MixerCommandKind.SetBusVolume:
					_busVolume[(int)command.Bus] = Math.Clamp(command.Value, 0f, 1f);
					break;

				default:
					break;
			}
		}
	}

	/// <summary>
	/// Finds the voice a command addresses.
	/// </summary>
	/// <param name="handleId">Identity the command carries.</param>
	/// <returns>The voice, or <c>null</c> when it is gone or already on its way out.</returns>
	/// <remarks>
	/// A voice that is fading out to be removed is deliberately not found any more: a stop is final.
	/// Otherwise a late resume would revive it - which is exactly what happens when a sting is cut
	/// short after the music it ducked has already been told to stop.
	/// </remarks>
	private MixerVoice? Find(int handleId)
	{
		for (int index = 0; index < _voices.Count; index++)
		{
			MixerVoice voice = _voices[index];
			if (voice.Handle == null || voice.Handle.Id != handleId) continue;
			if (voice.Finished || voice.FadeAction == VoiceFadeAction.Stop) continue;

			return voice;
		}

		return null;
	}

	private static void PauseVoice(MixerVoice? voice, int fadeSamples)
	{
		if (voice == null || voice.Paused) return;

		voice.FadeTo(0f, fadeSamples, VoiceFadeAction.Pause);
	}

	private static void ResumeVoice(MixerVoice? voice, int fadeSamples)
	{
		if (voice == null) return;

		voice.Paused = false;
		voice.Handle?.SetPaused(paused: false);
		voice.FadeTo(voice.Volume, fadeSamples, VoiceFadeAction.None);
	}

	private static void SetVoiceVolume(MixerVoice? voice, float volume)
	{
		if (voice == null) return;

		voice.Volume = Math.Clamp(volume, 0f, 1f);

		// A running fade owns the gain until it arrives; changing the volume only moves where it is
		// heading, so a fade in that is interrupted by a volume change does not jump.
		if (voice.GainStep > 0f)
		{
			if (voice.TargetGain > 0f) voice.TargetGain = voice.Volume;
			return;
		}

		if (!voice.Paused) voice.Gain = voice.Volume;
	}

	private void StopBus(SoundBus bus, int fadeSamples)
	{
		for (int index = 0; index < _voices.Count; index++)
		{
			MixerVoice voice = _voices[index];
			if (voice.Bus != bus || voice.Finished) continue;

			voice.FadeTo(0f, fadeSamples, VoiceFadeAction.Stop);
		}
	}

	private void MixVoice(MixerVoice voice, Span<float> buffer)
	{
		float busVolume = _busVolume[(int)voice.Bus];

		for (int index = 0; index < buffer.Length; index++)
		{
			// A paused voice contributes nothing and, above all, does not advance: it continues from
			// the very sample it stopped on.
			if (voice.Paused || voice.Finished) return;

			// A tail left behind by a loop turnaround joins part way into the buffer, at the very
			// sample the turnaround happened on.
			if (voice.StartDelay > 0)
			{
				voice.StartDelay--;
				continue;
			}

			if (voice.Position < 0 || voice.Position >= voice.Samples.Length)
			{
				voice.Finished = true;
				return;
			}

			buffer[index] += voice.Samples[voice.Position] * voice.Gain * busVolume;

			voice.Position++;
			voice.AdvanceGain();

			if (voice.Loop && voice.Position >= voice.LoopEnd)
			{
				WrapLoop(voice, index);
				continue;
			}

			if (voice.Position >= voice.Samples.Length) voice.Finished = true;
		}
	}

	/// <summary>
	/// Turns a looping voice around, overlapping the old tail with the new head when asked for.
	/// </summary>
	/// <remarks>
	/// The overlap is modelled as a second voice rather than a second read position: the tail keeps
	/// reading past the turnaround while the voice itself starts again, and the tail removes itself
	/// once it has faded out. That is the same mechanism a cross fade between two pieces uses.
	/// </remarks>
	private void WrapLoop(MixerVoice voice, int bufferIndex)
	{
		// A loop that does not move forward would spawn a tail on every single sample.
		if (voice.LoopEnd <= voice.LoopStart)
		{
			voice.Loop = false;
			return;
		}

		int crossFade = Math.Min(voice.LoopCrossFade, voice.Samples.Length - voice.LoopEnd);

		if (crossFade > 0)
		{
			var tail = new MixerVoice
			{
				Samples = voice.Samples,
				Handle = null,
				Bus = voice.Bus,
				Position = voice.Position,
				Loop = false,
				Volume = voice.Volume,
				Gain = voice.Gain,
				TargetGain = voice.Gain,
				StartDelay = bufferIndex + 1
			};

			tail.FadeTo(0f, crossFade, VoiceFadeAction.Stop);
			_voices.Add(tail);
		}

		voice.Position = voice.LoopStart;

		if (crossFade > 0)
		{
			voice.Gain = 0f;
			voice.FadeTo(voice.Volume, crossFade, VoiceFadeAction.None);
		}
	}

	private void RemoveFinished()
	{
		for (int index = _voices.Count - 1; index >= 0; index--)
		{
			MixerVoice voice = _voices[index];
			if (!voice.Finished) continue;

			_voices.RemoveAt(index);

			if (voice.Handle == null) continue;

			voice.Handle.SetEnded();
			_ended.Enqueue(voice.Handle);
		}
	}
}
