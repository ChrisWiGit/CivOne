using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl;

#nullable enable

internal sealed class SoundPackIndexEntry
{
    public int TuneId { get; set; }
    public required string Title { get; set; }
    public TuneScoreKind Kind { get; set; }

    /// <summary>Dateiname im selben Ordner, oder <c>null</c> bei bewusst stummen Tunes.</summary>
    public string? File { get; set; }

    public int StepCount { get; set; }
    public int TotalTicks { get; set; }
}

/// <summary>
/// <c>index.json</c> eines Sound-Packs: was der Ordner enthält und welcher Name aus der
/// Spiellogik auf welche Tune-Nummer zeigt. Die Zuordnung steht hier und nicht in den
/// Dateinamen, damit die Extraktion treiberneutral bleibt und pro Pack angepasst werden kann.
/// </summary>
internal sealed class SoundPackIndex
{
    public int SchemaVersion { get; set; } = 1;
    public required string PackId { get; set; }
    public required string DisplayName { get; set; }
    public required string Driver { get; set; }
    public required string Device { get; set; }

    public string? SourceFile { get; set; }
    public string? SourceSignature { get; set; }

    public List<SoundPackIndexEntry> Tunes { get; set; } = [];

    /// <summary>Name aus <c>PlaySound</c> -> Tune-Nummer.</summary>
    public Dictionary<string, int> SoundNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Namen, für die dieses Pack keinen Tune anbietet – rein informativ.</summary>
    public List<string> UnmappedSoundNames { get; set; } = [];

    /// <summary>Dateiname des Index innerhalb eines Pack-Ordners.</summary>
    public const string FileName = "index.json";
}

internal static class SoundPackIndexJson
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static SoundPackIndex Load(string path)
    {
        string json = File.ReadAllText(path);
        var index = JsonSerializer.Deserialize<SoundPackIndex>(json, _options)
            ?? throw new InvalidOperationException($"Konnte Sound-Pack-Index aus {path} nicht laden.");

        // Nach dem Deserialisieren ist der Vergleicher der Standardvergleicher.
        index.SoundNames = new Dictionary<string, int>(index.SoundNames, StringComparer.OrdinalIgnoreCase);
        return index;
    }

    public static void Save(string path, SoundPackIndex index)
    {
        ArgumentNullException.ThrowIfNull(index);

        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        File.WriteAllText(path, JsonSerializer.Serialize(index, _options));
    }
}
