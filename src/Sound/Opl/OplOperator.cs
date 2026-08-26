namespace CivOne.Sound.Opl;

#nullable enable

/// <summary>The four phases an operator's envelope runs through.</summary>
internal enum OplEnvelopeState
{
    /// <summary>Key is up and the note has faded out; the operator produces silence.</summary>
    Off,

    /// <summary>Rising from silence to full level after a key-on.</summary>
    Attack,

    /// <summary>Falling from full level to the sustain level.</summary>
    Decay,

    /// <summary>Holding at the sustain level while the key is held.</summary>
    Sustain,

    /// <summary>Falling to silence after a key-off.</summary>
    Release
}

/// <summary>
/// One FM operator: a sine oscillator with its own envelope.
/// </summary>
/// <remarks>
/// A channel pairs two of these. Which one is heard and which one only bends the other's phase
/// depends on the channel's connection setting.
/// </remarks>
internal sealed class OplOperator
{
    /// <summary>Silence, as a 9-bit envelope attenuation in units of 0.1875 dB.</summary>
    public const int Silence = 0x1FF;

    /// <summary>Steps of the phase accumulator that make one full cycle.</summary>
    public const int PhaseCycle = 1 << 20;

    /// <summary>Number of waveform positions per cycle.</summary>
    public const int PhaseSteps = 1 << 10;

    /// <summary>
    /// Frequency multipliers, doubled so that the 0.5 of setting 0 stays an integer.
    /// </summary>
    private static readonly int[] _doubledMultipliers =
        [1, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 20, 24, 24, 30, 30];

    /// <summary>Right shift applied to the key scale level, one entry per setting.</summary>
    private static readonly int[] _keyScaleShifts = [31, 1, 2, 0];

    private int _phase;
    private int _envelope = Silence;
    private int _keyScaleLevel;
    private int _output;
    private int _previousOutput;

    /// <summary>Gets or sets whether the tremolo LFO modulates this operator's level.</summary>
    public bool Tremolo { get; set; }

    /// <summary>Gets or sets whether the vibrato LFO modulates this operator's pitch.</summary>
    public bool Vibrato { get; set; }

    /// <summary>
    /// Gets or sets whether the envelope holds at the sustain level instead of decaying past it.
    /// </summary>
    public bool Sustaining { get; set; }

    /// <summary>Gets or sets whether the envelope rates scale with the note's pitch.</summary>
    public bool KeyScaleRate { get; set; }

    /// <summary>Gets or sets the frequency multiplier setting, 0..15.</summary>
    public int Multiplier { get; set; }

    /// <summary>Gets or sets how strongly a high note is attenuated, 0..3.</summary>
    public int KeyScaleLevelSetting { get; set; }

    /// <summary>Gets or sets the base attenuation, 0..63 in units of 0.75 dB.</summary>
    public int TotalLevel { get; set; }

    /// <summary>Gets or sets the attack rate, 0..15.</summary>
    public int AttackRate { get; set; }

    /// <summary>Gets or sets the decay rate, 0..15.</summary>
    public int DecayRate { get; set; }

    /// <summary>Gets or sets the sustain level, 0..15.</summary>
    public int SustainLevel { get; set; }

    /// <summary>Gets or sets the release rate, 0..15.</summary>
    public int ReleaseRate { get; set; }

    /// <summary>Gets or sets the waveform, 0..3.</summary>
    public int Waveform { get; set; }

    /// <summary>Gets the current envelope phase.</summary>
    public OplEnvelopeState State { get; private set; } = OplEnvelopeState.Off;

    /// <summary>Gets whether the operator currently contributes anything audible.</summary>
    public bool IsActive => State != OplEnvelopeState.Off;

    /// <summary>Gets the operator's most recent output, between -4084 and 4084.</summary>
    public int Output => _output;

