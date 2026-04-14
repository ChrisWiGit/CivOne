using System.Linq;
using CivOne.Persistence.Game;

namespace CivOne.Persistence.Resolver
{
	public class GamePlayerOwnerResolver(IPlayerGame gameInstance) : IPlayerOwnerResolver
	{
		public bool TryResolveOwnerId(IPlayer player, out byte ownerId)
		{
			ownerId = 0;

			if (player is not Player concretePlayer)
			{
				return false;
			}

			var players = gameInstance.Players?.ToList() ?? [];
			var playerIndex = players.IndexOf(concretePlayer);
			if (playerIndex < 0 || playerIndex > byte.MaxValue)
			{
				return false;
			}

			ownerId = (byte)playerIndex;
			return true;
		}
	}
}
