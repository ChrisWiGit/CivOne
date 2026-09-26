using System;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Sound.Dsp;

/// <summary>
/// Keeps a summed signal inside the output range without turning loud passages into square waves.
/// </summary>
/// <remarks>
/// Samples below the knee pass through untouched, so ordinary material is not coloured at all.
/// Above it the excess is eased towards full scale, which makes a busy passage quieter rather than
/// distorted.
/// <para>
/// Used both by the offline render and by the runtime mixer, so both stay at the same loudness.
/// </para>
/// </remarks>
internal sealed class SoftLimiterDelegate
{
	/// <summary>Level above which the limiter starts to compress instead of passing through.</summary>
	public const float Knee = 0.70f;

	private const float Headroom = 1f - Knee;

	/// <summary>
	/// Limits one sample.
	/// </summary>
	/// <param name="value">The sample to limit.</param>
	/// <returns>The sample, eased towards full scale when it is above the knee.</returns>
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
	public float Limit(float value)
	{
		float magnitude = Math.Abs(value);
		if (magnitude <= Knee) return value;

		float excess = (magnitude - Knee) / Headroom;
		float limited = Knee + (Headroom * MathF.Tanh(excess));

		return value < 0f ? -limited : limited;
	}

	/// <summary>
	/// Limits a whole buffer in place.
	/// </summary>
	/// <param name="buffer">The samples to limit.</param>
	public void Limit(Span<float> buffer)
	{
		for (int index = 0; index < buffer.Length; index++)
		{
			buffer[index] = Limit(buffer[index]);
		}
	}
}
