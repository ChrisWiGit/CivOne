using System.Collections.Generic;

namespace CivOne.Sound.Cvl;

/// <summary>
/// One playable version of a PC speaker tune.
/// </summary>
/// <remarks>
/// A tune usually has a single arrangement. The driver holds a few tunes four times over and picks
/// between them, which is why an arrangement is its own object rather than one list of steps on the
/// tune.
/// </remarks>
internal sealed class TuneArrangement
{
    /// <summary>
    /// Gets or sets the ordered list of tones and rests that make up this arrangement.
    /// </summary>
    public List<TuneStep> Steps { get; set; } = [];
}
