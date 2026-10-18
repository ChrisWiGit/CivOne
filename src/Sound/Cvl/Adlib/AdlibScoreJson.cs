using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl.Adlib;



/// <summary>
/// Persistence for the AdLib pack: one <see cref="AdlibSoundBank"/> per pack and one
/// <see cref="AdlibTuneScore"/> per tune. This is the form in which CivOne ships the FM music,
/// without the original CVL file.
/// </summary>
internal static class AdlibScoreJson
{
    /// <summary>
    /// The bank is the part a human reads or tweaks, so it stays indented.
    /// </summary>
    private static readonly JsonSerializerOptions _bankOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Tune files hold tens of thousands of events, so they are written without indentation.
    /// Indenting them costs several megabytes across a pack and buys nothing.
    /// </summary>
    private static readonly JsonSerializerOptions _tuneOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Loads the shared instrument bank of a pack.
    /// </summary>
    /// <param name="path">Path of the bank file.</param>
    /// <returns>The bank.</returns>
    /// <exception cref="InvalidOperationException">The file is empty or fails validation.</exception>
    public static AdlibSoundBank LoadBank(string path)
    {
        var bank = JsonSerializer.Deserialize<AdlibSoundBank>(File.ReadAllText(path), _bankOptions)
            ?? throw new InvalidOperationException($"Could not load AdLib bank from {path}.");

        ValidateBank(bank, path);
        return bank;
    }

    /// <summary>
    /// Writes the shared instrument bank of a pack.
    /// </summary>
    /// <param name="path">Path of the bank file.</param>
    /// <param name="bank">The bank to write.</param>
    public static void SaveBank(string path, AdlibSoundBank bank)
    {
        ArgumentNullException.ThrowIfNull(bank);
        ValidateBank(bank, path);
        Write(path, JsonSerializer.Serialize(bank, _bankOptions));
    }

    /// <summary>
    /// Loads a single tune.
    /// </summary>
    /// <param name="path">Path of the tune file.</param>
    /// <returns>The tune.</returns>
    /// <exception cref="InvalidOperationException">The file is empty or fails validation.</exception>
    public static AdlibTuneScore LoadTune(string path)
    {
        var tune = JsonSerializer.Deserialize<AdlibTuneScore>(File.ReadAllText(path), _tuneOptions)
            ?? throw new InvalidOperationException($"Could not load AdLib tune from {path}.");

        ValidateTune(tune, path);
        return tune;
    }

    /// <summary>
    /// Writes a single tune.
    /// </summary>
    /// <param name="path">Path of the tune file.</param>
    /// <param name="tune">The tune to write.</param>
    public static void SaveTune(string path, AdlibTuneScore tune)
    {
        ArgumentNullException.ThrowIfNull(tune);
        ValidateTune(tune, path);
        Write(path, JsonSerializer.Serialize(tune, _tuneOptions));
    }

    private static void Write(string path, string json)
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        File.WriteAllText(path, json);
    }

    private static void ValidateBank(AdlibSoundBank bank, string source)
    {
        Expect(bank.SchemaVersion, source);

        if (bank.Instruments.Count == 0) throw new InvalidOperationException($"{source}: instruments must not be empty.");
        if (bank.FrequencyNumbers.Count != 12)
            throw new InvalidOperationException($"{source}: frequencyNumbers needs exactly 12 entries.");
        if (bank.ModulatorOffsets.Count == 0 || bank.ModulatorOffsets.Count != bank.CarrierOffsets.Count)
            throw new InvalidOperationException($"{source}: modulatorOffsets and carrierOffsets do not match.");
    }

    /// <summary>
    /// Rejects a file written by a build with a different pack layout.
    /// </summary>
    private static void Expect(int schemaVersion, string source)
    {
        if (schemaVersion == SoundPackIndex.CurrentSchemaVersion) return;

        throw new InvalidOperationException(
            $"{source}: schemaVersion must be {SoundPackIndex.CurrentSchemaVersion}.");
    }

    private static void ValidateTune(AdlibTuneScore tune, string source)
    {
        Expect(tune.SchemaVersion, source);

        if (string.IsNullOrWhiteSpace(tune.Title)) throw new InvalidOperationException($"{source}: title is missing.");
        if (tune.Arrangements.Count == 0) throw new InvalidOperationException($"{source}: arrangements must not be empty.");

        foreach (AdlibArrangement arrangement in tune.Arrangements)
        {
            if (arrangement.Voices.Count == 0)
                throw new InvalidOperationException($"{source}: tune {tune.TuneId} has an arrangement without voices.");

            foreach (AdlibVoice voice in arrangement.Voices)
            {
                if (voice.Channel < 0)
                    throw new InvalidOperationException($"{source}: tune {tune.TuneId} has a negative channel number.");
                if (voice.Events.Count == 0)
                    throw new InvalidOperationException($"{source}: tune {tune.TuneId}, channel {voice.Channel} has no events.");
            }
        }
    }
}
