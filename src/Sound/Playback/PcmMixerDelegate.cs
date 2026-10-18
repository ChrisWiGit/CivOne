using System;
using CivOne.Sound.Dsp;

namespace CivOne.Sound.Playback;



/// <summary>
/// The last stage before a file is written: applies the device's gain, keeps loud passages inside
/// the output range and converts to 16-bit samples.
/// </summary>
/// <remarks>
/// An emulated device produces whatever its channels happen to add up to, which for nine FM voices
/// is well past full scale. Deciding how loud that should come out is a mixing question, not a
/// question for the chip, so it is settled here.
/// </remarks>
internal sealed class PcmMixerDelegate
{
    private const short FullScale = short.MaxValue;

    private readonly SoftLimiterDelegate _limiter = new();

    /// <summary>
    /// Applies gain and limiting and converts to 16-bit samples.
    /// </summary>
    /// <param name="samples">The samples to convert.</param>
    /// <param name="gain">Gain to apply before limiting.</param>
    /// <returns>The converted samples.</returns>
    public short[] ToPcm16(float[] samples, float gain)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var result = new short[samples.Length];

        for (int index = 0; index < samples.Length; index++)
        {
            float value = _limiter.Limit(samples[index] * gain);
            result[index] = (short)Math.Round(value * FullScale);
        }

        return result;
    }
}
