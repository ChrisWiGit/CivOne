namespace CivOne.Sound.Cvl.Ibm;

/// <summary>
/// Code addresses of ISOUND.CVL derived from the module itself. None of this is
/// hardcoded, so the parser is not tied to a single build.
/// </summary>
internal sealed class IsoundLayout
{
    public required int DispatchTable { get; init; }
    public required int MaxTuneId { get; init; }
    public required int MusicPlayer { get; init; }
    public required int EffectPlayer { get; init; }
    public required int EffectParamTable { get; init; }

    /// <summary>Timbre code for which the driver plays without an effect (0x7E in the 11-14-91 build).</summary>
    public required int PlainTimbreCode { get; init; }

    /// <summary>First code that points into the effect table (0x65 in the 11-14-91 build).</summary>
    public required int FirstTimbreCode { get; init; }

    public int TuneCount => MaxTuneId + 1;
}
