using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl.Adlib;

/// <summary>
/// One playable version of a tune.
/// </summary>
/// <remarks>
/// Most tunes have exactly one arrangement. The leader themes ship four, and the original picks
/// between them with the second argument of <c>PlayTune</c>.
/// </remarks>
internal sealed class AdlibArrangement
{
    /// <summary>Gets or sets the voices that play together.</summary>
    public List<AdlibVoice> Voices { get; set; } = [];

    /// <summary>Gets the total number of events across all voices.</summary>
    [JsonIgnore]
    public int EventCount
    {
        get
        {
            int total = 0;
            foreach (AdlibVoice voice in Voices) total += voice.Events.Count;
            return total;
        }
    }
}
