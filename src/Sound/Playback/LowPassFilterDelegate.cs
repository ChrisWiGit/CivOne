using System;

namespace CivOne.Sound.Playback;

#nullable enable

/// <summary>
/// A cascade of one-pole low-pass filters, the same shape DOSBox puts on its sound card channels.
/// </summary>
/// <remarks>
/// Real cards do not hand their raw output to the speaker: there is always an analog stage in
/// between, and leaving it out is what makes emulated audio sound harsher than the hardware did.
/// Several stages in series give the gentle roll-off of such a stage rather than the sharp edge of
/// a designed digital filter.
/// </remarks>
internal sealed class LowPassFilterDelegate
{
    private readonly float _coefficient;
    private readonly float[] _state;

    /// <summary>
    /// Creates a filter.
    /// </summary>
    /// <param name="cutoffHz">Frequency at which the roll-off of one stage begins, in Hz.</param>
    /// <param name="sampleRate">Rate of the samples to filter, in Hz.</param>
    /// <param name="stages">How many one-pole stages to put in series.</param>
    public LowPassFilterDelegate(double cutoffHz, int sampleRate, int stages)
    {
        if (cutoffHz <= 0d) throw new ArgumentOutOfRangeException(nameof(cutoffHz));
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (stages <= 0) throw new ArgumentOutOfRangeException(nameof(stages));

        double interval = 1d / sampleRate;
        double timeConstant = 1d / (2d * Math.PI * cutoffHz);

        _coefficient = (float)(interval / (timeConstant + interval));
        _state = new float[stages];
    }

    /// <summary>
    /// Filters a buffer in place.
    /// </summary>
    /// <param name="samples">The samples to filter.</param>
    public void Apply(float[] samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        for (int index = 0; index < samples.Length; index++)
        {
            float value = samples[index];

            for (int stage = 0; stage < _state.Length; stage++)
            {
                _state[stage] += _coefficient * (value - _state[stage]);
                value = _state[stage];
            }

            samples[index] = value;
        }
    }
}
