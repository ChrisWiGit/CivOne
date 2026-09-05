using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace CivOne.Sound.Cvl;

/// <summary>
/// Lists the sound packs present in the profile: every subfolder of
/// <c>sounds/</c> that <see cref="CvlSoundConversionService"/> described with a
/// <see cref="SoundPackIndex"/>.
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
                // Corrupted or foreign folder under sounds/ - skip it instead of aborting the list.
            }
        }

        return packs.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
