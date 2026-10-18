namespace CivOne.Sound.Cvl.Adlib;

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
