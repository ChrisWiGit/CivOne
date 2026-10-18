using System;

namespace CivOne.Sound.Cvl;

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
            reason = $"Device is {detected}, this converter serves {Device}.";
            return false;
        }

        return CanConvertDevice(image, out reason);
    }

    /// <inheritdoc/>
    public abstract SoundPackContent Convert(CvlImage image);

    /// <summary>Additional check once the device matches (e.g. whether the code layout is recognized).</summary>
    protected virtual bool CanConvertDevice(CvlImage image, out string? reason)
    {
        reason = null;
        return true;
    }
}
