namespace CivOne.Sound.Cvl;

/// <summary>
/// One tune of a sound pack, as listed in the pack's <see cref="SoundPackIndex"/>.
/// </summary>
internal sealed class SoundPackIndexEntry
{
    /// <summary>Name <c>PlaySound</c> plays this tune by, from <see cref="SoundNames"/>.</summary>
    public required string Name { get; set; }

    /// <summary>English display title, shown in the sound test. Translated only when displayed.</summary>
    public required string Title { get; set; }

    public TuneScoreKind Kind { get; set; }

    /// <summary>File name within the same folder, or <c>null</c> for deliberately silent tunes.</summary>
    public string? File { get; set; }

    public int StepCount { get; set; }
    public int TotalTicks { get; set; }

    /// <summary>
    /// How many interchangeable arrangements the tune offers. One for everything except the AdLib
    /// leader themes, which the original picks between at random.
    /// </summary>
    public int ArrangementCount { get; set; } = 1;
}
