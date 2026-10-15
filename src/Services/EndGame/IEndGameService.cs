using System.Threading.Tasks;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Orchestrates the end-game screen sequence based on the reason the game ended.
	/// </summary>
	internal interface IEndGameService
	{
		/// <summary>
		/// Handles the end-game sequence for a conquest victory.
		/// Shows VictoryScreen, PowerGraph, CivilizationScore, TopLeaderScreen, HallOfFame, then returns to credits.
		/// </summary>
		Task HandleConquestAsync();

		/// <summary>
		/// Handles the end-game sequence for a defeat.
		/// Shows DefeatScreen, then returns to credits; a defeat has no score, as in the original.
		/// </summary>
		Task HandleDefeatAsync();

		/// <summary>
		/// Handles the end of the game found by the end-of-turn check: conquest, a spaceship arrival or the final year.
		/// Shows the news and victory screens of each reason, the end-game flow if no score was recorded yet, and then
		/// asks "keep playing?" (not after a conquest).
		/// </summary>
		/// <param name="sequence">What to show this turn.</param>
		/// <returns><c>true</c> if the player keeps playing; <c>false</c> if the game ended and the credits are shown.</returns>
		Task<bool> HandleGameEndAsync(GameEndSequence sequence);

		/// <summary>
		/// Handles the end-game sequence for a player retirement.
		/// Shows PowerGraph, CivilizationScore, TopLeaderScreen, HallOfFame, then returns to credits.
		/// After "keep playing" only the power graph is shown.
		/// </summary>
		Task HandleRetireAsync();
	}
}