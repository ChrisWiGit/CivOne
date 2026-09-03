using System;

namespace CivOne.Sound.Cvl;

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
    /// <summary>Gets the name <c>PlaySound</c> plays this tune by, from <see cref="SoundNames"/>.</summary>
    public required string Name { get; init; }

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
