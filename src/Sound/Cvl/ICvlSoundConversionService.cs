namespace CivOne.Sound.Cvl;

/// <summary>Converts the original game's CVL sound modules into sound packs.</summary>
internal interface ICvlSoundConversionService
{
    /// <summary>Converts all supported CVL modules of a folder into <paramref name="targetFolder"/>.</summary>
    CvlConversionReport ConvertFolder(string sourceFolder, string targetFolder);

    /// <summary>Converts a single CVL module.</summary>
    CvlConversionResult ConvertFile(string cvlPath, string targetFolder);
}
