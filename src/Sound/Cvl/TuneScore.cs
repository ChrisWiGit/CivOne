using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl;

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
