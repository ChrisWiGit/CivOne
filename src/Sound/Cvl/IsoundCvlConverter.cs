using System.Collections.Generic;

namespace CivOne.Sound.Cvl;



/// <summary>Converter for ISOUND.CVL – IBM PC speaker.</summary>
internal sealed class IsoundCvlConverter : CvlSoundConverterBase
{
    public const string Id = "pc-speaker";

    public override string PackId => Id;

    // Comment is only for translation scanner.
    // 	Translate("PC Speaker")
    public override string DisplayName => "PC Speaker";

    public override CvlDevice Device => CvlDevice.PcSpeaker;

    protected override bool CanConvertDevice(CvlImage image, out string? reason)
        => IsoundParser.TryCreate(image, out _, out reason);

    /// <inheritdoc/>
    public override SoundPackContent Convert(CvlImage image)
    {
        var content = new SoundPackContent
        {
            Driver = IsoundScoreExporter.DriverName,
            Device = IsoundScoreExporter.DeviceName,
            SourceSignature = image?.Signature,
            FastTickHz = IsoundScoreExporter.FastTickHz,
            WorkerTickDivider = IsoundScoreExporter.WorkerTickDivider,
            PitClockHz = IsoundScoreExporter.PitClockHz
        };

        foreach (TuneScore tune in IsoundScoreExporter.Export(image!))
        {
            TuneScore captured = tune;

            content.Tunes.Add(new SoundPackTune
            {
                Name = CvlTuneCatalog.ResolveName(tune.TuneId),
                Title = tune.Title,
                Kind = tune.Kind,
                EndlessLoop = tune.EndlessLoop,
                StepCount = tune.Steps.Count,
                TotalTicks = tune.TotalTicks,

                // Deliberately silent tunes get no file but still appear in the index, so the game
                // logic can tell "intentionally silent" apart from "not present".
                WriteTo = tune.Steps.Count == 0 ? null : path => TuneScoreJson.Save(path, captured)
            });
        }

        return content;
    }
}
