using System.Collections.Generic;

#nullable enable

namespace CivOne.Sound.Cvl;

internal enum SoundEventType
{
    Worker,
    PortWrite,
    Interrupt
}

internal sealed class SoundPack
{
    public int SchemaVersion { get; set; } = 1;
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Format { get; set; }
    public string? Driver { get; set; }
    public int TickRate { get; set; } = 300;
    public List<TuneDefinition> Tunes { get; set; } = [];
}

internal sealed class TuneDefinition
{
    public int TuneId { get; set; }
    public required string Title { get; set; }
    public bool EndlessLoop { get; set; }
    public List<SoundEvent> Events { get; set; } = [];
}

internal sealed class SoundEvent
{
    public long T { get; set; }
    public SoundEventType Type { get; set; }
    public string? Kind { get; set; }
    public int? Port { get; set; }
    public int? Value { get; set; }
    public int? IntNo { get; set; }
}


