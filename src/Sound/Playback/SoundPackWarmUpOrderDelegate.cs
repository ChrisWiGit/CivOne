using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;

/// <summary>
/// Decides in which order the tunes of a sound pack are pre-rendered.
/// </summary>
/// <remarks>
/// A whole pack takes a while to render, so the order matters: the tunes the game asks for first
/// should be finished first. <see cref="CvlTuneCatalog.WarmUpOrder"/> lists the identified tunes in
/// that order, so it forms the front of the queue. Everything else the pack contains follows.
/// </remarks>
internal sealed class SoundPackWarmUpOrderDelegate
{
    /// <summary>
    /// Lists the tune files of a pack in the order they should be rendered.
    /// </summary>
    /// <param name="packFolder">Folder of the sound pack.</param>
    /// <returns>
    /// The file names, most useful first. Empty when the pack has no readable index; deliberately
    /// silent tunes are left out because they have no file to render.
    /// </returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A corrupted or outdated index must not take the background warm-up down; nothing is pre-rendered then.")]
    public IReadOnlyList<string> Order(string packFolder)
    {
        string indexPath = Path.Combine(packFolder, SoundPackIndex.FileName);
        if (!File.Exists(indexPath)) return [];

        SoundPackIndex index;
        try
        {
            index = SoundPackIndexJson.Load(indexPath);
        }
        catch (Exception)
        {
            return [];
        }

        return Order(index);
    }

    /// <summary>
    /// Lists the tune files of an already loaded index in the order they should be rendered.
    /// </summary>
    /// <param name="index">The pack's manifest.</param>
    /// <returns>The file names, most useful first.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Delegate members stay instance members so the delegate can be replaced.")]
    public IReadOnlyList<string> Order(SoundPackIndex index)
    {
        ArgumentNullException.ThrowIfNull(index);

        var ordered = new List<string>(index.Tunes.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string soundName in CvlTuneCatalog.WarmUpOrder)
        {
            if (!index.TryGetByName(soundName, out SoundPackIndexEntry? entry)) continue;
            if (string.IsNullOrEmpty(entry.File)) continue;
            if (seen.Add(entry.File)) ordered.Add(entry.File);
        }

        foreach (SoundPackIndexEntry entry in index.Tunes)
        {
            if (string.IsNullOrEmpty(entry.File)) continue;
            if (seen.Add(entry.File)) ordered.Add(entry.File);
        }

        return ordered;
    }
}
