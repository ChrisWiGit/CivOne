using System;
using System.IO;
using System.Linq;

namespace CivOne.Sound.Playback;

internal sealed class WaveSoundPlaybackStrategy : ISoundPlaybackStrategy
{
	public bool PlaySound(string soundName)
	{
		string? soundFile = GetWaveSoundFile(soundName);
		if (soundFile == null) return false;

		RuntimeHandler.Runtime.PlaySound(soundFile);
		return true;
	}

	public void Abort()
	{
		RuntimeHandler.Runtime.StopSound();
	}

	private static string? GetWaveSoundFile(string soundName)
	{
		string soundsDirectory = Settings.Instance.SoundsDirectory;
		if (!Directory.Exists(soundsDirectory)) return null;

		return Directory.GetFiles(soundsDirectory)
			.FirstOrDefault(file => Path.GetFileName(file).Equals($"{soundName}.wav", StringComparison.OrdinalIgnoreCase));
	}
}