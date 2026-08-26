using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl;



/// <summary>
/// Persistence for a single PC speaker tune (<c>*.sound.json</c>). This is the form in which
/// CivOne ships the music – without the original CVL files.
/// </summary>
internal static class TuneScoreJson
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Loads one tune.
    /// </summary>
    /// <param name="path">Path of the tune file.</param>
    /// <returns>The tune.</returns>
    /// <exception cref="InvalidOperationException">The file is empty or fails validation.</exception>
    public static TuneScore Load(string path)
    {
        var tune = JsonSerializer.Deserialize<TuneScore>(File.ReadAllText(path), _options)
            ?? throw new InvalidOperationException($"Konnte Tune aus {path} nicht laden.");

        Validate(tune, path);
        return tune;
    }

    /// <summary>
    /// Writes one tune, creating the folder if needed.
    /// </summary>
    /// <param name="path">Path of the tune file.</param>
    /// <param name="tune">The tune to write.</param>
    public static void Save(string path, TuneScore tune)
    {
        ArgumentNullException.ThrowIfNull(tune);
        Validate(tune, path);

        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        File.WriteAllText(path, JsonSerializer.Serialize(tune, _options));
    }

    /// <summary>
    /// Serializes one tune without writing it anywhere.
    /// </summary>
    /// <param name="tune">The tune to serialize.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(TuneScore tune)
    {
        ArgumentNullException.ThrowIfNull(tune);
        Validate(tune, "<memory>");
        return JsonSerializer.Serialize(tune, _options);
    }

    private static void Validate(TuneScore tune, string source)
    {
        if (tune.SchemaVersion != SoundPackIndex.CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"{source}: schemaVersion muss {SoundPackIndex.CurrentSchemaVersion} sein.");
        }

        if (string.IsNullOrWhiteSpace(tune.Title)) throw new InvalidOperationException($"{source}: title fehlt.");

        // Silent and Unsupported may be empty – but an actual sequence must not be.
        if (tune.Kind is TuneScoreKind.Music or TuneScoreKind.Effect && tune.Steps.Count == 0)
            throw new InvalidOperationException($"{source}: tune {tune.TuneId} ist als {tune.Kind} markiert, hat aber keine steps.");

        foreach (var step in tune.Steps)
        {
            if (step.Duration < 0)
                throw new InvalidOperationException($"{source}: tune {tune.TuneId} hat einen negativen duration-Wert.");
            if (step.Divisor is < 0 or > 0xFFFF)
                throw new InvalidOperationException($"{source}: tune {tune.TuneId} hat einen ungültigen divisor.");
            if (step.Timbre is < 0 or > 0xFFFF)
                throw new InvalidOperationException($"{source}: tune {tune.TuneId} hat einen ungültigen timbre-Wert.");
            if (step.NoiseMask is < 0 or > 0xFFFF)
                throw new InvalidOperationException($"{source}: tune {tune.TuneId} hat eine ungültige noiseMask.");
            if (step.Effect is < 0 or > 0xFFFF)
                throw new InvalidOperationException($"{source}: tune {tune.TuneId} hat einen ungültigen effect-Wert.");
        }
    }
}
