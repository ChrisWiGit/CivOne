using System;
using System.Collections.Generic;
using System.IO;

namespace CivOne.Sound.Cvl;

internal sealed class SoundPackRegistryResult
{
    public List<SoundPack> Packs { get; } = [];
    public List<string> Errors { get; } = [];
}

internal static class SoundPackRegistry
{
    public static SoundPackRegistryResult LoadFromFolder(string folderPath, string pattern = "*.sound.json")
    {
        var result = new SoundPackRegistryResult();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(folderPath))
        {
            return result;
        }

        foreach (var file in Directory.EnumerateFiles(folderPath, pattern, SearchOption.TopDirectoryOnly))
        {
            try
            {
                var pack = SoundPackJson.Load(file);
                if (!seenIds.Add(pack.Id))
                {
                    result.Errors.Add($"Duplicate pack id '{pack.Id}' in file {Path.GetFileName(file)}");
                    continue;
                }

                result.Packs.Add(pack);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        result.Packs.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return result;
    }
}
