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
		/// Shows VictoryScreen, CivilizationScore, TopLeaderScreen, HallOfFame, then returns to credits.
		/// </summary>
		Task HandleConquestAsync();

		/// <summary>
		/// Handles the end-game sequence for a defeat.
		/// Shows DefeatScreen, CivilizationScore, TopLeaderScreen, HallOfFame, then returns to credits.
		/// </summary>
		Task HandleDefeatAsync();

		/// <summary>
		/// Handles the end-game sequence for an Alpha Centauri victory.
		/// Shows SpaceVictory, CivilizationScore, TopLeaderScreen, HallOfFame, then returns to credits.
		/// </summary>
		Task HandleAlphaCentauriAsync();

		/// <summary>
		/// Handles the end-game sequence for a player retirement.
		/// Shows CivilizationScore, then HallOfFame, then returns to credits (no TopLeaderScreen).
		/// </summary>
		Task HandleRetireAsync();
	}
}