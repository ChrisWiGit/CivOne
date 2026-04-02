namespace CivOne.Persistence.Model
{
	public interface IPlayerOwnerResolver
	{
		bool TryResolveOwnerId(IPlayer player, out byte ownerId);
	}
}
