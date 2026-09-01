using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CivOne.Sound.Cvl;

/// <summary>
/// One tune of a CVL driver: its number in the driver, the name the game plays it by, and how it
/// behaves.
/// </summary>
/// <param name="TuneId">Number the CVL dispatch table addresses this tune by.</param>
/// <param name="Name">Name from <see cref="SoundNames"/> that plays this tune.</param>
/// <param name="Title">English display title, shown in the sound test.</param>
/// <param name="IsMusic">Whether this is a music piece rather than a short sound effect.</param>
/// <param name="EndlessLoop">Whether the tune repeats instead of ending.</param>
internal sealed record CvlTuneDefinition(int TuneId, string Name, string Title, bool IsMusic, bool EndlessLoop);

/// <summary>
/// Everything known about the tunes of the CVL modules. CIVPLAY allows tune numbers 3..44; this
/// table names the ones we have identified.
/// </summary>
/// <remarks>
/// <para>
/// The tune number only exists inside the CVL modules, where it is the index into the driver's
/// dispatch table. It is resolved to a name here, at the boundary between reading a module and
/// writing a sound pack; everything after that - pack index, file names, the wave cache, playback -
/// works on names alone.
/// </para>
/// <para>
/// None of the names or titles below are read from the modules. The modules contain numbers and
/// note data, no strings at all. Every entry here comes from analysing where the game calls a tune
/// from; see <c>docs/CVL-ASOUND-AdLib.md</c> for how certain each one is.
/// </para>
/// </remarks>
internal static class CvlTuneCatalog
{
    private static readonly CvlTuneDefinition[] _tunes =
    [
        new(3, SoundNames.MusicTitle, "Title Music", IsMusic: true, EndlessLoop: true),
        new(4, SoundNames.MusicEvolution, "Evolution Music", IsMusic: true, EndlessLoop: true),

        new(5, SoundNames.LeaderLincoln, "Lincoln (Long)", IsMusic: true, EndlessLoop: false),
        new(6, SoundNames.LeaderMontezuma, "Montezuma (Long)", IsMusic: true, EndlessLoop: false),
        new(7, SoundNames.LeaderRamesses, "Ramesses (Long)", IsMusic: true, EndlessLoop: false),
        new(8, SoundNames.LeaderShaka, "Shaka Zulu (Long)", IsMusic: true, EndlessLoop: false),
        new(9, SoundNames.LeaderNapoleon, "Napoleon (Long)", IsMusic: true, EndlessLoop: false),
        new(10, SoundNames.LeaderCaesar, "Caesar (Long)", IsMusic: true, EndlessLoop: false),
        new(11, SoundNames.LeaderStalin, "Stalin (Long)", IsMusic: true, EndlessLoop: false),
        new(12, SoundNames.LeaderAlexander, "Alexander the Great (Long)", IsMusic: true, EndlessLoop: false),
        new(13, SoundNames.LeaderElizabeth, "Elizabeth (Long)", IsMusic: true, EndlessLoop: false),
        new(14, SoundNames.LeaderHammurabi, "Hammurabi (Long)", IsMusic: true, EndlessLoop: false),
        new(15, SoundNames.LeaderMao, "Mao (Long)", IsMusic: true, EndlessLoop: false),
        new(16, SoundNames.LeaderGenghis, "Genghis Khan (Long)", IsMusic: true, EndlessLoop: false),
        new(17, SoundNames.LeaderGandhi, "Gandhi (Long)", IsMusic: true, EndlessLoop: false),
        new(18, SoundNames.LeaderFrederick, "Frederick (Long)", IsMusic: true, EndlessLoop: false),

        new(19, SoundNames.LeaderLincolnShort, "Lincoln (Short)", IsMusic: true, EndlessLoop: false),
        new(20, SoundNames.LeaderMontezumaShort, "Montezuma (Short)", IsMusic: true, EndlessLoop: false),
        new(21, SoundNames.LeaderRamessesShort, "Ramesses (Short)", IsMusic: true, EndlessLoop: false),
        new(22, SoundNames.LeaderShakaShort, "Shaka Zulu (Short)", IsMusic: true, EndlessLoop: false),
        new(23, SoundNames.LeaderNapoleonShort, "Napoleon (Short)", IsMusic: true, EndlessLoop: false),
        new(24, SoundNames.LeaderCaesarShort, "Caesar (Short)", IsMusic: true, EndlessLoop: false),
        new(25, SoundNames.LeaderStalinShort, "Stalin (Short)", IsMusic: true, EndlessLoop: false),
        new(26, SoundNames.LeaderAlexanderShort, "Alexander the Great (Short)", IsMusic: true, EndlessLoop: false),
        new(27, SoundNames.LeaderElizabethShort, "Elizabeth (Short)", IsMusic: true, EndlessLoop: false),
        new(28, SoundNames.LeaderHammurabiShort, "Hammurabi (Short)", IsMusic: true, EndlessLoop: false),
        new(29, SoundNames.LeaderMaoShort, "Mao (Short)", IsMusic: true, EndlessLoop: false),
        new(30, SoundNames.LeaderGenghisShort, "Genghis Khan (Short)", IsMusic: true, EndlessLoop: false),
        new(31, SoundNames.LeaderGandhiShort, "Gandhi (Short)", IsMusic: true, EndlessLoop: false),
        new(32, SoundNames.LeaderFrederickShort, "Frederick (Short)", IsMusic: true, EndlessLoop: false),

        new(33, SoundNames.EventAudience, "Foreign Leader Audience Sting", IsMusic: true, EndlessLoop: false),
        new(34, SoundNames.MusicWin, "Win Music", IsMusic: true, EndlessLoop: false),
        new(35, SoundNames.MusicLose, "Lose Music", IsMusic: true, EndlessLoop: false),
        new(36, SoundNames.EventAlarm, "Alarm - Barbarian Theme", IsMusic: true, EndlessLoop: false),

        // The only call site is the error message beep. The same tune may well be the original's
        // "unit arrived" cue, which CivOne has no trigger for yet.
        new(37, SoundNames.UiBeep, "Beep", IsMusic: false, EndlessLoop: false),

        new(38, SoundNames.CombatWinWeak, "Combat Win (Weak Unit)", IsMusic: false, EndlessLoop: false),
        new(39, SoundNames.CombatLossWeak, "Combat Loss (Weak Unit)", IsMusic: false, EndlessLoop: false),
        new(40, SoundNames.CombatWinStrong, "Combat Win (Strong Unit)", IsMusic: false, EndlessLoop: false),
        new(41, SoundNames.CombatLossStrong, "Combat Loss (Strong Unit)", IsMusic: false, EndlessLoop: false),

        new(42, SoundNames.EventNuclearBlast, "Nuclear Blast", IsMusic: false, EndlessLoop: false),
        new(43, SoundNames.CombatAirStrike, "Air Strike", IsMusic: false, EndlessLoop: false),
        new(44, SoundNames.EventCityViewOpened, "City View Opened", IsMusic: false, EndlessLoop: false)
    ];

