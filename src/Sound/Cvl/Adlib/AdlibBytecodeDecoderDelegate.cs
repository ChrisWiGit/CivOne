using System;
using System.Collections.Generic;

namespace CivOne.Sound.Cvl.Adlib;

#nullable enable

/// <summary>
/// Turns one raw ASOUND voice stream into a list of <see cref="AdlibEvent"/>.
/// </summary>
/// <remarks>
/// <para>
/// The stream is a flat byte sequence. Values below <see cref="FirstOpcode"/> start a two-byte note
/// record, everything else is a control opcode with a fixed operand count. A note record whose
/// duration is zero ends the voice, so decoding stops there.
/// </para>
/// <para>
/// Loops are kept as events rather than unrolled: the driver treats <c>0xFF</c> and <c>0xFE</c> as
/// backward repeats that remember where the current block started, and the player reproduces that
/// pointer arithmetic on event indices.
/// </para>
/// </remarks>
internal sealed class AdlibBytecodeDecoderDelegate
{
    /// <summary>Lowest byte value that is a control opcode instead of a note.</summary>
    public const int FirstOpcode = 0xF3;

    private const int MaxEventsPerVoice = 8192;
    private const int MaxChoices = 64;

    private readonly Dictionary<int, EventField> _fields = [];

    /// <summary>
    /// Decodes the voice stream starting at <paramref name="dataOffset"/>.
    /// </summary>
    /// <param name="image">The loaded CVL module.</param>
    /// <param name="dataOffset">Data-segment offset of the first byte of the stream.</param>
    /// <returns>The decoded events, empty when the offset is outside the file.</returns>
    public List<AdlibEvent> Decode(CvlImage image, int dataOffset)
    {
        ArgumentNullException.ThrowIfNull(image);

        _fields.Clear();
        var events = new List<AdlibEvent>();
        int offset = dataOffset;

        while (events.Count < MaxEventsPerVoice)
        {
            if (!image.TryDataByte(offset, out byte opcode)) break;

            if (opcode < FirstOpcode)
            {
                if (!image.TryDataByte(offset + 1, out byte duration)) break;

                Add(events, new AdlibEvent
                {
                    Kind = AdlibEventKind.Note,
                    Note = opcode,
                    Duration = duration,
                    SourceOffset = offset
                }, offset, AdlibEventField.Note, AdlibEventField.Duration);

                offset += 2;

                // A duration of zero switches the voice off, so nothing after it is ever read.
                if (duration == 0) break;
                continue;
            }

            if (opcode == 0xF6)
            {
                if (!TryDecodeRandomVariant(image, events, ref offset)) break;
                continue;
            }

            if (!TryDecodeControl(image, events, opcode, ref offset)) break;

            // Restart rewinds the pointer to the start of the stream, so nothing behind it is ever
            // read. The next voice's data usually begins right there.
            if (opcode == 0xFD) break;
        }

        ResolveRandomTargets(events);
        return events;
    }

    private bool TryDecodeControl(CvlImage image, List<AdlibEvent> events, byte opcode, ref int offset)
    {
        AdlibEventKind kind = KindOf(opcode);
        int operandCount = OperandCountOf(opcode);
        int start = offset;

        var decoded = new AdlibEvent { Kind = kind, SourceOffset = start };

        if (operandCount >= 1)
        {
            if (!image.TryDataByte(start + 1, out byte first)) return false;
            decoded.Value = first;
        }

        if (operandCount >= 2)
        {
            if (!image.TryDataByte(start + 2, out byte second)) return false;
            decoded.Delta = second;
        }

        Add(events, decoded, start + 1, AdlibEventField.Value, AdlibEventField.Delta, operandCount);
        offset = start + 1 + operandCount;
        return true;
    }

    private bool TryDecodeRandomVariant(CvlImage image, List<AdlibEvent> events, ref int offset)
    {
        int start = offset;
        if (!image.TryDataByte(start + 1, out byte count)) return false;
        if (count is 0 or > MaxChoices) return false;

        var choices = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            if (!image.TryDataByte(start + 2 + i, out byte choice)) return false;
            choices.Add(choice);
        }

        if (!image.TryDataByte(start + 2 + count, out byte target)) return false;

        // The driver writes the chosen byte relative to the first byte after this opcode.
        int next = start + count + 3;

        events.Add(new AdlibEvent
        {
            Kind = AdlibEventKind.RandomVariant,
            Value = count,
            Delta = (sbyte)target,
            Choices = choices,
            SourceOffset = start
        });

        offset = next;
        return true;
    }

    private void Add(List<AdlibEvent> events, AdlibEvent decoded, int firstByte,
        AdlibEventField firstField, AdlibEventField secondField, int byteCount = 2)
    {
        int index = events.Count;
        events.Add(decoded);

        if (byteCount >= 1) _fields[firstByte] = new EventField(index, firstField);
        if (byteCount >= 2) _fields[firstByte + 1] = new EventField(index, secondField);
    }

    /// <summary>
    /// Binds every random-variant event to the event whose byte it overwrites. This can only run
    /// once the whole stream is decoded, because the target usually lies further ahead.
    /// </summary>
    private void ResolveRandomTargets(List<AdlibEvent> events)
    {
        foreach (AdlibEvent decoded in events)
        {
            if (decoded.Kind != AdlibEventKind.RandomVariant) continue;

            int target = decoded.SourceOffset + decoded.Value + 3 + decoded.Delta;
            if (!_fields.TryGetValue(target, out EventField field)) continue;

            decoded.TargetEventIndex = field.Index;
            decoded.TargetField = field.Field;
        }
    }

    private static AdlibEventKind KindOf(byte opcode) => opcode switch
    {
        0xFF => AdlibEventKind.LoopOuter,
        0xFE => AdlibEventKind.LoopInner,
        0xFD => AdlibEventKind.Restart,
        0xFC => AdlibEventKind.SetInstrument,
        0xFB => AdlibEventKind.SetGate,
        0xFA => AdlibEventKind.SetPitchSlide,
        0xF9 => AdlibEventKind.SetVolume,
        0xF8 => AdlibEventKind.VolumeEnvelope,
        0xF7 => AdlibEventKind.SetDetune,
        0xF5 => AdlibEventKind.SetVolumeOffset,
        0xF4 => AdlibEventKind.SetPan,
        _ => AdlibEventKind.PanEnvelope
    };

    private static int OperandCountOf(byte opcode) => opcode switch
    {
        0xFD => 0,
        0xF8 or 0xF3 => 2,
        _ => 1
    };

    private readonly record struct EventField(int Index, AdlibEventField Field);
}
