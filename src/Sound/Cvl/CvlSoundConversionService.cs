using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace CivOne.Sound.Cvl;

#nullable enable

internal sealed class CvlConversionResult
{
    public required string SourceFile { get; init; }
    public bool Converted { get; init; }
    public string? PackId { get; init; }
    public string? PackFolder { get; init; }
    public CvlDevice Device { get; init; }
    public int TuneCount { get; init; }
    public int MappedSoundNames { get; init; }
    public IReadOnlyList<string> UnmappedSoundNames { get; init; } = [];
    public required string Message { get; init; }
}

internal sealed class CvlConversionReport
{
    public List<CvlConversionResult> Results { get; } = [];

    public bool AnyConverted => Results.Any(r => r.Converted);

    public IEnumerable<string> Messages => Results.Select(r => r.Message);
}

internal interface ICvlSoundConversionService
{
    /// <summary>Converts all supported CVL modules of a folder into <paramref name="targetFolder"/>.</summary>
    CvlConversionReport ConvertFolder(string sourceFolder, string targetFolder);

    /// <summary>Converts a single CVL module.</summary>
    CvlConversionResult ConvertFile(string cvlPath, string targetFolder);
}

/// <summary>
/// Converts the original game's CVL sound modules into our own note data, once.
///
/// Result per supported module: a folder <c>&lt;targetFolder&gt;/&lt;packId&gt;/</c> containing one
/// <c>*.sound.json</c> per tune and an <c>index.json</c>. After that the CVL files are no
/// longer needed; they are also not copied into the profile.
/// </summary>
internal sealed class CvlSoundConversionService : ICvlSoundConversionService
{
    public const string ScoreFileExtension = ".sound.json";

    private readonly IReadOnlyList<ICvlSoundConverter> _converters;

    public CvlSoundConversionService(IEnumerable<ICvlSoundConverter>? converters = null)
        => _converters = converters?.ToArray() ?? [new IsoundCvlConverter()];

