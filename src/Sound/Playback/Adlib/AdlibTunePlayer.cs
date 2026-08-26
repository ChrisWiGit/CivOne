using System;
using System.Collections.Generic;
using CivOne.Sound.Cvl.Adlib;
using CivOne.Sound.Opl;

namespace CivOne.Sound.Playback.Adlib;

#nullable enable

/// <summary>
/// Plays an <see cref="AdlibArrangement"/> the way ASOUND.CVL did, by writing OPL registers.
/// </summary>
/// <remarks>
/// <para>
/// The driver ran two timers. The sequencer read events and moved envelopes 60 times a second;
/// a faster one at 300 Hz only jittered the pitch of the noise instruments. <see cref="Tick"/> is
/// one step of the fast timer and runs the sequencer on every n-th of them, so both keep their
/// original relationship.
/// </para>
/// <para>
/// The player keeps its own copy of the register values, exactly as the driver did, because
/// several operations only change some bits of a register and need to know the rest.
/// </para>
/// </remarks>
internal sealed class AdlibTunePlayer
{
    /// <summary>Bit of register <c>0xB0</c> that holds the key down.</summary>
    private const int KeyOnBit = 0x20;

    /// <summary>Number of noise voices the driver can run at the same time.</summary>
    private const int NoiseSlots = 2;

    private const int SemitonesPerOctave = 12;
    private const int MaxLevel = 0x3F;

    private readonly AdlibSoundBank _bank;
    private readonly IOplChip _chip;
    private readonly int _workerTickDivider;
    private readonly AdlibRandomDelegate _random;
    private readonly byte[] _registers = new byte[0x100];

    private readonly List<AdlibVoiceState> _voices = [];
    private readonly AdlibNoiseSlot[] _noise = [new(), new()];
    private readonly AdlibInstrument?[] _channelInstruments;

    private int _nextNoiseSlot;
    private int _fastTick;

    /// <summary>
    /// Creates a player for one instrument bank.
    /// </summary>
    /// <param name="bank">The pack's shared instrument bank.</param>
    /// <param name="chip">The chip to drive.</param>
    /// <param name="workerTickDivider">
    /// How many steps of the fast timer make one sequencer step, from the pack's manifest.
    /// </param>
    /// <param name="random">
    /// The generator behind the noise instruments, or <c>null</c> for one seeded with zero.
    /// </param>
    public AdlibTunePlayer(AdlibSoundBank bank, IOplChip chip, int workerTickDivider,
        AdlibRandomDelegate? random = null)
    {
        ArgumentNullException.ThrowIfNull(bank);
        ArgumentNullException.ThrowIfNull(chip);

        _bank = bank;
        _chip = chip;
        _workerTickDivider = Math.Max(1, workerTickDivider);
        _random = random ?? new AdlibRandomDelegate();
        _channelInstruments = new AdlibInstrument?[Math.Max(bank.ChannelCount, chip.ChannelCount)];
    }

