using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl.Adlib;

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
