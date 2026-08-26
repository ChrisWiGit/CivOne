using System;
using System.Collections.Generic;

namespace CivOne.Sound.Cvl;



/// <summary>
/// Links the names the game logic uses to call <c>PlaySound</c> with the tune numbers of
/// the CVL drivers.
///
/// Music and leader themes can be mapped unambiguously. For the effects
/// (<c>cannon</c>, <c>s_beep</c>, …) the tune number is not yet known; they are deliberately
/// left open here and reported as "unmapped" during conversion. The mapping is written into
/// the <c>index.json</c> per pack and can be adjusted there.
/// </summary>
internal static class SoundNameMap
{
    private static readonly Dictionary<string, int> _defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["opening"] = 3,
        ["wintune"] = 34,
        ["lose2"] = 35,

        // Leader themes – order matches tunes 5..18.
        ["linc"] = 5,
        ["mont"] = 6,
        ["rams"] = 7,
        ["shak"] = 8,
        ["napo"] = 9,
        ["ceas"] = 10,
        ["stal"] = 11,
        ["alex"] = 12,
        ["eliz"] = 13,
        ["hama"] = 14,
        ["mao"] = 15,
        ["geng"] = 16,
        ["gand"] = 17,
        ["fred"] = 18
    };

    private static readonly string[] _engineSoundNames =
    [
        "opening", "wintune", "lose2",
        "linc", "mont", "rams", "shak", "napo", "ceas", "stal",
        "alex", "eliz", "hama", "mao", "geng", "gand", "fred",

        // Not yet mapped to a known tune number.
        "airnuke", "s_nuke", "s_land", "cannon", "they_die", "we_die", "s_beep"
    ];

    public static IReadOnlyDictionary<string, int> Defaults => _defaults;

    /// <summary>All names the game logic currently passes to <c>PlaySound</c>.</summary>
    public static IReadOnlyList<string> EngineSoundNames => _engineSoundNames;

    public static bool TryGetTuneId(string soundName, out int tuneId)
        => _defaults.TryGetValue(soundName ?? string.Empty, out tuneId);
}
