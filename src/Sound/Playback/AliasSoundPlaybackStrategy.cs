using System;

namespace CivOne.Sound.Playback;

/// <summary>
/// Applies the sound name redirects before handing the name to the strategy that plays it.
/// </summary>
/// <remarks>
/// This sits in front of every strategy rather than inside one of them, so a redirect can move a
/// sound from one source to another - pointing a tune of a converted pack at a plain wave file, for
/// instance. A strategy that resolved names itself could only ever redirect within its own source.
/// </remarks>
/// <param name="inner">The strategy that plays the resolved name.</param>
/// <param name="aliases">The redirects in force.</param>
internal sealed class AliasSoundPlaybackStrategy(ISoundPlaybackStrategy inner, ISoundAliasRegistry aliases)
    : ISoundPlaybackStrategy
{
    private readonly ISoundPlaybackStrategy _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly ISoundAliasRegistry _aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));

    /// <inheritdoc/>
    public bool PlaySound(string soundName) => _inner.PlaySound(_aliases.Resolve(soundName));

    /// <inheritdoc/>
    public void Abort() => _inner.Abort();

    /// <inheritdoc/>
    public bool TryGetDuration(string soundName, out TimeSpan duration)
        => _inner.TryGetDuration(_aliases.Resolve(soundName), out duration);
}
