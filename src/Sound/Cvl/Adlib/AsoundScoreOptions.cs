namespace CivOne.Sound.Cvl.Adlib;

/// <summary>
/// Options for turning ASOUND.CVL into a sound pack.
/// </summary>
internal sealed class AsoundScoreOptions
{
    /// <summary>
    /// Gets whether tunes without a sequence are skipped instead of being listed as
    /// <see cref="TuneScoreKind.Unsupported"/>. Those handlers are control functions such as stop
    /// or a status query.
    /// </summary>
    public bool SkipUnsupported { get; init; } = true;
}
