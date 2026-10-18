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
