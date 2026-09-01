using CivOne.Enums;
using CivOne.Sound.Playback;

namespace CivOne.Sound;

/// <summary>
/// Starts and stops the evolution music that accompanies the generation of a new world.
/// </summary>
/// <remarks>
/// <para>
/// The music begins when the generation of a new world starts and runs until the screen that
/// follows the generation takes over. Nothing else ends it, so the screen that shows the world
/// being generated has to stop it when it hands over.
/// </para>
/// <para>
/// No game exists while a world is being generated (<c>Game.CreateGame</c> only runs once the new
/// game setup is finished), so <see cref="BaseInstance.PlaySound"/> would swallow the call. The
/// playback strategy is therefore used directly, and the sound setting - the only part of that
/// gate which still applies here - is checked below.
/// </para>
/// </remarks>
internal sealed class WorldGenerationMusicDelegate
{
    private readonly ISoundPlaybackStrategy? _strategy;

    private ISoundPlaybackStrategy Strategy => _strategy ?? SoundPlaybackStrategyProvider.Current;

    /// <summary>
    /// Creates the delegate.
    /// </summary>
    /// <param name="strategy">
    /// Playback strategy to use, or <c>null</c> to take the one the chosen sound source provides.
    /// </param>
    public WorldGenerationMusicDelegate(ISoundPlaybackStrategy? strategy = null) => _strategy = strategy;

    /// <summary>
    /// Starts the evolution music, unless sound is switched off.
    /// </summary>
    /// <returns><c>true</c> when the music was started.</returns>
    public bool Start()
    {
        if (Settings.Instance.Sound == GameOption.Off)
        {
            return false;
        }

        return Strategy.PlaySound(SoundNames.MusicEvolution);
    }

    /// <summary>
    /// Stops the music, so it does not run on underneath the next screen.
    /// </summary>
    public void Stop() => Strategy.Abort();
}
