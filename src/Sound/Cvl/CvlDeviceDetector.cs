using System;

namespace CivOne.Sound.Cvl;

#nullable enable

/// <summary>Die Tonerzeuger, für die es CVL-Treiber gibt.</summary>
internal enum CvlDevice
{
    Unknown,

    /// <summary>NSOUND.CVL – der Treiber ohne Tonausgabe.</summary>
    Silent,

    /// <summary>ISOUND.CVL – IBM-PC-Lautsprecher über PIT-Kanal 2.</summary>
    PcSpeaker,

    /// <summary>TSOUND.CVL – Tandy/PCjr, SN76496 auf Port 0xC0.</summary>
    Tandy,

    /// <summary>ASOUND.CVL – AdLib und Sound Blaster, OPL2 auf Port 0x388.</summary>
    AdLib,

    /// <summary>RSOUND.CVL – Roland MT-32/LAPC-1 über MPU-401 auf Port 0x330.</summary>
    Roland
}

/// <summary>
/// Erkennt das Zielgerät eines CVL-Moduls an den Portzugriffen im Codesegment.
/// Die Signaturstrings taugen dafür nicht: ASOUND und RSOUND tragen beide "RLND Cvlzatn12-03-91".
/// </summary>
internal static class CvlDeviceDetector
{
    public static CvlDevice Detect(CvlImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        // Nur im Codesegment suchen, damit Notendaten keine Fehltreffer erzeugen.
        int start = image.CodeStart;
        int end = Math.Min(image.DataStart, image.Bytes.Length);

        // mov dx,0x388 – der Datenport 0x389 wird über 'inc dx' erreicht und taucht nie als Immediate auf.
        if (Contains(image, start, end, 0xBA, 0x88, 0x03)) return CvlDevice.AdLib;

        // mov dx,0x330 – MPU-401.
        if (Contains(image, start, end, 0xBA, 0x30, 0x03)) return CvlDevice.Roland;

        // out 0xC0,al – SN76496.
        if (Contains(image, start, end, 0xE6, 0xC0)) return CvlDevice.Tandy;

        // out 0x42,al (PIT-Kanal 2) zusammen mit out 0x61,al (Speaker-Gate).
        if (Contains(image, start, end, 0xE6, 0x42) && Contains(image, start, end, 0xE6, 0x61))
            return CvlDevice.PcSpeaker;

        if (image.Signature.Contains("NoSnd", StringComparison.OrdinalIgnoreCase)) return CvlDevice.Silent;

        return CvlDevice.Unknown;
    }

    private static bool Contains(CvlImage image, int start, int end, params byte[] pattern)
    {
        byte[] bytes = image.Bytes;
        for (int i = Math.Max(start, 0); i <= end - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (bytes[i + j] == pattern[j]) continue;
                match = false;
                break;
            }

            if (match) return true;
        }

        return false;
    }
}
