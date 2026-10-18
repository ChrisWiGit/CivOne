using System.Collections.Generic;

namespace CivOne.Sound.Cvl.Adlib;



/// <summary>
/// Addresses of ASOUND.CVL, all derived from the module itself. Nothing here is hardcoded, so the
/// parser is not tied to one particular build.
/// </summary>
internal sealed class AsoundLayout
{
    /// <summary>Code offset of the tune dispatch table.</summary>
    public required int DispatchTable { get; init; }

    /// <summary>Highest tune number the driver accepts.</summary>
    public required int MaxTuneId { get; init; }

    /// <summary>
    /// Code offsets of the per-voice start thunks, ordered by OPL channel (index 0 = channel 0).
    /// </summary>
    public required IReadOnlyList<int> VoiceThunks { get; init; }

    /// <summary>Data offset of the instrument bank.</summary>
    public required int InstrumentBank { get; init; }

    /// <summary>Size of one instrument in bytes (44 in the 12-03-91 build).</summary>
    public required int InstrumentStride { get; init; }

    /// <summary>Size of one operator block inside an instrument (22 in the 12-03-91 build).</summary>
    public required int OperatorStride { get; init; }

    /// <summary>Data offset of the table mapping an OPL channel to its two operator indices.</summary>
    public required int ChannelOperatorTable { get; init; }

    /// <summary>Data offset of the table mapping an operator index to its OPL register offset.</summary>
    public required int OperatorRegisterTable { get; init; }

    /// <summary>Data offset of the twelve F-numbers, one per semitone.</summary>
    public required int FrequencyNumberTable { get; init; }

    /// <summary>Data offset of the flag that selects the deeper tremolo depth.</summary>
    public required int DeepTremoloFlag { get; init; }

    /// <summary>Data offset of the flag that selects the deeper vibrato depth.</summary>
    public required int DeepVibratoFlag { get; init; }

    /// <summary>Data offset of the flag that moves the chip's keyboard split point.</summary>
    public required int NoteSelectFlag { get; init; }

    /// <summary>Stereo position a voice starts with; <c>0x40</c> is centre.</summary>
    public required int DefaultPan { get; init; }

    /// <summary>Number of voices, i.e. the number of OPL channels the driver drives.</summary>
    public int VoiceCount => VoiceThunks.Count;

    /// <summary>Number of tune slots in the dispatch table.</summary>
    public int TuneCount => MaxTuneId + 1;
}
