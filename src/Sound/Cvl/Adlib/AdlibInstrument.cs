using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl.Adlib;



/// <summary>
/// One FM operator of an instrument.
/// </summary>
/// <remarks>
/// The driver stores every OPL field in its own byte rather than pre-packed registers, so the
/// values here are the plain field values. See <see cref="AdlibInstrument"/> for how they map onto
/// the chip.
/// </remarks>
internal sealed class AdlibOperator
{
    /// <summary>Gets or sets the attack rate, 0..15 (OPL register <c>0x60</c>, bits 7-4).</summary>
    public int AttackRate { get; set; }

    /// <summary>Gets or sets the decay rate, 0..15 (OPL register <c>0x60</c>, bits 3-0).</summary>
    public int DecayRate { get; set; }

    /// <summary>Gets or sets the sustain level, 0..15 (OPL register <c>0x80</c>, bits 7-4).</summary>
    public int SustainLevel { get; set; }

    /// <summary>Gets or sets the release rate, 0..15 (OPL register <c>0x80</c>, bits 3-0).</summary>
    public int ReleaseRate { get; set; }

    /// <summary>
    /// Gets or sets whether the envelope holds at the sustain level instead of decaying away
    /// (OPL register <c>0x20</c>, bit 5).
    /// </summary>
    public bool Sustaining { get; set; }

    /// <summary>
    /// Gets or sets whether envelope rates scale with the note pitch
    /// (OPL register <c>0x20</c>, bit 4).
    /// </summary>
    public bool KeyScaleRate { get; set; }

    /// <summary>
    /// Gets or sets the output level, 0..63, where 63 is loudest.
    /// The chip wants attenuation, so the renderer writes <c>63 - Level</c>.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the key scale level, 0..3 (OPL register <c>0x40</c>, bits 7-6).
    /// </summary>
    public int KeyScaleLevel { get; set; }

    /// <summary>Gets or sets the waveform, 0..3 (OPL register <c>0xE0</c>).</summary>
    public int Waveform { get; set; }

    /// <summary>
    /// Gets or sets the frequency multiplier, 0..15 (OPL register <c>0x20</c>, bits 3-0).
    /// </summary>
    public int FrequencyMultiplier { get; set; }

    /// <summary>
    /// Gets or sets the modulation feedback, 0..7 (OPL register <c>0xC0</c>, bits 3-1).
    /// The chip keeps this per channel; both operators of an instrument carry the same value.
    /// </summary>
    public int Feedback { get; set; }

    /// <summary>Gets or sets whether amplitude modulation is on (OPL register <c>0x20</c>, bit 7).</summary>
    public bool Tremolo { get; set; }

    /// <summary>Gets or sets whether pitch vibrato is on (OPL register <c>0x20</c>, bit 6).</summary>
    public bool Vibrato { get; set; }

    /// <summary>
    /// Gets or sets whether the two operators are chained as modulator and carrier (frequency
    /// modulation) instead of being added together (OPL register <c>0xC0</c>, bit 0, inverted).
    /// </summary>
    public bool FrequencyModulation { get; set; }
}

/// <summary>
/// One entry of the ASOUND instrument bank: two FM operators plus the parameters of the driver's
/// own noise generator.
/// </summary>
/// <remarks>
/// In the CVL each entry is 44 bytes: 22 per operator, of which the first 14 are the OPL fields
/// above. The remaining 8 bytes of the <em>first</em> operator block hold <see cref="NoiseDuration"/>,
/// <see cref="NoiseMask"/>, <see cref="NoiseBase"/> and <see cref="NoiseStep"/>; the same bytes of
/// the second block are unused.
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
