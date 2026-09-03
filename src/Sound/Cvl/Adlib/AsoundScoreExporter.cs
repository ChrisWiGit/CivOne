using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Sound.Cvl.Adlib;

/// <summary>
/// Extracts the instrument bank and every playable tune from ASOUND.CVL.
/// </summary>
internal sealed class AsoundScoreExporter
{
    /// <summary>Driver name written into the pack.</summary>
    public const string DriverName = "ASOUND";

    /// <summary>Device name written into the pack.</summary>
    public const string DeviceName = "adlib";

    /// <summary>Base tick rate of the CIVPLAY scheduler.</summary>
    public const int FastTickHz = 300;

    /// <summary>The sequencer runs every nth base tick.</summary>
    public const int WorkerTickDivider = 5;

    /// <summary>
    /// Reads the shared instrument bank.
    /// </summary>
    /// <param name="parser">Parser for the module.</param>
    /// <returns>The bank.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Exporter members stay instance members so the exporter can be replaced.")]
    public AdlibSoundBank ExportBank(AsoundParser parser)
    {
        ArgumentNullException.ThrowIfNull(parser);

        (int Modulator, int Carrier)[] operators = parser.ReadChannelOperators();
        var modulators = new List<int>(operators.Length);
        var carriers = new List<int>(operators.Length);

        foreach ((int modulator, int carrier) in operators)
        {
            modulators.Add(modulator);
            carriers.Add(carrier);
        }

        (bool deepTremolo, bool deepVibrato, bool noteSelect) = parser.ReadChipFlags();

        return new AdlibSoundBank
        {
            DefaultPan = parser.Layout.DefaultPan,
            DeepTremolo = deepTremolo,
            DeepVibrato = deepVibrato,
            NoteSelect = noteSelect,
            FrequencyNumbers = [.. parser.ReadFrequencyNumbers()],
            ModulatorOffsets = modulators,
            CarrierOffsets = carriers,
            Instruments = parser.ReadInstruments()
        };
    }

    /// <summary>
    /// Reads every tune the driver can play.
    /// </summary>
    /// <param name="parser">Parser for the module.</param>
    /// <param name="options">Extraction options, or <c>null</c> for the defaults.</param>
    /// <returns>The tunes, ordered by tune number.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Exporter members stay instance members so the exporter can be replaced.")]
    public List<AdlibTuneScore> ExportTunes(AsoundParser parser, AsoundScoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(parser);
        options ??= new AsoundScoreOptions();

        var tunes = new List<AdlibTuneScore>();

        for (int tuneId = CvlTuneCatalog.FirstPlayableTuneId; tuneId <= parser.Layout.MaxTuneId; tuneId++)
        {
            AsoundTuneInfo info = parser.ParseTune(tuneId);
            if (options.SkipUnsupported && info.Kind == TuneScoreKind.Unsupported) continue;

            tunes.Add(new AdlibTuneScore
            {
                TuneId = tuneId,
                Title = CvlTuneCatalog.ResolveTitle(tuneId),
                Kind = info.Kind,
                EndlessLoop = CvlTuneCatalog.IsEndlessLoop(tuneId),
                Diagnostic = info.Diagnostic,
                Arrangements = BuildArrangements(parser, info)
            });
        }

        return tunes;
    }

    private static List<AdlibArrangement> BuildArrangements(AsoundParser parser, AsoundTuneInfo info)
    {
        var arrangements = new List<AdlibArrangement>();

        foreach (List<AsoundVoiceRef> references in info.Arrangements)
        {
            var arrangement = new AdlibArrangement();

            foreach (AsoundVoiceRef reference in references)
            {
                List<AdlibEvent> events = parser.DecodeVoice(reference.DataOffset);
                if (events.Count == 0) continue;

                arrangement.Voices.Add(new AdlibVoice
                {
                    Channel = reference.Channel,
                    SourceOffset = reference.DataOffset,
                    Events = events
                });
            }

            if (arrangement.Voices.Count > 0) arrangements.Add(arrangement);
        }

        return arrangements;
    }
}
