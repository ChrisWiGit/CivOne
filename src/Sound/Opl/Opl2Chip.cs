using System;

namespace CivOne.Sound.Opl;

#nullable enable

/// <summary>
/// A YM3812 (OPL2): nine two-operator FM channels, mono.
/// </summary>
/// <remarks>
/// <para>
/// This is the chip an AdLib card carries and the one a Sound Blaster is compatible with, so it is
/// what CivOne's <c>ASOUND.CVL</c> music was written for. The implementation follows the hardware's
/// own structure - logarithmic sine, additive attenuation, one exponential at the end - because
/// that is what gives the sound its character.
/// </para>
/// <para>
/// The chip's rhythm mode is not implemented. The driver never enables it: it only ever writes the
/// tremolo and vibrato depth bits of register <c>0xBD</c>.
/// </para>
/// </remarks>
internal sealed class Opl2Chip : IOplChip
{
    /// <summary>Rate at which the real chip produces samples: 3.579545 MHz divided by 72.</summary>
    public const int NativeSampleRate = 49716;

    /// <summary>Number of two-operator channels.</summary>
    public const int Channels = 9;

    /// <summary>Full-scale output of one operator, used to normalize the mix.</summary>
    private const float OperatorScale = 4084f;

    /// <summary>Samples in one cycle of the tremolo LFO, which runs at 3.7 Hz.</summary>
    private static readonly int _tremoloPeriod = (int)Math.Round(NativeSampleRate / 3.7);

    /// <summary>Samples in one cycle of the vibrato LFO, which runs at 6.1 Hz.</summary>
    private static readonly int _vibratoPeriod = (int)Math.Round(NativeSampleRate / 6.1);

    /// <summary>Deepest tremolo attenuation, 4.8 dB in units of 0.1875 dB.</summary>
    private const int TremoloDepth = 26;

    /// <summary>Eighth-cycle triangle the vibrato LFO follows.</summary>
    private static readonly int[] _vibratoShape = [0, 1, 2, 1, 0, -1, -2, -1];

    /// <summary>
    /// Register offset of each operator within a bank, in channel order: modulator then carrier.
    /// </summary>
    private static readonly int[] _operatorOffsets =
        [0x00, 0x01, 0x02, 0x08, 0x09, 0x0A, 0x10, 0x11, 0x12];

    private readonly OplChannel[] _channels = new OplChannel[Channels];
    private readonly byte[] _registers = new byte[0x100];

    private long _envelopeCounter;
    private int _tremoloPhase;
    private int _vibratoPhase;

    private bool _waveformSelectEnabled;
    private bool _noteSelect;
    private bool _deepTremolo;
    private bool _deepVibrato;

    /// <summary>
    /// Creates a chip in its power-on state.
    /// </summary>
    public Opl2Chip()
    {
        for (int channel = 0; channel < Channels; channel++)
        {
            _channels[channel] = new OplChannel();
        }

        Reset();
    }

    /// <inheritdoc/>
    public int SampleRate => NativeSampleRate;

    /// <inheritdoc/>
    public int ChannelCount => Channels;

