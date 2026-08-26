using System;
using System.IO;
using System.Linq;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;



/// <summary>
/// Plays a named sound from a converted sound pack.
/// </summary>
internal sealed class SoundPackPlaybackService(
	SoundPackWaveRenderService renderer,
	ArrangementPickerDelegate? arrangements = null)
{
	private readonly ArrangementPickerDelegate _arrangements = arrangements ?? new ArrangementPickerDelegate();

	/// <summary>
	/// Plays the tune a sound name maps to.
	/// </summary>
	/// <param name="soundName">Name the game logic uses, e.g. <c>"opening"</c>.</param>
	/// <param name="packId">Id of the pack to play from.</param>
	/// <returns><c>true</c> when the pack handled the sound, including when it is deliberately silent.</returns>
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

	/// <summary>
	/// Plays one tune of a pack, rendering it first if it is not cached yet.
	/// </summary>
	/// <param name="packId">Id of the pack.</param>
	/// <param name="entry">Index entry of the tune.</param>
	/// <returns><c>true</c> when the tune was handed to the runtime.</returns>
	public bool TryPlayTune(string packId, SoundPackIndexEntry entry)
		=> TryPlayTune(packId, entry, _arrangements.Pick(entry?.ArrangementCount ?? 1));

	/// <summary>
	/// Plays one arrangement of a tune.
	/// </summary>
	/// <param name="packId">Id of the pack.</param>
	/// <param name="entry">Index entry of the tune.</param>
	/// <param name="arrangement">Which arrangement to play.</param>
	/// <returns><c>true</c> when the tune was handed to the runtime.</returns>
	public bool TryPlayTune(string packId, SoundPackIndexEntry entry, int arrangement)
	{
		ArgumentNullException.ThrowIfNull(entry);

		string? fileName = entry.File;
		if (string.IsNullOrEmpty(fileName)) return false;

		string packFolder = Path.Combine(Settings.Instance.SoundsDirectory, packId);
		string? soundFile = renderer.Render(packFolder, fileName, arrangement);
		if (soundFile == null) return false;

		RuntimeHandler.Runtime.PlaySound(soundFile);
		return true;
	}
}
