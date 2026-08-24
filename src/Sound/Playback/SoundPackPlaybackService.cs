using System;
using System.IO;
using System.Linq;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;

#nullable enable

internal sealed class SoundPackPlaybackService(SoundPackWaveRenderService renderer)
{
	public bool TryPlay(string soundName, string packId)
	{
		string packFolder = Path.Combine(Settings.Instance.SoundsDirectory, packId);
		string indexPath = Path.Combine(packFolder, SoundPackIndex.FileName);
		if (!File.Exists(indexPath)) return false;

		SoundPackIndex index = SoundPackIndexJson.Load(indexPath);
		if (!index.SoundNames.TryGetValue(soundName, out int tuneId)) return false;

		SoundPackIndexEntry? entry = index.Tunes.FirstOrDefault(tune => tune.TuneId == tuneId);
		if (entry == null) return false;

		if (string.IsNullOrEmpty(entry.File)) return true;

		return TryPlayTune(packId, entry);
	}

	public bool TryPlayTune(string packId, SoundPackIndexEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry);

		string? fileName = entry.File;
		if (string.IsNullOrEmpty(fileName)) return false;

		string packFolder = Path.Combine(Settings.Instance.SoundsDirectory, packId);
		string? soundFile = renderer.Render(packFolder, fileName);
		if (soundFile == null) return false;

		RuntimeHandler.Runtime.PlaySound(soundFile);
		return true;
	}
}