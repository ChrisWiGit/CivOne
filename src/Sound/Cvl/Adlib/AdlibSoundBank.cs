using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl.Adlib;

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
