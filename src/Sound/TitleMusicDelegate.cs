using CivOne.Enums;
using CivOne.Sound.Playback;

namespace CivOne.Sound;

/// <summary>
/// Starts and stops the title music of the main menu.
/// </summary>
/// <remarks>
/// <para>
/// The music begins with the credits screen and runs far longer than that screen does.
/// Nothing ends it on its own, so whoever takes over - above all a game that is being loaded -
/// has to stop it.
/// </para>
/// <para>
/// No game exists while the main menu is shown, so <see cref="BaseInstance.PlaySound"/> would
/// swallow the call. The playback strategy is therefore used directly, and the sound setting -
/// the only part of that gate which still applies here - is checked below.
/// </para>
/// </remarks>
internal sealed class TitleMusicDelegate
{
	private readonly ISoundPlaybackStrategy? _strategy;

	private ISoundPlaybackStrategy Strategy => _strategy ?? SoundPlaybackStrategyProvider.Current;

	/// <summary>
	/// Creates the delegate.
	/// </summary>
	/// <param name="strategy">
	/// Playback strategy to use, or <c>null</c> to take the one the chosen sound source provides.
	/// </param>
	public TitleMusicDelegate(ISoundPlaybackStrategy? strategy = null) => _strategy = strategy;

	/// <summary>
	/// Starts the title music, unless sound is switched off.
	/// </summary>
	/// <returns><c>true</c> when the music was started.</returns>
	public bool Start()
	{
		if (Settings.Instance.Sound == GameOption.Off)
		{
			return false;
		}

		return Strategy.PlaySound(SoundNames.MusicTitle);
	}

	/// <summary>
	/// Stops the music, so it does not run on underneath the game that follows.
	/// </summary>
	public void Stop() => Strategy.Abort();
}
