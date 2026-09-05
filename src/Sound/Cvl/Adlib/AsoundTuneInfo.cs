using System.Collections.Generic;

namespace CivOne.Sound.Cvl.Adlib;

/// <summary>Result of analyzing a single tune handler.</summary>
internal sealed class AsoundTuneInfo
{
    /// <summary>Gets the tune number this result belongs to.</summary>
    public int TuneId { get; init; }

    /// <summary>Gets how the driver realizes the tune.</summary>
    public TuneScoreKind Kind { get; init; }

    /// <summary>Gets the code offset of the handler, or <c>-1</c>.</summary>
    public int HandlerOffset { get; init; }

    /// <summary>
    /// Gets the arrangements of the tune. Most tunes have exactly one; the leader themes offer four
    /// and the driver picks between them with the second <c>PlayTune</c> argument.
    /// </summary>
    public List<List<AsoundVoiceRef>> Arrangements { get; init; } = [];

    /// <summary>
    /// Gets a note about anything in the handler that could not be interpreted, or <c>null</c>.
    /// </summary>
    public string? Diagnostic { get; init; }
}
