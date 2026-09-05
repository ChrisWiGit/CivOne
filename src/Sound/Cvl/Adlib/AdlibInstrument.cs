using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl.Adlib;

/// <summary>
/// One entry of the ASOUND instrument bank: two FM operators plus the parameters of the driver's
/// own noise generator.
/// </summary>
/// <remarks>
/// In the CVL each entry is 44 bytes: 22 per operator, of which the first 14 are the OPL fields
/// of <see cref="AdlibOperator"/>. The remaining 8 bytes of the <em>first</em> operator block hold
/// <see cref="NoiseDuration"/>, <see cref="NoiseMask"/>, <see cref="NoiseBase"/> and
/// <see cref="NoiseStep"/>; the same bytes of the second block are unused.
/// </remarks>
internal sealed class AdlibInstrument
{
    /// <summary>Gets or sets the index of this instrument in the bank.</summary>
    public int Index { get; set; }

    /// <summary>Gets or sets the modulator, the first operator of the channel's operator pair.</summary>
    public required AdlibOperator Modulator { get; set; }

    /// <summary>Gets or sets the carrier, the second operator of the channel's operator pair.</summary>
    public required AdlibOperator Carrier { get; set; }

    /// <summary>
    /// Gets or sets how many sequencer ticks the noise generator runs, <c>0</c> for a normal
    /// melodic instrument.
    /// </summary>
    public int NoiseDuration { get; set; }

    /// <summary>
    /// Gets or sets the mask applied to the pseudo-random value, which bounds how far the pitch
    /// jumps around.
    /// </summary>
    public int NoiseMask { get; set; }

    /// <summary>Gets or sets the F-number the noise starts from.</summary>
    public int NoiseBase { get; set; }

    /// <summary>Gets or sets the value added to <see cref="NoiseBase"/> on every sequencer tick.</summary>
    public int NoiseStep { get; set; }

    /// <summary>
    /// Gets whether this instrument drives the driver's noise generator rather than a plain FM note.
    /// </summary>
    [JsonIgnore]
    public bool IsNoise => NoiseDuration > 0;
}
