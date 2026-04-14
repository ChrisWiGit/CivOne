using CivOne.Persistence.Game;
using CivOne.Persistence.Model;

namespace CivOne.Persistence.Resolver
{
	public interface IPlayerOwnerResolver
	{
		bool TryResolveOwnerId(IPlayer player, out byte ownerId);
	}
}