    public CvlConversionReport ConvertFolder(string sourceFolder, string targetFolder)
    {
        var report = new CvlConversionReport();

        if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
        {
            report.Results.Add(new CvlConversionResult
            {
                SourceFile = sourceFolder ?? string.Empty,
                Message = $"Quellordner nicht gefunden: {sourceFolder}"
            });
            return report;
        }

        var files = Directory
            .EnumerateFiles(sourceFolder)
            .Where(f => string.Equals(Path.GetExtension(f), ".cvl", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
        {
            report.Results.Add(new CvlConversionResult
            {
                SourceFile = sourceFolder,
                Message = $"Keine CVL-Dateien in {sourceFolder} gefunden."
            });
            return report;
        }

        foreach (string file in files)
        {
            report.Results.Add(ConvertFile(file, targetFolder));
        }

        return report;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A single unreadable or unconvertible CVL module must not abort the batch; it is reported and skipped.")]
    public CvlConversionResult ConvertFile(string cvlPath, string targetFolder)
    {
        string name = Path.GetFileName(cvlPath);

        CvlImage image;
        try
        {
            image = CvlImage.Load(cvlPath);
        }
        catch (Exception ex)
        {
            return Failed(cvlPath, $"{name}: nicht lesbar – {ex.Message}");
        }

        var device = CvlDeviceDetector.Detect(image);
        var converter = FindConverter(image, out string? reason);

        if (converter == null)
        {
            return new CvlConversionResult
            {
                SourceFile = cvlPath,
                Device = device,
                Message = device == CvlDevice.Silent
                    ? $"{name}: Treiber ohne Tonausgabe, übersprungen."
                    : $"{name}: kein Konverter für Gerät {device}{(reason == null ? "" : $" ({reason})")}, übersprungen."
            };
        }

        try
        {
            var pack = converter.Convert(image);
            return Write(pack, converter, cvlPath, targetFolder, device);
        }
        catch (Exception ex)
        {
            return Failed(cvlPath, $"{name}: Konvertierung fehlgeschlagen – {ex.Message}");
        }
    }

    private ICvlSoundConverter? FindConverter(CvlImage image, out string? reason)
    {
        reason = null;

        foreach (var converter in _converters)
        {
            if (converter.CanConvert(image, out string? converterReason)) return converter;

            // Only keep the reason from the converter that actually serves this device.
            if (converter.Device == CvlDeviceDetector.Detect(image)) reason = converterReason;
        }

        return null;
    }

    private static CvlConversionResult Write(TuneScorePack pack, ICvlSoundConverter converter,
        string cvlPath, string targetFolder, CvlDevice device)
    {
        string packFolder = Path.Combine(targetFolder, converter.PackId);
        Directory.CreateDirectory(packFolder);

        var index = new SoundPackIndex
        {
            PackId = converter.PackId,
            DisplayName = converter.DisplayName,
            Driver = pack.Driver,
            Device = pack.Device,
            SourceFile = Path.GetFileName(cvlPath),
            SourceSignature = pack.SourceSignature
        };

        foreach (var tune in pack.Tunes)
        {
            var entry = new SoundPackIndexEntry
            {
                TuneId = tune.TuneId,
                Title = tune.Title,
                Kind = tune.Kind,
                StepCount = tune.Steps.Count,
                TotalTicks = tune.TotalTicks
            };

            // Deliberately silent tunes get no file but still appear in the index, so the
            // game logic can tell "intentionally silent" apart from "not present".
            if (tune.Steps.Count > 0)
            {
                entry.File = $"{tune.TuneId:00}-{Slug(tune.Title)}{ScoreFileExtension}";
                TuneScoreJson.Save(Path.Combine(packFolder, entry.File), SingleTunePack(pack, tune));
            }

            index.Tunes.Add(entry);
        }

        var available = index.Tunes.Select(t => t.TuneId).ToHashSet();

        foreach (string soundName in SoundNameMap.EngineSoundNames)
        {
            if (SoundNameMap.TryGetTuneId(soundName, out int tuneId) && available.Contains(tuneId))
            {
                index.SoundNames[soundName] = tuneId;
                continue;
            }

            index.UnmappedSoundNames.Add(soundName);
        }

        SoundPackIndexJson.Save(Path.Combine(packFolder, SoundPackIndex.FileName), index);

        int withFile = index.Tunes.Count(t => t.File != null);
        string unmapped = index.UnmappedSoundNames.Count == 0
            ? string.Empty
            : $"; ohne Zuordnung: {string.Join(", ", index.UnmappedSoundNames)}";

        return new CvlConversionResult
        {
            SourceFile = cvlPath,
            Converted = true,
            PackId = converter.PackId,
            PackFolder = packFolder,
            Device = device,
            TuneCount = withFile,
            MappedSoundNames = index.SoundNames.Count,
            UnmappedSoundNames = index.UnmappedSoundNames,
            Message = $"{Path.GetFileName(cvlPath)} -> {converter.PackId}: {withFile} Tunes, "
                      + $"{index.SoundNames.Count} von {SoundNameMap.EngineSoundNames.Count} Namen zugeordnet{unmapped}"
        };
    }

    private static TuneScorePack SingleTunePack(TuneScorePack pack, TuneScore tune) => new()
    {
        SchemaVersion = pack.SchemaVersion,
        Id = pack.Id,
        DisplayName = pack.DisplayName,
        Driver = pack.Driver,
        Device = pack.Device,
        SourceSignature = pack.SourceSignature,
        PitClockHz = pack.PitClockHz,
        FastTickHz = pack.FastTickHz,
        WorkerTickDivider = pack.WorkerTickDivider,
        Tunes = [tune]
    };

    private static CvlConversionResult Failed(string cvlPath, string message)
        => new() { SourceFile = cvlPath, Message = message };

    /// <summary>"Alexander the Great" -> "alexander-the-great".</summary>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Lowercase is required here for the on-disk file name, not for a culture-independent comparison.")]
    internal static string Slug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "tune";

        var slug = new StringBuilder(text.Length);
        bool separator = false;

        foreach (char c in text.ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                slug.Append(c);
                separator = false;
                continue;
            }

            if (separator || slug.Length == 0) continue;

            slug.Append('-');
            separator = true;
        }

        return slug.ToString().Trim('-') is { Length: > 0 } result ? result : "tune";
    }
}
