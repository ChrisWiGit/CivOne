using System;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;

internal static class SoundPlaybackStrategyProvider
{
	private static readonly SoundPackPlaybackService _soundPackPlaybackService = new(new SoundPackWaveRenderService());
	private static string? _activeSoundPack;
	private static ISoundPlaybackStrategy? _activeStrategy;

	public static ISoundPlaybackStrategy Current
	{
		get
		{
			string soundPack = Settings.Instance.SoundPack;
			if (_activeStrategy != null && string.Equals(_activeSoundPack, soundPack, StringComparison.Ordinal)) return _activeStrategy;

			_activeSoundPack = soundPack;
			_activeStrategy = SoundPlaybackStrategyFactory.Create(soundPack);
			return _activeStrategy;
		}
	}

	public static bool PlayTune(string packId, SoundPackIndexEntry entry)
	{
		return _soundPackPlaybackService.TryPlayTune(packId, entry);
	}
}