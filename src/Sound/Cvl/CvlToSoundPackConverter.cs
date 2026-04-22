using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CivOne.Sound.Cvl;

#nullable enable

internal sealed class CvlConversionOptions
{
    public required string PackId { get; init; }
    public required string DisplayName { get; init; }
    public required string Format { get; init; }
    public string? Driver { get; init; }
    public IReadOnlyList<int> TuneIds { get; init; } = new[] { 3, 4, 34, 35 };
    public int TickRate { get; init; } = 300;
    public int SchedulerTicks { get; init; } = 32;
    public int MaxRecordedInstructions { get; init; } = 256;
}

internal static class CvlToSoundPackConverter
{
    private static readonly Dictionary<int, string> _knownTitles = new()
    {
        [3] = "Title Music",
        [4] = "Evolution Music",
        [5] = "Lincoln",
        [6] = "Montezuma",
        [7] = "Ramses",
        [8] = "Shaka Zulu",
        [9] = "Napoleon",
        [10] = "Caesar",
        [11] = "Stalin",
        [12] = "Alexander the Great",
        [13] = "Elizabeth",
        [14] = "Hammurabi",
        [15] = "Mao",
        [16] = "Genghis Khan",
        [17] = "Gandhi",
        [18] = "Frederick",
        [34] = "Win Music",
        [35] = "Lose Music"
    };

    public static SoundPack Convert(string cvlPath, CvlConversionOptions options)
    {
        if (string.IsNullOrWhiteSpace(cvlPath)) throw new ArgumentException("cvlPath fehlt.", nameof(cvlPath));
        if (!File.Exists(cvlPath)) throw new FileNotFoundException("CVL-Datei nicht gefunden.", cvlPath);

        var module = CvlModule.Load(cvlPath);

        var pack = new SoundPack
        {
            SchemaVersion = 1,
            Id = options.PackId,
            DisplayName = options.DisplayName,
            Format = options.Format,
            Driver = options.Driver,
            TickRate = options.TickRate,
            Tunes = []
        };

        foreach (var tuneId in options.TuneIds.Distinct())
        {
            var events = CvlRecordRunner.RecordEvents(module, options, tuneId);

            pack.Tunes.Add(new TuneDefinition
            {
                TuneId = tuneId,
                Title = ResolveTitle(tuneId),
                EndlessLoop = tuneId is 3 or 4,
                Events = events
            });
        }

        return pack;
    }

    public static void ConvertToFile(string cvlPath, string outputPath, CvlConversionOptions options)
    {
        var pack = Convert(cvlPath, options);
        SoundPackJson.Save(outputPath, pack);
    }

    private static string ResolveTitle(int tuneId)
        => _knownTitles.TryGetValue(tuneId, out var title) ? title : $"Tune {tuneId}";

}
