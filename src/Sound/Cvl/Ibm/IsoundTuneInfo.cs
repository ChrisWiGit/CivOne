using System.Collections.Generic;

namespace CivOne.Sound.Cvl.Ibm;

/// <summary>Result of analyzing a single tune handler.</summary>
internal sealed class IsoundTuneInfo
{
    public int TuneId { get; init; }
    public TuneScoreKind Kind { get; init; }
    public int HandlerOffset { get; init; }

    /// <summary>Data-segment offset of the sequence, or -1.</summary>
    public int DataOffset { get; init; } = -1;

    /// <summary>Code offset of the player routine the handler jumps to, or -1.</summary>
    public int PlayerOffset { get; init; } = -1;

    /// <summary>
    /// The interchangeable arrangements of the tune, in the order the driver's table holds them.
    /// One entry for an ordinary tune, four for one the driver varies between playbacks.
    /// </summary>
    public List<List<TuneStep>> Arrangements { get; init; } = [];

    /// <summary>The steps of the first arrangement, which is what an ordinary tune has.</summary>
    public List<TuneStep> Steps => Arrangements.Count == 0 ? [] : Arrangements[0];

    /// <summary>Reason why the handler could not be interpreted as a sequence, if any.</summary>
    public string? Diagnostic { get; init; }
}
