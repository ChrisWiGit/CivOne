using System;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;

internal static class SoundPlaybackStrategyProvider
{
	private static SoundPackPlaybackService? _soundPackPlaybackService;
	private static SoundAliasRegistry? _aliases;
	private static string? _activeSoundPack;
	private static ISoundPlaybackStrategy? _activeStrategy;

	private static readonly SoundPackSelectionDelegate _selection = new();

	private static SoundPackPlaybackService SoundPackPlaybackService
		=> _soundPackPlaybackService ??= new(RuntimeHandler.Runtime, new SoundPackRenderQueue());

	/// <summary>
	/// The sound name redirects in force, shared by every strategy this provider hands out.
	/// </summary>
	public static ISoundAliasRegistry Aliases => _aliases ??= new SoundAliasRegistry();

	/// <summary>
	/// The id of the sound source in use, resolved from the setting.
	/// </summary>
	public static string SelectedPack => _selection.Resolve();

	public static ISoundPlaybackStrategy Current
	{
		get
		{
			string soundPack = SelectedPack;
			if (_activeStrategy != null && string.Equals(_activeSoundPack, soundPack, StringComparison.Ordinal)) return _activeStrategy;

			_activeSoundPack = soundPack;
			_activeStrategy = SoundPlaybackStrategyFactory.Create(soundPack, SoundPackPlaybackService, Aliases);

			// A pack that was just switched to has nothing rendered yet, so start on it right away
			// rather than when the first sound is due.
			WarmUp();

			return _activeStrategy;
		}
	}

	public static bool PlayTune(string packId, SoundPackIndexEntry entry)
	{
		return SoundPackPlaybackService.TryPlayTune(packId, entry);
	}

	/// <summary>
	/// Starts rendering the sounds of the pack currently in use, before the game asks for them.
	/// </summary>
	/// <remarks>
	/// Safe to call at any time and as often as wanted: a pack that is already being worked on is
	/// not started again. Does nothing when the chosen source is not a converted pack, which is the
	/// case for the wave files and for silence.
	/// </remarks>
	public static void WarmUp()
	{
		string soundPack = SelectedPack;

		if (string.Equals(soundPack, SoundPlaybackStrategyConstants.NoSoundPack, StringComparison.OrdinalIgnoreCase)) return;
		if (string.Equals(soundPack, SoundPlaybackStrategyConstants.WaveSoundPack, StringComparison.OrdinalIgnoreCase)) return;

		SoundPackPlaybackService.WarmUp(soundPack);
	}

	/// <summary>
	/// Gives a sound that had to be rendered first the chance to start.
	/// </summary>
	/// <remarks>
	/// Sound pack tunes are rendered off the game thread, so a sound the game asked for may only
	/// become playable a moment later. Calling this once per frame from the game thread is what
	/// starts it; without it such a sound would never be heard.
	/// </remarks>
	public static void Process() => SoundPackPlaybackService.Process();
}
