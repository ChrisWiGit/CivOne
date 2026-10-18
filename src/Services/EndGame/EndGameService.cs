using System;
using System.Linq;
using System.Threading.Tasks;
using CivOne.Screens;
using CivOne.Screens.Dialogs;
using CivOne.Services.Screen;
using CivOne.Sound;
using CivOne.Sound.Playback;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Orchestrates the end-game screen sequence based on the reason the game ended.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Follows the original game. The end-game flow is power graph, score, Famous Persons and Hall of Fame entry.
	/// It runs once: afterwards the "scoring completed" latch is set, and later end events (a second spaceship,
	/// the final year) show their screens and ask "keep playing?" again, but record no further score.
	/// After a conquest the game always ends. A human defeat shows no score.
	/// </para>
	/// <para>
	/// Deliberate deviations: the replay the original shows before the score is skipped, because CivOneX has no
	/// replay screen yet; and "No, I'm done" leads to the credits instead of an exit to DOS.
	/// See <c>docs/plan-endOfGameContinue.impl.md</c>.
	/// </para>
	/// </remarks>
	/// <param name="screenCommand">Provides screen management commands (AddScreen, DestroyScreen).</param>
	/// <param name="screenQuery">Provides screen stack queries (Screens, TopScreen).</param>
	/// <param name="gameService">Provides game state management (End).</param>
	/// <param name="screens">Creates the screens; <c>null</c> creates the real game screens.</param>
	/// <param name="endOfGameState">Holds the "scoring completed" latch; <c>null</c> uses the running game.</param>
	/// <param name="sound">
	/// Plays and stops the tunes; <c>null</c> uses the game's sound, which respects the sound options.
	/// </param>
	internal class EndGameService(
		IScreenCommandService screenCommand,
		IScreenQueryService screenQuery,
		IGameService gameService,
		IEndGameScreenFactory? screens = null,
		IEndOfGameState? endOfGameState = null,
		ISoundPlaybackStrategy? sound = null) : IEndGameService
	{
		private readonly IScreenCommandService _screenCommand = screenCommand;
		private readonly IScreenQueryService _screenQuery = screenQuery;
		private readonly IGameService _gameService = gameService;
		private readonly IEndOfGameState? _endOfGameState = endOfGameState;
		private IEndGameScreenFactory? _screens = screens;
		private ISoundPlaybackStrategy? _sound = sound;

		private IEndGameScreenFactory Screens => _screens ??= EndGameServiceFactory.CreateScreenFactory();

		private IEndOfGameState State => _endOfGameState ?? Game.Instance;

		private ISoundPlaybackStrategy Sound => _sound ??= SoundPlaybackStrategyFactory.CreateGated();

		/// <inheritdoc/>
		public async Task HandleConquestAsync()
		{
			ClearScreensAndTasks();
			await ShowScreenAsync(Screens.Victory()).ConfigureAwait(continueOnCapturedContext: true);
			await ShowFinalScoreOnceAsync().ConfigureAwait(continueOnCapturedContext: true);
			ReturnToCredits();
		}

		/// <inheritdoc/>
		public async Task HandleDefeatAsync()
		{
			ClearScreensAndTasks();
			await ShowScreenAsync(Screens.Defeat()).ConfigureAwait(continueOnCapturedContext: true);
			// As in the original, a defeated player gets no score and no Hall of Fame entry.
			ReturnToCredits();
		}

		/// <inheritdoc/>
		public async Task HandleRetireAsync()
		{
			ClearScreensAndTasks();
			if (State.ScoringCompleted)
			{
				// As in the original, after "keep playing" retiring shows only the power graph.
				await ShowScreenAsync(Screens.PowerGraph()).ConfigureAwait(continueOnCapturedContext: true);
			}
			else
			{
				await ShowFinalScoreOnceAsync().ConfigureAwait(continueOnCapturedContext: true);
			}

			ReturnToCredits();
		}

		/// <inheritdoc/>
		public async Task<bool> HandleGameEndAsync(GameEndSequence sequence)
		{
			ArgumentNullException.ThrowIfNull(sequence);

			if (sequence.ConquestNews != null)
			{
				PlaySound(SoundNames.MusicWin);
				await ShowScreenAsync(Screens.Newspaper(sequence.ConquestNews)).ConfigureAwait(continueOnCapturedContext: true);
				await ShowScreenAsync(Screens.Conquest()).ConfigureAwait(continueOnCapturedContext: true);
			}
			else if (sequence.SpaceVictoryText != null)
			{
				await ShowSpaceVictoryAsync(sequence).ConfigureAwait(continueOnCapturedContext: true);
				await ShowNewsAsync(sequence.SpaceshipNews).ConfigureAwait(continueOnCapturedContext: true);
			}

			if (sequence.YearEndNews != null)
			{
				// Like the original, the leader's tune plays with the news and stops before the end-game flow.
				PlaySound(sequence.YearEndTune);
				await ShowNewsAsync(sequence.YearEndNews).ConfigureAwait(continueOnCapturedContext: true);
				Sound.Abort();
			}

			if (sequence.ConquestNews != null)
			{
				await ShowScreenAsync(Screens.Victory()).ConfigureAwait(continueOnCapturedContext: true);
			}

			await ShowFinalScoreOnceAsync().ConfigureAwait(continueOnCapturedContext: true);

			if (sequence.ConquestNews != null
				|| !await AskKeepPlayingAsync().ConfigureAwait(continueOnCapturedContext: true))
			{
				ReturnToCredits();
				return false;
			}

			return true;
		}

		/// <summary>
		/// Shows the flight to Alpha Centauri with the title music. For the human player, the win music replaces it
		/// when the space station appears; for a rival the title music keeps playing over the human player's palace.
		/// The flight is timed to the length of the title music.
		/// </summary>
		/// <remarks>
		/// The original plays tune 3 during the flight and tune 34 for the space station.
		/// Which music these tune numbers are is not known; the title and the win music are an assumption.
		/// </remarks>
		private async Task ShowSpaceVictoryAsync(GameEndSequence sequence)
		{
			PlaySound(SoundNames.MusicTitle);

			Action? onLanding = null;
			if (sequence.SpaceshipWinnerIsHuman)
			{
				onLanding = () =>
				{
					Sound.Abort();
					PlaySound(SoundNames.MusicWin);
				};
			}

			PalaceData? rivalBackground = sequence.SpaceshipWinnerIsHuman ? null : sequence.HumanPalace;
			TimeSpan? flightDuration = Sound.TryGetDuration(SoundNames.MusicTitle, out TimeSpan titleLength)
				? titleLength
				: null;
			IScreen spaceVictory = Screens.SpaceVictory(sequence.SpaceVictoryText!, rivalBackground, onLanding, flightDuration);

			await ShowScreenAsync(spaceVictory).ConfigureAwait(continueOnCapturedContext: true);
			Sound.Abort();
		}

		private void PlaySound(string? soundName)
		{
			if (string.IsNullOrEmpty(soundName))
			{
				return;
			}

			_ = Sound.PlaySound(soundName);
		}

		/// <summary>
		/// Shows the end-game flow unless the "scoring completed" latch is set, then sets the latch.
		/// </summary>
		private async Task ShowFinalScoreOnceAsync()
		{
			if (!State.ScoringCompleted)
			{
				await ShowScreenAsync(Screens.PowerGraph()).ConfigureAwait(continueOnCapturedContext: true);
				await ShowScreenAsync(Screens.CivilizationScore()).ConfigureAwait(continueOnCapturedContext: true);
				await ShowScreenAsync(Screens.FamousPersons()).ConfigureAwait(continueOnCapturedContext: true);
				await ShowScreenAsync(Screens.HallOfFame()).ConfigureAwait(continueOnCapturedContext: true);
			}

			State.ScoringCompleted = true;
		}

		private async Task<bool> AskKeepPlayingAsync()
		{
			IKeepPlayingDialog dialog = Screens.KeepPlaying();
			await ShowScreenAsync(dialog).ConfigureAwait(continueOnCapturedContext: true);
			return dialog.KeepPlaying;
		}

		private async Task ShowNewsAsync(string[]? lines)
		{
			if (lines == null)
			{
				return;
			}

			await ShowScreenAsync(Screens.Newspaper(lines)).ConfigureAwait(continueOnCapturedContext: true);
		}

		private Task<bool> ShowScreenAsync(IScreen screen)
		{
			TaskCompletionSource<bool> tcs = new();

			void OnClosed(object? _, EventArgs __)
			{
				screen.Closed -= OnClosed;
				tcs.TrySetResult(true);
			}

			screen.Closed += OnClosed;
			_screenCommand.AddScreen(screen);
			return tcs.Task;
		}

		private void ClearScreensAndTasks()
		{
			GameTask.ClearAll();
			foreach (IScreen screen in _screenQuery.Screens.ToArray())
			{
				_screenCommand.DestroyScreen(screen);
			}
		}

		private void ReturnToCredits()
		{
			ClearScreensAndTasks();
			_gameService.End();
			_screenCommand.AddScreen(Screens.Credits());
		}
	}
}
