using System.Collections.Generic;

namespace CivOne.Sound.Cvl;

/// <summary>What converting one CVL module produced, or why it produced nothing.</summary>
internal sealed class CvlConversionResult
{
    public required string SourceFile { get; init; }
    public bool Converted { get; init; }
    public string? PackId { get; init; }
    public string? PackFolder { get; init; }
    public CvlDevice Device { get; init; }
    public int TuneCount { get; init; }
    public int MappedSoundNames { get; init; }
    public IReadOnlyList<string> UnavailableSoundNames { get; init; } = [];
    public required string Message { get; init; }
}
