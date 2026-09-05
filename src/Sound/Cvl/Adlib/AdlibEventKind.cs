namespace CivOne.Sound.Cvl.Adlib;

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
