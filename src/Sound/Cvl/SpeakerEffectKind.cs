namespace CivOne.Sound.Cvl;

/// <summary>
/// The kind of pitch modulation applied while a <see cref="TuneStep"/> plays.
/// </summary>
internal enum SpeakerEffectKind
{
    /// <summary>
    /// No modulation; the divisor stays fixed for the duration of the step.
    /// </summary>
    None,

    /// <summary>
    /// The divisor oscillates in <see cref="SpeakerEffect.Step"/> increments within
    /// ±<see cref="SpeakerEffect.Range"/> of its base value.
    /// </summary>
    Vibrato,

    /// <summary>
    /// <see cref="SpeakerEffect.Delta"/> is added to the divisor on every worker tick.
    /// The divisor is a 16-bit timer register, so the sum wraps around rather than being clamped.
    /// </summary>
    Slide
}
