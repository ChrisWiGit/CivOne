using System.Collections.Generic;

namespace CivOne.Sound.Cvl.Adlib;

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
