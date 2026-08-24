namespace CivOne.Sound.Cvl;

#nullable enable

/// <summary>Converter for ISOUND.CVL – IBM PC speaker.</summary>
internal sealed class IsoundCvlConverter : CvlSoundConverterBase
{
    public const string Id = "pc-speaker";

    public override string PackId => Id;

    public override string DisplayName => "PC Speaker";

    public override CvlDevice Device => CvlDevice.PcSpeaker;

    protected override bool CanConvertDevice(CvlImage image, out string? reason)
        => IsoundParser.TryCreate(image, out _, out reason);

    public override TuneScorePack Convert(CvlImage image)
        => IsoundScoreExporter.Export(image, new IsoundScoreOptions
        {
            PackId = PackId,
            DisplayName = DisplayName
        });
}
