using System;

namespace CivOne.Sound.Cvl;

#nullable enable

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

    /// <summary>Extracts all playable tunes.</summary>
    TuneScorePack Convert(CvlImage image);
}

/// <summary>Common base: device comparison plus deriving the display name.</summary>
internal abstract class CvlSoundConverterBase : ICvlSoundConverter
{
    public abstract string PackId { get; }
    public abstract string DisplayName { get; }
    public abstract CvlDevice Device { get; }

    public virtual bool CanConvert(CvlImage image, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(image);

        var detected = CvlDeviceDetector.Detect(image);
        if (detected != Device)
        {
            reason = $"Gerät ist {detected}, dieser Konverter bedient {Device}.";
            return false;
        }

        return CanConvertDevice(image, out reason);
    }

    public abstract TuneScorePack Convert(CvlImage image);

    /// <summary>Additional check once the device matches (e.g. whether the code layout is recognized).</summary>
    protected virtual bool CanConvertDevice(CvlImage image, out string? reason)
    {
        reason = null;
        return true;
    }
}
