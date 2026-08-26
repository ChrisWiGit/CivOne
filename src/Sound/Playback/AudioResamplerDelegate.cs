using System;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Sound.Playback;



/// <summary>
/// Converts a buffer from one sample rate to another by linear interpolation.
/// </summary>
/// <remarks>
/// The emulated devices run at their own rates - 49716 Hz for the OPL chip, far higher for the PC
/// speaker's own timeline - while the output file has a fixed one. Low-pass filtering before this
/// step is what keeps the conversion clean, so this stage can stay simple.
/// </remarks>
internal sealed class AudioResamplerDelegate
{
    /// <summary>
    /// Resamples a buffer.
    /// </summary>
    /// <param name="samples">The samples to convert.</param>
    /// <param name="sourceRate">Rate the samples are at, in Hz.</param>
    /// <param name="targetRate">Rate to convert them to, in Hz.</param>
    /// <returns>The converted samples, or the input itself when the rates already match.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
    public float[] Resample(float[] samples, int sourceRate, int targetRate)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetRate);

        if (sourceRate == targetRate || samples.Length == 0) return samples;

        long length = (long)samples.Length * targetRate / sourceRate;
        if (length <= 0) return [];

        var result = new float[length];
        double step = sourceRate / (double)targetRate;

        for (int index = 0; index < result.Length; index++)
        {
            double position = index * step;
            int left = (int)position;
            if (left >= samples.Length - 1)
            {
                result[index] = samples[^1];
                continue;
            }

            float fraction = (float)(position - left);
            result[index] = samples[left] + ((samples[left + 1] - samples[left]) * fraction);
        }

        return result;
    }
}
