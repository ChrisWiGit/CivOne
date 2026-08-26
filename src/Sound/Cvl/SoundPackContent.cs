using System;
using System.Collections.Generic;

namespace CivOne.Sound.Cvl;

#nullable enable

/// <summary>
/// One tune of a converted pack: everything the index needs, plus how to write the tune itself.
/// </summary>
/// <remarks>
/// The metadata is device-neutral so <see cref="CvlSoundConversionService"/> can build the index
/// without knowing what a PC speaker step or an FM voice looks like. Only
/// <see cref="WriteTo"/> knows the actual file format.
/// </remarks>
internal sealed class SoundPackTune
{
    /// <summary>Gets the numeric tune id, as used by <c>PlaySound</c>.</summary>
    public required int TuneId { get; init; }

    /// <summary>Gets the display title of the tune.</summary>
    public required string Title { get; init; }

    /// <summary>Gets how the driver realizes this tune.</summary>
    public TuneScoreKind Kind { get; init; }

    /// <summary>Gets whether the tune is meant to repeat instead of ending.</summary>
    public bool EndlessLoop { get; init; }

    /// <summary>Gets the number of notes or events, for the index only.</summary>
    public int StepCount { get; init; }

    /// <summary>Gets the total length in sequencer ticks, for the index only.</summary>
    public int TotalTicks { get; init; }

    /// <summary>
    /// Gets how many interchangeable arrangements the tune offers. One for everything except the
    /// AdLib leader themes, which the original picks between at random.
    /// </summary>
    public int ArrangementCount { get; init; } = 1;

    /// <summary>
    /// Gets the callback that writes the tune to a path, or <c>null</c> when the driver plays
    /// nothing for this tune and no file is needed.
    /// </summary>
    public Action<string>? WriteTo { get; init; }
}

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
