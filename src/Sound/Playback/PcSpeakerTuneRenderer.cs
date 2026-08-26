using System;
using System.Collections.Generic;
using System.IO;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;



/// <summary>
/// Renders a PC speaker tune from its note data.
/// </summary>
/// <remarks>
/// <para>
/// The speaker is a single bit driven by timer channel 2, so the waveform is a square wave whose
/// period the driver keeps changing. Rather than emit one fixed tone per note, this builds the
/// signal along a timeline at a high internal rate: the slide and vibrato of a step are applied on
/// every tick of the driver's fast timer, exactly where the original applied them.
/// </para>
/// <para>
/// The result then goes through the same low-pass stage DOSBox puts on its speaker channel. Without
/// it the square edges are far brighter than any real speaker could reproduce.
/// </para>
/// </remarks>
internal sealed class PcSpeakerTuneRenderer : ITuneRenderer
{
    /// <summary>Device name of the packs this renderer serves.</summary>
    public const string DeviceName = "pcSpeaker";

    /// <summary>Rate the rendered wave files use.</summary>
    public const int OutputSampleRate = 44100;

    /// <summary>
    /// Internal rate the square wave is built at. Four times the output rate keeps the switching
    /// edges close enough to their true position for the filter to place them correctly.
    /// </summary>
    private const int InternalSampleRate = OutputSampleRate * 4;

    /// <summary>Roll-off of the speaker itself, which cannot reproduce a square edge.</summary>
    private const double CutoffHz = 10000d;

    private const int FilterStages = 3;

    /// <summary>Level the speaker is mixed at. It is a single voice, so it can be fairly loud.</summary>
    private const float MixGain = 0.5f;

    /// <summary>Longest tune we are willing to render, as a safety net.</summary>
    private const double MaxSeconds = 240d;

    /// <summary>
    /// Clock of the PC's timer chip, used when the pack does not state one of its own.
    /// </summary>
    private const int DefaultPitClockHz = 1_193_182;

    private readonly AudioResamplerDelegate _resampler = new();

    /// <inheritdoc/>
    public string Device => DeviceName;

    /// <summary>Gets the gain this device's audio is mixed at.</summary>
    public static float Gain => MixGain;

    /// <inheritdoc/>
    public RenderedTune? Render(SoundPackIndex index, string packFolder, string scoreFileName, int arrangement)
    {
        ArgumentNullException.ThrowIfNull(index);

        string scorePath = Path.Combine(packFolder, scoreFileName);
        if (!File.Exists(scorePath)) return null;

        TuneScore tune = TuneScoreJson.Load(scorePath);
        if (tune.Steps.Count == 0) return null;

        float[] samples = RenderSteps(index, tune);
        if (samples.Length == 0) return null;

        new LowPassFilterDelegate(CutoffHz, InternalSampleRate, FilterStages).Apply(samples);

        return new RenderedTune(
            _resampler.Resample(samples, InternalSampleRate, OutputSampleRate),
            OutputSampleRate);
    }

    /// <summary>
    /// Walks the tune one fast tick at a time, keeping a running phase so the waveform stays
    /// continuous when the pitch changes inside a note.
    /// </summary>
    private static float[] RenderSteps(SoundPackIndex index, TuneScore tune)
    {
        int fastTicksPerStep = Math.Max(1, index.WorkerTickDivider);
        double samplesPerFastTick = InternalSampleRate / (double)Math.Max(1, index.FastTickHz);
        int pitClockHz = index.PitClockHz is > 0 ? index.PitClockHz.Value : DefaultPitClockHz;
        int maxSamples = (int)(MaxSeconds * InternalSampleRate);

        var samples = new List<float>();
        double phase = 0d;
        double carry = 0d;

        foreach (TuneStep step in tune.Steps)
        {
            var pitch = new SpeakerPitchDelegate(step);
            int ticks = step.Duration * fastTicksPerStep;

            for (int tick = 0; tick < ticks && samples.Count < maxSamples; tick++)
            {
                carry += samplesPerFastTick;
                int count = (int)carry;
                carry -= count;

                double frequency = pitch.Advance(pitClockHz);
                double increment = frequency / InternalSampleRate;

                for (int sample = 0; sample < count; sample++)
                {
                    if (frequency <= 0d)
                    {
                        samples.Add(0f);
                        continue;
                    }

                    phase += increment;
                    if (phase >= 1d) phase -= 1d;

                    samples.Add(phase < 0.5d ? 1f : -1f);
                }
            }
        }

        return [.. samples];
    }
}

/// <summary>
/// Works out the frequency of one step on every tick of the driver's fast timer, applying whatever
/// slide or vibrato the step carries.
/// </summary>
internal sealed class SpeakerPitchDelegate
{
    private readonly SpeakerEffect _effect;
    private readonly int _baseDivisor;

    private int _divisor;
    private int _vibrato;
    private int _direction = 1;

    /// <summary>
    /// Creates the delegate for one step.
    /// </summary>
    /// <param name="step">The step whose pitch is being followed.</param>
    public SpeakerPitchDelegate(TuneStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        _effect = step.DecodedEffect;
        _baseDivisor = step.Divisor;
        _divisor = step.Divisor;
    }

    /// <summary>
    /// Advances by one fast tick and returns the frequency to use for it.
    /// </summary>
    /// <param name="pitClockHz">Clock frequency of the timer, in Hz.</param>
    /// <returns>The frequency in Hz, or <c>0</c> for a rest.</returns>
    public double Advance(int pitClockHz)
    {
        if (_baseDivisor <= 0) return 0d;

        switch (_effect.Kind)
        {
            case SpeakerEffectKind.Slide:
                _divisor += _effect.Delta;
                break;

            case SpeakerEffectKind.Vibrato:
                AdvanceVibrato();
                break;

            default:
                break;
        }

        // A divisor of zero would divide by nothing and one below it wraps the timer.
        return _divisor <= 0 ? 0d : pitClockHz / (double)_divisor;
    }

    private void AdvanceVibrato()
    {
        int step = Math.Max(1, _effect.Step);

        _vibrato += step * _direction;

        if (_vibrato >= _effect.Range)
        {
            _vibrato = _effect.Range;
            _direction = -1;
        }
        else if (_vibrato <= -_effect.Range)
        {
            _vibrato = -_effect.Range;
            _direction = 1;
        }

        _divisor = _baseDivisor + _vibrato;
    }
}
