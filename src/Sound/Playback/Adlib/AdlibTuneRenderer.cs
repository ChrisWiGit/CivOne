using System;
using System.Collections.Generic;
using System.IO;
using CivOne.Sound.Cvl;
using CivOne.Sound.Cvl.Adlib;
using CivOne.Sound.Opl;

namespace CivOne.Sound.Playback.Adlib;



/// <summary>
/// Renders an AdLib tune by running the original driver logic against an emulated OPL2.
/// </summary>
/// <remarks>
/// The chain is the one the hardware used: the sequencer writes chip registers, the chip turns
/// them into samples at its own rate, and only then is the result filtered and resampled for the
/// output file.
/// </remarks>
internal sealed class AdlibTuneRenderer : ITuneRenderer
{
    /// <summary>Rate the rendered wave files use.</summary>
    public const int OutputSampleRate = 44100;

    /// <summary>
    /// Gentle roll-off standing in for the analog stage of an AdLib or Sound Blaster card. It sits
    /// high enough to leave the FM timbre intact and only takes the hardest edges off.
    /// </summary>
    private const double CutoffHz = 12000d;

    private const int FilterStages = 2;

    /// <summary>
    /// Level a single FM operator is mixed at. Tunes use up to nine voices at once, so one voice
    /// on its own has to stay well below full scale.
    /// </summary>
    private const float MixGain = 0.30f;

    /// <summary>Longest tune we are willing to render, as a safety net against endless loops.</summary>
    private const double MaxSeconds = 240d;

    /// <summary>Extra time rendered after the last note so envelopes can die away.</summary>
    private const double TailSeconds = 0.5d;

    private readonly AudioResamplerDelegate _resampler = new();
    private readonly Dictionary<string, AdlibSoundBank> _banks = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public string Device => AsoundScoreExporter.DeviceName;

    /// <inheritdoc/>
    public RenderedTune? Render(SoundPackIndex index, string packFolder, string scoreFileName, int arrangement)
    {
        ArgumentNullException.ThrowIfNull(index);

        string scorePath = Path.Combine(packFolder, scoreFileName);
        if (!File.Exists(scorePath)) return null;

        AdlibSoundBank bank = LoadBank(packFolder);
        AdlibTuneScore tune = AdlibScoreJson.LoadTune(scorePath);

        if (tune.Arrangements.Count == 0) return null;
        int chosen = arrangement < 0 || arrangement >= tune.Arrangements.Count ? 0 : arrangement;

        float[] samples = RenderArrangement(index, bank, tune.Arrangements[chosen]);
        if (samples.Length == 0) return null;

        new LowPassFilterDelegate(CutoffHz, Opl2Chip.NativeSampleRate, FilterStages).Apply(samples);

        return new RenderedTune(
            _resampler.Resample(samples, Opl2Chip.NativeSampleRate, OutputSampleRate),
            OutputSampleRate);
    }

    /// <summary>
    /// Gets the gain this device's audio is mixed at.
    /// </summary>
    public static float Gain => MixGain;

    private AdlibSoundBank LoadBank(string packFolder)
    {
        if (_banks.TryGetValue(packFolder, out AdlibSoundBank? cached)) return cached;

        AdlibSoundBank bank = AdlibScoreJson.LoadBank(Path.Combine(packFolder, AdlibSoundBank.FileName));
        _banks[packFolder] = bank;
        return bank;
    }

    /// <summary>
    /// Runs the driver and the chip in lockstep, one block of chip samples per driver tick.
    /// </summary>
    private static float[] RenderArrangement(SoundPackIndex index, AdlibSoundBank bank,
        AdlibArrangement arrangement)
    {
        var chip = new Opl2Chip();
        var player = new AdlibTunePlayer(bank, chip, index.WorkerTickDivider);
        player.Start(arrangement);

        double samplesPerTick = chip.SampleRate / Math.Max(1d, index.FastTickHz);
        int maxTicks = (int)(MaxSeconds * index.FastTickHz);
        int tailTicks = (int)(TailSeconds * index.FastTickHz);

        var samples = new List<float>((int)(samplesPerTick * Math.Min(maxTicks, 4096)));
        var block = new float[(int)Math.Ceiling(samplesPerTick) + 1];

        double carry = 0d;
        int tail = 0;

        for (int tick = 0; tick < maxTicks; tick++)
        {
            bool playing = player.Tick() && !player.PassCompleted;

            carry += samplesPerTick;
            int count = (int)carry;
            carry -= count;

            Span<float> span = block.AsSpan(0, count);
            chip.Render(span);
            samples.AddRange(span);

            if (playing) continue;

            // Keep going for a moment so releases are not cut off, then stop once the chip is quiet.
            if (++tail >= tailTicks || !chip.IsActive) break;
        }

        return [.. samples];
    }
}
