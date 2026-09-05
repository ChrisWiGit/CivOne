using System;
using System.Diagnostics.CodeAnalysis;

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
    /// <summary>Level above which the limiter starts to compress instead of passing through.</summary>
    private const float Knee = 0.70f;

    private const float Headroom = 1f - Knee;
    private const short FullScale = short.MaxValue;

    /// <summary>
    /// Applies gain and limiting and converts to 16-bit samples.
    /// </summary>
    /// <param name="samples">The samples to convert.</param>
    /// <param name="gain">Gain to apply before limiting.</param>
    /// <returns>The converted samples.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
    public short[] ToPcm16(float[] samples, float gain)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var result = new short[samples.Length];

        for (int index = 0; index < samples.Length; index++)
        {
            float value = Limit(samples[index] * gain);
            result[index] = (short)Math.Round(value * FullScale);
        }

        return result;
    }

    /// <summary>
    /// Passes quiet samples through untouched and eases loud ones towards full scale, so a busy
    /// passage gets quieter rather than square.
    /// </summary>
    private static float Limit(float value)
    {
        float magnitude = Math.Abs(value);
        if (magnitude <= Knee) return value;

        float excess = (magnitude - Knee) / Headroom;
        float limited = Knee + (Headroom * MathF.Tanh(excess));

        return value < 0f ? -limited : limited;
    }
}
