using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl;

/// <summary>
/// How a tune is realized inside the driver.
/// </summary>
internal enum TuneScoreKind
{
    /// <summary>
    /// The handler is not a tune sequence (stop, status query, special-case logic).
    /// </summary>
    Unsupported,

    /// <summary>
    /// The handler returns immediately - the tune is deliberately empty in the driver.
    /// </summary>
    Silent,

    /// <summary>
    /// Music sequence: 4-byte records of {timbre, duration, PIT divisor}.
    /// </summary>
    Music,

    /// <summary>
    /// Effect sequence: 10-byte records with their own noise mask and slide parameters.
    /// </summary>
    Effect
}

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

/// <summary>
/// One step of the sequence: a tone or a rest of fixed length.
/// </summary>
/// <remarks>
/// Deliberately driver-independent - the PC speaker can be rendered from this data alone,
/// without reading the CVL again.
/// </remarks>
internal sealed class TuneStep
{
    /// <summary>
    /// Length in worker ticks (see <see cref="TuneScorePack.WorkerTickHz"/>).
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// PIT channel-2 divisor. <c>0</c> means a rest (speaker gate closed).
    /// </summary>
    public int Divisor { get; set; }

    /// <summary>
    /// Timbre/priority code from the record; selects the effect used by the driver.
    /// </summary>
    public int Timbre { get; set; }

    /// <summary>
    /// Mask for the noise LFSR: <c>1</c> for music, taken from the record for effects.
    /// </summary>
    public int NoiseMask { get; set; }

    /// <summary>
    /// Raw slide/vibrato word, see <see cref="SpeakerEffect.Decode"/>.
    /// </summary>
    public int Effect { get; set; }

    /// <summary>
    /// Gets whether this step is a rest, i.e. <see cref="Divisor"/> is zero.
    /// </summary>
    [JsonIgnore]
    public bool IsRest => Divisor == 0;

    /// <summary>
    /// Gets the decoded slide/vibrato parameter for <see cref="Effect"/>.
    /// </summary>
    [JsonIgnore]
    public SpeakerEffect DecodedEffect => SpeakerEffect.Decode(Effect);

    /// <summary>
    /// Computes the tone frequency in Hz for the given PIT clock rate.
    /// </summary>
    /// <param name="pitClockHz">PIT clock frequency in Hz (see <see cref="TuneScorePack.PitClockHz"/>).</param>
    /// <returns><c>0</c> for a rest, otherwise <paramref name="pitClockHz"/> divided by <see cref="Divisor"/>.</returns>
    public double FrequencyHz(int pitClockHz)
        => Divisor <= 0 ? 0d : pitClockHz / (double)Divisor;
}

/// <summary>
/// A single PC speaker tune, as stored in one <c>*.sound.json</c>.
/// </summary>
/// <remarks>
/// Everything the whole pack shares - who it came from and how fast its clocks ran - lives in the
/// pack's <see cref="SoundPackIndex"/>, not in here.
/// </remarks>
internal sealed class TuneScore
{
    /// <summary>
    /// Gets or sets the schema version of this file. It matches
    /// <see cref="SoundPackIndex.CurrentSchemaVersion"/>.
    /// </summary>
    public int SchemaVersion { get; set; } = SoundPackIndex.CurrentSchemaVersion;

    /// <summary>
    /// Gets or sets the numeric tune id, as used by <c>PlaySound</c>.
    /// </summary>
    public int TuneId { get; set; }

    /// <summary>
    /// Gets or sets the display title of the tune.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets how this tune is realized in the driver.
    /// </summary>
    public TuneScoreKind Kind { get; set; }

    /// <summary>
    /// Gets or sets whether the tune loops indefinitely instead of ending after its last step.
    /// </summary>
    public bool EndlessLoop { get; set; }

    /// <summary>
    /// Data-segment offset of the sequence in the source file (kept only for traceability).
    /// </summary>
    public int SourceOffset { get; set; }

    /// <summary>
    /// Gets or sets the ordered list of tones and rests that make up the tune.
    /// </summary>
    public List<TuneStep> Steps { get; set; } = [];

    /// <summary>
    /// Gets the total duration of the tune in worker ticks, i.e. the sum of all <see cref="Steps"/> durations.
    /// </summary>
    [JsonIgnore]
    public int TotalTicks
    {
        get
        {
            int total = 0;
            foreach (var step in Steps) total += step.Duration;
            return total;
        }
    }
}
