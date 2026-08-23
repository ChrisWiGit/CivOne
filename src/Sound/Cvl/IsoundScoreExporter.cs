using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Sound.Cvl;

#nullable enable

internal sealed class IsoundScoreOptions
{
    public string PackId { get; init; } = "isound";
    public string DisplayName { get; init; } = "IBM PC Speaker";

    /// <summary>Welche Tunes extrahiert werden sollen. Standard: alle vom Host adressierbaren.</summary>
    public IReadOnlyList<int>? TuneIds { get; init; }

    /// <summary>Basis-Tickrate des CIVPLAY-Schedulers.</summary>
    public int FastTickHz { get; init; } = 300;

    /// <summary>SoundWorkerFn läuft jeden n-ten Basis-Tick.</summary>
    public int WorkerTickDivider { get; init; } = 5;

    /// <summary>
    /// Tunes ohne Sequenz (Steuerfunktionen wie Stop oder Statusabfrage) weglassen,
    /// statt sie als <see cref="TuneScoreKind.Unsupported"/> mitzuschreiben.
    /// </summary>
    public bool SkipUnsupported { get; init; } = true;
}

/// <summary>
/// Extrahiert die Notendaten aus ISOUND.CVL in ein eigenständiges <see cref="TuneScorePack"/>.
/// Das Ergebnis wird einmalig als <c>*.score.json</c> abgelegt; zur Laufzeit wird die CVL
/// danach nicht mehr benötigt.
/// </summary>
internal static class IsoundScoreExporter
{
    public static TuneScorePack ExportFromFile(string cvlPath, IsoundScoreOptions? options = null)
        => Export(CvlImage.Load(cvlPath), options);

    public static void ExportToFile(string cvlPath, string outputPath, IsoundScoreOptions? options = null)
        => TuneScoreJson.Save(outputPath, ExportFromFile(cvlPath, options));

    public static TuneScorePack Export(CvlImage image, IsoundScoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= new IsoundScoreOptions();

        var parser = IsoundParser.Create(image);

        var pack = new TuneScorePack
        {
            SchemaVersion = 1,
            Id = options.PackId,
            DisplayName = options.DisplayName,
            Driver = "ISOUND",
            Device = "pcSpeaker",
            SourceSignature = image.Signature,
            PitClockHz = 1_193_182,
            FastTickHz = options.FastTickHz,
            WorkerTickDivider = options.WorkerTickDivider,
            Tunes = []
        };

        var tuneIds = options.TuneIds ?? CvlTuneCatalog.PlayableTuneIds.ToArray();

        foreach (int tuneId in tuneIds.Distinct().OrderBy(x => x))
        {
            var info = parser.ParseTune(tuneId);

            if (options.SkipUnsupported && info.Kind == TuneScoreKind.Unsupported) continue;

            pack.Tunes.Add(new TuneScore
            {
                TuneId = tuneId,
                Title = CvlTuneCatalog.ResolveTitle(tuneId),
                Kind = info.Kind,
                EndlessLoop = CvlTuneCatalog.IsEndlessLoop(tuneId),
                SourceOffset = info.DataOffset,
                Steps = info.Steps
            });
        }

        return pack;
    }
}
