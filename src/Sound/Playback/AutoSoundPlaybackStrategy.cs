using System.Collections.Generic;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;

internal sealed class AutoSoundPlaybackStrategy(
	SoundPackPlaybackService soundPackPlaybackService,
	ISoundPlaybackStrategy fallback) : ISoundPlaybackStrategy
{
	public bool PlaySound(string soundName)
	{
		IReadOnlyList<SoundPackSummary> packs = SoundPackCatalog.GetAvailablePacks(Settings.Instance.SoundsDirectory);
		if (packs.Count == 1 && soundPackPlaybackService.TryPlay(soundName, packs[0].PackId)) return true;

		return fallback.PlaySound(soundName);
	}

	public void Abort()
	{
		// A tune that is still being rendered must not start after the game has silenced everything.
		soundPackPlaybackService.CancelPending();
		RuntimeHandler.Runtime.StopSound();
	}
}