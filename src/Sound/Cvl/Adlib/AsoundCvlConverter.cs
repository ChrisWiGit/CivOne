using System.Collections.Generic;

namespace CivOne.Sound.Cvl.Adlib;



/// <summary>Converter for ASOUND.CVL - AdLib and Sound Blaster (OPL2 FM).</summary>
internal sealed class AsoundCvlConverter : CvlSoundConverterBase
{
    /// <summary>Folder name and pack id of this converter's output.</summary>
    public const string Id = "adlib";

    private readonly AsoundScoreExporter _exporter = new();

    /// <inheritdoc/>
    public override string PackId => Id;

    // 	Translate("AdLib / Sound Blaster")
    // Comment is only for translation scanner.
    /// <inheritdoc/>
    public override string DisplayName => "AdLib / Sound Blaster";

    /// <inheritdoc/>
    public override CvlDevice Device => CvlDevice.AdLib;

    /// <inheritdoc/>
    protected override bool CanConvertDevice(CvlImage image, out string? reason)
        => AsoundParser.TryCreate(image, out _, out reason);

    /// <inheritdoc/>
    public override SoundPackContent Convert(CvlImage image)
    {
        AsoundParser parser = AsoundParser.Create(image);
        AdlibSoundBank bank = _exporter.ExportBank(parser);

        var content = new SoundPackContent
        {
            Driver = AsoundScoreExporter.DriverName,
            Device = AsoundScoreExporter.DeviceName,
            SourceSignature = parser.Image.Signature,
            FastTickHz = AsoundScoreExporter.FastTickHz,
            WorkerTickDivider = AsoundScoreExporter.WorkerTickDivider
        };

        content.SharedFiles[AdlibSoundBank.FileName] = path => AdlibScoreJson.SaveBank(path, bank);

        foreach (AdlibTuneScore tune in _exporter.ExportTunes(parser))
        {
            AdlibTuneScore captured = tune;

            content.Tunes.Add(new SoundPackTune
            {
                Name = CvlTuneCatalog.ResolveName(tune.TuneId),
                Title = tune.Title,
                Kind = tune.Kind,
                StepCount = tune.EventCount,
                TotalTicks = tune.TotalTicks,
                ArrangementCount = tune.Arrangements.Count,
                WriteTo = tune.Arrangements.Count == 0
                    ? null
                    : path => AdlibScoreJson.SaveTune(path, captured)
            });
        }

        return content;
    }
}
