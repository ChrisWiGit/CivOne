using System;
using CivOne.Enums;

namespace CivOne.Sound.Playback;

/// <summary>
/// Plays sounds only while a game is running and sound is switched on,
/// both in the game options and in the setup.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="PlaySound"/> is gated.
/// Stopping and reading a duration always pass through, so a tune that is playing can still be stopped
/// after the player switched sound off.
/// </para>
/// <para>
/// Without an inner strategy, every call goes to the strategy the sound setting selects,
/// resolved at the time of the call, so a change of the sound pack takes effect at once.
/// </para>
/// </remarks>
/// <param name="inner">Plays the sounds; <c>null</c> uses the strategy the sound setting selects.</param>
/// <param name="game">Holds the in-game sound option; <c>null</c> uses the running game, if any.</param>
/// <param name="settings">Holds the sound setting of the setup; <c>null</c> uses the global settings.</param>
internal sealed class SettingsGatedSoundPlaybackStrategy(
	ISoundPlaybackStrategy? inner = null,
	IGameSoundOption? game = null,
	ISettings? settings = null) : ISoundPlaybackStrategy
{
	private readonly ISoundPlaybackStrategy? _inner = inner;
	private readonly IGameSoundOption? _game = game;
	private readonly ISettings? _settings = settings;

	private ISoundPlaybackStrategy Inner => _inner ?? SoundPlaybackStrategyProvider.Current;

	private IGameSoundOption? Game => _game ?? (CivOne.Game.Started ? CivOne.Game.Instance : null);

	private ISettings Settings => _settings ?? CivOne.Settings.Instance;

	/// <inheritdoc/>
	/// <remarks>
	/// Reports success without playing when the sound name is empty or sound is off,
	/// because there is no failure the caller would have to handle.
	/// </remarks>
	public bool PlaySound(string soundName)
	{
		if (string.IsNullOrEmpty(soundName) || !IsSoundOn())
		{
			return true;
		}

		return Inner.PlaySound(soundName);
	}

	/// <inheritdoc/>
	public void Abort()
	{
		if (_inner == null)
		{
			// Unlike Current.Abort(), this does not create a strategy just to stop nothing.
			SoundPlaybackStrategyProvider.Abort();
			return;
		}

		_inner.Abort();
	}

	/// <inheritdoc/>
	public bool TryGetDuration(string soundName, out TimeSpan duration) => Inner.TryGetDuration(soundName, out duration);

	private bool IsSoundOn()
	{
		IGameSoundOption? game = Game;
		if (game == null || !game.Sound)
		{
			return false;
		}

		return Settings.Sound != GameOption.Off;
	}
}
