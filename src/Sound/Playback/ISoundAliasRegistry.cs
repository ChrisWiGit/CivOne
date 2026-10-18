namespace CivOne.Sound.Playback;

/// <summary>
/// Redirects one sound name to another before anything tries to play it.
/// </summary>
/// <remarks>
/// This is how a plugin replaces a sound it does not own: rather than overwriting a tune or a wave
/// file, it points an existing name at a name of its own, which then resolves to whatever the
/// plugin shipped.
/// </remarks>
internal interface ISoundAliasRegistry
{
    /// <summary>
    /// Resolves a sound name to the name that should actually be played.
    /// </summary>
    /// <param name="soundName">The name the game logic asked for.</param>
    /// <returns>
    /// The name to play. The same name when nothing redirects it, so callers never have to check.
    /// </returns>
    string Resolve(string soundName);

    /// <summary>
    /// Redirects a sound name to another name.
    /// </summary>
    /// <param name="soundName">The name the game logic asks for.</param>
    /// <param name="targetName">The name to play instead, or <c>null</c> to drop a redirect.</param>
    void SetAlias(string soundName, string? targetName);
}
