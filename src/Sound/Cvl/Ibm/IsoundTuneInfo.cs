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

    public List<TuneStep> Steps { get; init; } = [];

    /// <summary>Reason why the handler could not be interpreted as a sequence, if any.</summary>
    public string? Diagnostic { get; init; }
}
