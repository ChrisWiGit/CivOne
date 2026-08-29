using System;
using System.Collections.Generic;

namespace CivOne.Sound.Cvl;



/// <summary>
/// Links the names <c>PlaySound</c> can be called with to the tune numbers of the CVL drivers.
///
/// Music and leader themes can be mapped unambiguously. For four of the effects
/// (<c>cannon</c>, <c>s_land</c>, <c>they_die</c>, <c>we_die</c>) which tune id is which is
/// still open; they are deliberately left unmapped here and reported as "unmapped" during
/// conversion. The mapping is written into the <c>index.json</c> per pack and can be adjusted
/// there.
///
/// This is the reverse direction of <see cref="CvlTuneCatalog"/>: that class maps a tune id to
/// its display title, used when generating pack files. This class maps a name to the tune id
/// behind it. Some names are already hardcoded call sites (e.g. <c>"linc"</c>, used per
/// civilization in <c>Civilizations/*.cs</c>); others (the short leader jingles, the audience
/// sting, the alarm sting, the city view flourish) have no call site in the game logic yet and
/// use a name of this map's own choosing, reserved for when that call site is added. Adding a
/// new tune the game should ever play needs an entry in both: a title in
/// <see cref="CvlTuneCatalog"/>, and - once the tune id is known - a name here.
/// </summary>
internal static class SoundNameMap
{
    private static readonly Dictionary<string, int> _defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["opening"] = 3,
        ["evolution"] = 4,
        ["wintune"] = 34,
        ["win"] = 34, // VictoryScreen calls PlaySound("win") rather than "wintune".
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
        ["fred"] = 18,

        // Same leaders, short jingle – order matches tunes 19..32. No call site plays these yet
        // (Discovery.cs currently reuses the long theme above); names are this map's own
        // convention, not an original-game name.
        ["linc_short"] = 19,
        ["mont_short"] = 20,
        ["rams_short"] = 21,
        ["shak_short"] = 22,
        ["napo_short"] = 23,
        ["ceas_short"] = 24,
        ["stal_short"] = 25,
        ["alex_short"] = 26,
        ["eliz_short"] = 27,
        ["hama_short"] = 28,
        ["mao_short"] = 29,
        ["geng_short"] = 30,
        ["gand_short"] = 31,
        ["fred_short"] = 32,

        // No call site yet either; names are this map's own convention.
        ["audience"] = 33, // Sting for an audience with a foreign leader.
        ["alarm"] = 36,    // Famine, civil disorder, government overthrown, nuclear accident; also the barbarians' theme.
        ["cityview"] = 44, // Short flourish on opening the city view.

        // Effects identified from their code trigger rather than by ear (medium confidence,
        // see docs/CVL-ASOUND-AdLib.md "Open items").
        ["s_beep"] = 37,
        ["s_nuke"] = 42,
        ["airnuke"] = 43,

        // Combat outcome, called from BaseUnit.PlayAttackSound - see the confidence note there.
        // Names of this map's own choosing; which of cannon/s_land/they_die/we_die below (real
        // names extracted from the game data) each of these four actually is remains open.
        ["combat_win_weak"] = 38,
        ["combat_loss_weak"] = 39,
        ["combat_win_strong"] = 40,
        ["combat_loss_strong"] = 41
    };

    private static readonly string[] _engineSoundNames =
    [
        "opening", "evolution", "wintune", "win", "lose2",
        "linc", "mont", "rams", "shak", "napo", "ceas", "stal",
        "alex", "eliz", "hama", "mao", "geng", "gand", "fred",
        "linc_short", "mont_short", "rams_short", "shak_short", "napo_short", "ceas_short", "stal_short",
        "alex_short", "eliz_short", "hama_short", "mao_short", "geng_short", "gand_short", "fred_short",
        "audience", "alarm", "cityview",
        "s_beep", "s_nuke", "airnuke",
        "combat_win_weak", "combat_loss_weak", "combat_win_strong", "combat_loss_strong",

        // Real names extracted from the game data (tune ids 38..41, in some order), but no longer
        // called from game logic - BaseUnit.PlayAttackSound now calls the four combat_* names
        // above instead, see docs/CVL-ASOUND-AdLib.md "Open items".
        "s_land", "cannon", "they_die", "we_die"
    ];

    public static IReadOnlyDictionary<string, int> Defaults => _defaults;

    /// <summary>
    /// All names this map knows a tune id for, plus the effects still open. Includes names no
    /// call site passes to <c>PlaySound</c> yet - see the class remarks.
    /// </summary>
    public static IReadOnlyList<string> EngineSoundNames => _engineSoundNames;

    public static bool TryGetTuneId(string soundName, out int tuneId)
        => _defaults.TryGetValue(soundName ?? string.Empty, out tuneId);
}
