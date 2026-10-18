using System;
using System.Collections.Generic;

namespace CivOne.Sound.Cvl;

/// <summary>
/// Everything one converted CVL module contributes to a sound pack.
/// </summary>
internal sealed class SoundPackContent
{
    /// <summary>Gets the source driver, e.g. <c>"ISOUND"</c>.</summary>
    public required string Driver { get; init; }

    /// <summary>Gets the target device, e.g. <c>"pcSpeaker"</c>.</summary>
    public required string Device { get; init; }

    /// <summary>Gets the signature of the source file, documenting which build it came from.</summary>
    public string? SourceSignature { get; init; }

    /// <summary>Gets the base tick rate of the CIVPLAY scheduler in Hz.</summary>
    public int FastTickHz { get; init; } = 300;

    /// <summary>Gets how many base ticks make one sequencer tick.</summary>
    public int WorkerTickDivider { get; init; } = 5;

    /// <summary>
    /// Gets the clock frequency of the PC's timer chip in Hz, or <c>null</c> when the device does
    /// not derive its pitch from it.
    /// </summary>
    public int? PitClockHz { get; init; }

    /// <summary>Gets the tunes of the pack.</summary>
    public List<SoundPackTune> Tunes { get; init; } = [];

    /// <summary>
    /// Gets files written once per pack rather than once per tune, keyed by file name.
    /// The AdLib pack uses this for its shared instrument bank.
    /// </summary>
    public Dictionary<string, Action<string>> SharedFiles { get; init; } = [];
}
