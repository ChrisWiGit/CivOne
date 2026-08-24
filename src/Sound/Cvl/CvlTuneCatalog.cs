using System.Collections.Generic;

namespace CivOne.Sound.Cvl;

#nullable enable

/// <summary>
/// Known tune numbers of the CVL modules. CIVPLAY allows 3..44; only part of that range is named.
/// </summary>
internal static class CvlTuneCatalog
{
    private static readonly Dictionary<int, string> _titles = new()
    {
        [3] = "Title Music",
        [4] = "Evolution Music",
        [5] = "Lincoln",
        [6] = "Montezuma",
        [7] = "Ramses",
        [8] = "Shaka Zulu",
        [9] = "Napoleon",
        [10] = "Caesar",
        [11] = "Stalin",
        [12] = "Alexander the Great",
        [13] = "Elizabeth",
        [14] = "Hammurabi",
        [15] = "Mao",
        [16] = "Genghis Khan",
        [17] = "Gandhi",
        [18] = "Frederick",
        [34] = "Win Music",
        [35] = "Lose Music"
    };

    /// <summary>First tune number addressable by the host.</summary>
    public const int FirstPlayableTuneId = 3;

    /// <summary>Last tune number addressable by the host.</summary>
    public const int LastPlayableTuneId = 44;

    public static string ResolveTitle(int tuneId)
        => _titles.TryGetValue(tuneId, out string? title) ? title : $"Tune {tuneId}";

    /// <summary>Tunes that loop indefinitely in the original (title and evolution music).</summary>
    public static bool IsEndlessLoop(int tuneId) => tuneId is 3 or 4;

    public static IEnumerable<int> PlayableTuneIds
    {
        get
        {
            for (int tuneId = FirstPlayableTuneId; tuneId <= LastPlayableTuneId; tuneId++)
            {
                yield return tuneId;
            }
        }
    }
}