    /// <summary>Gets whether any voice or noise slot is still running.</summary>
    public bool IsPlaying
    {
        get
        {
            foreach (AdlibVoiceState voice in _voices)
            {
                if (voice.IsActive) return true;
            }

            foreach (AdlibNoiseSlot slot in _noise)
            {
                if (slot.Ticks > 0) return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets whether every voice has either ended or looped back to its start once.
    /// A renderer uses this to capture one pass of a tune that would otherwise repeat forever.
    /// </summary>
    public bool PassCompleted
    {
        get
        {
            foreach (AdlibVoiceState voice in _voices)
            {
                if (!voice.PassCompleted) return false;
            }

            return _voices.Count > 0;
        }
    }

    /// <summary>
    /// Resets the chip and starts an arrangement.
    /// </summary>
    /// <param name="arrangement">The arrangement to play.</param>
    public void Start(AdlibArrangement arrangement)
    {
        ArgumentNullException.ThrowIfNull(arrangement);

        ResetChip();
        _voices.Clear();
        _fastTick = 0;

        foreach (AdlibVoice voice in arrangement.Voices)
        {
            if (voice.Channel < 0 || voice.Channel >= _chip.ChannelCount) continue;

            var state = new AdlibVoiceState();
            state.Start(voice.Channel, voice.Events, _bank.DefaultPan);
            _voices.Add(state);
        }
    }

    /// <summary>
    /// Advances the player by one step of the fast timer.
    /// </summary>
    /// <returns><c>true</c> while something is still playing.</returns>
    public bool Tick()
    {
        if (_fastTick == 0) SequencerTick();

        _fastTick++;
        if (_fastTick >= _workerTickDivider) _fastTick = 0;

        NoiseTick();
        return IsPlaying;
    }

    /// <summary>
    /// Silences everything, leaving the chip in the state the driver's stop routine leaves it in.
    /// </summary>
    public void Stop()
    {
        foreach (AdlibVoiceState voice in _voices) voice.Stop();
        foreach (AdlibNoiseSlot slot in _noise) slot.Ticks = 0;

        ResetChip();
    }

    /// <summary>
    /// Brings the chip into the state the driver's reset routine leaves it in: every operator
    /// muted, every register cleared and waveform select switched on.
    /// </summary>
    private void ResetChip()
    {
        _chip.Reset();
        Array.Clear(_registers);

        for (int register = 0x4F; register >= 0x40; register--) Write(register, MaxLevel);
        for (int register = 0xFF; register >= 0x60; register--) Write(register, 0x00);
        for (int register = 0x3F; register >= 0x01; register--) Write(register, 0x00);

        Write(0x01, 0x20);

        Array.Clear(_channelInstruments);
        foreach (AdlibNoiseSlot slot in _noise) slot.Reset();
        _nextNoiseSlot = 0;
    }

    private void Write(int register, int value)
    {
        _registers[register & 0xFF] = (byte)value;
        _chip.WriteRegister(register, value);
    }

    private byte Read(int register) => _registers[register & 0xFF];

    /// <summary>
    /// One step of the sequencer: every voice reads events and moves its envelopes, then the
    /// noise slots age by one step.
    /// </summary>
    private void SequencerTick()
    {
        _random.Next();

        foreach (AdlibVoiceState voice in _voices) AdvanceVoice(voice);

        AgeNoiseSlots();
    }

    private void AdvanceVoice(AdlibVoiceState voice)
    {
        if (!voice.IsActive) return;

        if (voice.GateCounter != 0 && --voice.GateCounter == 0) KeyOff(voice.Channel);

        if (--voice.Duration <= 0)
        {
            voice.Duration = 0;
            ReadEvents(voice);
        }

        Modulate(voice);
    }

    /// <summary>
    /// Reads control opcodes until a note record gives the voice something to wait for.
    /// </summary>
    private void ReadEvents(AdlibVoiceState voice)
    {
        bool refreshVolume = false;

        // A repeat can walk the same opcodes many times before reaching a note, so the guard is
        // generous. It only exists so a sequence that never plays anything cannot spin forever.
        int limit = (voice.Events.Count * 4) + 256;

        for (int guard = 0; guard < limit; guard++)
        {
            if (voice.Index < 0 || voice.Index >= voice.Events.Count)
            {
                voice.Stop();
                KeyOff(voice.Channel);
                return;
            }

            AdlibEvent current = voice.Events[voice.Index];

            if (current.Kind == AdlibEventKind.Note)
            {
                PlayNote(voice, current, refreshVolume);
                return;
            }

            refreshVolume |= Control(voice, current);
        }

        // A block of opcodes that never reaches a note would spin forever on the real driver too.
        voice.Stop();
        KeyOff(voice.Channel);
    }

    private void PlayNote(AdlibVoiceState voice, AdlibEvent note, bool refreshVolume)
    {
        if (refreshVolume) ApplyVolume(voice);

        voice.Note = note.Note;
        voice.Duration = note.Duration;
        voice.Index++;

        if (voice.Note == 0 || voice.Duration == 0)
        {
            KeyOff(voice.Channel);
            return;
        }

        voice.GateCounter = voice.Duration - voice.Gate;
        NoteOn(voice);
    }

    /// <summary>
    /// Applies one control opcode.
    /// </summary>
    /// <returns><c>true</c> when the change means the level has to be rewritten.</returns>
    private bool Control(AdlibVoiceState voice, AdlibEvent current)
    {
        int index = voice.Index;

        switch (current.Kind)
        {
            case AdlibEventKind.LoopOuter:
                Loop(voice, current.Value, inner: false);
                return false;

            case AdlibEventKind.LoopInner:
                Loop(voice, current.Value, inner: true);
                return false;

            case AdlibEventKind.Restart:
                voice.Restart(_bank.DefaultPan);
                return false;

            case AdlibEventKind.SetInstrument:
                voice.Instrument = current.Value;
                voice.Index = index + 1;
                LoadInstrument(voice);
                return false;

            case AdlibEventKind.SetGate:
                voice.Gate = current.Value;
                voice.Index = index + 1;
                return false;

            case AdlibEventKind.SetPitchSlide:
                voice.PitchSlide = (sbyte)current.Value;
                voice.Index = index + 1;
                return false;

            case AdlibEventKind.SetVolume:
                voice.Volume = ((sbyte)current.Value) >> 1;
                voice.Index = index + 1;
                return true;

            case AdlibEventKind.VolumeEnvelope:
                voice.VolumeEnvelopePeriod = current.Value;
                voice.VolumeDelta = (sbyte)current.Delta;
                voice.VolumeEnvelopeCounter = 1;
                voice.Index = index + 1;
                return false;

            case AdlibEventKind.SetDetune:
                voice.Detune = (sbyte)current.Value;
                voice.Index = index + 1;
                return false;

            case AdlibEventKind.SetVolumeOffset:
                voice.VolumeOffset = (sbyte)current.Value;
                voice.Index = index + 1;
                return true;

            case AdlibEventKind.SetPan:
                voice.Pan = current.Value;
                voice.Index = index + 1;
                return true;

            case AdlibEventKind.PanEnvelope:
                voice.PanEnvelopePeriod = current.Value;
                voice.PanDelta = (sbyte)current.Delta;
                voice.PanEnvelopeCounter = 1;
                voice.Index = index + 1;
                return false;

            default:
                // The random variant opcode exists in the driver but no tune of the game uses it.
                voice.Index = index + 1;
                return false;
        }
    }

    /// <summary>
    /// Applies a repeat opcode. Both levels work the same way: the opcode sits at the <em>end</em>
    /// of the block it repeats, and remembers where the next block begins once it is done.
    /// </summary>
    private static void Loop(AdlibVoiceState voice, int count, bool inner)
    {
        int next = voice.Index + 1;
        int counter = inner ? voice.InnerLoopCounter : voice.OuterLoopCounter;

        // The driver's "repeat forever" idiom. The music still plays on, but a renderer now knows
        // that nothing new follows.
        if (counter == 0 && count == AdlibVoiceState.EndlessRepeatCount) voice.EndlessRepeat = true;

        if (counter == 0)
        {
            if (count == 0)
            {
                voice.Index = next;
                LeaveLoop(voice, inner, next);
                return;
            }

            if (inner)
            {
                voice.InnerLoopCounter = count;
                voice.Index = voice.InnerLoopStart;
                voice.OuterLoopStart = voice.InnerLoopStart;
                return;
            }

            voice.OuterLoopCounter = count;
            voice.Index = voice.OuterLoopStart;
            return;
        }

        counter--;
        if (inner) voice.InnerLoopCounter = counter;
        else voice.OuterLoopCounter = counter;

        if (counter == 0)
        {
            voice.Index = next;
            LeaveLoop(voice, inner, next);
            return;
        }

        if (inner)
        {
            voice.Index = voice.InnerLoopStart;
            voice.OuterLoopStart = voice.InnerLoopStart;
            return;
        }

        voice.Index = voice.OuterLoopStart;
    }

    private static void LeaveLoop(AdlibVoiceState voice, bool inner, int next)
    {
        voice.OuterLoopStart = next;

        if (!inner) return;

        voice.InnerLoopStart = next;

        // Leaving the inner block also drops whatever the outer one was counting.
        voice.OuterLoopCounter = 0;
        voice.InnerLoopCounter = 0;
    }

    /// <summary>
    /// The per-tick modulation: pitch slide, then the volume and pan envelopes.
    /// </summary>
    private void Modulate(AdlibVoiceState voice)
    {
        if (voice.PitchSlide != 0) SlidePitch(voice);

        if (voice.VolumeEnvelopeCounter == 0 && voice.PanEnvelopeCounter == 0) return;

        bool refresh = false;

        // Once either envelope is running the driver counts both of them down, wrapping the one
        // that was never started. With a step of zero that is harmless, so the quirk is kept.
        voice.VolumeEnvelopeCounter = (voice.VolumeEnvelopeCounter - 1) & 0xFF;
        if (voice.VolumeEnvelopeCounter == 0)
        {
            voice.VolumeEnvelopeCounter = voice.VolumeEnvelopePeriod;
            refresh |= StepVolumeEnvelope(voice);
        }

        voice.PanEnvelopeCounter = (voice.PanEnvelopeCounter - 1) & 0xFF;
        if (voice.PanEnvelopeCounter == 0)
        {
            voice.PanEnvelopeCounter = voice.PanEnvelopePeriod;

            if (voice.PanDelta != 0)
            {
                voice.Pan = (sbyte)(voice.Pan + voice.PanDelta);
                refresh = true;
            }
        }

        if (refresh) ApplyVolume(voice);
    }

    /// <summary>
    /// Moves the volume envelope one step. Reaching either end stops the envelope and clears the
    /// base volume, which is how the driver's fades settle at full level or at silence.
    /// </summary>
    /// <returns><c>true</c> when the level has to be rewritten.</returns>
    private static bool StepVolumeEnvelope(AdlibVoiceState voice)
    {
        if (voice.VolumeDelta == 0) return false;

        voice.VolumeOffset = (sbyte)(voice.VolumeOffset + voice.VolumeDelta);

        if (voice.VolumeOffset <= 0)
        {
            voice.VolumeEnvelopeCounter = 0;
            voice.VolumeOffset = 0;
            voice.Volume = 0;
        }
        else if (voice.VolumeOffset >= AdlibVoiceState.MaxVolumeOffset)
        {
            voice.VolumeEnvelopeCounter = 0;
            voice.VolumeOffset = AdlibVoiceState.MaxVolumeOffset;
            voice.Volume = 0;
        }

        return true;
    }

    /// <summary>
    /// Writes the channel's level, keeping the key scale bits the instrument set.
    /// </summary>
    private void ApplyVolume(AdlibVoiceState voice)
    {
        int carrier = CarrierOffset(voice.Channel);
        if (carrier < 0) return;

        int level = voice.Volume + voice.VolumeOffset;
        if (level < 0) level = 0;
        if (level > MaxLevel) level = MaxLevel;

        int keyScale = Read(0x40 + carrier) & 0xC0;
        Write(0x40 + carrier, keyScale | (MaxLevel - level));
    }

    /// <summary>
    /// Adds the slide step to the running pitch, keeping the key down.
    /// </summary>
    private void SlidePitch(AdlibVoiceState voice)
    {
        int channel = voice.Channel;
        int pitch = ((Read(0xB0 + channel) & 0x1F) << 8) | Read(0xA0 + channel);

        pitch += voice.PitchSlide;

        Write(0xA0 + channel, pitch & 0xFF);
        Write(0xB0 + channel, (Read(0xB0 + channel) & KeyOnBit) | ((pitch >> 8) & 0x1F));
    }

    private void NoteOn(AdlibVoiceState voice)
    {
        AdlibInstrument? instrument = _channelInstruments[voice.Channel];

        if (instrument != null && instrument.IsNoise)
        {
            StartNoise(voice, instrument);
            return;
        }

        SetPitch(voice);
        Write(0xB0 + voice.Channel, Read(0xB0 + voice.Channel) | KeyOnBit);
    }

    private void KeyOff(int channel)
        => Write(0xB0 + channel, Read(0xB0 + channel) & ~KeyOnBit);

    /// <summary>
    /// Splits the note into an octave and a semitone and writes the resulting pitch.
    /// </summary>
    private void SetPitch(AdlibVoiceState voice)
    {
        int semitone = voice.Note % SemitonesPerOctave;
        int octave = voice.Note / SemitonesPerOctave;

        int pitch = _bank.FrequencyNumbers[semitone] + voice.Detune;

        Write(0xA0 + voice.Channel, pitch & 0xFF);
        Write(0xB0 + voice.Channel,
            (Read(0xB0 + voice.Channel) & KeyOnBit) | ((octave & 7) << 2) | ((pitch >> 8) & 3));
    }

    /// <summary>
    /// Loads an instrument into the voice's channel.
    /// </summary>
    private void LoadInstrument(AdlibVoiceState voice)
    {
        if (voice.Instrument < 0 || voice.Instrument >= _bank.Instruments.Count) return;

        int channel = voice.Channel;
        AdlibInstrument instrument = _bank.Instruments[voice.Instrument];
        _channelInstruments[channel] = instrument;

        KeyOff(channel);

        LoadOperator(instrument.Modulator, ModulatorOffset(channel), channel);
        LoadOperator(instrument.Carrier, CarrierOffset(channel), channel);
    }

    /// <summary>
    /// Writes one operator's registers, in the order the driver writes them.
    /// </summary>
    private void LoadOperator(AdlibOperator settings, int offset, int channel)
    {
        if (offset < 0) return;

        // Mute the operator while its envelope is being rewritten.
        Write(0x40 + offset, MaxLevel);

        Write(0xBD, (_bank.DeepTremolo ? 0x80 : 0) | (_bank.DeepVibrato ? 0x40 : 0));
        Write(0x08, _bank.NoteSelect ? 0x40 : 0);

        Write(0xC0 + channel, (settings.Feedback << 1) | (settings.FrequencyModulation ? 0 : 1));
        Write(0x60 + offset, (settings.AttackRate << 4) | (settings.DecayRate & 0x0F));
        Write(0x80 + offset, (settings.SustainLevel << 4) | (settings.ReleaseRate & 0x0F));

        Write(0x20 + offset,
            (settings.Tremolo ? 0x80 : 0)
            + (settings.Vibrato ? 0x40 : 0)
            + (settings.Sustaining ? 0x20 : 0)
            + (settings.KeyScaleRate ? 0x10 : 0)
            + (settings.FrequencyMultiplier & 0x0F));

        Write(0xE0 + offset, settings.Waveform & 3);
        Write(0x40 + offset, (MaxLevel - (settings.Level & MaxLevel)) | (settings.KeyScaleLevel << 6));
    }

    private int ModulatorOffset(int channel)
        => channel >= 0 && channel < _bank.ModulatorOffsets.Count ? _bank.ModulatorOffsets[channel] : -1;

    private int CarrierOffset(int channel)
        => channel >= 0 && channel < _bank.CarrierOffsets.Count ? _bank.CarrierOffsets[channel] : -1;

    /// <summary>
    /// Hands a channel to one of the two noise slots. A channel that already owns a slot keeps it.
    /// </summary>
    private void StartNoise(AdlibVoiceState voice, AdlibInstrument instrument)
    {
        for (int index = 0; index < NoiseSlots; index++)
        {
            if (_noise[index].Channel == voice.Channel) _nextNoiseSlot = index;
        }

        AdlibNoiseSlot slot = _noise[_nextNoiseSlot];
        _nextNoiseSlot = _nextNoiseSlot == 0 ? 1 : 0;

        // Whatever the slot was doing is cut short.
        if (slot.Ticks != 0) KeyOff(slot.Channel);

        slot.Channel = voice.Channel;
        slot.Ticks = instrument.NoiseDuration;
        slot.Mask = instrument.NoiseMask;
        slot.Base = instrument.NoiseBase;
        slot.Step = instrument.NoiseStep;
    }

    /// <summary>
    /// Moves both noise slots forward by one sequencer tick and silences the ones that ran out.
    /// </summary>
    private void AgeNoiseSlots()
    {
        for (int index = 0; index < NoiseSlots; index++)
        {
            AdlibNoiseSlot slot = _noise[index];
            if (slot.Ticks == 0) continue;

            slot.Base = (slot.Base + slot.Step) & 0xFFFF;
            if (--slot.Ticks != 0) continue;

            // Do not silence a channel the other slot has taken over in the meantime.
            AdlibNoiseSlot other = _noise[index == 0 ? 1 : 0];
            if (other.Ticks != 0 && other.Channel == slot.Channel) continue;

            Write(0xA0 + slot.Channel, 0);
            Write(0xB0 + slot.Channel, 0);
        }
    }

    /// <summary>
    /// One step of the fast timer: jitters the pitch of every running noise slot.
    /// </summary>
    private void NoiseTick()
    {
        int value = _random.Next();

        // The two slots take opposite halves of the same random value, so two explosions at once
        // do not move in lockstep.
        WriteNoisePitch(_noise[0], (~value) & 0xFFFF);
        WriteNoisePitch(_noise[1], value);
    }

    private void WriteNoisePitch(AdlibNoiseSlot slot, int value)
    {
        if (slot.Ticks == 0) return;

        int pitch = ((value & slot.Mask) + slot.Base) & 0xFFFF;

        Write(0xA0 + slot.Channel, pitch & 0xFF);
        Write(0xB0 + slot.Channel, ((pitch >> 8) & 0x1F) | KeyOnBit);
    }
}

/// <summary>
/// One of the two channels the driver can turn into a noise generator.
/// </summary>
internal sealed class AdlibNoiseSlot
{
    /// <summary>Gets or sets the OPL channel this slot drives.</summary>
    public int Channel { get; set; }

    /// <summary>Gets or sets how many sequencer ticks the slot has left.</summary>
    public int Ticks { get; set; }

    /// <summary>Gets or sets the mask that bounds how far the pitch jumps.</summary>
    public int Mask { get; set; }

    /// <summary>Gets or sets the pitch the jitter is added to.</summary>
    public int Base { get; set; }

    /// <summary>Gets or sets how much <see cref="Base"/> moves per sequencer tick.</summary>
    public int Step { get; set; }

    /// <summary>Clears the slot.</summary>
    public void Reset()
    {
        Channel = 0;
        Ticks = 0;
        Mask = 0;
        Base = 0;
        Step = 0;
    }
}
