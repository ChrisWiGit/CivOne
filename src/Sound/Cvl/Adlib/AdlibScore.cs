using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl.Adlib;

#nullable enable

/// <summary>
/// One voice of an arrangement: the OPL channel it uses and the events it plays.
/// </summary>
internal sealed class AdlibVoice
{
    /// <summary>Gets or sets the OPL channel, 0..8.</summary>
    public int Channel { get; set; }

    /// <summary>
    /// Gets or sets the data-segment offset the stream came from, kept only for traceability.
    /// </summary>
    public int SourceOffset { get; set; }

    /// <summary>Gets or sets the decoded events of this voice.</summary>
    public List<AdlibEvent> Events { get; set; } = [];
}

/// <summary>
/// One playable version of a tune.
/// </summary>
/// <remarks>
/// Most tunes have exactly one arrangement. The leader themes ship four, and the original picks
/// between them with the second argument of <c>PlayTune</c>.
/// </remarks>
internal sealed class AdlibArrangement
{
    /// <summary>Gets or sets the voices that play together.</summary>
    public List<AdlibVoice> Voices { get; set; } = [];

    /// <summary>Gets the total number of events across all voices.</summary>
    [JsonIgnore]
    public int EventCount
    {
        get
        {
            int total = 0;
            foreach (AdlibVoice voice in Voices) total += voice.Events.Count;
            return total;
        }
    }
}

/// <summary>
/// A single tune extracted from the AdLib driver, as stored in one <c>*.sound.json</c>.
/// </summary>
internal sealed class AdlibTuneScore
{
    /// <summary>
    /// Gets or sets the schema version of this file. It matches
    /// <see cref="SoundPackIndex.CurrentSchemaVersion"/>.
    /// </summary>
    public int SchemaVersion { get; set; } = SoundPackIndex.CurrentSchemaVersion;

    /// <summary>Gets or sets the numeric tune id, as used by <c>PlaySound</c>.</summary>
    public int TuneId { get; set; }

    /// <summary>Gets or sets the display title of the tune.</summary>
    public required string Title { get; set; }

    /// <summary>Gets or sets how the driver realizes this tune.</summary>
    public TuneScoreKind Kind { get; set; }

    /// <summary>Gets or sets whether the tune repeats instead of ending after its last event.</summary>
    public bool EndlessLoop { get; set; }

    /// <summary>
    /// Gets or sets a note about anything in the handler that could not be reproduced, or <c>null</c>.
    /// </summary>
    public string? Diagnostic { get; set; }

    /// <summary>Gets or sets the interchangeable arrangements of this tune.</summary>
    public List<AdlibArrangement> Arrangements { get; set; } = [];

    /// <summary>Gets the total number of events of the first arrangement.</summary>
    [JsonIgnore]
    public int EventCount => Arrangements.Count == 0 ? 0 : Arrangements[0].EventCount;

    /// <summary>
    /// Gets the length of the longest voice of the first arrangement in sequencer ticks.
    /// Loops are not followed, so this is a lower bound on the real playing time.
    /// </summary>
    [JsonIgnore]
    public int TotalTicks
    {
        get
        {
            if (Arrangements.Count == 0) return 0;

            int longest = 0;
            foreach (AdlibVoice voice in Arrangements[0].Voices)
            {
                int ticks = 0;
                foreach (AdlibEvent decoded in voice.Events) ticks += decoded.Duration;
                if (ticks > longest) longest = ticks;
            }

            return longest;
        }
    }
}

/// <summary>
/// The part of an AdLib pack that all tunes share: the instrument bank and the chip tables the
/// driver was built around.
/// </summary>
/// <remarks>
/// Stored once per pack as <see cref="FileName"/> so the bank is not repeated in every tune file.
/// </remarks>
internal sealed class AdlibSoundBank
{
    /// <summary>File name of the bank inside a pack folder.</summary>
    public const string FileName = "bank.json";

    /// <summary>
    /// Gets or sets the schema version of this file. It matches
    /// <see cref="SoundPackIndex.CurrentSchemaVersion"/>.
    /// </summary>
    public int SchemaVersion { get; set; } = SoundPackIndex.CurrentSchemaVersion;

    /// <summary>Gets or sets the stereo position a voice starts with; <c>0x40</c> is centre.</summary>
    public int DefaultPan { get; set; } = 0x40;

    /// <summary>Gets or sets whether the chip uses its deeper tremolo depth of 4.8 dB.</summary>
    public bool DeepTremolo { get; set; }

    /// <summary>Gets or sets whether the chip uses its deeper vibrato depth of 14 cents.</summary>
    public bool DeepVibrato { get; set; }

    /// <summary>
    /// Gets or sets whether the chip splits the keyboard one F-number bit lower, which changes how
    /// strongly the envelope rates scale with pitch.
    /// </summary>
    public bool NoteSelect { get; set; }

    /// <summary>
    /// Gets or sets the F-number of each semitone of an octave. A note is split into
    /// <c>note / 12</c> as the block and <c>note % 12</c> as the index into this table.
    /// </summary>
    public List<int> FrequencyNumbers { get; set; } = [];

    /// <summary>
    /// Gets or sets the OPL register offset of each channel's modulator, indexed by channel.
    /// </summary>
    public List<int> ModulatorOffsets { get; set; } = [];

    /// <summary>
    /// Gets or sets the OPL register offset of each channel's carrier, indexed by channel.
    /// </summary>
    public List<int> CarrierOffsets { get; set; } = [];

    /// <summary>Gets or sets the instrument bank, in bank order.</summary>
    public List<AdlibInstrument> Instruments { get; set; } = [];

    /// <summary>Gets the number of channels the driver plays on.</summary>
    [JsonIgnore]
    public int ChannelCount => ModulatorOffsets.Count;
}
