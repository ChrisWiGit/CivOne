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
		/// Handles the end-game sequence for an Alpha Centauri victory.
		/// Shows SpaceVictory, PowerGraph, CivilizationScore, TopLeaderScreen, HallOfFame, then returns to credits.
		/// </summary>
		Task HandleAlphaCentauriAsync();

		/// <summary>
		/// Handles the end-game sequence when a rival's spaceship reaches Alpha Centauri.
		/// Shows PowerGraph, CivilizationScore, TopLeaderScreen, HallOfFame, then returns to credits.
		/// </summary>
		Task HandleRivalSpaceArrivalAsync();

		/// <summary>
		/// Handles the end-game sequence for a player retirement.
		/// Shows PowerGraph, CivilizationScore, TopLeaderScreen, HallOfFame, then returns to credits.
		/// </summary>
		Task HandleRetireAsync();
	}
}