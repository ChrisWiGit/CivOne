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

    /// <summary>File name within the same folder, or <c>null</c> for deliberately silent tunes.</summary>
    public string? File { get; set; }

    public int StepCount { get; set; }
    public int TotalTicks { get; set; }
}

/// <summary>
/// <c>index.json</c> of a sound pack: what the folder contains and which name from the
/// game logic points to which tune number. The mapping lives here rather than in the file
/// names, so extraction stays driver-neutral and can be adjusted per pack.
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

    /// <summary>Name from <c>PlaySound</c> -> tune number.</summary>
    public Dictionary<string, int> SoundNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Names for which this pack offers no tune – informational only.</summary>
    public List<string> UnmappedSoundNames { get; set; } = [];

    /// <summary>File name of the index within a pack folder.</summary>
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

        // After deserialization the comparer is the default comparer.
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
