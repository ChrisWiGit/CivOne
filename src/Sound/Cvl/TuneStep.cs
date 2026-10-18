using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl;

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
