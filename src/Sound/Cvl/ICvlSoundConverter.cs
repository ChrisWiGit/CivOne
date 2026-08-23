using System;

namespace CivOne.Sound.Cvl;

#nullable enable

/// <summary>
/// Wandelt ein CVL-Modul eines bestimmten Tonerzeugers in ein <see cref="TuneScorePack"/>.
/// Pro Gerät gibt es eine Implementierung; der Konvertierungsdienst wählt anhand von
/// <see cref="CanConvert"/> die passende aus.
/// </summary>
internal interface ICvlSoundConverter
{
    /// <summary>Ordnername und Pack-Id, z.B. "pc-speaker".</summary>
    string PackId { get; }

    /// <summary>Anzeigename für die Auswahl in den Einstellungen, z.B. "PC Speaker".</summary>
    string DisplayName { get; }

    /// <summary>Gerät, das dieser Konverter bedient.</summary>
    CvlDevice Device { get; }

    /// <summary>Prüft, ob dieses Modul von diesem Konverter gelesen werden kann.</summary>
    bool CanConvert(CvlImage image, out string? reason);

    /// <summary>Extrahiert alle spielbaren Tunes.</summary>
    TuneScorePack Convert(CvlImage image);
}

/// <summary>Gemeinsame Basis: Gerätevergleich plus Ableitung des Anzeigenamens.</summary>
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

    /// <summary>Zusätzliche Prüfung, nachdem das Gerät passt (z.B. ob das Codelayout erkannt wird).</summary>
    protected virtual bool CanConvertDevice(CvlImage image, out string? reason)
    {
        reason = null;
        return true;
    }
}
