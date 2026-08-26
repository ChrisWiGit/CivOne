using System;
using System.Collections.Generic;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;

internal static class SoundPlaybackStrategyProvider
{
	private static readonly SoundPackPlaybackService _soundPackPlaybackService = new(new SoundPackRenderQueue());
	private static string? _activeSoundPack;
	private static ISoundPlaybackStrategy? _activeStrategy;

	public static ISoundPlaybackStrategy Current
	{
		get
		{
			string soundPack = Settings.Instance.SoundPack;
			if (_activeStrategy != null && string.Equals(_activeSoundPack, soundPack, StringComparison.Ordinal)) return _activeStrategy;

			_activeSoundPack = soundPack;
			_activeStrategy = SoundPlaybackStrategyFactory.Create(soundPack, _soundPackPlaybackService);

			// A pack that was just switched to has nothing rendered yet, so start on it right away
			// rather than when the first sound is due.
			WarmUp();

			return _activeStrategy;
		}
	}

	public static bool PlayTune(string packId, SoundPackIndexEntry entry)
	{
		return _soundPackPlaybackService.TryPlayTune(packId, entry);
	}

	/// <summary>
	/// Starts rendering the sounds of the pack currently in use, before the game asks for them.
	/// </summary>
	/// <remarks>
	/// Safe to call at any time and as often as wanted: a pack that is already being worked on is
	/// not started again. Does nothing when no pack is in play, which is the case for the original
	/// wave files, for silence, and while several packs are available without one being chosen.
	/// </remarks>
	public static void WarmUp()
	{
		string soundPack = Settings.Instance.SoundPack;

		if (string.Equals(soundPack, SoundPlaybackStrategyConstants.NoSoundPack, StringComparison.OrdinalIgnoreCase)) return;
		if (string.Equals(soundPack, SoundPlaybackStrategyConstants.WaveSoundPack, StringComparison.OrdinalIgnoreCase)) return;

		if (!string.IsNullOrEmpty(soundPack))
		{
			_soundPackPlaybackService.WarmUp(soundPack);
			return;
		}

		// Automatic choice, so warm up what would actually be played: the same single pack
		// AutoSoundPlaybackStrategy settles on.
		IReadOnlyList<SoundPackSummary> packs = SoundPackCatalog.GetAvailablePacks(Settings.Instance.SoundsDirectory);
		if (packs.Count == 1) _soundPackPlaybackService.WarmUp(packs[0].PackId);
	}

	/// <summary>
	/// Gives a sound that had to be rendered first the chance to start.
	/// </summary>
	/// <remarks>
	/// Sound pack tunes are rendered off the game thread, so a sound the game asked for may only
	/// become playable a moment later. Calling this once per frame from the game thread is what
	/// starts it; without it such a sound would never be heard.
	/// </remarks>
	public static void Process() => _soundPackPlaybackService.Process();
}
