using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl.Adlib;

#nullable enable

/// <summary>
/// One instruction of an ASOUND voice stream.
/// <para>
/// Bytes <c>0x00</c>..<c>0xF2</c> in the original stream are two-byte note records, everything from
/// <c>0xF3</c> upwards is a control opcode. The names here describe what the driver does with the
/// operand, not the raw opcode number.
/// </para>
/// </summary>
internal enum AdlibEventKind
{
    /// <summary>
    /// Note record: <see cref="AdlibEvent.Note"/> and <see cref="AdlibEvent.Duration"/>.
    /// A note of <c>0</c> is a rest, a duration of <c>0</c> ends the voice.
    /// </summary>
    Note,

    /// <summary>Opcode <c>0xFD</c>: rewind to the start of the stream and clear all modifiers.</summary>
    Restart,

    /// <summary>
    /// Opcode <c>0xFF</c>: outer repeat. <see cref="AdlibEvent.Value"/> is the repeat count,
    /// <c>0</c> leaves the block.
    /// </summary>
    LoopOuter,

    /// <summary>Opcode <c>0xFE</c>: inner repeat, nested inside <see cref="LoopOuter"/>.</summary>
    LoopInner,

    /// <summary>Opcode <c>0xFC</c>: select an instrument from the bank.</summary>
    SetInstrument,

    /// <summary>
    /// Opcode <c>0xFB</c>: release lead time. The note is keyed off this many ticks before the
    /// record's duration has elapsed.
    /// </summary>
    SetGate,

    /// <summary>
    /// Opcode <c>0xFA</c>: signed F-number delta added to the running pitch on every tick
    /// (portamento). <c>0</c> switches the slide off.
    /// </summary>
    SetPitchSlide,

    /// <summary>Opcode <c>0xF9</c>: channel volume. The driver stores <c>operand &gt;&gt; 1</c>.</summary>
    SetVolume,

    /// <summary>
    /// Opcode <c>0xF8</c>: volume envelope. <see cref="AdlibEvent.Value"/> is the tick period,
    /// <see cref="AdlibEvent.Delta"/> the signed step added to the volume offset.
    /// </summary>
    VolumeEnvelope,

    /// <summary>Opcode <c>0xF7</c>: signed detune in F-number units.</summary>
    SetDetune,

    /// <summary>
    /// Opcode <c>0xF6</c>: pick one of <see cref="AdlibEvent.Choices"/> at random and patch it into
    /// a later byte of the stream, addressed by <see cref="AdlibEvent.TargetEventIndex"/> and
    /// <see cref="AdlibEvent.TargetField"/>.
    /// </summary>
    RandomVariant,

    /// <summary>Opcode <c>0xF5</c>: volume offset added on top of <see cref="SetVolume"/>.</summary>
    SetVolumeOffset,

    /// <summary>
    /// Opcode <c>0xF4</c>: stereo position, <c>0x40</c> is centre. The original driver only applies
    /// this on an OPL3 card, so it has no effect on the OPL2 renderer.
    /// </summary>
    SetPan,

    /// <summary>
    /// Opcode <c>0xF3</c>: pan envelope. <see cref="AdlibEvent.Value"/> is the tick period,
    /// <see cref="AdlibEvent.Delta"/> the signed step. OPL3 only, like <see cref="SetPan"/>.
    /// </summary>
    PanEnvelope
}

/// <summary>
/// The byte of a target event that a <see cref="AdlibEventKind.RandomVariant"/> event overwrites.
/// </summary>
internal enum AdlibEventField
{
    /// <summary>The random byte does not land on a field we can address.</summary>
    None,

    /// <summary>The note number of a <see cref="AdlibEventKind.Note"/> event.</summary>
    Note,

    /// <summary>The duration of a <see cref="AdlibEventKind.Note"/> event.</summary>
    Duration,

    /// <summary>The first operand of a control opcode, i.e. <see cref="AdlibEvent.Value"/>.</summary>
    Value,

    /// <summary>The second operand of a control opcode, i.e. <see cref="AdlibEvent.Delta"/>.</summary>
    Delta
}

/// <summary>
/// A decoded instruction of a voice stream.
/// </summary>
/// <remarks>
/// Deliberately driver-independent: the player works from this data alone and never reads the CVL
/// again. Unused fields stay at their default so they disappear from the JSON.
/// </remarks>
internal sealed class AdlibEvent
{
    /// <summary>
    /// Gets or sets what this event does.
    /// </summary>
    public AdlibEventKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the note number of a <see cref="AdlibEventKind.Note"/> event, <c>0</c> for a rest.
    /// </summary>
    public int Note { get; set; }

    /// <summary>
    /// Gets or sets the length of a <see cref="AdlibEventKind.Note"/> event in sequencer ticks.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Gets or sets the first operand of a control opcode.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the second operand of a control opcode, signed where the driver treats it as such.
    /// </summary>
    public int Delta { get; set; }

    /// <summary>
    /// Gets or sets the candidate bytes of a <see cref="AdlibEventKind.RandomVariant"/> event,
    /// <c>null</c> for every other kind.
    /// </summary>
    public List<int>? Choices { get; set; }

    /// <summary>
    /// Gets or sets the index of the event that a <see cref="AdlibEventKind.RandomVariant"/> patches,
    /// or <c>null</c> when there is nothing to patch or the target could not be resolved.
    /// </summary>
    public int? TargetEventIndex { get; set; }

    /// <summary>
    /// Gets or sets which byte of the target event is overwritten.
    /// </summary>
    public AdlibEventField TargetField { get; set; }

    /// <summary>
    /// Gets or sets the byte offset of this event inside the original voice stream.
    /// Kept for traceability and to resolve random-variant targets.
    /// </summary>
    public int SourceOffset { get; set; }

    /// <summary>
    /// Gets whether this event is a rest, i.e. a note record without a pitch.
    /// </summary>
    [JsonIgnore]
    public bool IsRest => Kind == AdlibEventKind.Note && Note == 0;
}
