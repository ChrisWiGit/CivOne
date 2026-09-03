namespace CivOne.Sound.Cvl;

/// <summary>
/// Converts a CVL module of a specific sound generator into a <see cref="TuneScorePack"/>.
/// There is one implementation per device; the conversion service picks the right one via
/// <see cref="CanConvert"/>.
/// </summary>
internal interface ICvlSoundConverter
{
    /// <summary>Folder name and pack id, e.g. "pc-speaker".</summary>
    string PackId { get; }

    /// <summary>Display name for the selection in the settings, e.g. "PC Speaker".</summary>
    string DisplayName { get; }

    /// <summary>Device that this converter serves.</summary>
    CvlDevice Device { get; }

    /// <summary>Checks whether this module can be read by this converter.</summary>
    bool CanConvert(CvlImage image, out string? reason);

    /// <summary>
    /// Extracts all playable tunes plus any file the pack shares between them.
    /// </summary>
    /// <param name="image">The loaded CVL module.</param>
    /// <returns>The pack contents, ready to be written by the conversion service.</returns>
    SoundPackContent Convert(CvlImage image);
}
