using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CivOne.Enums;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;

/// <summary>
/// Answers which sound source the game is set to.
/// </summary>
/// <remarks>
/// <para>
/// The source is a deliberate choice: one converted pack, the profile's wave files, or nothing at
/// all. Whatever is chosen is also the only thing that plays - a sound the chosen source does not
/// carry stays silent rather than being taken from somewhere else.
/// </para>
/// <para>
/// A profile that has never made that choice carries an empty setting. That is resolved here, once,
/// into a real one and written back, so the setting the player sees is the setting that plays. The
/// choice made for them is the one the removed automatic mode would have made: the single converted
/// pack when there is exactly one, and the wave files otherwise.
/// </para>
/// </remarks>
internal sealed class SoundPackSelectionDelegate
{
    /// <summary>
    /// Gets the id of the sound source in use.
    /// </summary>
    /// <returns>
    /// A pack id, <see cref="SoundPlaybackStrategyConstants.WaveSoundPack"/> or
    /// <see cref="SoundPlaybackStrategyConstants.NoSoundPack"/>. Never empty.
    /// </returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Delegate members stay instance members so the delegate can be replaced.")]
    public string Resolve()
    {
        string configured = Settings.Instance.SoundPack;
        if (!string.IsNullOrEmpty(configured)) return configured;

        IReadOnlyList<SoundPackSummary> packs = SoundPackCatalog.GetAvailablePacks(Settings.Instance.SoundsDirectory);
        string resolved = packs.Count == 1 ? packs[0].PackId : SoundPlaybackStrategyConstants.WaveSoundPack;

        // Choosing a source switches sound on, which is right when a player picks one but wrong
        // here: this only writes down what was already in effect, so whether sound is on stays the
        // player's own setting.
        GameOption sound = Settings.Instance.Sound;
        Settings.Instance.SoundPack = resolved;
        Settings.Instance.Sound = sound;

        return resolved;
    }
}
