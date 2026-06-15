// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using System.Threading.Tasks;
using CivOne.Persistence.Game;
using CivOne.Screens;
using CivOne.Screens.Reports;
using CivOne.Services.Screen;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Orchestrates the end-game screen sequence based on the reason the game ended.
	/// </summary>
	/// <remarks>
	/// Sequences are chained via ShowScreenAsync and sequential orchestration.
	/// HallOfFame is always shown as the last screen before returning to credits.
	/// Retire excludes TopLeaderScreen; all other end reasons include it between CivilizationScore and HallOfFame.
	/// </remarks>
	/// <remarks>
	/// Initializes a new instance of <see cref="EndGameService"/>.
	/// </remarks>
	/// <param name="screenCommand">Provides screen management commands (AddScreen, DestroyScreen).</param>
	/// <param name="screenQuery">Provides screen stack queries (Screens, TopScreen).</param>
	/// <param name="gameService">Provides game state management (End).</param>
	/// <param name="winner">The player who won the game.</param>
	internal class EndGameService(IScreenCommandService screenCommand, IScreenQueryService screenQuery, IGameService gameService, IPlayer winner) : IEndGameService
	{
		private readonly IScreenCommandService _screenCommand = screenCommand;
		private readonly IScreenQueryService _screenQuery = screenQuery;
		private readonly IGameService _gameService = gameService;
		private readonly IPlayer _winner = winner;

		/// <inheritdoc/>
		public async Task HandleConquestAsync()
		{
			ClearScreensAndTasks();
			await ShowScreenAsync(new VictoryScreen());
			await ShowScreenAsync(new CivilizationScore());
			await ShowScreenAsync(TopLeaderScreenFactory.Create());
			await ShowScreenAsync(HallOfFameScreenFactory.AddScore());

			ReturnToCredits();
		}

		/// <inheritdoc/>
		public async Task HandleDefeatAsync() 
		{
			ClearScreensAndTasks();
			await ShowScreenAsync(new DefeatScreen());
			await ShowScreenAsync(new CivilizationScore());
			await ShowScreenAsync(TopLeaderScreenFactory.Create());
			// No HallOfFame entry for defeat.

			ReturnToCredits();
		}

		/// <inheritdoc/>
		public async Task HandleAlphaCentauriAsync()
		{
			ClearScreensAndTasks();
			await ShowScreenAsync(new SpaceVictory(_winner));
			await ShowScreenAsync(new CivilizationScore());
			await ShowScreenAsync(TopLeaderScreenFactory.Create());
			await ShowScreenAsync(HallOfFameScreenFactory.AddScore());

			ReturnToCredits();
		}

		/// <inheritdoc/>
		public async Task HandleRetireAsync()
		{
			ClearScreensAndTasks();
			
			await ShowScreenAsync(new CivilizationScore());
			await ShowScreenAsync(HallOfFameScreenFactory.AddScore());
			
			ReturnToCredits();
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
			_screenCommand.AddScreen(new Credits());
		}
	}
}
