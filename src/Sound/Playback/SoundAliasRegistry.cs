using System;
using System.Collections.Generic;

namespace CivOne.Sound.Playback;

/// <summary>
/// Holds the sound name redirects that are currently in force.
/// </summary>
/// <remarks>
/// A redirect may point at a name that is itself redirected, so resolving follows the chain. The
/// chain is bounded: once a name repeats, or after <see cref="MaxHops"/> steps, resolving stops and
/// returns what it has. A cycle therefore costs a few dictionary lookups rather than the game.
/// </remarks>
internal sealed class SoundAliasRegistry : ISoundAliasRegistry
{
    /// <summary>How many redirects are followed before resolving gives up.</summary>
    private const int MaxHops = 8;

    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public string Resolve(string soundName)
    {
        if (string.IsNullOrEmpty(soundName) || _aliases.Count == 0) return soundName;

        string current = soundName;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { current };

        for (int hop = 0; hop < MaxHops; hop++)
        {
            if (!_aliases.TryGetValue(current, out string? target)) return current;
            if (!seen.Add(target)) return current;

            current = target;
        }

        return current;
    }

    /// <inheritdoc/>
    public void SetAlias(string soundName, string? targetName)
    {
        ArgumentException.ThrowIfNullOrEmpty(soundName);

        if (string.IsNullOrEmpty(targetName))
        {
            _aliases.Remove(soundName);
            return;
        }

        _aliases[soundName] = targetName;
    }
}
