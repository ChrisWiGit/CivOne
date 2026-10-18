namespace CivOne.Sound.Cvl;

/// <summary>
/// Decoded slide/vibrato parameter (<c>ds:0x6F</c> in the driver).
/// <para>
/// High nibble <c>8</c> means vibrato (low byte = range, middle nibble = step size);
/// any other value is an addition applied to the divisor.
/// </para>
/// <para>
/// The driver's own table holds entries such as <c>0xD204</c> that look like vibrato but do not
/// have the high nibble the check demands, so they act as very large additions. They are the
/// percussion of the tunes and must not be mistaken for a small downward slide: read as a signed
/// delta they would push the divisor below zero and silence the note.
/// </para>
/// </summary>
internal readonly record struct SpeakerEffect(SpeakerEffectKind Kind, int Range, int Step, int Delta, int Raw)
{
    /// <summary>
    /// Decodes the raw 16-bit slide/vibrato word into a <see cref="SpeakerEffect"/>.
    /// </summary>
    /// <param name="raw">Raw word as read from the driver's effect field.</param>
    /// <returns>
    /// <see cref="SpeakerEffectKind.None"/> for a raw value of zero, <see cref="SpeakerEffectKind.Vibrato"/>
    /// when the high nibble is <c>8</c>, otherwise <see cref="SpeakerEffectKind.Slide"/> with the remaining
    /// bits interpreted as a signed delta.
    /// </returns>
    public static SpeakerEffect Decode(int raw)
    {
        int word = raw & 0xFFFF;
        if (word == 0) return new SpeakerEffect(SpeakerEffectKind.None, 0, 0, 0, 0);

        if ((word & 0xF000) == 0x8000)
            return new SpeakerEffect(SpeakerEffectKind.Vibrato, word & 0xFF, (word >> 8) & 0x0F, 0, word);

        return new SpeakerEffect(SpeakerEffectKind.Slide, 0, 0, (short)word, word);
    }
}
