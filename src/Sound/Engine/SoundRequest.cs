using System;

namespace CivOne.Sound.Engine;

/// <summary>
/// What to play and how to play it.
/// </summary>
/// <param name="FilePath">Path of the wave file to play.</param>
/// <param name="Bus">Which bus the sound belongs to.</param>
/// <param name="Loop">Whether the sound turns around and keeps playing instead of ending.</param>
/// <param name="Volume">Volume of this sound, from <c>0</c> to <c>1</c>.</param>
/// <param name="FadeIn">How long to fade in from silence.</param>
/// <param name="Replaces">
/// A sound this one takes over from.
/// It fades out over <paramref name="CrossFade"/> while this one fades in, instead of being cut off.
/// </param>
/// <param name="CrossFade">Length of the overlap when <paramref name="Replaces"/> is given.</param>
/// <param name="LoopStartSample">Sample the loop returns to; <c>0</c> is the start of the file.</param>
/// <param name="LoopEndSample">
/// Sample the loop turns around at, or <c>null</c> for the end of the file.
/// For the converted tunes the end of the file is the wrong place: the render runs until every
/// voice of the tune has wrapped once, so its tail already contains the repeated beginning of the
/// shorter voices.
/// </param>
/// <param name="LoopCrossFade">
/// Overlap at the turnaround.
/// The tail past <paramref name="LoopEndSample"/> fades out while the head fades in, instead of
/// cutting. <see cref="TimeSpan.Zero"/> turns around hard.
/// </param>
internal readonly record struct SoundRequest(
	string FilePath,
	SoundBus Bus = SoundBus.Effect,
	SoundLoop Loop = SoundLoop.None,
	float Volume = 1f,
	TimeSpan FadeIn = default,
	ISoundHandle? Replaces = null,
	TimeSpan CrossFade = default,
	int LoopStartSample = 0,
	int? LoopEndSample = null,
	TimeSpan LoopCrossFade = default);
