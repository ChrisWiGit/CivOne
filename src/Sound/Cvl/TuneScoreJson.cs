using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl;

#nullable enable

/// <summary>
/// Persistence for <see cref="TuneScorePack"/> (<c>*.score.json</c>). This is the form
/// in which CivOne ships the music – without the original CVL files.
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

    public static TuneScorePack Load(string path)
    {
        string json = File.ReadAllText(path);
        var pack = JsonSerializer.Deserialize<TuneScorePack>(json, _options)
            ?? throw new InvalidOperationException($"Konnte TuneScorePack aus {path} nicht laden.");

        Validate(pack, path);
        return pack;
    }

    public static void Save(string path, TuneScorePack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        Validate(pack, path);

        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        File.WriteAllText(path, JsonSerializer.Serialize(pack, _options));
    }

    public static string Serialize(TuneScorePack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        Validate(pack, "<memory>");
        return JsonSerializer.Serialize(pack, _options);
    }

    private static void Validate(TuneScorePack pack, string source)
    {
        if (pack.SchemaVersion != 1) throw new InvalidOperationException($"{source}: schemaVersion muss 1 sein.");
        if (string.IsNullOrWhiteSpace(pack.Id)) throw new InvalidOperationException($"{source}: id fehlt.");
        if (string.IsNullOrWhiteSpace(pack.DisplayName)) throw new InvalidOperationException($"{source}: displayName fehlt.");
        if (string.IsNullOrWhiteSpace(pack.Driver)) throw new InvalidOperationException($"{source}: driver fehlt.");
        if (string.IsNullOrWhiteSpace(pack.Device)) throw new InvalidOperationException($"{source}: device fehlt.");
        if (pack.PitClockHz <= 0) throw new InvalidOperationException($"{source}: pitClockHz muss > 0 sein.");
        if (pack.FastTickHz <= 0) throw new InvalidOperationException($"{source}: fastTickHz muss > 0 sein.");
        if (pack.WorkerTickDivider <= 0) throw new InvalidOperationException($"{source}: workerTickDivider muss > 0 sein.");
        if (pack.Tunes.Count == 0) throw new InvalidOperationException($"{source}: tunes darf nicht leer sein.");

        var seenTuneIds = new HashSet<int>();
        foreach (var tune in pack.Tunes)
        {
            if (!seenTuneIds.Add(tune.TuneId))
                throw new InvalidOperationException($"{source}: tuneId {tune.TuneId} ist doppelt.");
            if (string.IsNullOrWhiteSpace(tune.Title))
                throw new InvalidOperationException($"{source}: tune {tune.TuneId} hat keinen title.");

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
}