    /// <summary>
    /// Returns the operator to its power-on state.
    /// </summary>
    public void Reset()
    {
        _phase = 0;
        _envelope = Silence;
        _keyScaleLevel = 0;
        _output = 0;
        _previousOutput = 0;
        State = OplEnvelopeState.Off;

        Tremolo = false;
        Vibrato = false;
        Sustaining = false;
        KeyScaleRate = false;
        Multiplier = 0;
        KeyScaleLevelSetting = 0;
        TotalLevel = 0;
        AttackRate = 0;
        DecayRate = 0;
        SustainLevel = 0;
        ReleaseRate = 0;
        Waveform = 0;
    }

    /// <summary>
    /// Starts a note. The phase restarts from zero, as it does on the real chip.
    /// </summary>
    public void KeyOn()
    {
        _phase = 0;
        State = OplEnvelopeState.Attack;
    }

    /// <summary>
    /// Ends a note and lets the envelope fall to silence.
    /// </summary>
    public void KeyOff()
    {
        if (State != OplEnvelopeState.Off) State = OplEnvelopeState.Release;
    }

    /// <summary>
    /// Recomputes the pitch-dependent attenuation. Call this whenever the channel's F-number or
    /// block changes.
    /// </summary>
    /// <param name="frequencyNumber">The channel's 10-bit F-number.</param>
    /// <param name="block">The channel's octave, 0..7.</param>
    public void UpdateKeyScaleLevel(int frequencyNumber, int block)
    {
        int level = (OplTables.KeyScaleLevel[(frequencyNumber >> 6) & 0x0F] << 2) - ((8 - block) << 5);
        if (level < 0) level = 0;

        _keyScaleLevel = level >> _keyScaleShifts[KeyScaleLevelSetting & 3];
    }

    /// <summary>
    /// Advances the envelope by one sample.
    /// </summary>
    /// <param name="keyScaleNumber">Pitch-derived rate offset, 0..15.</param>
    /// <param name="counter">The chip's envelope counter.</param>
    public void AdvanceEnvelope(int keyScaleNumber, long counter)
    {
        switch (State)
        {
            case OplEnvelopeState.Off:
                return;

            case OplEnvelopeState.Attack:
                AdvanceAttack(EffectiveRate(AttackRate, keyScaleNumber), counter);
                return;

            case OplEnvelopeState.Decay:
                AdvanceDecay(EffectiveRate(DecayRate, keyScaleNumber), counter);
                return;

            case OplEnvelopeState.Sustain:
                if (Sustaining) return;
                AdvanceRelease(EffectiveRate(ReleaseRate, keyScaleNumber), counter);
                return;

            default:
                AdvanceRelease(EffectiveRate(ReleaseRate, keyScaleNumber), counter);
                return;
        }
    }

    /// <summary>
    /// Advances the oscillator by one sample and produces the operator's output.
    /// </summary>
    /// <param name="frequencyNumber">The channel's 10-bit F-number, already including vibrato.</param>
    /// <param name="block">The channel's octave, 0..7.</param>
    /// <param name="modulation">Phase offset from the modulator or from feedback, in waveform steps.</param>
    /// <param name="tremolo">Current tremolo attenuation in units of 0.1875 dB.</param>
    /// <returns>The output sample, between -4084 and 4084.</returns>
    public int Advance(int frequencyNumber, int block, int modulation, int tremolo)
    {
        _phase = (_phase + (((frequencyNumber << block) * _doubledMultipliers[Multiplier & 0x0F]) >> 1))
                 & (PhaseCycle - 1);

        _previousOutput = _output;
        _output = State == OplEnvelopeState.Off
            ? 0
            : Sample((_phase >> 10) + modulation, Attenuation(tremolo));

        return _output;
    }

    /// <summary>
    /// Gets the feedback the modulator sends into its own phase input.
    /// </summary>
    /// <param name="feedback">Feedback setting, 0..7; 0 means no feedback.</param>
    /// <returns>A phase offset in waveform steps.</returns>
    public int Feedback(int feedback)
        => feedback == 0 ? 0 : (_output + _previousOutput) >> (9 - feedback);

    /// <summary>
    /// Adds up everything that makes this operator quieter.
    /// </summary>
    private int Attenuation(int tremolo)
    {
        int total = _envelope + (TotalLevel << 2) + _keyScaleLevel;
        if (Tremolo) total += tremolo;

        return total << 3;
    }

