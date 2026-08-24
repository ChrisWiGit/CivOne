using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Sound.Cvl;

#nullable enable

internal sealed class IsoundScoreOptions
{
    public string PackId { get; init; } = "isound";
    public string DisplayName { get; init; } = "IBM PC Speaker";

    /// <summary>Which tunes to extract. Default: every tune addressable by the host.</summary>
    public IReadOnlyList<int>? TuneIds { get; init; }

    /// <summary>Base tick rate of the CIVPLAY scheduler.</summary>
    public int FastTickHz { get; init; } = 300;

    /// <summary>SoundWorkerFn runs every nth base tick.</summary>
    public int WorkerTickDivider { get; init; } = 5;

    /// <summary>
    /// Skip tunes without a sequence (control functions such as stop or status query)
    /// instead of writing them out as <see cref="TuneScoreKind.Unsupported"/>.
    /// </summary>
    public bool SkipUnsupported { get; init; } = true;
}

/// <summary>
/// Extracts the note data from ISOUND.CVL into a standalone <see cref="TuneScorePack"/>.
/// The result is written once as <c>*.score.json</c>; the CVL is no longer needed at
/// runtime afterwards.
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
