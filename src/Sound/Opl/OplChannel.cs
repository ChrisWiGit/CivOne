namespace CivOne.Sound.Opl;

#nullable enable

/// <summary>
/// One OPL channel: a pair of operators, a pitch and how the two are wired together.
/// </summary>
internal sealed class OplChannel
{
    /// <summary>Gets the operator that modulates, or that is simply added in additive mode.</summary>
    public OplOperator Modulator { get; } = new();

    /// <summary>Gets the operator that is heard.</summary>
    public OplOperator Carrier { get; } = new();

    /// <summary>Gets or sets the 10-bit F-number.</summary>
    public int FrequencyNumber { get; private set; }

    /// <summary>Gets or sets the octave, 0..7.</summary>
    public int Block { get; private set; }

    /// <summary>Gets whether the key is currently down.</summary>
    public bool KeyOn { get; private set; }

    /// <summary>Gets or sets how strongly the modulator feeds back into itself, 0..7.</summary>
    public int FeedbackLevel { get; set; }

    /// <summary>
    /// Gets or sets whether the two operators are added together instead of the first modulating
    /// the second.
    /// </summary>
    public bool Additive { get; set; }

    /// <summary>Gets whether either operator still produces sound.</summary>
    public bool IsActive => Carrier.IsActive || Modulator.IsActive;

    /// <summary>
    /// Returns the channel to its power-on state.
    /// </summary>
    public void Reset()
    {
        Modulator.Reset();
        Carrier.Reset();
        FrequencyNumber = 0;
        Block = 0;
        KeyOn = false;
        FeedbackLevel = 0;
        Additive = false;
    }

    /// <summary>
    /// Sets the pitch and updates both operators' key scaling.
    /// </summary>
    /// <param name="frequencyNumber">The 10-bit F-number.</param>
    /// <param name="block">The octave, 0..7.</param>
    public void SetPitch(int frequencyNumber, int block)
    {
        FrequencyNumber = frequencyNumber & 0x3FF;
        Block = block & 7;
        RefreshKeyScaling();
    }

    /// <summary>
    /// Recomputes the pitch-dependent attenuation of both operators. Needed after a change to
    /// either operator's key scale level setting.
    /// </summary>
    public void RefreshKeyScaling()
    {
        Modulator.UpdateKeyScaleLevel(FrequencyNumber, Block);
        Carrier.UpdateKeyScaleLevel(FrequencyNumber, Block);
    }

    /// <summary>
    /// Presses or releases the key.
    /// </summary>
    /// <param name="down">Whether the key is down.</param>
    public void SetKey(bool down)
    {
        if (down == KeyOn) return;
        KeyOn = down;

        if (down)
        {
            Modulator.KeyOn();
            Carrier.KeyOn();
            return;
        }

        Modulator.KeyOff();
        Carrier.KeyOff();
    }

    /// <summary>
    /// Gets the rate offset the envelopes derive from the current pitch.
    /// </summary>
    /// <param name="noteSelect">
    /// Whether the chip splits the keyboard on the second-highest F-number bit instead of the
    /// highest one.
    /// </param>
    /// <returns>The key scale number, 0..15.</returns>
    public int KeyScaleNumber(bool noteSelect)
        => (Block << 1) | ((FrequencyNumber >> (noteSelect ? 8 : 9)) & 1);

    /// <summary>
    /// Advances both operators' envelopes by one sample.
    /// </summary>
    /// <param name="keyScaleNumber">Pitch-derived rate offset, 0..15.</param>
    /// <param name="counter">The chip's envelope counter.</param>
    public void AdvanceEnvelopes(int keyScaleNumber, long counter)
    {
        Modulator.AdvanceEnvelope(keyScaleNumber, counter);
        Carrier.AdvanceEnvelope(keyScaleNumber, counter);
    }

    /// <summary>
    /// Renders one sample of this channel.
    /// </summary>
    /// <param name="vibrato">Pitch offset from the vibrato LFO, in F-number units.</param>
    /// <param name="tremolo">Level offset from the tremolo LFO, in units of 0.1875 dB.</param>
    /// <returns>The sample, roughly between -8168 and 8168 in additive mode.</returns>
    public int Render(int vibrato, int tremolo)
    {
        int modulatorPitch = Pitch(Modulator.Vibrato ? vibrato : 0);
        int carrierPitch = Pitch(Carrier.Vibrato ? vibrato : 0);

        int feedback = Modulator.Feedback(FeedbackLevel);
        int modulated = Modulator.Advance(modulatorPitch, Block, feedback, tremolo);

        // In FM mode the modulator bends the carrier's phase; the operator output is twice the
        // resolution of the phase index, so it is halved on the way across.
        int modulation = Additive ? 0 : modulated >> 1;
        int carried = Carrier.Advance(carrierPitch, Block, modulation, tremolo);

        return Additive ? modulated + carried : carried;
    }

    private int Pitch(int vibrato)
    {
        int pitch = FrequencyNumber + vibrato;
        if (pitch < 0) return 0;

        return pitch > 0x3FF ? 0x3FF : pitch;
    }
}
