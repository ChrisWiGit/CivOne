using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl;



internal sealed class SoundPackIndexEntry
{
    public int TuneId { get; set; }
    public required string Title { get; set; }
    public TuneScoreKind Kind { get; set; }

    /// <summary>File name within the same folder, or <c>null</c> for deliberately silent tunes.</summary>
    public string? File { get; set; }

    public int StepCount { get; set; }
    public int TotalTicks { get; set; }

    /// <summary>
    /// How many interchangeable arrangements the tune offers. One for everything except the AdLib
    /// leader themes, which the original picks between at random.
    /// </summary>
    public int ArrangementCount { get; set; } = 1;
}

/// <summary>
/// <c>index.json</c> of a sound pack: what the folder contains and which name from the
/// game logic points to which tune number. The mapping lives here rather than in the file
/// names, so extraction stays driver-neutral and can be adjusted per pack.
/// </summary>
internal sealed class SoundPackIndex
{
    /// <summary>
    /// Schema of the whole pack, including its tune files. Raised whenever their shape changes, or
    /// whenever <see cref="SoundNameMap"/> changes what it maps, so a pack written by an older
    /// build - whose <see cref="SoundPackIndexEntry.Title"/>s or <c>SoundNames</c> would otherwise
    /// silently stay stale - is skipped instead of being read as something it is not.
    /// </summary>
    public const int CurrentSchemaVersion = 3;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public required string PackId { get; set; }
    public required string DisplayName { get; set; }
    public required string Driver { get; set; }
    public required string Device { get; set; }

    public string? SourceFile { get; set; }
    public string? SourceSignature { get; set; }

    /// <summary>
    /// Base tick rate of the CIVPLAY scheduler in Hz. Every driver was clocked by the same
    /// scheduler, so this belongs to the pack rather than to a device.
    /// </summary>
    public int FastTickHz { get; set; } = 300;

    /// <summary>
    /// How many base ticks make one sequencer tick. Note and event durations count in those.
    /// </summary>
    public int WorkerTickDivider { get; set; } = 5;

    /// <summary>
    /// Clock frequency of the PC's timer chip in Hz, from which the PC speaker's tone frequency is
    /// derived. <c>null</c> for devices that do not derive their pitch from it.
    /// </summary>
    public int? PitClockHz { get; set; }

    /// <summary>
    /// Files the pack shares between all its tunes, e.g. the AdLib instrument bank.
    /// </summary>
    public List<string> SharedFiles { get; set; } = [];

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
        string json     = File.ReadAllText(path);
        var index = JsonSerializer.Deserialize<SoundPackIndex>(json, _options)
            ?? throw new InvalidOperationException($"Could not load sound pack index from {path}.");

        if (index.SchemaVersion != SoundPackIndex.CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"{path}: schemaVersion {index.SchemaVersion} is not supported, expected "
                + $"{SoundPackIndex.CurrentSchemaVersion}. Re-import the original game's sound data.");
        }

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
