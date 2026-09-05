using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Sound.Playback;

/// <summary>
/// Names the wave files an older CivOne, and the Windows version of the original game, used for a
/// sound.
/// </summary>
/// <remarks>
/// <para>
/// CivOne played wave files taken from the Windows release of the original game, addressed by that
/// release's own file names - <c>OPENING.WAV</c>, <c>LINC.WAV</c>, <c>THEY_DIE.WAV</c>; see
/// <c>FileSystem.SOUND_FILES</c> for the ones that release ships. Those names describe the file,
/// not the situation, so they are no longer what the game asks for. Every name an older CivOne
/// asked for lives on here as a fall-back file name, which keeps an existing collection of wave
/// files working unchanged.
/// </para>
/// <para>
/// The combat names are an approximation, not a translation. The Windows release picked its combat
/// sound by the unit type and played the same file whether that unit won or lost, while the DOS
/// drivers pick by outcome and by how strong the deciding unit is. The two schemes do not map onto
/// each other, so <c>s_land</c> is offered for the two "weak" outcomes - the one group both schemes
/// share, Musketeers and Riflemen - and <c>they_die</c> / <c>we_die</c> carry the win/loss
/// distinction. <c>cannon</c> has no counterpart at all and is not offered; a plugin alias can
/// still reach it.
/// </para>
/// </remarks>
internal sealed class LegacyWaveNameDelegate
{
    private static readonly Dictionary<string, string[]> _legacyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        [SoundNames.MusicTitle] = ["opening"],
        [SoundNames.MusicEvolution] = ["evolution"],
        [SoundNames.MusicWin] = ["wintune", "win"],
        [SoundNames.MusicLose] = ["lose2"],

        [SoundNames.LeaderLincoln] = ["linc"],
        [SoundNames.LeaderMontezuma] = ["mont"],
        [SoundNames.LeaderRamesses] = ["rams"],
        [SoundNames.LeaderShaka] = ["shak"],
        [SoundNames.LeaderNapoleon] = ["napo"],
        [SoundNames.LeaderCaesar] = ["ceas"],
        [SoundNames.LeaderStalin] = ["stal"],
        [SoundNames.LeaderAlexander] = ["alex"],
        [SoundNames.LeaderElizabeth] = ["eliz"],
        [SoundNames.LeaderHammurabi] = ["hama"],
        [SoundNames.LeaderMao] = ["mao"],
        [SoundNames.LeaderGenghis] = ["geng"],
        [SoundNames.LeaderGandhi] = ["gand"],
        [SoundNames.LeaderFrederick] = ["fred"],

        [SoundNames.LeaderLincolnShort] = ["linc_short"],
        [SoundNames.LeaderMontezumaShort] = ["mont_short"],
        [SoundNames.LeaderRamessesShort] = ["rams_short"],
        [SoundNames.LeaderShakaShort] = ["shak_short"],
        [SoundNames.LeaderNapoleonShort] = ["napo_short"],
        [SoundNames.LeaderCaesarShort] = ["ceas_short"],
        [SoundNames.LeaderStalinShort] = ["stal_short"],
        [SoundNames.LeaderAlexanderShort] = ["alex_short"],
        [SoundNames.LeaderElizabethShort] = ["eliz_short"],
        [SoundNames.LeaderHammurabiShort] = ["hama_short"],
        [SoundNames.LeaderMaoShort] = ["mao_short"],
        [SoundNames.LeaderGenghisShort] = ["geng_short"],
        [SoundNames.LeaderGandhiShort] = ["gand_short"],
        [SoundNames.LeaderFrederickShort] = ["fred_short"],

        [SoundNames.EventAudience] = ["audience"],
        [SoundNames.EventAlarm] = ["alarm"],
        [SoundNames.EventCityViewOpened] = ["cityview"],
        [SoundNames.EventNuclearBlast] = ["s_nuke"],
        [SoundNames.UiBeep] = ["s_beep"],

        [SoundNames.CombatAirStrike] = ["airnuke"],
        [SoundNames.CombatWinWeak] = ["s_land", "they_die"],
        [SoundNames.CombatLossWeak] = ["s_land", "we_die"],
        [SoundNames.CombatWinStrong] = ["they_die"],
        [SoundNames.CombatLossStrong] = ["we_die"]
    };

    /// <summary>
    /// Lists the file names to look for when no file carries the sound name itself.
    /// </summary>
    /// <param name="soundName">The name the game logic asked for.</param>
    /// <returns>
    /// The fall-back file names without extension, most fitting first. Empty for a name that never
    /// had a wave file, which includes every name a plugin brings.
    /// </returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Delegate members stay instance members so the delegate can be replaced.")]
    public IReadOnlyList<string> Candidates(string soundName)
        => soundName != null && _legacyNames.TryGetValue(soundName, out string[]? names) ? names : [];
}
