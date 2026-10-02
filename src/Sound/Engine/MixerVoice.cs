using System;

namespace CivOne.Sound.Engine;

/// <summary>
/// One sound inside the mixer.
/// </summary>
/// <remarks>
/// Only the mixer touches this, and only from the audio thread, so nothing here is synchronised.
/// The game thread reaches a voice through its <see cref="Handle"/>, which turns every request into
/// a command the mixer picks up on its next pass.
/// <para>
/// <see cref="Position"/> is a whole sample rather than a fraction: nothing here changes playback
/// speed, and an exact counter cannot drift over a piece that loops for an hour.
/// </para>
/// </remarks>
internal sealed class MixerVoice
{
	/// <summary>Sample data. Shared with other voices and never modified.</summary>
	/// <remarks>
	/// Settable rather than init-only so the mixer can hand a spent voice back out as the tail of a
	/// loop turnaround. Only the mixer does that, and only on a voice it has already removed.
	/// </remarks>
	public required float[] Samples { get; set; }

	/// <summary>The handle the game thread holds, or <c>null</c> for a voice nobody can reach.</summary>
	/// <remarks>
	/// The fading tail left behind by a loop turnaround has no handle: it is an echo of another
	/// voice and ends on its own.
	/// </remarks>
	public SoundHandle? Handle { get; set; }

	/// <summary>Which bus the voice plays on.</summary>
	public SoundBus Bus { get; set; }

	/// <summary>Read position in samples. Frozen while <see cref="Paused"/>.</summary>
	public int Position { get; set; }

	/// <summary>Whether the voice turns around at <see cref="LoopEnd"/> instead of ending.</summary>
	public bool Loop { get; set; }

	/// <summary>Sample the loop returns to.</summary>
	public int LoopStart { get; set; }

	/// <summary>Sample the loop turns around at.</summary>
	public int LoopEnd { get; set; }

	/// <summary>Length of the overlap at the turnaround, in samples. <c>0</c> turns around hard.</summary>
	public int LoopCrossFade { get; set; }

	/// <summary>Volume the voice was asked to play at, which a fade returns to.</summary>
	public float Volume { get; set; } = 1f;

	/// <summary>Current gain, moved towards <see cref="TargetGain"/> by <see cref="GainStep"/>.</summary>
	public float Gain { get; set; } = 1f;

	/// <summary>Gain the running fade is heading for.</summary>
	public float TargetGain { get; set; } = 1f;

	/// <summary>Gain change per sample, always positive. <c>0</c> means no fade is running.</summary>
	public float GainStep { get; set; }

	/// <summary>What to do once the running fade reaches <see cref="TargetGain"/>.</summary>
	public VoiceFadeAction FadeAction { get; set; }

	/// <summary>Whether the position stops advancing. This is how ducking is modelled.</summary>
	public bool Paused { get; set; }

	/// <summary>Whether the voice has ended and is waiting to be removed.</summary>
	public bool Finished { get; set; }

	/// <summary>
	/// How many output samples to skip before this voice is heard.
	/// </summary>
	/// <remarks>
	/// A tail left behind by a loop turnaround is added part way through a buffer and has to join at
	/// the sample the turnaround happened on, not at the start of the buffer.
	/// </remarks>
	public int StartDelay { get; set; }

	/// <summary>
	/// Starts a fade towards a gain.
	/// </summary>
	/// <param name="target">Gain to reach.</param>
	/// <param name="samples">How many samples the fade takes; <c>0</c> or less jumps straight there.</param>
	/// <param name="action">What to do once the fade has arrived.</param>
	public void FadeTo(float target, int samples, VoiceFadeAction action)
	{
		TargetGain = target;
		FadeAction = action;

		if (samples <= 0)
		{
			Gain = target;
			GainStep = 0f;
			ApplyFadeAction();
			return;
		}

		float distance = Math.Abs(target - Gain);
		GainStep = distance <= 0f ? 0f : distance / samples;

		if (GainStep <= 0f)
		{
			Gain = target;
			ApplyFadeAction();
		}
	}

	/// <summary>
	/// Moves the gain one sample further along a running fade.
	/// </summary>
	public void AdvanceGain()
	{
		if (GainStep <= 0f) return;

		if (Gain < TargetGain)
		{
			Gain += GainStep;
			if (Gain >= TargetGain) ArriveAtTarget();
			return;
		}

		Gain -= GainStep;
		if (Gain <= TargetGain) ArriveAtTarget();
	}

	private void ArriveAtTarget()
	{
		Gain = TargetGain;
		GainStep = 0f;
		ApplyFadeAction();
	}

	private void ApplyFadeAction()
	{
		switch (FadeAction)
		{
			case VoiceFadeAction.Pause:
				Paused = true;
				Handle?.SetPaused(paused: true);
				break;

			case VoiceFadeAction.Stop:
				Finished = true;
				break;

			default:
				break;
		}

		FadeAction = VoiceFadeAction.None;
	}
}
