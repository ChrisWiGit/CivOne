// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;

namespace CivOne
{
	public partial class Game : IPlayerGame
	{
		// Explicit implementations for members that are internal in Game.
		bool IPlayerGame.Started => Game.Started;
		Player IPlayerGame.HumanPlayer => HumanPlayer;
		Player IPlayerGame.CurrentPlayer => CurrentPlayer;
		IEnumerable<Player> IPlayerGame.Players => Players;
		byte IPlayerGame.PlayerNumber(Player player) => PlayerNumber(player);
	}
}
