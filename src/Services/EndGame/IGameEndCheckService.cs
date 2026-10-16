using System.Collections.Generic;
using CivOne.Services.SpaceShip;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Decides once per turn whether the game ends: by world conquest, by a spaceship arriving at Alpha Centauri
	/// or by reaching the final year, and whether the retirement warning is due.
	/// </summary>
	internal interface IGameEndCheckService
	{
		/// <summary>
		/// Checks the turn that has just begun.
		/// </summary>
		/// <param name="playersBySlot">The players by slot; slot 0 (barbarians) is ignored.</param>
		/// <param name="turn">The turn that has just begun.</param>
		/// <param name="difficulty">The difficulty, 0 (Chieftain) to 5 (Deity).</param>
		/// <param name="worldConquered">Whether only the human player and the barbarians are left.</param>
		/// <returns>What happens this turn.</returns>
		GameEndCheckResult Check(IReadOnlyList<IPlayerSpaceRace> playersBySlot, ushort turn, int difficulty, bool worldConquered);
	}
}
