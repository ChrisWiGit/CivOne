using System;
using System.Collections.Generic;

namespace CivOne.Sound.Cvl;

#nullable enable

/// <summary>
/// Verbindet die Namen, mit denen die Spiellogik <c>PlaySound</c> aufruft, mit den
/// Tune-Nummern der CVL-Treiber.
///
/// Musik und Herrscherthemen lassen sich eindeutig zuordnen. Für die Effekte
/// (<c>cannon</c>, <c>s_beep</c>, …) ist die Tune-Nummer noch nicht bekannt; sie bleiben
/// hier absichtlich offen und werden beim Konvertieren als "nicht zugeordnet" gemeldet.
/// Die Zuordnung wird pro Pack in die <c>index.json</c> geschrieben und ist dort anpassbar.
/// </summary>
internal static class SoundNameMap
{
    private static readonly Dictionary<string, int> _defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["opening"] = 3,
        ["wintune"] = 34,
        ["lose2"] = 35,

        // Herrscherthemen – Reihenfolge entspricht den Tunes 5..18.
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

        // Noch ohne bekannte Tune-Nummer.
        "airnuke", "s_nuke", "s_land", "cannon", "they_die", "we_die", "s_beep"
    ];

    public static IReadOnlyDictionary<string, int> Defaults => _defaults;

    /// <summary>Alle Namen, die die Spiellogik derzeit an <c>PlaySound</c> übergibt.</summary>
    public static IReadOnlyList<string> EngineSoundNames => _engineSoundNames;

    public static bool TryGetTuneId(string soundName, out int tuneId)
        => _defaults.TryGetValue(soundName ?? string.Empty, out tuneId);
}
