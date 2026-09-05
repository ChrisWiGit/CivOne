using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl;

/// <summary>
/// Reads and writes the <c>index.json</c> of a sound pack.
/// </summary>
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
