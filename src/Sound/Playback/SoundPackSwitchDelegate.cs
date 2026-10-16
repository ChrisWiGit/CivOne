using System;
using CivOne.Enums;
using CivOne.Services;
using CivOne.Tasks;

namespace CivOne.Sound.Playback;

/// <summary>
/// Switches the game to another sound pack.
/// </summary>
/// <remarks>
/// <para>
/// Writing the setting also switches sound on, or off for "None".
/// The in-game sound option follows, so the new pack is heard at once.
/// </para>
/// <para>
/// Used by Setup and by the sound pack picker, so both behave the same.
/// Every step can be replaced, which lets tests run without a game or a runtime.
/// </para>
/// </remarks>
/// <param name="writeSoundPack">Stores the pack id; <c>null</c> writes the global setting.</param>
/// <param name="readSound">Reads whether sound is on in Setup; <c>null</c> reads the global setting.</param>
/// <param name="setGameSound">Sets the in-game sound option; <c>null</c> sets it on the running game, if any.</param>
/// <param name="abort">Stops what is playing; <c>null</c> stops the active playback strategy.</param>
/// <param name="warmUp">Starts rendering the new pack; <c>null</c> warms up the active playback strategy.</param>
/// <param name="notify">Tells the player the new pack name; <c>null</c> shows a message while a game runs.</param>
internal sealed class SoundPackSwitchDelegate(
	Action<string>? writeSoundPack = null,
	Func<GameOption>? readSound = null,
	Action<bool>? setGameSound = null,
	Action? abort = null,
	Action? warmUp = null,
	Action<string>? notify = null)
{
	private readonly Action<string> _writeSoundPack = writeSoundPack ?? (packId => Settings.Instance.SoundPack = packId);
	private readonly Func<GameOption> _readSound = readSound ?? (() => Settings.Instance.Sound);
	private readonly Action<bool> _setGameSound = setGameSound ?? SetRunningGameSound;
	private readonly Action _abort = abort ?? SoundPlaybackStrategyProvider.Abort;
	private readonly Action _warmUp = warmUp ?? SoundPlaybackStrategyProvider.WarmUp;
	private readonly Action<string> _notify = notify ?? NotifyRunningGame;

	/// <summary>
	/// Selects a sound pack.
	/// </summary>
	/// <param name="packId">
	/// A pack id, <see cref="SoundPlaybackStrategyConstants.WaveSoundPack"/> or
	/// <see cref="SoundPlaybackStrategyConstants.NoSoundPack"/>.
	/// </param>
	/// <param name="packName">
	/// Returns the name shown to the player.
	/// Called after the switch, so it may read the new selection.
	/// </param>
	public void Switch(string? packId, Func<string> packName)
	{
		ArgumentNullException.ThrowIfNull(packName);

		_writeSoundPack(packId ?? string.Empty);

		bool soundOn = _readSound() != GameOption.Off;
		if (!soundOn)
		{
			_abort();
		}
		_setGameSound(soundOn);

		// Also covers picking the pack that was already selected, which changes no setting and
		// would therefore not be noticed anywhere else.
		_warmUp();

		_notify(packName());
	}

	private static void SetRunningGameSound(bool soundOn)
	{
		if (!Game.Started)
		{
			return;
		}
		Game.Instance.Sound = soundOn;
	}

	private static void NotifyRunningGame(string packName)
	{
		if (!Game.Started)
		{
			return;
		}
		ITranslationService translation = TranslationServiceFactory.GetCurrent();
		GameTask.Enqueue(Message.General(translation.TranslateFormatted("Sound pack switched to {0}.", packName)));
	}
}
