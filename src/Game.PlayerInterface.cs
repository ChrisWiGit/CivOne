using System.Collections.Generic;

namespace CivOne
{
	public partial class Game : IPlayerGame
	{
		// Explicit implementations for members that are internal in Game.
		bool IPlayerGame.Started => Game.Started;
		bool IPlayerGame.DisableBuddyCivilizationRespawn => DisableBuddyCivilizationRespawn;
		Player IPlayerGame.HumanPlayer => HumanPlayer;
		Player IPlayerGame.CurrentPlayer => CurrentPlayer;
		IEnumerable<Player> IPlayerGame.Players => Players;
		byte IPlayerGame.PlayerNumber(Player player) => PlayerNumber(player);
	}
}