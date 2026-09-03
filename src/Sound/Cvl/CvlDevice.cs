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
