namespace CivOne.Units;

/// <summary>
/// Audio playback service specific to confrontation logic.
/// Abstracts Common.Audio static dependency for dependency injection.
/// </summary>
public interface IAudioPlayService
{
    /// <summary>
    /// Play a sound effect by name.
    /// </summary>
    void PlaySound(string soundName);
}
