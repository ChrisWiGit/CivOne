using System.Collections.Generic;

namespace CivOne.Sound.Cvl;



/// <summary>
/// Known tune numbers of the CVL modules. CIVPLAY allows 3..44; only part of that range is named.
/// </summary>
internal static class CvlTuneCatalog
{
    private static readonly Dictionary<int, string> _titles = new()
    {
        [3] = "Title Music",
        [4] = "Evolution Music",
        [5] = "Lincoln (Long)",
        [6] = "Montezuma (Long)",
        [7] = "Ramesses (Long)",
        [8] = "Shaka Zulu (Long)",
        [9] = "Napoleon (Long)",
        [10] = "Caesar (Long)",
        [11] = "Stalin (Long)",
        [12] = "Alexander the Great (Long)",
        [13] = "Elizabeth (Long)",
        [14] = "Hammurabi (Long)",
        [15] = "Mao (Long)",
        [16] = "Genghis Khan (Long)",
        [17] = "Gandhi (Long)",
        [18] = "Frederick (Long)",
        [19] = "Lincoln (Short)",
        [20] = "Montezuma (Short)",
        [21] = "Ramesses (Short)",
        [22] = "Shaka Zulu (Short)",
        [23] = "Napoleon (Short)",
        [24] = "Caesar (Short)",
        [25] = "Stalin (Short)",
        [26] = "Alexander the Great (Short)",
        [27] = "Elizabeth (Short)",
        [28] = "Hammurabi (Short)",
        [29] = "Mao (Short)",
        [30] = "Genghis Khan (Short)",
        [31] = "Gandhi (Short)",
        [32] = "Frederick (Short)",
        [33] = "Foreign Leader Audience Sting",
        [34] = "Win Music",
        [35] = "Lose Music",
        [36] = "Alarm - Barbarian Theme",
        [37] = "Unit Arrived",
        [38] = "Combat Outcome 1",
        [39] = "Combat Outcome 2",
        [40] = "Combat Outcome 3",
        [41] = "Combat Outcome 4",
        [42] = "Nuclear Meltdown",
        [43] = "Bomber Shot Down",
        [44] = "City View Opened"
    };

    /// <summary>Tune ids that are music (as opposed to short sound effects), used to classify a tune's <c>Kind</c>.</summary>
    private static readonly HashSet<int> _musicTuneIds = new()
    {
        3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18,
        19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
        33, 34, 35, 36
    };

    /// <summary>First tune number addressable by the host.</summary>
    public const int FirstPlayableTuneId = 3;

    /// <summary>Last tune number addressable by the host.</summary>
    public const int LastPlayableTuneId = 44;

    public static string ResolveTitle(int tuneId)
        => _titles.TryGetValue(tuneId, out string? title) ? title : $"Tune {tuneId}";

    /// <summary>
    /// Gets whether the tune number is a music piece (title, evolution, leader themes long and
    /// short, win, lose, the foreign-audience sting and the barbarian/alarm theme) rather than a
    /// short sound effect.
    /// </summary>
    /// <param name="tuneId">The tune number to check.</param>
    /// <returns><c>true</c> when the tune is classified as music.</returns>
    public static bool IsNamedTune(int tuneId) => _musicTuneIds.Contains(tuneId);

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
