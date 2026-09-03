using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Sound.Cvl.Ibm;

/// <summary>
/// Extracts the note data from ISOUND.CVL into standalone <see cref="TuneScore"/> objects.
/// Each is written once as a <c>*.sound.json</c>; the CVL is no longer needed at runtime
/// afterwards.
/// </summary>
internal static class IsoundScoreExporter
{
    /// <summary>Clock frequency of the PC's timer chip, from which a tone frequency is derived.</summary>
    public const int PitClockHz = 1_193_182;

    /// <summary>Base tick rate of the CIVPLAY scheduler.</summary>
    public const int FastTickHz = 300;

    /// <summary>The sequencer runs every nth base tick.</summary>
    public const int WorkerTickDivider = 5;

    /// <summary>Driver name written into the pack.</summary>
    public const string DriverName = "ISOUND";

    /// <summary>Device name written into the pack.</summary>
    public const string DeviceName = "pcSpeaker";

    /// <summary>
    /// Extracts every tune the driver can play.
    /// </summary>
    /// <param name="image">The loaded CVL module.</param>
    /// <param name="options">Extraction options, or <c>null</c> for the defaults.</param>
    /// <returns>The tunes, ordered by tune number.</returns>
    public static List<TuneScore> Export(CvlImage image, IsoundScoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= new IsoundScoreOptions();

        var parser = IsoundParser.Create(image);
        var tunes = new List<TuneScore>();
        var tuneIds = options.TuneIds ?? CvlTuneCatalog.PlayableTuneIds.ToArray();

        foreach (int tuneId in tuneIds.Distinct().OrderBy(x => x))
        {
            var info = parser.ParseTune(tuneId);

            if (options.SkipUnsupported && info.Kind == TuneScoreKind.Unsupported) continue;

            tunes.Add(new TuneScore
            {
                TuneId = tuneId,
                Title = CvlTuneCatalog.ResolveTitle(tuneId),
                Kind = info.Kind,
                EndlessLoop = CvlTuneCatalog.IsEndlessLoop(tuneId),
                SourceOffset = info.DataOffset,
                Steps = info.Steps
            });
        }

        return tunes;
    }

    /// <summary>
    /// Extracts every tune of a CVL file.
    /// </summary>
    /// <param name="cvlPath">Path of the CVL module.</param>
    /// <param name="options">Extraction options, or <c>null</c> for the defaults.</param>
    /// <returns>The tunes, ordered by tune number.</returns>
    public static List<TuneScore> ExportFromFile(string cvlPath, IsoundScoreOptions? options = null)
        => Export(CvlImage.Load(cvlPath), options);
}