    /// <summary>
    /// Gets whether any channel is still sounding. Useful to tell when a rendered tune has died away.
    /// </summary>
    public bool IsActive
    {
        get
        {
            foreach (OplChannel channel in _channels)
            {
                if (channel.IsActive) return true;
            }

            return false;
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        Array.Clear(_registers);

        foreach (OplChannel channel in _channels) channel.Reset();

        _envelopeCounter = 0;
        _tremoloPhase = 0;
        _vibratoPhase = 0;
        _waveformSelectEnabled = false;
        _noteSelect = false;
        _deepTremolo = false;
        _deepVibrato = false;
    }

    /// <summary>
    /// Reads back the last value written to a register, the way the driver's own shadow copy does.
    /// </summary>
    /// <param name="register">Register number.</param>
    /// <returns>The stored value.</returns>
    public byte ReadRegister(int register) => _registers[register & 0xFF];

    /// <inheritdoc/>
    public void WriteRegister(int register, int value)
    {
        register &= 0xFF;
        byte data = (byte)value;
        _registers[register] = data;

        switch (register & 0xF0)
        {
            case 0x00:
                WriteGlobal(register, data);
                return;

            case 0x20:
            case 0x30:
                WriteOperator(register - 0x20, data, ApplyMultiplier);
                return;

            case 0x40:
            case 0x50:
                WriteOperator(register - 0x40, data, ApplyLevel);
                return;

            case 0x60:
            case 0x70:
                WriteOperator(register - 0x60, data, ApplyAttackDecay);
                return;

            case 0x80:
            case 0x90:
                WriteOperator(register - 0x80, data, ApplySustainRelease);
                return;

            case 0xA0:
                WritePitchLow(register - 0xA0, data);
                return;

            case 0xB0:
                if (register == 0xBD) WriteDepth(data);
                else WritePitchHigh(register - 0xB0, data);
                return;

            case 0xC0:
                WriteConnection(register - 0xC0, data);
                return;

            case 0xE0:
            case 0xF0:
                WriteOperator(register - 0xE0, data, ApplyWaveform);
                return;

            default:
                return;
        }
    }

    /// <inheritdoc/>
    public void Render(Span<float> buffer)
    {
        for (int index = 0; index < buffer.Length; index++)
        {
            buffer[index] = RenderSample();
        }
    }

    private float RenderSample()
    {
        int tremolo = Tremolo();
        int vibratoShape = _vibratoShape[(_vibratoPhase * 8 / _vibratoPeriod) & 7];
        int sum = 0;

        foreach (OplChannel channel in _channels)
        {
            if (!channel.IsActive) continue;

            int vibrato = Vibrato(channel.FrequencyNumber, vibratoShape);
            channel.AdvanceEnvelopes(channel.KeyScaleNumber(_noteSelect), _envelopeCounter);
            sum += channel.Render(vibrato, tremolo);
        }

        _envelopeCounter++;
        if (++_tremoloPhase >= _tremoloPeriod) _tremoloPhase = 0;
        if (++_vibratoPhase >= _vibratoPeriod) _vibratoPhase = 0;

        return sum / OperatorScale;
    }

    /// <summary>
    /// The tremolo LFO as a triangle between no attenuation and the configured depth.
    /// </summary>
    private int Tremolo()
    {
        int half = _tremoloPeriod / 2;
        int position = _tremoloPhase < half ? _tremoloPhase : _tremoloPeriod - _tremoloPhase;
        int depth = (position * TremoloDepth) / half;

        return _deepTremolo ? depth : depth >> 2;
    }

    /// <summary>
    /// The vibrato LFO as a pitch offset proportional to the note's own F-number, so the effect
    /// stays the same musical interval across the keyboard.
    /// </summary>
    private int Vibrato(int frequencyNumber, int shape)
    {
        int depth = frequencyNumber >> 7;
        return (depth * shape) >> (_deepVibrato ? 1 : 2);
    }

    private void WriteGlobal(int register, byte value)
    {
        if (register == 0x01)
        {
            _waveformSelectEnabled = (value & 0x20) != 0;
            if (!_waveformSelectEnabled) ResetWaveforms();
            return;
        }

        if (register == 0x08) _noteSelect = (value & 0x40) != 0;
    }

    private void WriteDepth(byte value)
    {
        _deepTremolo = (value & 0x80) != 0;
        _deepVibrato = (value & 0x40) != 0;
    }

    private void WriteOperator(int offset, byte value, Action<OplOperator, byte> apply)
    {
        OplOperator? target = FindOperator(offset, out OplChannel? channel);
        if (target == null || channel == null) return;

        apply(target, value);

        // Key scaling depends on the operator's setting as well as on the channel's pitch.
        channel.RefreshKeyScaling();
    }

    private static void ApplyMultiplier(OplOperator target, byte value)
    {
        target.Tremolo = (value & 0x80) != 0;
        target.Vibrato = (value & 0x40) != 0;
        target.Sustaining = (value & 0x20) != 0;
        target.KeyScaleRate = (value & 0x10) != 0;
        target.Multiplier = value & 0x0F;
    }

    private static void ApplyLevel(OplOperator target, byte value)
    {
        target.KeyScaleLevelSetting = (value >> 6) & 3;
        target.TotalLevel = value & 0x3F;
    }

    private static void ApplyAttackDecay(OplOperator target, byte value)
    {
        target.AttackRate = (value >> 4) & 0x0F;
        target.DecayRate = value & 0x0F;
    }

    private static void ApplySustainRelease(OplOperator target, byte value)
    {
        target.SustainLevel = (value >> 4) & 0x0F;
        target.ReleaseRate = value & 0x0F;
    }

    private void ApplyWaveform(OplOperator target, byte value)
        => target.Waveform = _waveformSelectEnabled ? value & 3 : 0;

    private void ResetWaveforms()
    {
        foreach (OplChannel channel in _channels)
        {
            channel.Modulator.Waveform = 0;
            channel.Carrier.Waveform = 0;
        }
    }

    private void WritePitchLow(int index, byte value)
    {
        if (index >= Channels) return;

        OplChannel channel = _channels[index];
        channel.SetPitch((channel.FrequencyNumber & 0x300) | value, channel.Block);
    }

    private void WritePitchHigh(int index, byte value)
    {
        if (index >= Channels) return;

        OplChannel channel = _channels[index];
        channel.SetPitch((channel.FrequencyNumber & 0xFF) | ((value & 3) << 8), (value >> 2) & 7);
        channel.SetKey((value & 0x20) != 0);
    }

    private void WriteConnection(int index, byte value)
    {
        if (index >= Channels) return;

        _channels[index].FeedbackLevel = (value >> 1) & 7;
        _channels[index].Additive = (value & 1) != 0;
    }

    /// <summary>
    /// Maps an operator register offset onto the operator it belongs to.
    /// </summary>
    private OplOperator? FindOperator(int offset, out OplChannel? channel)
    {
        for (int index = 0; index < Channels; index++)
        {
            if (_operatorOffsets[index] == offset)
            {
                channel = _channels[index];
                return channel.Modulator;
            }

            if (_operatorOffsets[index] + 3 == offset)
            {
                channel = _channels[index];
                return channel.Carrier;
            }
        }

        channel = null;
        return null;
    }
}
