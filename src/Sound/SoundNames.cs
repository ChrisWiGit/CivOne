namespace CivOne.Sound;

/// <summary>
/// The names <c>PlaySound</c> understands.
///
/// A name describes the <em>situation</em> the game is in, not the sound that is heard for it -
/// <see cref="CombatWinStrong"/> rather than "cannon". Which tune or wave file a name ends up
/// playing is decided by whatever is playing it: a converted sound pack looks the name up in its
/// index, the wave playback looks for a file of that name.
///
/// Plugins may pass any string they like. A name nobody knows is not an error: it simply falls
/// through to the wave lookup, so a plugin that ships <c>my_sound.wav</c> can play it without
/// registering anything.
/// </summary>
/// <remarks>
/// <para>
/// The values are frozen. They are file names on disk and they are compiled into plugin
/// assemblies, so changing one silently breaks both. Add names, never rename them.
/// </para>
/// <para>
/// Names are lower case with <c>_</c> as the only separator, so every one of them is usable as a
/// file name unchanged.
/// </para>
/// </remarks>
public static class SoundNames
{
    /// <summary>Title music, plays on the opening screen and in the credits. Loops.</summary>
    public const string MusicTitle = "music_title";

    /// <summary>Music of the evolution intro. Loops.</summary>
    public const string MusicEvolution = "music_evolution";

    /// <summary>Music for a won game.</summary>
    public const string MusicWin = "music_win";

    /// <summary>Music for a lost game.</summary>
    public const string MusicLose = "music_lose";

    /// <summary>Leader theme of the Americans.</summary>
    public const string LeaderLincoln = "leader_lincoln";

    /// <summary>Leader theme of the Aztecs.</summary>
    public const string LeaderMontezuma = "leader_montezuma";

    /// <summary>Leader theme of the Egyptians.</summary>
    public const string LeaderRamesses = "leader_ramesses";

    /// <summary>Leader theme of the Zulus.</summary>
    public const string LeaderShaka = "leader_shaka";

    /// <summary>Leader theme of the French.</summary>
    public const string LeaderNapoleon = "leader_napoleon";

    /// <summary>Leader theme of the Romans.</summary>
    public const string LeaderCaesar = "leader_caesar";

    /// <summary>Leader theme of the Russians.</summary>
    public const string LeaderStalin = "leader_stalin";

    /// <summary>Leader theme of the Greeks.</summary>
    public const string LeaderAlexander = "leader_alexander";

    /// <summary>Leader theme of the English.</summary>
    public const string LeaderElizabeth = "leader_elizabeth";

    /// <summary>Leader theme of the Babylonians.</summary>
    public const string LeaderHammurabi = "leader_hammurabi";

    /// <summary>Leader theme of the Chinese.</summary>
    public const string LeaderMao = "leader_mao";

    /// <summary>Leader theme of the Mongols.</summary>
    public const string LeaderGenghis = "leader_genghis";

    /// <summary>Leader theme of the Indians.</summary>
    public const string LeaderGandhi = "leader_gandhi";

    /// <summary>Leader theme of the Germans.</summary>
    public const string LeaderFrederick = "leader_frederick";

    /// <summary>Suffix that turns a leader theme into its short jingle.</summary>
    /// <remarks>
    /// Call sites build the short name from a civilization's <c>Tune</c>, which carries the long
    /// name. Every <c>Leader*Short</c> constant below therefore has to be its long counterpart plus
    /// this suffix; a test enforces that.
    /// </remarks>
    public const string ShortSuffix = "_short";

    /// <summary>Short jingle of the Americans' leader theme.</summary>
    public const string LeaderLincolnShort = LeaderLincoln + ShortSuffix;

    /// <summary>Short jingle of the Aztecs' leader theme.</summary>
    public const string LeaderMontezumaShort = LeaderMontezuma + ShortSuffix;

    /// <summary>Short jingle of the Egyptians' leader theme.</summary>
    public const string LeaderRamessesShort = LeaderRamesses + ShortSuffix;

    /// <summary>Short jingle of the Zulus' leader theme.</summary>
    public const string LeaderShakaShort = LeaderShaka + ShortSuffix;

    /// <summary>Short jingle of the French leader theme.</summary>
    public const string LeaderNapoleonShort = LeaderNapoleon + ShortSuffix;

    /// <summary>Short jingle of the Romans' leader theme.</summary>
    public const string LeaderCaesarShort = LeaderCaesar + ShortSuffix;

    /// <summary>Short jingle of the Russians' leader theme.</summary>
    public const string LeaderStalinShort = LeaderStalin + ShortSuffix;

    /// <summary>Short jingle of the Greeks' leader theme.</summary>
    public const string LeaderAlexanderShort = LeaderAlexander + ShortSuffix;

    /// <summary>Short jingle of the English leader theme.</summary>
    public const string LeaderElizabethShort = LeaderElizabeth + ShortSuffix;

    /// <summary>Short jingle of the Babylonians' leader theme.</summary>
    public const string LeaderHammurabiShort = LeaderHammurabi + ShortSuffix;

    /// <summary>Short jingle of the Chinese leader theme.</summary>
    public const string LeaderMaoShort = LeaderMao + ShortSuffix;

    /// <summary>Short jingle of the Mongols' leader theme.</summary>
    public const string LeaderGenghisShort = LeaderGenghis + ShortSuffix;

    /// <summary>Short jingle of the Indians' leader theme.</summary>
    public const string LeaderGandhiShort = LeaderGandhi + ShortSuffix;

    /// <summary>Short jingle of the Germans' leader theme.</summary>
    public const string LeaderFrederickShort = LeaderFrederick + ShortSuffix;

    /// <summary>Sting for an audience with a foreign leader.</summary>
    public const string EventAudience = "event_audience";

    /// <summary>
    /// Sting for famine, civil disorder, a government overthrown or a nuclear accident. Doubles as
    /// the barbarians' leader theme.
    /// </summary>
    public const string EventAlarm = "event_alarm";

    /// <summary>Short flourish on opening the city view.</summary>
    public const string EventCityViewOpened = "event_city_view_opened";

    /// <summary>A nuclear device going off outside a city.</summary>
    public const string EventNuclearBlast = "event_nuclear_blast";

    /// <summary>Short beep accompanying an error message.</summary>
    public const string UiBeep = "ui_beep";

    /// <summary>Combat the human won, decided by a unit that is neither strong nor fragile.</summary>
    public const string CombatWinWeak = "combat_win_weak";

    /// <summary>Combat the human lost, decided by a unit that is neither strong nor fragile.</summary>
    public const string CombatLossWeak = "combat_loss_weak";

    /// <summary>Combat the human won, decided by a strong unit.</summary>
    public const string CombatWinStrong = "combat_win_strong";

    /// <summary>Combat the human lost, decided by a strong unit.</summary>
    public const string CombatLossStrong = "combat_loss_strong";

    /// <summary>An air-delivered strike: a winning bomber, or a nuclear device hitting a city.</summary>
    public const string CombatAirStrike = "combat_air_strike";
}
