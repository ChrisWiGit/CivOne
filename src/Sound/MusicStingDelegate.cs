using System;
using CivOne.Sound.Engine;

namespace CivOne.Sound;

/// <summary>
/// Plays a short sound over a running piece of music the way the original driver did: the music is
/// frozen for the length of the sting and continues on the very note it stopped on.
/// </summary>
/// <remarks>
/// Nothing is heard of the music while the sting plays. The original froze all eight theme voices
/// and played the sting on the ninth, so this is a sequence, not a layering.
/// <para>
/// The short fades are not in the original. They only keep a hard cut in the middle of a waveform
/// from being audible as a click.
/// </para>
/// </remarks>
/// <param name="startSting">
/// Starts the sting and returns its handle, or <c>null</c> when nothing could be started.
/// Left out, the sting goes to <see cref="SoundSystemProvider"/>; a test passes its own so the
/// ducking can be examined without an audio device.
/// </param>
internal sealed class MusicStingDelegate(Func<SoundRequest, ISoundHandle?>? startSting = null)
{
	/// <summary>How long the music takes to duck out and to come back.</summary>
	private static readonly TimeSpan Blend = TimeSpan.FromMilliseconds(60);

	private readonly Func<SoundRequest, ISoundHandle?>? _startSting = startSting;

	private Func<SoundRequest, ISoundHandle?> StartSting => _startSting ?? SoundSystemProvider.Play;

	/// <summary>
	/// Plays a sting over the music.
	/// </summary>
	/// <param name="music">The running music, or <c>null</c> when nothing is playing.</param>
	/// <param name="stingFile">Path of the wave file of the sting.</param>
	/// <returns>
	/// The handle of the sting, or <c>null</c> when it could not be started - in which case the
	/// music is left running untouched.
	/// </returns>
	public ISoundHandle? Play(ISoundHandle? music, string stingFile)
	{
		ISoundHandle? sting = StartSting(new SoundRequest(stingFile, SoundBus.Effect));
		if (sting == null) return null;

		if (music == null || !music.IsPlaying) return sting;

		music.Pause(Blend);

		// The music comes back on its own. Nothing has to watch the sting from the outside, and a
		// sting that is cut short brings the music back just the same.
		sting.Ended += () => music.Resume(Blend);

		return sting;
	}
}
