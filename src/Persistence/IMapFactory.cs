namespace CivOne.Persistence
{
	public interface IMapFactory
	{
		IMap Create(IGameData data);
	}
}