    /// <summary>
    /// Reads the waveform at a phase position and applies the attenuation.
    /// </summary>
    private int Sample(int phase, int attenuation)
    {
        int position = phase & (PhaseSteps - 1);
        int quadrant = position >> 8;
        bool negative = quadrant >= 2;

        switch (Waveform & 3)
        {
            case 1:
                // Half sine: the negative half of the wave is cut away.
                if (negative) return 0;
                negative = false;
                break;

            case 2:
                // Absolute sine: the negative half is mirrored upwards.
                negative = false;
                break;

            case 3:
                // Pulse sine: only the rising quarter of each half survives.
                if ((position & 0x100) != 0) return 0;
                negative = false;
                break;

            default:
                break;
        }

        int index = (quadrant & 1) == 0 ? position & 0xFF : 0xFF - (position & 0xFF);
        int amplitude = OplTables.Amplitude(OplTables.LogSine[index] + attenuation);

        return negative ? -amplitude : amplitude;
    }

    /// <summary>
    /// Combines a register rate with the pitch-derived offset, as the chip does.
    /// A rate of zero stays zero, which means the envelope never moves.
    /// </summary>
    private int EffectiveRate(int rate, int keyScaleNumber)
    {
        if (rate == 0) return 0;

        int offset = KeyScaleRate ? keyScaleNumber : keyScaleNumber >> 2;
        int effective = (rate << 2) + offset;

        return effective > 63 ? 63 : effective;
    }

    private void AdvanceAttack(int rate, long counter)
    {
        // The fastest rates snap to full level within a sample.
        if (rate >= 60)
        {
            _envelope = 0;
            State = OplEnvelopeState.Decay;
            return;
        }

        int step = EnvelopeStep(rate, counter);
        if (step == 0) return;

        // The attack approaches full level proportionally, which gives the OPL its rounded onset.
        _envelope -= ((_envelope + 1) * step) >> 3;

        if (_envelope <= 0)
        {
            _envelope = 0;
            State = OplEnvelopeState.Decay;
        }
    }

    private void AdvanceDecay(int rate, long counter)
    {
        int target = SustainLevel >= 15 ? Silence : SustainLevel << 4;

        _envelope += EnvelopeStep(rate, counter);

        if (_envelope < target) return;

        _envelope = target;
        State = OplEnvelopeState.Sustain;
    }

    private void AdvanceRelease(int rate, long counter)
    {
        _envelope += EnvelopeStep(rate, counter);
        if (_envelope < Silence) return;

        _envelope = Silence;
        State = OplEnvelopeState.Off;
    }

    /// <summary>
    /// How far the envelope moves this sample, in units of 0.1875 dB.
    /// </summary>
    /// <remarks>
    /// Each step of four in the rate doubles the speed. The low two bits scale it further by
    /// <c>(4 + low) / 4</c>, which the chip spreads over eight cycles instead of using a fraction.
    /// </remarks>
    private static int EnvelopeStep(int rate, long counter)
    {
        if (rate < 4) return 0;

        int high = rate >> 2;
        int low = rate & 3;

        // Above rate 52 the envelope moves more than one unit per sample.
        if (high >= 13)
        {
            int scale = 1 << (high - 13);
            return scale * Pattern(low, (int)(counter & 7));
        }

        int shift = 13 - high;
        if ((counter & ((1L << shift) - 1)) != 0) return 0;

        return Pattern(low, (int)((counter >> shift) & 7));
    }

    /// <summary>
    /// Spreads the fractional part of a rate over eight cycles, so the average increment is
    /// <c>(4 + low) / 4</c>.
    /// </summary>
    /// <remarks>
    /// The extra units are distributed as evenly as possible, which reproduces the chip's own
    /// patterns: <c>1,1,1,2,…</c> for a low of one, <c>1,2,1,2,…</c> for two and
    /// <c>1,2,2,2,…</c> for three.
    /// </remarks>
    private static int Pattern(int low, int cycle)
        => 1 + ((((cycle + 1) * low) >> 2) - ((cycle * low) >> 2));
}
