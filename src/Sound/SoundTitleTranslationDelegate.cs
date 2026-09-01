using CivOne.Services;

namespace CivOne.Sound;

/// <summary>
/// Translates the display title of a tune.
/// </summary>
/// <remarks>
/// A pack's <c>index.json</c> stores titles in English, because it is data on disk and has to stay
/// readable no matter which language the game runs in. The translation therefore happens here, when
/// a title is shown. Every key below is spelled out so the extraction script can find it; a title
/// the game does not know is shown as it stands.
/// </remarks>
internal sealed class SoundTitleTranslationDelegate
{
    private readonly ITranslationService? _translation;

    private ITranslationService Translation => _translation ?? TranslationServiceFactory.GetCurrent();

    /// <summary>
    /// Creates the delegate.
    /// </summary>
    /// <param name="translation">
    /// Translation service to use, or <c>null</c> to take the one the game currently runs with.
    /// </param>
    public SoundTitleTranslationDelegate(ITranslationService? translation = null) => _translation = translation;

    /// <summary>
    /// Translates a tune title.
    /// </summary>
    /// <param name="soundName">Name of the sound, used to pick the key.</param>
    /// <param name="englishTitle">The title as stored in the pack, used when the name is unknown.</param>
    /// <returns>The title in the current language.</returns>
    public string Translate(string soundName, string englishTitle)
        => soundName switch
        {
            SoundNames.MusicTitle => Translation.Translate("Title Music"),
            SoundNames.MusicEvolution => Translation.Translate("Evolution Music"),
            SoundNames.MusicWin => Translation.Translate("Win Music"),
            SoundNames.MusicLose => Translation.Translate("Lose Music"),

            SoundNames.LeaderLincoln => Translation.Translate("Lincoln (Long)"),
            SoundNames.LeaderMontezuma => Translation.Translate("Montezuma (Long)"),
            SoundNames.LeaderRamesses => Translation.Translate("Ramesses (Long)"),
            SoundNames.LeaderShaka => Translation.Translate("Shaka Zulu (Long)"),
            SoundNames.LeaderNapoleon => Translation.Translate("Napoleon (Long)"),
            SoundNames.LeaderCaesar => Translation.Translate("Caesar (Long)"),
            SoundNames.LeaderStalin => Translation.Translate("Stalin (Long)"),
            SoundNames.LeaderAlexander => Translation.Translate("Alexander the Great (Long)"),
            SoundNames.LeaderElizabeth => Translation.Translate("Elizabeth (Long)"),
            SoundNames.LeaderHammurabi => Translation.Translate("Hammurabi (Long)"),
            SoundNames.LeaderMao => Translation.Translate("Mao (Long)"),
            SoundNames.LeaderGenghis => Translation.Translate("Genghis Khan (Long)"),
            SoundNames.LeaderGandhi => Translation.Translate("Gandhi (Long)"),
            SoundNames.LeaderFrederick => Translation.Translate("Frederick (Long)"),

            SoundNames.LeaderLincolnShort => Translation.Translate("Lincoln (Short)"),
            SoundNames.LeaderMontezumaShort => Translation.Translate("Montezuma (Short)"),
            SoundNames.LeaderRamessesShort => Translation.Translate("Ramesses (Short)"),
            SoundNames.LeaderShakaShort => Translation.Translate("Shaka Zulu (Short)"),
            SoundNames.LeaderNapoleonShort => Translation.Translate("Napoleon (Short)"),
            SoundNames.LeaderCaesarShort => Translation.Translate("Caesar (Short)"),
            SoundNames.LeaderStalinShort => Translation.Translate("Stalin (Short)"),
            SoundNames.LeaderAlexanderShort => Translation.Translate("Alexander the Great (Short)"),
            SoundNames.LeaderElizabethShort => Translation.Translate("Elizabeth (Short)"),
            SoundNames.LeaderHammurabiShort => Translation.Translate("Hammurabi (Short)"),
            SoundNames.LeaderMaoShort => Translation.Translate("Mao (Short)"),
            SoundNames.LeaderGenghisShort => Translation.Translate("Genghis Khan (Short)"),
            SoundNames.LeaderGandhiShort => Translation.Translate("Gandhi (Short)"),
            SoundNames.LeaderFrederickShort => Translation.Translate("Frederick (Short)"),

            SoundNames.EventAudience => Translation.Translate("Foreign Leader Audience Sting"),
            SoundNames.EventAlarm => Translation.Translate("Alarm - Barbarian Theme"),
            SoundNames.EventCityViewOpened => Translation.Translate("City View Opened"),
            SoundNames.EventNuclearBlast => Translation.Translate("Nuclear Blast"),
            SoundNames.UiBeep => Translation.Translate("Beep"),

            SoundNames.CombatWinWeak => Translation.Translate("Combat Win (Weak Unit)"),
            SoundNames.CombatLossWeak => Translation.Translate("Combat Loss (Weak Unit)"),
            SoundNames.CombatWinStrong => Translation.Translate("Combat Win (Strong Unit)"),
            SoundNames.CombatLossStrong => Translation.Translate("Combat Loss (Strong Unit)"),
            SoundNames.CombatAirStrike => Translation.Translate("Air Strike"),

            _ => englishTitle
        };
}