    /// <summary>
    /// Tunes that should be rendered before the rest, because the game asks for them first or
    /// cannot wait for them.
    /// </summary>
    private static readonly int[] _warmUpFirst = [3, 4, 34, 35];

    private static Dictionary<int, CvlTuneDefinition>? _byTuneId;
    private static Dictionary<string, CvlTuneDefinition>? _byName;
    private static string[]? _warmUpOrder;

    /// <summary>First tune number addressable by the host.</summary>
    public const int FirstPlayableTuneId = 3;

    /// <summary>Last tune number addressable by the host.</summary>
    public const int LastPlayableTuneId = 44;

    /// <summary>All identified tunes, ordered by tune number.</summary>
    public static IReadOnlyList<CvlTuneDefinition> Tunes => _tunes;

    private static Dictionary<int, CvlTuneDefinition> ByTuneId
        => _byTuneId ??= _tunes.ToDictionary(tune => tune.TuneId);

    private static Dictionary<string, CvlTuneDefinition> ByName
        => _byName ??= _tunes.ToDictionary(tune => tune.Name, System.StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The names of all identified tunes, in the order their wave files should be rendered.
    /// </summary>
    /// <remarks>
    /// A whole pack takes a while to render, so the order matters. The title and evolution music
    /// are needed before anything else, and the win and lose music are long enough that starting
    /// them late is noticeable; the rest follows by tune number.
    /// </remarks>
    public static IReadOnlyList<string> WarmUpOrder
        => _warmUpOrder ??=
        [
            .. _warmUpFirst.Select(ResolveName),
            .. _tunes.Where(tune => !_warmUpFirst.Contains(tune.TuneId)).Select(tune => tune.Name)
        ];

    /// <summary>Finds a tune by its number.</summary>
    /// <param name="tuneId">The tune number.</param>
    /// <returns>The definition, or <c>null</c> when the number is not identified.</returns>
    public static CvlTuneDefinition? Find(int tuneId)
        => ByTuneId.TryGetValue(tuneId, out CvlTuneDefinition? tune) ? tune : null;

    /// <summary>Finds a tune by the name it is played with.</summary>
    /// <param name="name">The sound name.</param>
    /// <returns>The definition, or <c>null</c> when no tune carries that name.</returns>
    public static CvlTuneDefinition? Find(string name)
        => name != null && ByName.TryGetValue(name, out CvlTuneDefinition? tune) ? tune : null;

    /// <summary>
    /// Gets the name a tune number is played by.
    /// </summary>
    /// <param name="tuneId">The tune number.</param>
    /// <returns>
    /// The name from <see cref="SoundNames"/>, or a generated <c>tune_&lt;id&gt;</c> for a number we
    /// have not identified. The generated name still carries the number, so such a tune stays
    /// traceable back to the module.
    /// </returns>
    public static string ResolveName(int tuneId)
        => Find(tuneId)?.Name ?? string.Create(CultureInfo.InvariantCulture, $"tune_{tuneId}");

    /// <summary>Gets the display title of a tune number.</summary>
    /// <param name="tuneId">The tune number.</param>
    /// <returns>The English title, or a generated one for an unidentified number.</returns>
    public static string ResolveTitle(int tuneId)
        => Find(tuneId)?.Title ?? string.Create(CultureInfo.InvariantCulture, $"Tune {tuneId}");

    /// <summary>
    /// Gets whether the tune number is a music piece (title, evolution, leader themes long and
    /// short, win, lose, the foreign-audience sting and the barbarian/alarm theme) rather than a
    /// short sound effect.
    /// </summary>
    /// <param name="tuneId">The tune number to check.</param>
    /// <returns><c>true</c> when the tune is classified as music.</returns>
    public static bool IsNamedTune(int tuneId) => Find(tuneId)?.IsMusic ?? false;

    /// <summary>Gets whether the tune repeats indefinitely in the original.</summary>
    /// <param name="tuneId">The tune number to check.</param>
    /// <returns><c>true</c> for the title and evolution music.</returns>
    public static bool IsEndlessLoop(int tuneId) => Find(tuneId)?.EndlessLoop ?? false;

    /// <summary>Every tune number the host may ask a driver for.</summary>
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
