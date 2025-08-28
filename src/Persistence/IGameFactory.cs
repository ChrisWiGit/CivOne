namespace CivOne.Persistence
{
	public interface IGameFactory
	{
		IGame Create(IGameData data);
	}
}