using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CivOne.Sound.Cvl;

#nullable enable

/// <summary>Wie ein Tune im Treiber realisiert ist.</summary>
internal enum TuneScoreKind
{
    /// <summary>Der Handler ist keine Tune-Sequenz (Stop, Statusabfrage, Sonderlogik).</summary>
    Unsupported,

    /// <summary>Der Handler kehrt sofort zurück – der Tune ist im Treiber bewusst leer.</summary>
    Silent,

    /// <summary>Musiksequenz: 4-Byte-Records {Timbre, Dauer, PIT-Divisor}.</summary>
    Music,

    /// <summary>Effektsequenz: 10-Byte-Records mit eigener Noise-Maske und Slide-Parameter.</summary>
    Effect
}

internal enum SpeakerEffectKind
{
    None,

    /// <summary>Divisor pendelt in <see cref="SpeakerEffect.Step"/>-Schritten um ±<see cref="SpeakerEffect.Range"/>.</summary>
    Vibrato,

    /// <summary>Pro Worker-Tick wird <see cref="SpeakerEffect.Delta"/> auf den Divisor addiert.</summary>
    Slide
}

/// <summary>
/// Dekodierter Slide-/Vibrato-Parameter (im Treiber <c>ds:0x6F</c>).
/// Highnibble 8 = Vibrato (Lowbyte = Hub, mittleres Nibble = Schrittweite),
/// alles andere = vorzeichenbehaftete Addition auf den Divisor.
/// </summary>
internal readonly record struct SpeakerEffect(SpeakerEffectKind Kind, int Range, int Step, int Delta, int Raw)
{
    public static SpeakerEffect Decode(int raw)
    {
        int word = raw & 0xFFFF;
        if (word == 0) return new SpeakerEffect(SpeakerEffectKind.None, 0, 0, 0, 0);

        if ((word & 0xF000) == 0x8000)
            return new SpeakerEffect(SpeakerEffectKind.Vibrato, word & 0xFF, (word >> 8) & 0x0F, 0, word);

        return new SpeakerEffect(SpeakerEffectKind.Slide, 0, 0, (short)word, word);
    }
}

/// <summary>
/// Ein Schritt der Sequenz: ein Ton oder eine Pause fester Länge. Bewusst
/// treiberunabhängig – aus diesen Daten lässt sich der PC-Speaker rendern,
/// ohne die CVL erneut zu lesen.
/// </summary>
internal sealed class TuneStep
{
    /// <summary>Länge in Worker-Ticks (siehe <see cref="TuneScorePack.WorkerTickHz"/>).</summary>
    public int Duration { get; set; }

    /// <summary>PIT-Kanal-2-Divisor. 0 bedeutet Pause (Speaker-Gate zu).</summary>
    public int Divisor { get; set; }

    /// <summary>Timbre-/Prioritätscode aus dem Record; wählt im Treiber den Effekt aus.</summary>
    public int Timbre { get; set; }

    /// <summary>Maske für den Noise-LFSR: 1 bei Musik, aus dem Record bei Effekten.</summary>
    public int NoiseMask { get; set; }

    /// <summary>Rohes Slide-/Vibrato-Wort, siehe <see cref="SpeakerEffect.Decode"/>.</summary>
    public int Effect { get; set; }

    [JsonIgnore]
    public bool IsRest => Divisor == 0;

    [JsonIgnore]
    public SpeakerEffect DecodedEffect => SpeakerEffect.Decode(Effect);

    public double FrequencyHz(int pitClockHz)
        => Divisor <= 0 ? 0d : pitClockHz / (double)Divisor;
}

internal sealed class TuneScore
{
    public int TuneId { get; set; }
    public required string Title { get; set; }
    public TuneScoreKind Kind { get; set; }
    public bool EndlessLoop { get; set; }

    /// <summary>Datensegment-Offset der Sequenz in der Quelldatei (nur zur Nachvollziehbarkeit).</summary>
    public int SourceOffset { get; set; }

    public List<TuneStep> Steps { get; set; } = [];

    [JsonIgnore]
    public int TotalTicks
    {
        get
        {
            int total = 0;
            foreach (var step in Steps) total += step.Duration;
            return total;
        }
    }
}

/// <summary>
/// Vollständig aus einer CVL extrahierte Notendaten eines Treibers. Zur Laufzeit wird
/// nur noch diese Struktur (als <c>*.score.json</c>) benötigt – die CVL selbst nicht mehr.
/// </summary>
internal sealed class TuneScorePack
{
    public int SchemaVersion { get; set; } = 1;
    public required string Id { get; set; }
    public required string DisplayName { get; set; }

    /// <summary>Quelltreiber, z.B. "ISOUND".</summary>
    public required string Driver { get; set; }

    /// <summary>Zielgerät, z.B. "pcSpeaker".</summary>
    public required string Device { get; set; }

    /// <summary>Signatur der Quelldatei – dokumentiert, aus welchem Build extrahiert wurde.</summary>
    public string? SourceSignature { get; set; }

    /// <summary>Taktfrequenz des PIT; Frequenz = PitClockHz / Divisor.</summary>
    public int PitClockHz { get; set; } = 1_193_182;

    /// <summary>
    /// Basis-Tickrate des CIVPLAY-Schedulers. Auf diesem Takt läuft FastSoundWorkerFn
    /// (Vibrato, Slide, Noise).
    /// </summary>
    public int FastTickHz { get; set; } = 300;

    /// <summary>SoundWorkerFn läuft jeden n-ten Basis-Tick; die Dauern zählen in Worker-Ticks.</summary>
    public int WorkerTickDivider { get; set; } = 5;

    public List<TuneScore> Tunes { get; set; } = [];

    [JsonIgnore]
    public double WorkerTickHz => WorkerTickDivider <= 0 ? 0d : FastTickHz / (double)WorkerTickDivider;

    [JsonIgnore]
    public double WorkerTickSeconds => WorkerTickHz <= 0d ? 0d : 1d / WorkerTickHz;

    public double DurationSeconds(TuneStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return step.Duration * WorkerTickSeconds;
    }
}
