using System;
using System.IO;
using System.Threading.Tasks;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;

/// <summary>
/// Plays a named sound from a converted sound pack.
/// </summary>
/// <remarks>
/// <para>
/// A tune that is not rendered yet would cost the game thread up to about three seconds, which the
/// player sees as a freeze. This service therefore never renders: it either plays a wave file that
/// is already there, or it hands the work to <see cref="ISoundPackRenderQueue"/> and starts the
/// sound from <see cref="Process"/> once the render is done.
/// </para>
/// <para>
/// Only one sound can be waiting at a time. Asking for another sound, or aborting, drops the one
/// that has not started yet - the same way a new sound replaces a playing one.
/// </para>
/// <para>
/// Every member is meant to be called from the game thread only.
/// </para>
/// </remarks>
internal sealed class SoundPackPlaybackService
{
	private readonly IRuntime _runtime;
	private readonly ISoundPackRenderQueue _queue;
	private readonly ArrangementPickerDelegate _arrangements;

	private Task<string?>? _pending;

	/// <summary>
	/// Creates the service.
	/// </summary>
	/// <param name="runtime">Runtime used to play sounds and write diagnostic messages.</param>
	/// <param name="queue">Queue that renders tunes off the game thread.</param>
	/// <param name="arrangements">
	/// Picks which arrangement of a tune to play, or <c>null</c> for a random choice.
	/// </param>
	public SoundPackPlaybackService(IRuntime runtime, ISoundPackRenderQueue queue, ArrangementPickerDelegate? arrangements = null)
	{
		ArgumentNullException.ThrowIfNull(runtime);
		ArgumentNullException.ThrowIfNull(queue);

		_runtime = runtime;
		_queue = queue;
		_arrangements = arrangements ?? new ArrangementPickerDelegate();
	}

	/// <summary>
	/// Starts rendering a pack in the background before the game asks for any of its sounds.
	/// </summary>
	/// <remarks>
	/// Without this the first sound of a session is the one that pays for the render. Warming up as
	/// early as the pack is known - when the game starts, or right after the original sound data has
	/// been converted - usually gets the work done before anything is due.
	/// </remarks>
	/// <param name="packId">Id of the pack, or empty to do nothing.</param>
	public void WarmUp(string packId)
	{
		if (string.IsNullOrEmpty(packId)) return;

		_queue.WarmPack(Path.Combine(Settings.Instance.SoundsDirectory, packId));
	}

	/// <summary>
	/// Plays the tune a sound name maps to.
	/// </summary>
	/// <param name="soundName">Name the game logic uses, e.g. <see cref="SoundNames.MusicTitle"/>.</param>
	/// <param name="packId">Id of the pack to play from.</param>
	/// <returns><c>true</c> when the pack handled the sound, including when it is deliberately silent.</returns>
	public bool TryPlay(string soundName, string packId)
	{
		// Whatever was still waiting belongs to the previous sound, no matter how this call ends.
		CancelPending();

		string packFolder = Path.Combine(Settings.Instance.SoundsDirectory, packId);
		string indexPath = Path.Combine(packFolder, SoundPackIndex.FileName);
		if (!File.Exists(indexPath))
		{
			_runtime.Log("Sound pack '{0}' has no index file.", packId);
			return false;
		}

		SoundPackIndex index = SoundPackIndexJson.Load(indexPath);
		if (!index.TryGetByName(soundName, out SoundPackIndexEntry? entry))
		{
			_runtime.Log("Sound pack '{0}' has no tune for sound '{1}'.", packId, soundName);
			return false;
		}

		if (string.IsNullOrEmpty(entry.File)) return true;

		return TryPlayTune(packId, entry);
	}

	/// <summary>
	/// Plays one tune of a pack, rendering it in the background first if it is not cached yet.
	/// </summary>
	/// <param name="packId">Id of the pack.</param>
	/// <param name="entry">Index entry of the tune.</param>
	/// <returns><c>true</c> when the tune was started or is being rendered.</returns>
	public bool TryPlayTune(string packId, SoundPackIndexEntry entry)
		=> TryPlayTune(packId, entry, _arrangements.Pick(entry?.ArrangementCount ?? 1));

	/// <summary>
	/// Plays one arrangement of a tune.
	/// </summary>
	/// <param name="packId">Id of the pack.</param>
	/// <param name="entry">Index entry of the tune.</param>
	/// <param name="arrangement">Which arrangement to play.</param>
	/// <returns>
	/// <c>true</c> when the tune was handed to the runtime, or when its render has been queued and
	/// it will start as soon as that is finished.
	/// </returns>
	public bool TryPlayTune(string packId, SoundPackIndexEntry entry, int arrangement)
	{
		ArgumentNullException.ThrowIfNull(entry);

		CancelPending();

		string? fileName = entry.File;
		if (string.IsNullOrEmpty(fileName)) return false;

		string packFolder = Path.Combine(Settings.Instance.SoundsDirectory, packId);

		// The first sound of a session also sets the rest of the pack rendering in the background,
		// so the sounds that come later are ready before they are asked for.
		_queue.WarmPack(packFolder);

		string? cached = _queue.TryGetCached(packFolder, fileName, arrangement);
		if (cached != null)
		{
			_runtime.PlaySound(cached);
			return true;
		}

		_pending = _queue.Request(packFolder, fileName, arrangement);
		return true;
	}

	/// <summary>
	/// Starts a sound whose render has finished in the meantime.
	/// </summary>
	/// <remarks>
	/// Called once per frame from the game thread. Does nothing while no sound is waiting, and
	/// nothing when the render failed - a tune that cannot be rendered is simply silent.
	/// </remarks>
	public void Process()
	{
		Task<string?>? pending = _pending;
		if (pending == null || !pending.IsCompleted) return;

		_pending = null;
		if (pending.Status != TaskStatus.RanToCompletion) return;

		string? soundFile = pending.Result;
		if (soundFile == null) return;

		_runtime.PlaySound(soundFile);
	}

	/// <summary>
	/// Forgets a sound that has not started yet, so it cannot interrupt whatever plays instead.
	/// </summary>
	/// <remarks>
	/// The render itself keeps running - it is nearly done by then and its result stays in the
	/// cache for the next time the tune is asked for.
	/// </remarks>
	public void CancelPending() => _pending = null;
}
