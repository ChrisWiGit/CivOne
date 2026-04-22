using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl;

internal static class SoundPackJson
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static SoundPack Load(string path)
    {
        var json = File.ReadAllText(path);
        var pack = JsonSerializer.Deserialize<SoundPack>(json, _options)
            ?? throw new InvalidOperationException($"Konnte SoundPack aus {path} nicht laden.");

        Validate(pack, path);
        return pack;
    }

    public static void Save(string path, SoundPack pack)
    {
        Validate(pack, path);
        var json = JsonSerializer.Serialize(pack, _options);
        File.WriteAllText(path, json);
    }

    private static void Validate(SoundPack pack, string source)
    {
        if (pack.SchemaVersion != 1) throw new InvalidOperationException($"{source}: schemaVersion muss 1 sein.");
        if (string.IsNullOrWhiteSpace(pack.Id)) throw new InvalidOperationException($"{source}: id fehlt.");
        if (string.IsNullOrWhiteSpace(pack.DisplayName)) throw new InvalidOperationException($"{source}: displayName fehlt.");
        if (string.IsNullOrWhiteSpace(pack.Format)) throw new InvalidOperationException($"{source}: format fehlt.");
        if (pack.TickRate <= 0) throw new InvalidOperationException($"{source}: tickRate muss > 0 sein.");
        if (pack.Tunes.Count == 0) throw new InvalidOperationException($"{source}: tunes darf nicht leer sein.");

        var seenTuneIds = new HashSet<int>();
        foreach (var tune in pack.Tunes)
        {
            if (!seenTuneIds.Add(tune.TuneId))
                throw new InvalidOperationException($"{source}: tuneId {tune.TuneId} ist doppelt.");
            if (string.IsNullOrWhiteSpace(tune.Title))
                throw new InvalidOperationException($"{source}: tune {tune.TuneId} hat keinen title.");
            if (tune.Events.Count == 0)
                throw new InvalidOperationException($"{source}: tune {tune.TuneId} hat keine events.");

            long lastTick = long.MinValue;
            foreach (var e in tune.Events)
            {
                if (e.T < 0) throw new InvalidOperationException($"{source}: tune {tune.TuneId} enthält negatives t.");
                if (e.T < lastTick) throw new InvalidOperationException($"{source}: tune {tune.TuneId} events sind nicht monoton nach t.");

                switch (e.Type)
                {
                    case SoundEventType.Worker:
                        if (string.IsNullOrWhiteSpace(e.Kind))
                            throw new InvalidOperationException($"{source}: tune {tune.TuneId} worker event ohne kind.");
                        break;

                    case SoundEventType.PortWrite:
                        if (!e.Port.HasValue || e.Port.Value < 0 || e.Port.Value > 0xFFFF)
                            throw new InvalidOperationException($"{source}: tune {tune.TuneId} portWrite event mit ungültigem port.");
                        if (!e.Value.HasValue || e.Value.Value < 0 || e.Value.Value > 0xFF)
                            throw new InvalidOperationException($"{source}: tune {tune.TuneId} portWrite event mit ungültigem value.");
                        break;

                    case SoundEventType.Interrupt:
                        if (!e.IntNo.HasValue || e.IntNo.Value < 0 || e.IntNo.Value > 0xFF)
                            throw new InvalidOperationException($"{source}: tune {tune.TuneId} interrupt event mit ungültigem intNo.");
                        break;

                    default:
                        throw new InvalidOperationException($"{source}: tune {tune.TuneId} enthält unbekannten event type.");
                }

                lastTick = e.T;
            }
        }
    }
}


