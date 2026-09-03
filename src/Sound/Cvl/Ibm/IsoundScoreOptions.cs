using System.Collections.Generic;

namespace CivOne.Sound.Cvl.Ibm;

/// <summary>
/// Options for extracting the tunes of ISOUND.CVL.
/// </summary>
internal sealed class IsoundScoreOptions
{
    /// <summary>Which tunes to extract. Default: every tune addressable by the host.</summary>
    public IReadOnlyList<int>? TuneIds { get; init; }

    /// <summary>
    /// Skip tunes without a sequence (control functions such as stop or status query)
    /// instead of writing them out as <see cref="TuneScoreKind.Unsupported"/>.
    /// </summary>
    public bool SkipUnsupported { get; init; } = true;
}
