using System;
using System.Linq;
using System.Threading.Tasks;
using CivOne.Persistence.Game;
using CivOne.Screens;
using CivOne.Services.Screen;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Orchestrates the end-game screen sequence based on the reason the game ended.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Follows the original game:
	/// power graph, score, Famous Persons and Hall of Fame for retirement, conquest and every spaceship arrival,
	/// including a rival's; a human defeat shows no score.
	/// </para>
	/// <para>
	/// Deliberate deviations: the replay the original shows before the score is skipped, because CivOneX has no
	/// replay screen yet; and after a spaceship arrival the original asks "keep playing?", which CivOneX does not
	/// offer yet, so the game always ends.
	/// See <c>docs/plan-civilizationScore.impl.md</c>, Phase 7 and deviation 15.
	/// </para>
	/// </remarks>
	/// <param name="screenCommand">Provides screen management commands (AddScreen, DestroyScreen).</param>
	/// <param name="screenQuery">Provides screen stack queries (Screens, TopScreen).</param>
	/// <param name="gameService">Provides game state management (End).</param>
	/// <param name="winner">The player who won the game.</param>
	/// <param name="screens">Creates the screens; <c>null</c> creates the real game screens.</param>
	internal class EndGameService(
		IScreenCommandService screenCommand,
		IScreenQueryService screenQuery,
		IGameService gameService,
		IPlayer winner,
		IEndGameScreenFactory? screens = null) : IEndGameService
	{
		private readonly IScreenCommandService _screenCommand = screenCommand;
		private readonly IScreenQueryService _screenQuery = screenQuery;
		private readonly IGameService _gameService = gameService;
		private readonly IPlayer _winner = winner;
		private IEndGameScreenFactory? _screens = screens;

		private IEndGameScreenFactory Screens => _screens ??= EndGameServiceFactory.CreateScreenFactory();

		/// <inheritdoc/>
		public async Task HandleConquestAsync()
		{
			ClearScreensAndTasks();
			await ShowScreenAsync(Screens.Victory()).ConfigureAwait(continueOnCapturedContext: true);
			await ShowFinalScoreAsync().ConfigureAwait(continueOnCapturedContext: true);
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
		public async Task HandleAlphaCentauriAsync()
		{
			ClearScreensAndTasks();
			await ShowScreenAsync(Screens.SpaceVictory(_winner)).ConfigureAwait(continueOnCapturedContext: true);
			await ShowFinalScoreAsync().ConfigureAwait(continueOnCapturedContext: true);
			ReturnToCredits();
		}

		/// <inheritdoc/>
		public async Task HandleRivalSpaceArrivalAsync()
		{
			ClearScreensAndTasks();
			await ShowFinalScoreAsync().ConfigureAwait(continueOnCapturedContext: true);
			ReturnToCredits();
		}

		/// <inheritdoc/>
		public async Task HandleRetireAsync()
		{
			ClearScreensAndTasks();
			await ShowFinalScoreAsync().ConfigureAwait(continueOnCapturedContext: true);
			ReturnToCredits();
		}

		private async Task ShowFinalScoreAsync()
		{
			await ShowScreenAsync(Screens.PowerGraph()).ConfigureAwait(continueOnCapturedContext: true);
			await ShowScreenAsync(Screens.CivilizationScore()).ConfigureAwait(continueOnCapturedContext: true);
			await ShowScreenAsync(Screens.FamousPersons()).ConfigureAwait(continueOnCapturedContext: true);
			await ShowScreenAsync(Screens.HallOfFame()).ConfigureAwait(continueOnCapturedContext: true);
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