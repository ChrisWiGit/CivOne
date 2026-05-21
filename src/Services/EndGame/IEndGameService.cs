// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

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
