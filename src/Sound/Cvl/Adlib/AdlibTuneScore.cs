using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl.Adlib;

/// <summary>
/// A single tune extracted from the AdLib driver, as stored in one <c>*.sound.json</c>.
/// </summary>
internal sealed class AdlibTuneScore
{
    /// <summary>
    /// Gets or sets the schema version of this file. It matches
    /// <see cref="SoundPackIndex.CurrentSchemaVersion"/>.
    /// </summary>
    public int SchemaVersion { get; set; } = SoundPackIndex.CurrentSchemaVersion;

    /// <summary>Gets or sets the numeric tune id, as used by <c>PlaySound</c>.</summary>
    public int TuneId { get; set; }

    /// <summary>Gets or sets the display title of the tune.</summary>
    public required string Title { get; set; }

    /// <summary>Gets or sets how the driver realizes this tune.</summary>
    public TuneScoreKind Kind { get; set; }

    /// <summary>Gets or sets whether the tune repeats instead of ending after its last event.</summary>
    public bool EndlessLoop { get; set; }

    /// <summary>
    /// Gets or sets a note about anything in the handler that could not be reproduced, or <c>null</c>.
    /// </summary>
    public string? Diagnostic { get; set; }

    /// <summary>Gets or sets the interchangeable arrangements of this tune.</summary>
    public List<AdlibArrangement> Arrangements { get; set; } = [];

    /// <summary>Gets the total number of events of the first arrangement.</summary>
    [JsonIgnore]
    public int EventCount => Arrangements.Count == 0 ? 0 : Arrangements[0].EventCount;

    /// <summary>
    /// Gets the length of the longest voice of the first arrangement in sequencer ticks.
    /// Loops are not followed, so this is a lower bound on the real playing time.
    /// </summary>
    [JsonIgnore]
    public int TotalTicks
    {
        get
        {
            if (Arrangements.Count == 0) return 0;

            int longest = 0;
            foreach (AdlibVoice voice in Arrangements[0].Voices)
            {
                int ticks = 0;
                foreach (AdlibEvent decoded in voice.Events) ticks += decoded.Duration;
                if (ticks > longest) longest = ticks;
            }

            return longest;
        }
    }
}
