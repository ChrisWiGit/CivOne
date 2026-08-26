using System;

namespace CivOne.Sound.Cvl;



/// <summary>The sound generators for which CVL drivers exist.</summary>
internal enum CvlDevice
{
    Unknown,

    /// <summary>NSOUND.CVL – the driver with no sound output.</summary>
    Silent,

    /// <summary>ISOUND.CVL – IBM PC speaker via PIT channel 2.</summary>
    PcSpeaker,

    /// <summary>TSOUND.CVL – Tandy/PCjr, SN76496 on port 0xC0.</summary>
    Tandy,

    /// <summary>ASOUND.CVL – AdLib and Sound Blaster, OPL2 on port 0x388.</summary>
    AdLib,

    /// <summary>RSOUND.CVL – Roland MT-32/LAPC-1 via MPU-401 on port 0x330.</summary>
    Roland
}

/// <summary>
/// Detects the target device of a CVL module from the port accesses in its code segment.
/// The signature strings are not usable for this: both ASOUND and RSOUND carry "RLND Cvlzatn12-03-91".
/// </summary>
internal static class CvlDeviceDetector
{
    public static CvlDevice Detect(CvlImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        // Search only in the code segment, so note data can't produce false positives.
        int start = image.CodeStart;
        int end = Math.Min(image.DataStart, image.Bytes.Length);

        // mov dx,0x388 – data port 0x389 is reached via 'inc dx' and never appears as an immediate.
        if (Contains(image, start, end, 0xBA, 0x88, 0x03)) return CvlDevice.AdLib;

        // mov dx,0x330 – MPU-401.
        if (Contains(image, start, end, 0xBA, 0x30, 0x03)) return CvlDevice.Roland;

        // out 0xC0,al – SN76496.
        if (Contains(image, start, end, 0xE6, 0xC0)) return CvlDevice.Tandy;

        // out 0x42,al (PIT channel 2) together with out 0x61,al (speaker gate).
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
