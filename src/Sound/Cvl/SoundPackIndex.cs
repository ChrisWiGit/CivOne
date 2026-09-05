using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Sound.Cvl;

/// <summary>
/// <c>index.json</c> of a sound pack: what the folder contains, and under which name the game
/// plays each tune. A tune is addressed by name everywhere from here on - the entry, the tune file
/// and its rendered wave all carry the same name - so a pack can be inspected and edited without
/// knowing anything about the driver it came from.
/// </summary>
internal sealed class SoundPackIndex
{
    /// <summary>
    /// Schema of the whole pack, including its tune files. Raised whenever their shape changes, or
    /// whenever <see cref="CvlTuneCatalog"/> changes what it names, so a pack written by an older
    /// build - whose <see cref="SoundPackIndexEntry.Name"/>s or titles would otherwise silently
    /// stay stale - is skipped instead of being read as something it is not.
    /// </summary>
    public const int CurrentSchemaVersion = 4;

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

    /// <summary>Names the catalog knows but this driver has no data for – informational only.</summary>
    public List<string> UnavailableSoundNames { get; set; } = [];

    /// <summary>File name of the index within a pack folder.</summary>
    public const string FileName = "index.json";

    /// <summary>Folder inside a pack folder that holds the rendered wave files.</summary>
    public const string WaveCacheFolderName = "wav-cache";

    private Dictionary<string, SoundPackIndexEntry>? _byName;

    /// <summary>
    /// Finds the tune a sound name plays.
    /// </summary>
    /// <param name="soundName">Name the game logic uses, e.g. <see cref="SoundNames.MusicTitle"/>.</param>
    /// <param name="entry">The tune, when this pack has one for the name.</param>
    /// <returns><c>true</c> when the pack knows the name.</returns>
    /// <remarks>
    /// The lookup is built on first use rather than during deserialization, which never runs a
    /// constructor we control.
    /// </remarks>
    public bool TryGetByName(string soundName, [NotNullWhen(true)] out SoundPackIndexEntry? entry)
    {
        _byName ??= BuildLookup();
        return _byName.TryGetValue(soundName ?? string.Empty, out entry);
    }

    private Dictionary<string, SoundPackIndexEntry> BuildLookup()
    {
        var lookup = new Dictionary<string, SoundPackIndexEntry>(Tunes.Count, StringComparer.OrdinalIgnoreCase);

        foreach (SoundPackIndexEntry tune in Tunes)
        {
            // A pack with a duplicate name is malformed; the first entry wins rather than throwing,
            // so one bad tune does not cost the player the whole pack.
            lookup.TryAdd(tune.Name, tune);
        }

        return lookup;
    }
}
