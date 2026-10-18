namespace CivOne.Sound.Engine;

/// <summary>
/// The contents of a wave file, ready for the mixer.
/// </summary>
/// <param name="Samples">Mono samples, each in the range <c>-1</c> to <c>1</c>.</param>
/// <param name="SampleRate">Rate the samples were recorded at, in Hz.</param>
/// <param name="LoopStart">
/// Sample a loop returns to, when the file carries a loop point, or <c>null</c> when it does not.
/// </param>
/// <param name="LoopEnd">
/// Sample a loop turns around at, when the file carries a loop point, or <c>null</c> when it does
/// not.
/// </param>
internal readonly record struct LoadedWave(float[] Samples, int SampleRate, int? LoopStart, int? LoopEnd);
