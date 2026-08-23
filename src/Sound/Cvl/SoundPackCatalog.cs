using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace CivOne.Sound.Cvl;

#nullable enable

internal readonly record struct SoundPackSummary(string PackId, string DisplayName);

/// <summary>
/// Listet die im Profil vorhandenen Sound-Packs auf: jeder Unterordner von
/// <c>sounds/</c>, der von <see cref="CvlSoundConversionService"/> mit einer
/// <see cref="SoundPackIndex"/> beschrieben wurde.
/// </summary>
internal static class SoundPackCatalog
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A corrupted or foreign folder under sounds/ must not break the pack list; it is simply skipped.")]
    public static IReadOnlyList<SoundPackSummary> GetAvailablePacks(string soundsDirectory)
    {
        if (string.IsNullOrWhiteSpace(soundsDirectory) || !Directory.Exists(soundsDirectory))
            return [];

        var packs = new List<SoundPackSummary>();

        foreach (string folder in Directory.EnumerateDirectories(soundsDirectory))
        {
            string indexPath = Path.Combine(folder, SoundPackIndex.FileName);
            if (!File.Exists(indexPath)) continue;

            try
            {
                var index = SoundPackIndexJson.Load(indexPath);
                packs.Add(new SoundPackSummary(index.PackId, index.DisplayName));
            }
            catch
            {
                // Beschädigter oder fremder Ordner unter sounds/ - ignorieren statt die Liste abzubrechen.
            }
        }

        return packs.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
