namespace CivOne.Persistence
{
	public interface IGameLoaderProvider
	{
		IGameLoader GetLoader<T>() where T : IGameLoader;
	}
}